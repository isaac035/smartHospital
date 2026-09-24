using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Notifications;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

/// <summary>
/// Appointment-scoped notification endpoints.
/// Intentionally limited to booking confirmations, queue calls, and cancellations.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // ── GET /api/notifications ────────────────────────────────────────────────

    /// <summary>Returns all appointment notifications for the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        var notifications = await _notificationService.GetNotificationsAsync(userId.Value);
        return Ok(notifications);
    }

    // ── POST /api/notifications/{id}/read ─────────────────────────────────────

    /// <summary>Marks a specific notification as read. Users can only mark their own notifications.</summary>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            await _notificationService.MarkAsReadAsync(id, userId.Value);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
