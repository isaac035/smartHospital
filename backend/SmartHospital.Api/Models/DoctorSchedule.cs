namespace SmartHospital.Api.Models;

public class DoctorSchedule
{
    public int Id { get; set; }

    public int DoctorId { get; set; }

    public int ConsultationTypeId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    /// <summary>Duration of each bookable appointment slot in minutes, used by the Appointment module.</summary>
    public int SlotDurationMinutes { get; set; } = 30;

    /// <summary>Maximum number of patients the doctor accepts on this schedule day, used by the Appointment module.</summary>
    public int MaxPatientsPerDay { get; set; } = 20;

    public ScheduleStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Doctor? Doctor { get; set; }

    public ConsultationType? ConsultationType { get; set; }
}
