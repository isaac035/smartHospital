using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace SmartHospital.Api.Validation;

/// <summary>
/// Gives the built-in DataAnnotations attributes readable messages that match the React and
/// Flutter wording ("First name is required." instead of "The FirstName field is required.").
/// Attributes that already define their own ErrorMessage are left unchanged; no rules change.
/// </summary>
public sealed class FriendlyValidationMessages : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        if (context.Key.Name is null) return;
        var label = FieldLabel.FromName(context.Key.Name);

        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceType is not null) continue;

            var message = attribute switch
            {
                RequiredAttribute => $"{label} is required.",
                MaxLengthAttribute max => $"{label} cannot exceed {max.Length} characters.",
                MinLengthAttribute min => $"{label} must be at least {min.Length} characters.",
                StringLengthAttribute length when length.MinimumLength > 0 =>
                    $"{label} must be between {length.MinimumLength} and {length.MaximumLength} characters.",
                StringLengthAttribute length => $"{label} cannot exceed {length.MaximumLength} characters.",
                RangeAttribute range => $"{label} must be between {range.Minimum} and {range.Maximum}.",
                EmailAddressAttribute => "Email must be a valid email address.",
                _ => null
            };
            if (message is not null) attribute.ErrorMessage = message;
        }
    }
}
