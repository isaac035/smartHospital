using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Notifications;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger  = logger;
    }

    public async Task<List<NotificationResponse>> GetNotificationsAsync(int userId)
    {
        return await _context.AppointmentNotifications
            .Include(n => n.Appointment)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                Id                         = n.Id,
                UserId                     = n.UserId,
                AppointmentId              = n.AppointmentId,
                AppointmentReferenceNumber = n.Appointment != null ? n.Appointment.ReferenceNumber : string.Empty,
                Title                      = n.Title,
                Message                    = n.Message,
                IsRead                     = n.IsRead,
                CreatedAt                  = n.CreatedAt,
                ReadAt                     = n.ReadAt
            })
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.AppointmentNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId)
            ?? throw new KeyNotFoundException("Notification not found.");

        if (notification.UserId != userId)
            throw new UnauthorizedAccessException("You are not authorised to access this notification.");

        if (notification.IsRead) return; // idempotent

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task CreateAppointmentNotificationAsync(
        int userId,
        int appointmentId,
        string title,
        string message)
    {
        var notification = new AppointmentNotification
        {
            UserId        = userId,
            AppointmentId = appointmentId,
            Title         = title,
            Message       = message,
            IsRead        = false,
            CreatedAt     = DateTime.UtcNow
        };

        _context.AppointmentNotifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Notification '{Title}' created for user {UserId} re appointment {AppointmentId}.",
            title, userId, appointmentId);
    }
}
