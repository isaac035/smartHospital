using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Queues;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

/// <summary>
/// Manages the real-time patient queue for each doctor.
/// Exposes a polling endpoint for queue status (ready to back a future SignalR hub).
/// </summary>
[ApiController]
[Route("api/queues")]
[Authorize]
public class QueueController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.HospitalHub> _hub;

    public QueueController(IQueueService queueService, Microsoft.AspNetCore.SignalR.IHubContext<Hubs.HospitalHub> hub)
    {
        _queueService = queueService;
        _hub = hub;
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyQueue([FromQuery] int doctorId)
    {
        var patientId = GetCurrentUserId();
        if (!patientId.HasValue) return Unauthorized(new { message = "User is not authenticated." });
        var status = await _queueService.GetMyQueueEntryAsync(patientId.Value, doctorId);
        return status == null ? NotFound(new { message = "You have not checked in for this doctor." }) : Ok(status);
    }

    // ── GET /api/queues/{doctorId} ────────────────────────────────────────────

    /// <summary>
    /// Returns the ordered queue for a doctor (Emergency > Urgent > Normal, then FIFO).
    /// Designed to be polled by the frontend or to back a SignalR hub.
    /// </summary>
    [HttpGet("{doctorId:int}")]
    [Authorize(Roles = "Staff,Admin,Doctor")]
    [ProducesResponseType(typeof(QueueStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueue(int doctorId, [FromQuery] DateOnly? date)
    {
        var currentUserId = GetCurrentUserId();
        if (User.IsInRole("Doctor") && currentUserId != doctorId) return Forbid();
        try
        {
            var status = await _queueService.GetQueueStatusForDoctorAsync(doctorId, date);
            return Ok(status);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── POST /api/queues/{id}/call ────────────────────────────────────────────

    /// <summary>Calls a specific queue entry — marks it as Called and notifies the patient.</summary>
    [HttpPost("{id:int}/call")]
    [Authorize(Roles = "Staff,Admin,Doctor")]
    [ProducesResponseType(typeof(QueueEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CallQueueEntry(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var result = await _queueService.CallQueueEntryAsync(id, userId.Value);
            await BroadcastQueueEventAsync("PatientCalled", result);
            await BroadcastQueueEventAsync("ConsultationStarted", result);
            await BroadcastQueueEventAsync("QueueUpdated", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── GET /api/queues/{id}/status ───────────────────────────────────────────

    /// <summary>
    /// Returns the current status of a specific queue entry, including
    /// the patient's position and estimated wait time.
    /// </summary>
    [HttpGet("{id:int}/status")]
    [Authorize(Roles = "Staff,Admin,Doctor")]
    [ProducesResponseType(typeof(QueueEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQueueEntryStatus(int id)
    {
        var entry = await _queueService.GetQueueEntryStatusAsync(id);
        if (entry == null) return NotFound(new { message = "Queue entry not found." });
        return Ok(entry);
    }

    // ── POST /api/queues/check-in/{appointmentId} ─────────────────────────────

    /// <summary>Checks in a patient for their appointment, placing them into the active queue.</summary>
    [HttpPost("check-in/{appointmentId:int}")]
    [Authorize(Roles = "Staff,Admin,Patient")]
    [ProducesResponseType(typeof(QueueEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckIn(int appointmentId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var result = await _queueService.CheckInAsync(appointmentId, userId.Value);
            await BroadcastQueueEventAsync("PatientCheckedIn", result);
            await BroadcastQueueEventAsync("QueueUpdated", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── POST /api/queues/{id}/no-show ─────────────────────────────────────────

    /// <summary>Marks a queue entry as No-Show and updates the linked appointment accordingly.</summary>
    [HttpPost("{id:int}/no-show")]
    [Authorize(Roles = "Staff,Admin,Doctor")]
    [ProducesResponseType(typeof(QueueEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkNoShow(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var result = await _queueService.MarkNoShowAsync(id, userId.Value);
            await BroadcastQueueEventAsync("QueueUpdated", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── POST /api/queues/{id}/complete ────────────────────────────────────────

    /// <summary>Marks a queue entry as Completed and closes the linked appointment.</summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = "Staff,Admin,Doctor")]
    [ProducesResponseType(typeof(QueueEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkCompleted(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var result = await _queueService.MarkCompletedAsync(id, userId.Value);
            await BroadcastQueueEventAsync("ConsultationCompleted", result);
            await BroadcastQueueEventAsync("QueueUpdated", result);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private Task BroadcastQueueEventAsync(string eventName, QueueEntryResponse entry) =>
        _hub.Clients.Groups(new[] { $"doctor:{entry.DoctorId}", $"user:{entry.PatientId}", "staff", "admin" })
            .SendAsync(eventName, entry);
}
