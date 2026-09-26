namespace SmartHospital.Api.DTOs.Schedules;

public class ScheduleFilterRequest
{
    public int? DoctorId { get; set; }

    public int? DepartmentId { get; set; }

    public string? DayOfWeek { get; set; }
}
