using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Agent2;
using SmartHospital.Api.DTOs.Availability;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services.Agent2;

/// <summary>
/// Orchestrates Agent 2 using the database-backed availability service as the only source
/// of slots, then rechecks the suggestions before returning them to the patient.
/// </summary>
public class Agent2OptimizationService : IAgent2OptimizationService
{
    public const string InternalKeyHeader = "X-Internal-Api-Key";
    private const int SearchDays = 30;
    private const int MaxCandidateSlots = 500;
    private const int MaxReturnedSlots = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _context;
    private readonly IAvailabilityService _availability;
    private readonly HttpClient _httpClient;
    private readonly Agent2Settings _settings;
    private readonly ILogger<Agent2OptimizationService> _logger;

    public Agent2OptimizationService(
        AppDbContext context,
        IAvailabilityService availability,
        HttpClient httpClient,
        IOptions<Agent2Settings> settings,
        ILogger<Agent2OptimizationService> logger)
    {
        _context = context;
        _availability = availability;
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<OptimizeAppointmentResponse> OptimizeAsync(
        int patientId, OptimizeAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var priority = request.Priority;
        if (priority is not ("Normal" or "Urgent" or "Emergency"))
            throw new ArgumentException("Priority must be Normal, Urgent or Emergency.");

        var department = await _context.Departments.AsNoTracking()
            .Where(d => d.Status == DepartmentStatus.Active)
            .Where(d => d.Name == request.Category.Trim())
            .Select(d => new { d.Id, d.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (department == null)
            throw new ArgumentException("The recommended specialty is no longer available.");
        var category = department.Name;

        var requestedIds = request.RecommendedDoctors
            .Select(d => d.DoctorId).Where(id => id > 0).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new ArgumentException("No valid recommended doctors were provided.");

        // Re-query IDs and metadata; client supplied profile IDs/names are never trusted.
        var verified = await _context.Doctors.AsNoTracking()
            .Where(d => d.UserId != null
                        && requestedIds.Contains(d.UserId.Value)
                        && d.Status == DoctorStatus.Active
                        && d.Department != null
                        && d.Department.Status == DepartmentStatus.Active
                        && d.Department.Name == category
                        && _context.Users.Any(u => u.Id == d.UserId
                                                   && u.Role == UserRole.Doctor
                                                   && u.Status == UserStatus.Active)
                        && _context.DoctorSchedules.Any(s => s.DoctorId == d.Id
                                                             && s.Status == ScheduleStatus.Active))
            .Select(d => new Agent2DoctorDto
            {
                DoctorId = d.UserId!.Value,
                DoctorProfileId = d.Id,
                Name = d.FirstName + " " + d.LastName,
                Specialization = d.Specialization,
                Department = d.Department!.Name,
            })
            .ToListAsync(cancellationToken);

        var doctors = requestedIds.Select(id => verified.FirstOrDefault(d => d.DoctorId == id))
            .Where(d => d != null).Select(d => d!).ToList();
        if (doctors.Count == 0)
            throw new ArgumentException("The recommended doctors are no longer available for booking.");

        var slots = new List<AvailableSlotResponse>();
        var hospitalZone = ResolveHospitalTimeZone();
        var firstDateLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, hospitalZone).Date;
        var doctorsById = doctors.ToDictionary(d => d.DoctorId);
        for (var offset = 0; offset < SearchDays; offset++)
        {
            var date = DateTime.SpecifyKind(firstDateLocal.AddDays(offset), DateTimeKind.Utc);
            // One department-date lookup uses the same availability calculation while
            // avoiding a separate set of database queries for every recommended doctor.
            var dailySlots = await _availability.GetAvailableSlotsAsync(
                null, department.Id, date, includePast: false);
            slots.AddRange(dailySlots.Where(s => s.Status == "Available"
                && doctorsById.TryGetValue(s.DoctorId, out var doctor)
                && doctor.DoctorProfileId == s.DoctorProfileId));
        }
        slots = slots.OrderBy(s => s.SlotStart).ThenBy(s => s.DoctorId)
            .Take(MaxCandidateSlots).ToList();

        if (string.IsNullOrWhiteSpace(_settings.InternalApiKey))
            throw new InvalidOperationException("Agent 2 is not configured.");

        Agent2ServiceResponse? result;
        using (var message = new HttpRequestMessage(HttpMethod.Post, "v1/optimize-appointment")
        {
            Content = JsonContent.Create(new Agent2ServiceRequest
            {
                PatientId = patientId,
                Category = category,
                Priority = priority,
                RecommendedDoctors = doctors,
                AvailableSlots = slots.Select(s => new Agent2SlotDto
                {
                    DoctorId = s.DoctorId,
                    DoctorProfileId = s.DoctorProfileId,
                    SlotStart = s.SlotStart,
                    SlotEnd = s.SlotEnd,
                    DurationMinutes = s.DurationMinutes,
                    Status = s.Status,
                }).ToList(),
            }, options: JsonOptions),
        })
        {
            message.Headers.Add(InternalKeyHeader, _settings.InternalApiKey);
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Agent 2 returned HTTP {Status}.", (int)response.StatusCode);
                throw new InvalidOperationException("Agent 2 could not rank the available appointments.");
            }
            result = await response.Content.ReadFromJsonAsync<Agent2ServiceResponse>(JsonOptions, cancellationToken);
        }

        if (result == null)
            throw new InvalidOperationException("Agent 2 returned an invalid response.");

        var proposed = new List<(Agent2ServiceSlot Slot, bool Recommended)>();
        if (result.RecommendedSlot != null) proposed.Add((result.RecommendedSlot, true));
        proposed.AddRange(result.AlternativeSlots.Take(MaxReturnedSlots - 1).Select(s => (s, false)));
        if (proposed.Count == 0)
        {
            return new OptimizeAppointmentResponse
            {
                PatientId = patientId,
                DoctorId = doctors[0].DoctorId,
                Message = result.Message ?? "No available appointment slots were found in the next 30 days. Try another recommended doctor or browse manually.",
            };
        }

        // Re-read each proposed date from the same real availability service. A response
        // may already be stale by the time Agent 2 finishes ranking it.
        var verifiedSlots = new List<(AvailableSlotResponse Slot, string? Reason)>();
        foreach (var proposal in proposed)
        {
            var doctor = doctors.FirstOrDefault(d => d.DoctorId == proposal.Slot.DoctorId
                && d.DoctorProfileId == proposal.Slot.DoctorProfileId);
            if (doctor == null) continue;

            var proposedStartUtc = DateTime.SpecifyKind(proposal.Slot.SlotStart, DateTimeKind.Utc);
            var proposedLocalDate = TimeZoneInfo.ConvertTimeFromUtc(proposedStartUtc, hospitalZone).Date;
            var date = DateTime.SpecifyKind(proposedLocalDate, DateTimeKind.Utc);
            var fresh = await _availability.GetAvailableSlotsAsync(
                null, department.Id, date, includePast: false);
            var exact = fresh.FirstOrDefault(s => s.Status == "Available"
                && s.DoctorId == doctor.DoctorId
                && s.DoctorProfileId == doctor.DoctorProfileId
                && s.SlotStart == proposal.Slot.SlotStart
                && s.DurationMinutes == proposal.Slot.DurationMinutes);
            if (exact != null) verifiedSlots.Add((exact, proposal.Slot.Reason));
        }

        var patientResponse = new OptimizeAppointmentResponse
        {
            PatientId = patientId,
            DoctorId = doctors[0].DoctorId,
            Message = verifiedSlots.Count == 0
                ? "Those appointment suggestions are no longer available. Please try again or browse doctors and slots manually."
                : null,
        };
        if (verifiedSlots.Count == 0) return patientResponse;

        var first = verifiedSlots[0];
        patientResponse.DoctorId = first.Slot.DoctorId;
        patientResponse.RecommendedSlot = ToResponse(first.Slot, first.Reason
            ?? "Earliest available slot matching your priority.");
        patientResponse.AlternativeSlots = verifiedSlots.Skip(1)
            .Select(item => ToResponse(item.Slot, item.Reason)).ToList();
        return patientResponse;
    }

    private static OptimizedSlotDto ToResponse(AvailableSlotResponse slot, string? reason) => new()
    {
        DoctorId = slot.DoctorId,
        DoctorProfileId = slot.DoctorProfileId,
        SlotStart = DateTime.SpecifyKind(slot.SlotStart, DateTimeKind.Utc),
        SlotEnd = DateTime.SpecifyKind(slot.SlotEnd, DateTimeKind.Utc),
        DurationMinutes = slot.DurationMinutes,
        Reason = reason,
    };

    private static TimeZoneInfo ResolveHospitalTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"); }
    }
}
