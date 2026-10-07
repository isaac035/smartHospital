using SmartHospital.Api.Models;
using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Schedules;

public class CreateScheduleRequest : IValidatableObject
{
    [Required]
    [SelectedId]
    public int DoctorId { get; set; }

    [Required]
    [SelectedId]
    public int ConsultationTypeId { get; set; }

    [Required]
    [EnumName(typeof(SmartHospital.Api.Models.DayOfWeek))]
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>Optional date for one-day availability. Omit for weekly recurring availability.</summary>
    public DateOnly? SpecificDate { get; set; }

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
