using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class CreatePrescriptionRequest : IValidatableObject
{
    [Required(ErrorMessage = "Patient ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Patient ID must be a positive integer.")]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Medical record ID must be a positive integer.")]
    public int? MedicalRecordId { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(1000, ErrorMessage = "General instructions cannot exceed 1000 characters.")]
    public string? GeneralInstructions { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one prescription item is required.")]
    [MinLength(1, ErrorMessage = "At least one prescription item is required.")]
    public List<CreatePrescriptionItemRequest> Items { get; set; } = new();

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

        if (ExpiryDate.HasValue)
        {
            if (ExpiryDate.Value <= DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Expiry date must be in the future.",
                    new[] { nameof(ExpiryDate) });
            }
            else if (ExpiryDate.Value > DateTime.UtcNow.AddYears(1))
            {
                yield return new ValidationResult(
                    "Expiry date cannot be more than 1 year in the future.",
                    new[] { nameof(ExpiryDate) });
            }
        }

        if (Items == null || Items.Count == 0)
        {
            yield return new ValidationResult(
                "At least one prescription item is required.",
                new[] { nameof(Items) });
        }
        else
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (item == null)
                {
                    yield return new ValidationResult(
                        $"Prescription item at index {i} cannot be null.",
                        new[] { nameof(Items) });
                    continue;
                }

                var itemResults = new List<ValidationResult>();
                var itemContext = new ValidationContext(item, validationContext, validationContext.Items);
                if (!Validator.TryValidateObject(item, itemContext, itemResults, true))
                {
                    foreach (var res in itemResults)
                    {
                        yield return new ValidationResult(
                            $"Item [{i + 1}]: {res.ErrorMessage}",
                            res.MemberNames.Select(m => $"{nameof(Items)}[{i}].{m}"));
                    }
                }
            }
        }
    }
}

public class CreatePrescriptionItemRequest : IValidatableObject
{
    [Required(ErrorMessage = "Medicine name is required.")]
    [MaxLength(150, ErrorMessage = "Medicine name cannot exceed 150 characters.")]
    public string MedicineName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Dosage is required.")]
    [MaxLength(50, ErrorMessage = "Dosage cannot exceed 50 characters.")]
    public string Dosage { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Route cannot exceed 50 characters.")]
    public string Route { get; set; } = "Oral";

    [Required(ErrorMessage = "Frequency is required.")]
    [MaxLength(50, ErrorMessage = "Frequency cannot exceed 50 characters.")]
    public string Frequency { get; set; } = string.Empty;

    [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days.")]
    public int DurationDays { get; set; } = 1;

    [MaxLength(500, ErrorMessage = "Special instructions cannot exceed 500 characters.")]
    public string? SpecialInstructions { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(MedicineName))
        {
            yield return new ValidationResult(
                "Medicine name cannot be empty or whitespace.",
                new[] { nameof(MedicineName) });
        }

        if (string.IsNullOrWhiteSpace(Dosage))
        {
            yield return new ValidationResult(
                "Dosage cannot be empty or whitespace.",
                new[] { nameof(Dosage) });
        }

        if (string.IsNullOrWhiteSpace(Frequency))
        {
            yield return new ValidationResult(
                "Frequency cannot be empty or whitespace.",
                new[] { nameof(Frequency) });
        }

        if (DurationDays < 1 || DurationDays > 365)
        {
            yield return new ValidationResult(
                "Duration must be between 1 and 365 days.",
                new[] { nameof(DurationDays) });
        }
    }
}
