namespace SmartHospital.Api.DTOs.Schedules;

public class ScheduleResponse
{
    public int Id { get; set; }

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int ConsultationTypeId { get; set; }

    public string ConsultationTypeName { get; set; } = string.Empty;

    public string DayOfWeek { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
}
