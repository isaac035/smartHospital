using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.Models;

public class ClinicalTreatmentPlan
{
    public int Id { get; set; }

    [Required]
    public int MedicalRecordId { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public TreatmentPlanCategory Category { get; set; } = TreatmentPlanCategory.General;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Goals { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Interventions { get; set; } = string.Empty;

    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Active;

    public DateTime StartDate { get; set; }

    public DateTime? TargetDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? ReviewDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public MedicalRecord? MedicalRecord { get; set; }

    public User? Patient { get; set; }

    public User? Doctor { get; set; }
}
