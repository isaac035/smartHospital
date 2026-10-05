using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHospital.Api.DTOs.Agent2;

/// <summary>Agent 1 data passed through Smart Care. Patient identity is always taken from JWT.</summary>
public class OptimizeAppointmentRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    [Required, RegularExpression("^(Normal|Urgent|Emergency)$")]
    public string Priority { get; set; } = "Normal";

    [Required, MinLength(1), MaxLength(5)]
    public List<RecommendedDoctorInput> RecommendedDoctors { get; set; } = new();
}

public class RecommendedDoctorInput
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }

    [Range(1, int.MaxValue)]
    public int DoctorProfileId { get; set; }
}

public class OptimizedSlotDto
{
    public int DoctorId { get; set; }
    public int DoctorProfileId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public int DurationMinutes { get; set; }
    public string? Reason { get; set; }
}

public class OptimizeAppointmentResponse
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public OptimizedSlotDto? RecommendedSlot { get; set; }
    public List<OptimizedSlotDto> AlternativeSlots { get; set; } = new();
    public string? Message { get; set; }
}

// Private service-to-service wire contract.
public class Agent2ServiceRequest
{
    public int PatientId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public List<Agent2DoctorDto> RecommendedDoctors { get; set; } = new();
    public List<Agent2SlotDto> AvailableSlots { get; set; } = new();
}

public class Agent2DoctorDto
{
    public int DoctorId { get; set; }
    public int DoctorProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class Agent2SlotDto
{
    public int DoctorId { get; set; }
    public int DoctorProfileId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class Agent2ServiceResponse
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public Agent2ServiceSlot? RecommendedSlot { get; set; }
    public List<Agent2ServiceSlot> AlternativeSlots { get; set; } = new();
    public string? Message { get; set; }
}

public class Agent2ServiceSlot
{
    public int DoctorId { get; set; }
    public int DoctorProfileId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public int DurationMinutes { get; set; }
    public string? Reason { get; set; }
}

public class Agent2ServiceError
{
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}
