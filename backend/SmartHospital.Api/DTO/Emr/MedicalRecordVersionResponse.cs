namespace SmartHospital.Api.DTOs.Emr;

public class MedicalRecordVersionResponse
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public int VersionNumber { get; set; }

    public int ChangedByUserId { get; set; }

    public string ChangedByUserName { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    public string ChangeType { get; set; } = string.Empty;

    public string ChangeSummary { get; set; } = string.Empty;

    public string ChiefComplaint { get; set; } = string.Empty;

    public string Symptoms { get; set; } = string.Empty;

    public string ExaminationNotes { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string TreatmentPlan { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }

    // Previous clinical values
    public string? PreviousChiefComplaint { get; set; }

    public string? PreviousSymptoms { get; set; }

    public string? PreviousExaminationNotes { get; set; }

    public string? PreviousDiagnosis { get; set; }

    public string? PreviousTreatmentPlan { get; set; }

    public DateTime? PreviousFollowUpDate { get; set; }
}
