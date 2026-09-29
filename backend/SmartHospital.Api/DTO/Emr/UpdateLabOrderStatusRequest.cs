using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Emr;

public class UpdateLabOrderStatusRequest : IValidatableObject
{
    [Required(ErrorMessage = "Status is required.")]
    public LabOrderStatus Status { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(LabOrderStatus), Status))
        {
            yield return new ValidationResult(
                "Invalid lab order status.",
                new[] { nameof(Status) });
        }
    }
}
