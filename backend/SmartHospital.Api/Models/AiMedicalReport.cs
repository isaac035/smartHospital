namespace SmartHospital.Api.Models;

/// <summary>Immutable generated snapshot. A regeneration always inserts another row.</summary>
public class AiMedicalReport
{
    public int Id { get; set; }
    public Guid ReportId { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ContentJson { get; set; } = "{}";
    public Guid? ClientGenerationId { get; set; }
    public User? Patient { get; set; }
    public Appointment? Appointment { get; set; }
}
