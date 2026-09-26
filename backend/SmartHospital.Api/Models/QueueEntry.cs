namespace SmartHospital.Api.Models;

public class QueueEntry
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public int DoctorId { get; set; }

    public int QueueNumber { get; set; }

    public QueueEntryStatus Status { get; set; } = QueueEntryStatus.Waiting;

    public AppointmentPriority Priority { get; set; } = AppointmentPriority.Normal;

    /// <summary>Estimated minutes until this patient is called (recalculated on each queue update).</summary>
    public int? EstimatedWaitMinutes { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public DateTime? CalledAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // ── Navigation properties ─────────────────────────────────────────────────

    public Appointment? Appointment { get; set; }

    public User? Doctor { get; set; }
}
