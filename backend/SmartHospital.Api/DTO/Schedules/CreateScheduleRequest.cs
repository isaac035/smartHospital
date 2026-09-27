using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Schedules;

public class CreateScheduleRequest
{
    [Required]
    public int DoctorId { get; set; }

    [Required]
    public int ConsultationTypeId { get; set; }

    [Required]
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>Optional date for one-day slot generation. Omit for weekly recurring availability.</summary>
    public DateOnly? SpecificDate { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}
