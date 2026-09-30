using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHospital.Api.DTOs.Agent1;

/// <summary>Body of POST /api/agent1/triage-doctor-match. The patient id is taken from the JWT, never from here.</summary>
public class TriageDoctorMatchRequest
{
    [Required(ErrorMessage = "Please describe your symptoms or what you need help with.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Please use between 3 and 1000 characters.")]
    public string Symptoms { get; set; } = string.Empty;
}

public static class TriageStatus
{
    /// <summary>Triage succeeded and at least one bookable doctor matched.</summary>
    public const string Ok = "ok";

    /// <summary>Triage succeeded but no bookable doctor exists for the category.</summary>
    public const string NoDoctors = "no_doctors";

    /// <summary>The AI service was unavailable, timed out, or failed; the patient should browse manually.</summary>
    public const string AiUnavailable = "ai_unavailable";
}

public class TriageDoctorMatchResponse
{
    public string Status { get; set; } = TriageStatus.Ok;
    public int PatientId { get; set; }

    /// <summary>A real, active department name. Null only when Status is ai_unavailable.</summary>
    public string? Category { get; set; }

    /// <summary>Suggested AppointmentPriority name: Normal, Urgent or Emergency. The patient still chooses in the booking flow.</summary>
    public string? Priority { get; set; }

    public string? Reason { get; set; }
    public double? Confidence { get; set; }
    public bool PossibleEmergency { get; set; }
    public string? EmergencyNotice { get; set; }
    public bool UsedDefaultCategory { get; set; }
    public List<RecommendedDoctorDto> RecommendedDoctors { get; set; } = new();

    /// <summary>Patient-facing explanation when there is no recommendation (no doctors, AI unavailable).</summary>
    public string? Message { get; set; }
}

public class RecommendedDoctorDto
{
    /// <summary>The doctor's User id - the id POST /api/appointments books against.</summary>
    public int DoctorId { get; set; }

    /// <summary>Doctors table id, used by the patient app's doctor/slot screens.</summary>
    public int DoctorProfileId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

// ── Internal contract with the Python service (never exposed to clients) ────────

public class Agent1ServiceRequest
{
    public int PatientId { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public string DefaultCategory { get; set; } = string.Empty;
    public List<RecommendedDoctorDto> CandidateDoctors { get; set; } = new();
}

public class Agent1ServiceResponse
{
    public int PatientId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool PossibleEmergency { get; set; }
    public string? EmergencyNotice { get; set; }
    public bool UsedDefaultCategory { get; set; }
    public List<RecommendedDoctorDto> RecommendedDoctors { get; set; } = new();
    public string? Message { get; set; }
}

public class Agent1ServiceError
{
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("possibleEmergency")]
    public bool PossibleEmergency { get; set; }
}
