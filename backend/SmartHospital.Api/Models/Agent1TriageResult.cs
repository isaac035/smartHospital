namespace SmartHospital.Api.Models;

/// <summary>Immutable Agent 1 output retained so a later medical report can use the real triage result.</summary>
public class Agent1TriageResult
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? Reason { get; set; }
    public double? Confidence { get; set; }
    public bool PossibleEmergency { get; set; }
    public string? EmergencyNotice { get; set; }
    public bool UsedDefaultCategory { get; set; }
    public DateTime CreatedAt { get; set; }
    public Appointment? Appointment { get; set; }
}
