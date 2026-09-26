using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Emr;

public class CreateLabOrderRequest : IValidatableObject
{
    [Required(ErrorMessage = "Patient ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Patient ID must be a positive integer.")]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Medical record ID must be a positive integer.")]
    public int? MedicalRecordId { get; set; }

    [Required(ErrorMessage = "Test name is required.")]
    [MaxLength(120, ErrorMessage = "Test name cannot exceed 120 characters.")]
    public string TestName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required.")]
    public LabOrderPriority Priority { get; set; } = LabOrderPriority.Routine;

    [MaxLength(1000, ErrorMessage = "Clinical notes cannot exceed 1000 characters.")]
    public string ClinicalNotes { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PatientId <= 0)
        {
            yield return new ValidationResult(
                "Patient ID must be a positive integer.",
                new[] { nameof(PatientId) });
        }

        if (MedicalRecordId.HasValue && MedicalRecordId.Value <= 0)
        {
            yield return new ValidationResult(
                "Medical record ID must be a positive integer.",
                new[] { nameof(MedicalRecordId) });
        }

        if (string.IsNullOrWhiteSpace(TestName))
        {
            yield return new ValidationResult(
                "Test name cannot be empty or whitespace.",
                new[] { nameof(TestName) });
        }

        if (!Enum.IsDefined(typeof(LabOrderPriority), Priority))
        {
            yield return new ValidationResult(
                "Invalid lab order priority.",
                new[] { nameof(Priority) });
        }
    }
}
