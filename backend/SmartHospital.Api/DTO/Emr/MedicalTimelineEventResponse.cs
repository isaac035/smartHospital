namespace SmartHospital.Api.DTOs.Emr;

public class MedicalTimelineEventResponse
{
    public string EventType { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int? DoctorId { get; set; }

    public string? DoctorName { get; set; }

    public string Summary { get; set; } = string.Empty;

    public int SourceRecordId { get; set; }

    public string? Category { get; set; }

    public string? Status { get; set; }
}
