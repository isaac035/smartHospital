namespace SmartHospital.Api.Models;

/// <summary>
/// Appointment-scoped notification entity.
/// Intentionally kept separate from any shared Notification entity that
/// other modules may define.  Covers booking confirmations, queue calls,
/// reminders, and cancellations only.
/// </summary>
public class AppointmentNotification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AppointmentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    // ── Navigation properties ─────────────────────────────────────────────────

    public User? User { get; set; }

    public Appointment? Appointment { get; set; }
}
