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

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}
