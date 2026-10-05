using SmartHospital.Api.DTOs.Notifications;

namespace SmartHospital.Api.Services.Interfaces;

public interface INotificationService
{
    /// <summary>Returns all appointment-related notifications for the given user.</summary>
    Task<List<NotificationResponse>> GetNotificationsAsync(int userId);

    /// <summary>
    /// Marks a notification as read.
    /// Throws <see cref="UnauthorizedAccessException"/> if the notification does not belong to the user.
    /// </summary>
    Task MarkAsReadAsync(int notificationId, int userId);

    /// <summary>
    /// Creates an appointment-scoped notification for a user.
    /// Called internally by AppointmentService and QueueService.
    /// </summary>
    Task CreateAppointmentNotificationAsync(
        int userId,
        int appointmentId,
        string title,
        string message);
}
