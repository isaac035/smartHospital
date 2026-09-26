using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class MedicalRecordQueryFilter : IValidatableObject
{
    public int? PatientId { get; set; }

    public string? PatientSearch { get; set; }

    public int? DoctorId { get; set; }

    public string? DoctorSearch { get; set; }

    public string? RecordNumber { get; set; }

    public string? Diagnosis { get; set; }

    public DateTime? VisitDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PatientId.HasValue && PatientId.Value <= 0)
        {
            yield return new ValidationResult(
                "Patient ID must be a positive integer.",
                new[] { nameof(PatientId) });
        }

        if (DoctorId.HasValue && DoctorId.Value <= 0)
        {
            yield return new ValidationResult(
                "Doctor ID must be a positive integer.",
                new[] { nameof(DoctorId) });
        }

        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
        {
            yield return new ValidationResult(
                "Start date cannot be after end date.",
                new[] { nameof(StartDate), nameof(EndDate) });
        }

        if (Page < 1)
        {
            yield return new ValidationResult(
                "Page must be at least 1.",
                new[] { nameof(Page) });
        }

        if (PageSize < 1)
        {
            yield return new ValidationResult(
                "Page size must be at least 1.",
                new[] { nameof(PageSize) });
        }
    }
}
