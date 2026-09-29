namespace SmartHospital.Api.DTOs.Emr;

public class PatientMedicalTimelineResponse
{
    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int TotalEvents { get; set; }

    public List<MedicalTimelineEventResponse> Events { get; set; } = new();
}
