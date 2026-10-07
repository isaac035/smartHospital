using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Maintenance;

public class UpdateMaintenanceRequest : IValidatableObject
{
    [Required]
    [DefinedEnum]
    public MaintenanceType Type { get; set; } = MaintenanceType.RoutineInspection;

    [Required]
    [MaxLength(500)]
    [NoHtml]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ScheduledEnd.HasValue && ScheduledEnd.Value <= ScheduledStart)
            yield return new ValidationResult("Scheduled end time must be after scheduled start time.", new[] { nameof(ScheduledEnd) });
    }
}
