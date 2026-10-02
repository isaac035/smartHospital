using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Admissions;
using SmartHospital.Api.DTOs.Agent3;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services.Agent3;

public class Agent3ResourceAllocationService : IAgent3ResourceAllocationService
{
    private const string InternalKeyHeader = "X-Internal-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly Agent3Settings _settings;
    private readonly IAdmissionService _admissionService;
    private readonly ILogger<Agent3ResourceAllocationService> _logger;

    public Agent3ResourceAllocationService(AppDbContext db, HttpClient http, Microsoft.Extensions.Options.IOptions<Agent3Settings> settings, ILogger<Agent3ResourceAllocationService> logger, IAdmissionService admissionService)
    { _db = db; _http = http; _settings = settings.Value; _logger = logger; _admissionService = admissionService; }

    public async Task<RecommendResourcesResponse> RecommendAsync(int patientId, int appointmentId, CancellationToken ct)
    {
        var (appointment, department, doctorSpecialty, _) = await LoadContext(patientId, appointmentId, ct);
        var specialty = !string.IsNullOrWhiteSpace(appointment.Department?.Name) ? appointment.Department.Name
            : !string.IsNullOrWhiteSpace(department) ? department : doctorSpecialty;
        var patientHasActiveBed = await _db.BedAllocations.AnyAsync(a => a.Admission!.PatientId == patientId && a.Status == BedAllocationStatus.Active, ct);
        var patientHasOtherActiveAdmission = await _db.Admissions.AnyAsync(a => a.PatientId == patientId
            && (a.Status == AdmissionStatus.Admitted || a.Status == AdmissionStatus.Reserved)
            && (a.AppointmentId == null || a.AppointmentId != appointmentId), ct);
        var candidates = await GetCandidates(ct, includeBeds: !patientHasActiveBed && !patientHasOtherActiveAdmission);
        var priority = (appointment.RequestedPriority ?? appointment.Priority).ToString();
        var result = new RecommendResourcesResponse { AppointmentId = appointmentId, ClinicalSpecialty = specialty, DoctorSpecialty = doctorSpecialty, DepartmentName = department ?? string.Empty, Priority = priority };
        if (candidates.Count == 0)
        {
            result.Message = patientHasOtherActiveAdmission
                ? "You already have an active inpatient admission. Your appointment remains confirmed; contact your care team if you need another resource."
                : patientHasActiveBed
                    ? "You already have an active bed allocation. Your appointment remains confirmed; equipment options may still be available."
                    : "No suitable resource is currently available. Your appointment remains confirmed.";
            return result;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(_settings.InternalApiKey))
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, "v1/recommendations") { Content = JsonContent.Create(new Agent3ServiceRequest { AppointmentId = appointmentId, ClinicalSpecialty = specialty, DoctorSpecialty = doctorSpecialty, DepartmentName = department ?? string.Empty, Priority = priority, Candidates = candidates }, options: JsonOptions) };
                message.Headers.Add(InternalKeyHeader, _settings.InternalApiKey);
                using var response = await _http.SendAsync(message, ct);
                response.EnsureSuccessStatusCode();
                var ranked = await response.Content.ReadFromJsonAsync<RecommendResourcesResponse>(JsonOptions, ct);
                if (ranked != null)
                {
                    // Trust only ranking/explanation fields for candidates that the API itself supplied.
                    var allowed = candidates.ToDictionary(c => (c.Kind, c.ResourceId));
                    result.Recommendations = ranked.Recommendations.Where(c => allowed.ContainsKey((c.Kind, c.ResourceId)))
                        .Select(c => { var real = allowed[(c.Kind, c.ResourceId)]; real.Score = c.Score; real.Reason = c.Reason; return real; }).ToList();
                    result.Message = result.Recommendations.Count == 0 ? "No suitable resource is currently available. Your appointment remains confirmed." : null;
                    if (result.Recommendations.Count > 0 && result.Recommendations.Max(c => c.Score) < 20)
                        result.Message = "No specialty-specific or general-purpose resource is available; the remaining options are specialized for other care types.";
                    return result;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { _logger.LogWarning(ex, "Agent 3 unavailable; using deterministic API ranking."); }

        result.Recommendations = DeterministicRank(candidates, specialty, priority);
        if (result.Recommendations.Count > 0 && result.Recommendations.Max(c => c.Score) < 20)
            result.Message = "No specialty-specific or general-purpose resource is available; the remaining options are specialized for other care types.";
        return result;
    }

    public async Task<AllocationResponse> AllocateAsync(int patientId, int appointmentId, int resourceId, string kind, DateOnly admissionDate, CancellationToken ct)
    {
        var (appointment, department, doctorSpecialty, departmentId) = await LoadContext(patientId, appointmentId, ct);
        var specialty = appointment.Department?.Name ?? department ?? doctorSpecialty;
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;
        var kindValue = kind.Trim().ToLowerInvariant();
        AppointmentResourceAllocation allocation;
        if (kindValue == "bed")
        {
            var bed = await _db.Beds.Include(b => b.Room)!.ThenInclude(r => r!.Ward).FirstOrDefaultAsync(b => b.Id == resourceId, ct)
                ?? throw new KeyNotFoundException("Selected resource was not found.");
            if (!bed.IsActive || bed.Room?.IsActive != true || bed.Room.Ward?.IsActive != true) throw new InvalidOperationException("Selected resource is not active.");
            var sameAppointmentBedAllocation = await _db.AppointmentResourceAllocations
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.BedId == resourceId && a.IsActive, ct);
            var sameAppointmentAlreadyReserved = sameAppointmentBedAllocation != null && bed.Status == BedStatus.Reserved;
            if ((bed.Status != BedStatus.Available && !sameAppointmentAlreadyReserved) || await _db.AppointmentResourceAllocations.AnyAsync(a => a.BedId == resourceId && a.IsActive && a.AppointmentId != appointmentId, ct))
                throw new ResourceUnavailableException();
            var previousAppointmentAllocation = await _db.AppointmentResourceAllocations
                .Where(a => a.AppointmentId == appointmentId && a.IsActive && a.BedId != resourceId)
                .ToListAsync(ct);
            if (await _db.BedAllocations.AnyAsync(a => a.BedId == resourceId && a.Status == BedAllocationStatus.Active && a.Admission!.AppointmentId != appointmentId, ct))
                throw new ResourceUnavailableException();
            var admissionPriority = Enum.TryParse<AdmissionPriority>((appointment.RequestedPriority ?? appointment.Priority).ToString(), out var parsedPriority)
                ? parsedPriority : AdmissionPriority.Normal;
            var admission = await _admissionService.CreateAdmissionAsync(new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmittingDoctorId = appointment.DoctorId,
                Priority = admissionPriority,
                ReasonForAdmission = "AI-assisted medical checkup resource reservation",
                Diagnosis = specialty
            }, appointmentId, reserveStatus: true,
                admissionDateUtc: DateTime.SpecifyKind(admissionDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));

            await _admissionService.AllocateBedAsync(new AllocateBedRequest
            {
                AdmissionId = admission.Id,
                BedId = resourceId,
                Notes = "Reserved through Agent 3 medical checkup"
            }, reserveBed: true);

            foreach (var previous in previousAppointmentAllocation)
            {
                previous.IsActive = false;
                previous.ReleasedAt = now;
            }
            var sameAllocation = sameAppointmentBedAllocation;
            if (sameAllocation != null)
            {
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return (await GetForAppointment(appointmentId, ct)).Single(a => a.Id == sameAllocation.Id);
            }
            allocation = new AppointmentResourceAllocation { AppointmentId = appointmentId, PatientId = patientId, DoctorId = appointment.DoctorId, DepartmentId = departmentId, ClinicalSpecialty = specialty, Priority = appointment.RequestedPriority ?? appointment.Priority, BedId = resourceId, AllocatedAt = now };
        }
        else if (kindValue == "equipment")
        {
            var resource = await _db.MedicalResources.Include(r => r.Ward).Include(r => r.Room).FirstOrDefaultAsync(r => r.Id == resourceId, ct)
                ?? throw new KeyNotFoundException("Selected resource was not found.");
            if (!resource.IsActive || (resource.WardId != null && resource.Ward?.IsActive != true) || (resource.RoomId != null && resource.Room?.IsActive != true)) throw new InvalidOperationException("Selected resource is not active.");
            if (resource.Status != ResourceStatus.Available || await _db.AppointmentResourceAllocations.AnyAsync(a => a.MedicalResourceId == resourceId && a.IsActive, ct)) throw new ResourceUnavailableException();
            var changed = await _db.MedicalResources.Where(r => r.Id == resourceId && r.IsActive && r.Status == ResourceStatus.Available).ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ResourceStatus.InUse).SetProperty(r => r.UpdatedAt, now), ct);
            if (changed != 1) throw new ResourceUnavailableException();
            allocation = new AppointmentResourceAllocation { AppointmentId = appointmentId, PatientId = patientId, DoctorId = appointment.DoctorId, DepartmentId = departmentId, ClinicalSpecialty = specialty, Priority = appointment.RequestedPriority ?? appointment.Priority, MedicalResourceId = resourceId, AllocatedAt = now };
        }
        else throw new ArgumentException("Resource kind must be bed or equipment.");

        _db.AppointmentResourceAllocations.Add(allocation);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await GetForAppointment(appointmentId, ct)).Single(a => a.Id == allocation.Id);
    }

    public Task<List<AllocationResponse>> GetForAppointmentAsync(int appointmentId, CancellationToken ct = default) => GetForAppointment(appointmentId, ct);

    private async Task<List<AllocationResponse>> GetForAppointment(int appointmentId, CancellationToken ct) => await _db.AppointmentResourceAllocations.AsNoTracking()
        .Where(a => a.AppointmentId == appointmentId && a.IsActive)
        .Include(a => a.Bed)!.ThenInclude(b => b!.Room)!.ThenInclude(r => r!.Ward)
        .Include(a => a.MedicalResource)!.ThenInclude(r => r!.Ward)
        .Select(a => new AllocationResponse { Id = a.Id, ResourceId = a.BedId ?? a.MedicalResourceId!.Value, Kind = a.BedId.HasValue ? "bed" : "equipment", Name = a.BedId.HasValue ? $"Bed {a.Bed!.BedNumber}" : a.MedicalResource!.Name, Code = a.BedId.HasValue ? a.Bed!.BedNumber : a.MedicalResource!.ResourceCode, Type = a.BedId.HasValue ? $"{a.Bed!.Type} · {a.Bed.Room!.Ward!.Name}" : a.MedicalResource!.Category.ToString(), Location = a.BedId.HasValue ? $"Room {a.Bed!.Room!.RoomNumber} · {a.Bed.Room.Ward!.Name}" : a.MedicalResource!.LocationDescription, Status = a.BedId.HasValue ? a.Bed!.Status.ToString() : a.MedicalResource!.Status.ToString(), AllocatedAt = a.AllocatedAt })
        .ToListAsync(ct);

    private async Task<(Appointment Appointment, string? Department, string DoctorSpecialty, int? DepartmentId)> LoadContext(int patientId, int appointmentId, CancellationToken ct)
    {
        var appointment = await _db.Appointments.Include(a => a.Department).FirstOrDefaultAsync(a => a.Id == appointmentId, ct) ?? throw new KeyNotFoundException("Appointment not found.");
        if (appointment.PatientId != patientId) throw new UnauthorizedAccessException("Appointment does not belong to the authenticated patient.");
        var doctor = await _db.Doctors.Include(d => d.Department).FirstOrDefaultAsync(d => d.UserId == appointment.DoctorId, ct);
        var department = appointment.Department?.Name ?? doctor?.Department?.Name;
        return (appointment, department, doctor?.Specialization ?? string.Empty, appointment.DepartmentId ?? doctor?.DepartmentId);
    }

    private async Task<List<ResourceCandidateDto>> GetCandidates(CancellationToken ct, bool includeBeds)
    {
        var beds = includeBeds
            ? await _db.Beds.AsNoTracking()
                .Where(b => b.IsActive && b.Status == BedStatus.Available && b.Room!.IsActive && b.Room.Ward!.IsActive
                    && !_db.BedAllocations.Any(a => a.BedId == b.Id && a.Status == BedAllocationStatus.Active)
                    && !_db.AppointmentResourceAllocations.Any(a => a.BedId == b.Id && a.IsActive)
                    && b.Room.Ward.Capacity > _db.Beds.Count(wardBed => wardBed.Room!.WardId == b.Room.WardId
                        && (wardBed.Status == BedStatus.Occupied || wardBed.Status == BedStatus.Reserved)))
                .OrderBy(b => b.Id).Take(250)
                .Select(b => new ResourceCandidateDto { ResourceId = b.Id, Kind = "bed", Name = $"Bed {b.BedNumber}", Code = b.BedNumber, Type = b.Type.ToString(), Location = $"Room {b.Room!.RoomNumber} · {b.Room.Ward!.Name}", Status = b.Status.ToString(), SpecialtyText = $"{b.Room.Ward.Name} {b.Room.Ward.Type} {b.Type}" }).ToListAsync(ct)
            : new List<ResourceCandidateDto>();
        var equipment = await _db.MedicalResources.AsNoTracking().Where(r => r.IsActive && r.Status == ResourceStatus.Available && (r.WardId == null || r.Ward!.IsActive) && (r.RoomId == null || r.Room!.IsActive)
                && !_db.AppointmentResourceAllocations.Any(a => a.MedicalResourceId == r.Id && a.IsActive)).OrderBy(r => r.Id).Take(250)
            .Select(r => new ResourceCandidateDto { ResourceId = r.Id, Kind = "equipment", Name = r.Name, Code = r.ResourceCode, Type = r.Category.ToString(), Location = r.LocationDescription, Status = r.Status.ToString(), SpecialtyText = $"{r.Name} {r.Category} {r.Ward!.Name}" }).ToListAsync(ct);
        return beds.Concat(equipment).ToList();
    }

    private static List<ResourceCandidateDto> DeterministicRank(List<ResourceCandidateDto> candidates, string specialty, string priority)
    {
        string? Group(string value) => value.ToLowerInvariant() switch
        {
            var x when x.Contains("cardia") || x.Contains("heart") => "cardiology",
            var x when x.Contains("matern") || x.Contains("postnatal") || x.Contains("obst") || x.Contains("gyne") || x.Contains("birthing") || x.Contains("fetal") || x.Contains("newborn") || x.Contains("delivery") => "maternity",
            var x when x.Contains("pedi") || x.Contains("paedi") || x.Contains("child") || x.Contains("infant") || x.Contains("neonat") || x.Contains("nicu") => "pediatric",
            var x when x.Contains("emerg") || x.Contains("trauma") || x.Contains("accident") || x.Contains("er ward") || x.Contains("resuscitation") || x.Contains("defibrillator") => "emergency",
            var x when x.Contains("surg") || x.Contains("operating theater") || x.Contains("operating theatre") || x.Contains("orthop") => "surgical",
            var x when x.Contains("icu") || x.Contains("critical") || x.Contains("intensive") || x.Contains("ventilator") || x.Contains("high dependency") => "icu",
            var x when x.Contains("general") || x.Contains("internal medicine") || x.Contains("primary care") => "general",
            _ => null
        };
        var wanted = Group(specialty);
        foreach (var c in candidates)
        {
            var text = $"{c.Name} {c.Type} {c.SpecialtyText}".ToLowerInvariant();
            var group = Group(text);
            var matched = wanted != null && group == wanted;
            var general = group == "general" || text.Contains("standard");
            c.Score = matched ? 100 : general ? 35 : group == null ? 20 : -30;
            if (matched && priority == "Emergency" && (text.Contains("icu") || text.Contains("critical") || text.Contains("intensive"))) c.Score += 4;
            else if (matched && priority == "Urgent" && (text.Contains("high dependency") || text.Contains("step-down") || text.Contains("monitor"))) c.Score += 2;
            c.Reason = matched ? $"Matches the {specialty} specialty." : general ? $"General option; no {specialty}-specific match is available." : "Available option; specialty fit is not explicit in its record.";
            if (priority != "Normal") c.Reason += $" {priority} priority is a secondary tie-breaker only.";
        }
        return candidates.OrderByDescending(c => c.Score).ThenBy(c => c.Kind).ThenBy(c => c.Name).ThenBy(c => c.ResourceId).ToList();
    }
}

public sealed class ResourceUnavailableException : InvalidOperationException
{ public ResourceUnavailableException() : base("The selected resource is no longer available. Please select another available resource.") { } }
