using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class UpdateMedicalRecordRequest : IValidatableObject
{
    [Required(ErrorMessage = "Chief complaint is required.")]
    [MaxLength(500, ErrorMessage = "Chief complaint cannot exceed 500 characters.")]
    public string ChiefComplaint { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Symptoms cannot exceed 1000 characters.")]
    public string? Symptoms { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Examination notes cannot exceed 2000 characters.")]
    public string? ExaminationNotes { get; set; } = string.Empty;

    [Required(ErrorMessage = "Diagnosis is required.")]
    [MaxLength(500, ErrorMessage = "Diagnosis cannot exceed 500 characters.")]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Treatment plan cannot exceed 2000 characters.")]
    public string? TreatmentPlan { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Appointment ID must be a positive integer.")]
    public int? AppointmentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Admission ID must be a positive integer.")]
    public int? AdmissionId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ChiefComplaint))
        {
            yield return new ValidationResult(
                "Chief complaint cannot be empty or whitespace.",
                new[] { nameof(ChiefComplaint) });
        }

        if (string.IsNullOrWhiteSpace(Diagnosis))
        {
            yield return new ValidationResult(
                "Diagnosis cannot be empty or whitespace.",
                new[] { nameof(Diagnosis) });
        }

        if (AppointmentId.HasValue && AppointmentId.Value <= 0)
        {
            yield return new ValidationResult(
                "Appointment ID must be a positive integer.",
                new[] { nameof(AppointmentId) });
        }

        if (AdmissionId.HasValue && AdmissionId.Value <= 0)
        {
            yield return new ValidationResult(
                "Admission ID must be a positive integer.",
                new[] { nameof(AdmissionId) });
        }

        if (FollowUpDate.HasValue)
        {
            if (FollowUpDate.Value <= DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Follow-up date must be in the future.",
                    new[] { nameof(FollowUpDate) });
            }
            else if (FollowUpDate.Value > DateTime.UtcNow.AddYears(5))
            {
                yield return new ValidationResult(
                    "Follow-up date cannot be more than 5 years in the future.",
                    new[] { nameof(FollowUpDate) });
            }
        }
    }
}
