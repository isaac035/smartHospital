namespace SmartHospital.Api.Models;

// TODO: owned by Doctor & Clinical Schedule Management module.
// Minimal read-only stub created by Appointment & Queue module.
// The Appointment module ONLY queries this entity — it never creates, updates,
// or deletes DoctorSchedule records.
public class DoctorSchedule
{
    public int Id { get; set; }

    public int DoctorId { get; set; }

    public int? DepartmentId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    /// <summary>Duration of each bookable time slot in minutes.</summary>
    public int SlotDurationMinutes { get; set; } = 30;

    /// <summary>Maximum number of patients the doctor accepts on this schedule day.</summary>
    public int MaxPatientsPerDay { get; set; } = 20;

    public bool IsActive { get; set; } = true;

    // Navigation properties (read-only usage)
    public User? Doctor { get; set; }

    public Department? Department { get; set; }
}
