using SmartHospital.Api.Models;
using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Leaves;

public class UpdateLeaveRequest : IValidatableObject
{
    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [MaxLength(500)]
    [NoHtml]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [EnumName(typeof(LeaveStatus))]
    public string Status { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
            yield return new ValidationResult("End date must be on or after start date.", new[] { nameof(EndDate) });
    }
}
