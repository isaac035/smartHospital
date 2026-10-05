namespace SmartHospital.Api.DTOs.Notifications;

public class NotificationResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AppointmentId { get; set; }

    public string AppointmentReferenceNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
