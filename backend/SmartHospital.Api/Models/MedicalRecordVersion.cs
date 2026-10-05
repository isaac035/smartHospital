using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.Models;

public class MedicalRecordVersion
{
    public int Id { get; set; }

    [Required]
    public int MedicalRecordId { get; set; }

    [Required]
    public int VersionNumber { get; set; }

    [Required]
    public int ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    [MaxLength(50)]
    public string ChangeType { get; set; } = "Update";

    [MaxLength(1000)]
    public string ChangeSummary { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ChiefComplaint { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Symptoms { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ExaminationNotes { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string TreatmentPlan { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }

    // Previous clinical values
    [MaxLength(500)]
    public string? PreviousChiefComplaint { get; set; }

    [MaxLength(1000)]
    public string? PreviousSymptoms { get; set; }

    [MaxLength(2000)]
    public string? PreviousExaminationNotes { get; set; }

    [MaxLength(500)]
    public string? PreviousDiagnosis { get; set; }

    [MaxLength(2000)]
    public string? PreviousTreatmentPlan { get; set; }

    public DateTime? PreviousFollowUpDate { get; set; }

    // Navigation properties
    public MedicalRecord? MedicalRecord { get; set; }

    public User? ChangedByUser { get; set; }
}
