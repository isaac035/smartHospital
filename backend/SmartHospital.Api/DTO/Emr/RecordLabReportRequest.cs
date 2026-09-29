using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class RecordLabReportRequest : IValidatableObject
{
    [Required(ErrorMessage = "Result summary is required.")]
    [MaxLength(500, ErrorMessage = "Result summary cannot exceed 500 characters.")]
    public string ResultSummary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Findings are required.")]
    [MaxLength(2000, ErrorMessage = "Findings cannot exceed 2000 characters.")]
    public string Findings { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Reference range cannot exceed 500 characters.")]
    public string ReferenceRange { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Doctor remarks cannot exceed 1000 characters.")]
    public string DoctorRemarks { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Attachment URL cannot exceed 500 characters.")]
    public string AttachmentUrl { get; set; } = string.Empty;

    public DateTime? ReportDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ResultSummary))
        {
            yield return new ValidationResult(
                "Result summary cannot be empty or whitespace.",
                new[] { nameof(ResultSummary) });
        }

        if (string.IsNullOrWhiteSpace(Findings))
        {
            yield return new ValidationResult(
                "Findings cannot be empty or whitespace.",
                new[] { nameof(Findings) });
        }

        if (ReportDate.HasValue && ReportDate.Value > DateTime.UtcNow.AddMinutes(5))
        {
            yield return new ValidationResult(
                "Report date cannot be in the future.",
                new[] { nameof(ReportDate) });
        }

        if (!string.IsNullOrWhiteSpace(AttachmentUrl))
        {
            var trimmedUrl = AttachmentUrl.Trim();
            if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uriResult)
                || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                yield return new ValidationResult(
                    "Attachment URL must be a valid HTTP or HTTPS URL.",
                    new[] { nameof(AttachmentUrl) });
            }
        }
    }
}
