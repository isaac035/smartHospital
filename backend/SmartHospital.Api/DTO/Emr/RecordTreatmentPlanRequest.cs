using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Emr;

public class RecordTreatmentPlanRequest : IValidatableObject
{
    [Required(ErrorMessage = "Treatment plan title is required.")]
    [MaxLength(200, ErrorMessage = "Treatment plan title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    public TreatmentPlanCategory Category { get; set; } = TreatmentPlanCategory.General;

    [Required(ErrorMessage = "Treatment plan description is required.")]
    [MaxLength(2000, ErrorMessage = "Treatment plan description cannot exceed 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Goals cannot exceed 1000 characters.")]
    public string? Goals { get; set; }

    [MaxLength(2000, ErrorMessage = "Interventions cannot exceed 2000 characters.")]
    public string? Interventions { get; set; }

    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Active;

    public DateTime? StartDate { get; set; }

    public DateTime? TargetDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? ReviewDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult(
                "Treatment plan title cannot be empty or whitespace.",
                new[] { nameof(Title) });
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult(
                "Treatment plan description cannot be empty or whitespace.",
                new[] { nameof(Description) });
        }

        if (!Enum.IsDefined(typeof(TreatmentPlanCategory), Category))
        {
            yield return new ValidationResult(
                "Invalid treatment plan category.",
                new[] { nameof(Category) });
        }

        if (!Enum.IsDefined(typeof(TreatmentPlanStatus), Status))
        {
            yield return new ValidationResult(
                "Invalid treatment plan status.",
                new[] { nameof(Status) });
        }

        var effectiveStart = StartDate ?? DateTime.UtcNow;

        if (TargetDate.HasValue && TargetDate.Value < effectiveStart)
        {
            yield return new ValidationResult(
                "Target date must be on or after start date.",
                new[] { nameof(TargetDate) });
        }

        if (EndDate.HasValue && EndDate.Value < effectiveStart)
        {
            yield return new ValidationResult(
                "End date must be on or after start date.",
                new[] { nameof(EndDate) });
        }

        if (ReviewDate.HasValue && ReviewDate.Value < effectiveStart)
        {
            yield return new ValidationResult(
                "Review date must be on or after start date.",
                new[] { nameof(ReviewDate) });
        }
    }
}
