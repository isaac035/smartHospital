namespace SmartHospital.Api.Models;

public class AppointmentStatusHistory
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public AppointmentStatus OldStatus { get; set; }

    public AppointmentStatus NewStatus { get; set; }

    /// <summary>UserId of the user who triggered the status change.</summary>
    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Reason { get; set; }

    // ── Navigation properties ─────────────────────────────────────────────────

    public Appointment? Appointment { get; set; }

    public User? ChangedByUser { get; set; }
}
