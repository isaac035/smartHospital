namespace SmartHospital.Api.DTOs.Emr;

public class TreatmentPlanResponse
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Goals { get; set; } = string.Empty;

    public string Interventions { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? TargetDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? ReviewDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
