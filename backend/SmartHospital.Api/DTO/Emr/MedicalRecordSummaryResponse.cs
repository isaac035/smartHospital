namespace SmartHospital.Api.DTOs.Emr;

public class MedicalRecordSummaryResponse
{
    public int Id { get; set; }

    public string RecordNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public DateTime VisitDate { get; set; }

    public string ChiefComplaint { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }
}
