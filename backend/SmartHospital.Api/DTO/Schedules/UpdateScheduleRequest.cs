using SmartHospital.Api.Models;
using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Schedules;

public class UpdateScheduleRequest : IValidatableObject
{
    [Required]
    [SelectedId]
    public int ConsultationTypeId { get; set; }

    [Required]
    [EnumName(typeof(SmartHospital.Api.Models.DayOfWeek))]
    public string DayOfWeek { get; set; } = string.Empty;

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
            yield return new ValidationResult("End time must be after start time.", new[] { nameof(EndTime) });
    }
}
