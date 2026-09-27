using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Emr;

public class UpdateDiagnosisRequest : IValidatableObject
{
    [MaxLength(50, ErrorMessage = "Diagnosis code cannot exceed 50 characters.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Diagnosis description is required.")]
    [MaxLength(500, ErrorMessage = "Diagnosis description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    public DiagnosisType Type { get; set; } = DiagnosisType.Primary;

    public DiagnosisStatus Status { get; set; } = DiagnosisStatus.Active;

    public DiagnosisSeverity Severity { get; set; } = DiagnosisSeverity.Moderate;

    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
    public string? Notes { get; set; }

    public DateTime? DiagnosedAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult(
                "Diagnosis description cannot be empty or whitespace.",
                new[] { nameof(Description) });
        }

        if (!Enum.IsDefined(typeof(DiagnosisType), Type))
        {
            yield return new ValidationResult(
                "Invalid diagnosis type.",
                new[] { nameof(Type) });
        }

        if (!Enum.IsDefined(typeof(DiagnosisStatus), Status))
        {
            yield return new ValidationResult(
                "Invalid diagnosis status.",
                new[] { nameof(Status) });
        }

        if (!Enum.IsDefined(typeof(DiagnosisSeverity), Severity))
        {
            yield return new ValidationResult(
                "Invalid diagnosis severity.",
                new[] { nameof(Severity) });
        }

        if (DiagnosedAt.HasValue)
        {
            if (DiagnosedAt.Value > DateTime.UtcNow.AddDays(1))
            {
                yield return new ValidationResult(
                    "Diagnosed date cannot be in the future.",
                    new[] { nameof(DiagnosedAt) });
            }
            else if (DiagnosedAt.Value < DateTime.UtcNow.AddYears(-100))
            {
                yield return new ValidationResult(
                    "Diagnosed date cannot be more than 100 years in the past.",
                    new[] { nameof(DiagnosedAt) });
            }
        }
    }
}
