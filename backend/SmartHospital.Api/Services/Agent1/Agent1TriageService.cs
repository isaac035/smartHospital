using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Agent1;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services.Agent1;

/// <summary>
/// Gateway between patients and the internal Agent 1 Python service. Supplies the real
/// specialty vocabulary and the real bookable doctors, and re-verifies every doctor the
/// service recommends against the database before anything reaches the client.
/// </summary>
public class Agent1TriageService : IAgent1TriageService
{
    public const string InternalKeyHeader = "X-Internal-Api-Key";
    private const string DefaultCategoryName = "General Medicine";
    private const int MaxCandidates = 1000;

    private const string UnavailableMessage =
        "Smart Care is unavailable right now. You can still browse doctors and book an appointment yourself.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly Agent1Settings _settings;
    private readonly ILogger<Agent1TriageService> _logger;

    public Agent1TriageService(
        AppDbContext context,
        HttpClient httpClient,
        IOptions<Agent1Settings> settings,
        ILogger<Agent1TriageService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TriageDoctorMatchResponse> TriageAndMatchAsync(
        int patientId, string symptoms, CancellationToken cancellationToken = default)
    {
        var text = symptoms.Trim();
        var redFlag = EmergencyRedFlags.Matches(text);

        // The fixed category vocabulary: names of currently active departments.
        var categories = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Status == DepartmentStatus.Active)
            .OrderBy(d => d.Name)
            .Select(d => d.Name)
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            _logger.LogWarning("Agent 1: no active departments to triage against.");
            return await PersistResultAsync(patientId, text, Unavailable(patientId, redFlag), cancellationToken);
        }

        var defaultCategory = categories.FirstOrDefault(c =>
            string.Equals(c, DefaultCategoryName, StringComparison.OrdinalIgnoreCase)) ?? categories[0];

        var candidates = await BookableDoctors()
            .OrderBy(d => d.FirstName).ThenBy(d => d.LastName)
            .Take(MaxCandidates)
            .Select(d => new RecommendedDoctorDto
            {
                DoctorId = d.UserId!.Value,
                DoctorProfileId = d.Id,
                Name = d.FirstName + " " + d.LastName,
                Specialization = d.Specialization,
                Department = d.Department!.Name,
            })
            .ToListAsync(cancellationToken);

        var agentResult = await CallAgentAsync(new Agent1ServiceRequest
        {
            PatientId = patientId,
            Text = text,
            Categories = categories,
            DefaultCategory = defaultCategory,
            CandidateDoctors = candidates,
        }, cancellationToken);

        if (agentResult.Response is not { } result)
        {
            return await PersistResultAsync(patientId, text, Unavailable(patientId, redFlag || agentResult.PossibleEmergency), cancellationToken);
        }

        // Treat the service's output as untrusted: re-validate category and priority.
        var category = categories.FirstOrDefault(c =>
            string.Equals(c, result.Category?.Trim(), StringComparison.OrdinalIgnoreCase));
        var usedDefault = result.UsedDefaultCategory;
        if (category == null)
        {
            _logger.LogWarning("Agent 1 returned a category outside the active list; using {Default}.", defaultCategory);
            category = defaultCategory;
            usedDefault = true;
        }

        var priority = Enum.TryParse<AppointmentPriority>(result.Priority, ignoreCase: false, out var parsed)
                       && Enum.IsDefined(parsed)
            ? parsed
            : AppointmentPriority.Normal;

        var possibleEmergency = result.PossibleEmergency || redFlag;
        if (possibleEmergency) priority = AppointmentPriority.Emergency;

        var doctors = await VerifyDoctorsAsync(result.RecommendedDoctors, category, cancellationToken);

        var response = new TriageDoctorMatchResponse
        {
            Status = doctors.Count > 0 ? TriageStatus.Ok : TriageStatus.NoDoctors,
            PatientId = patientId,
            Category = category,
            Priority = priority.ToString(),
            Reason = result.Reason,
            Confidence = Math.Clamp(result.Confidence, 0, 1),
            PossibleEmergency = possibleEmergency,
            EmergencyNotice = possibleEmergency ? EmergencyRedFlags.Notice : null,
            UsedDefaultCategory = usedDefault,
            RecommendedDoctors = doctors,
            Message = doctors.Count > 0
                ? null
                : $"No {category} doctors are available for online booking right now. You can browse all doctors instead.",
        };
        return await PersistResultAsync(patientId, text, response, cancellationToken);
    }

    private async Task<TriageDoctorMatchResponse> PersistResultAsync(int patientId, string symptoms, TriageDoctorMatchResponse response, CancellationToken ct)
    {
        var entity = new Agent1TriageResult
        {
            PatientId = patientId, Symptoms = symptoms, Status = response.Status, Category = response.Category,
            Priority = response.Priority, Reason = response.Reason, Confidence = response.Confidence,
            PossibleEmergency = response.PossibleEmergency, EmergencyNotice = response.EmergencyNotice,
            UsedDefaultCategory = response.UsedDefaultCategory, CreatedAt = DateTime.UtcNow
        };
        _context.Agent1TriageResults.Add(entity);
        await _context.SaveChangesAsync(ct);
        response.TriageResultId = entity.Id;
        return response;
    }

    /// <summary>
    /// The same eligibility the booking path enforces: an Active doctor profile linked to an
    /// active User with the Doctor role (AppointmentService.BookAppointmentAsync), which the slot
    /// service only offers when the profile is Active and has a UserId (AvailabilityService).
    /// Additionally requires an active department and at least one active schedule, so a
    /// recommendation never leads to a doctor with no bookable sessions.
    /// </summary>
    private IQueryable<Doctor> BookableDoctors() =>
        _context.Doctors
            .AsNoTracking()
            .Where(d => d.Status == DoctorStatus.Active
                        && d.UserId != null
                        && d.Department != null
                        && d.Department.Status == DepartmentStatus.Active
                        && _context.Users.Any(u => u.Id == d.UserId
                                                   && u.Role == UserRole.Doctor
                                                   && u.Status == UserStatus.Active)
                        && _context.DoctorSchedules.Any(s => s.DoctorId == d.Id
                                                             && s.Status == ScheduleStatus.Active));

    /// <summary>
    /// Never forwards an AI-supplied doctor record: re-queries each id and returns the database's
    /// values, dropping any id that is unknown, no longer bookable, or outside the category.
    /// </summary>
    private async Task<List<RecommendedDoctorDto>> VerifyDoctorsAsync(
        IEnumerable<RecommendedDoctorDto>? suggested, string category, CancellationToken cancellationToken)
    {
        var ids = (suggested ?? Enumerable.Empty<RecommendedDoctorDto>())
            .Select(d => d.DoctorId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (ids.Count == 0) return new List<RecommendedDoctorDto>();

        var verified = await BookableDoctors()
            .Where(d => ids.Contains(d.UserId!.Value) && d.Department!.Name == category)
            .Select(d => new RecommendedDoctorDto
            {
                DoctorId = d.UserId!.Value,
                DoctorProfileId = d.Id,
                Name = d.FirstName + " " + d.LastName,
                Specialization = d.Specialization,
                Department = d.Department!.Name,
            })
            .ToListAsync(cancellationToken);

        var dropped = ids.Except(verified.Select(v => v.DoctorId)).ToList();
        if (dropped.Count > 0)
        {
            _logger.LogWarning("Agent 1 recommended doctor ids {Ids} that failed re-verification; dropped.", dropped);
        }

        // Keep the service's ordering.
        return ids.Select(id => verified.FirstOrDefault(v => v.DoctorId == id))
            .Where(v => v != null)
            .Select(v => v!)
            .ToList();
    }

    private async Task<(Agent1ServiceResponse? Response, bool PossibleEmergency)> CallAgentAsync(
        Agent1ServiceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.InternalApiKey))
        {
            _logger.LogWarning("Agent 1 is disabled: Agent1:InternalApiKey is not configured.");
            return (null, false);
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "v1/triage-doctor-match")
            {
                Content = JsonContent.Create(request, options: JsonOptions),
            };
            message.Headers.Add(InternalKeyHeader, _settings.InternalApiKey);

            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<Agent1ServiceResponse>(JsonOptions, cancellationToken);
                return (body, false);
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                var error = await TryReadErrorAsync(response, cancellationToken);
                _logger.LogWarning("Agent 1 unavailable: {Detail}", error?.Detail);
                return (null, error?.PossibleEmergency ?? false);
            }

            _logger.LogError("Agent 1 returned HTTP {Status}.", (int)response.StatusCode);
            return (null, false);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Agent 1 timed out after {Seconds}s.", _settings.TimeoutSeconds);
            return (null, false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Agent 1 could not be reached.");
            return (null, false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Agent 1 returned an unreadable response.");
            return (null, false);
        }
    }

    private static async Task<Agent1ServiceError?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<Agent1ServiceError>(JsonOptions, ct);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TriageDoctorMatchResponse Unavailable(int patientId, bool possibleEmergency) => new()
    {
        Status = TriageStatus.AiUnavailable,
        PatientId = patientId,
        PossibleEmergency = possibleEmergency,
        EmergencyNotice = possibleEmergency ? EmergencyRedFlags.Notice : null,
        Message = UnavailableMessage,
    };
}
