using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class UpdateMedicalRecordRequest
{
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
}
