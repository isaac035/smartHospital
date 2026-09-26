namespace SmartHospital.Api.Models;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int? DepartmentId { get; set; }

    public AppointmentType AppointmentType { get; set; } = AppointmentType.General;

    /// <summary>UTC start time of the booked slot.</summary>
    public DateTime ScheduledStart { get; set; }

    /// <summary>Estimated consultation duration in minutes.</summary>
    public int EstimatedDurationMinutes { get; set; } = 30;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public AppointmentPriority Priority { get; set; } = AppointmentPriority.Normal;

    /// <summary>Human-readable reference number (e.g. APT-20260924-1234).</summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>Daily sequential queue number assigned at booking.</summary>
    public int? QueueNumber { get; set; }

    public string? Notes { get; set; }

    public string? CancelledReason { get; set; }

    /// <summary>FK to the original appointment if this was created via reschedule.</summary>
    public int? RescheduledFromId { get; set; }

    /// <summary>
    /// Emergency-priority appointments require explicit confirmation by
    /// an authorised Staff or Admin user before they enter the queue.
    /// </summary>
    public bool EmergencyConfirmed { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // ── Navigation properties ─────────────────────────────────────────────────

    public User? Patient { get; set; }

    public User? Doctor { get; set; }

    public Department? Department { get; set; }

    public Appointment? RescheduledFrom { get; set; }

    public QueueEntry? QueueEntry { get; set; }

    public ICollection<AppointmentStatusHistory> StatusHistories { get; set; }
        = new List<AppointmentStatusHistory>();

    public ICollection<AppointmentNotification> Notifications { get; set; }
        = new List<AppointmentNotification>();
}
