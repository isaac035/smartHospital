using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Appointments;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

/// <summary>Manages appointment booking, updates, cancellation, and rescheduling.</summary>
[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentController : ControllerBase { private readonly IAppointmentService _appointmentService; private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.HospitalHub> _hub; public AppointmentController(IAppointmentService appointmentService, Microsoft.AspNetCore.SignalR.IHubContext<Hubs.HospitalHub> hub) { _appointmentService = appointmentService; _hub = hub; }

    // ── GET /api/appointments ─────────────────────────────────────────────────

    /// <summary>
    /// Lists appointments. Patients see only their own; Staff/Doctor/Admin see all.
    /// Supports filtering by status, priority, doctor, department, and date range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentListPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments([FromQuery] AppointmentQueryFilter filter)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        var role = GetCurrentUserRole();
        if (filter.Page.HasValue || filter.PageSize.HasValue)
        {
            var page = await _appointmentService.GetAppointmentsPageAsync(userId.Value, role, filter);
            return Ok(page);
        }
        var appointments = await _appointmentService.GetAppointmentsAsync(userId.Value, role, filter);
        return Ok(appointments);
    }

    // ── GET /api/appointments/{id} ────────────────────────────────────────────

    /// <summary>Returns a single appointment by ID. Patients can only retrieve their own.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var role        = GetCurrentUserRole();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id, userId.Value, role);
            if (appointment == null) return NotFound(new { message = "Appointment not found." });
            return Ok(appointment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── POST /api/appointments ────────────────────────────────────────────────

    /// <summary>
    /// Books a new appointment. Patients can book only for themselves.
    /// Staff/Admin can book on behalf of any patient.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        var role = GetCurrentUserRole();
        // The JWT is authoritative for patient bookings; never trust a body ID.
        if (role == "Patient") request.PatientId = userId.Value;

        try
        {
            var result = await _appointmentService.BookAppointmentAsync(userId.Value, request);
            await BroadcastAppointmentEventAsync("AppointmentCreated", result);
            await BroadcastAppointmentEventAsync("SlotBooked", result);
            return Created($"/api/appointments/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── PUT /api/appointments/{id} ────────────────────────────────────────────

    /// <summary>Updates mutable fields (notes, type, duration) on a non-terminal appointment.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var role   = GetCurrentUserRole();
            var result = await _appointmentService.UpdateAppointmentAsync(id, request, userId.Value, role);
            if (result == null) return NotFound(new { message = "Appointment not found." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Changes an appointment's queue priority. Only Staff or Admin may call this endpoint.</summary>
    [HttpPut("{id:int}/priority")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAppointmentPriority(int id, [FromBody] UpdateAppointmentPriorityRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var priority = request.Priority!.Value;
            var result = await _appointmentService.UpdateAppointmentPriorityAsync(id, priority, userId.Value);
            await BroadcastAppointmentEventAsync("AppointmentUpdated", result);
            await BroadcastAppointmentEventAsync("QueueUpdated", result);
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
    }

    // ── POST /api/appointments/{id}/cancel ────────────────────────────────────

    /// <summary>Cancels an appointment. Patients can only cancel their own.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelAppointment(int id, [FromBody] CancelAppointmentRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var role   = GetCurrentUserRole();
            var result = await _appointmentService.CancelAppointmentAsync(id, request, userId.Value, role);
            await BroadcastAppointmentEventAsync("AppointmentCancelled", result);
            await BroadcastAppointmentEventAsync("SlotReleased", result);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/appointments/{id}/reschedule ────────────────────────────────

    /// <summary>
    /// Reschedules to a new future slot.
    /// Marks original as Rescheduled and returns the new appointment.
    /// </summary>
    [HttpPost("{id:int}/reschedule")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var role   = GetCurrentUserRole();
            var result = await _appointmentService.RescheduleAppointmentAsync(id, request, userId.Value, role);
            await BroadcastAppointmentEventAsync("AppointmentUpdated", result);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── GET /api/appointments/{id}/history ────────────────────────────────────

    /// <summary>Returns the full status-change audit trail for an appointment.</summary>
    [HttpGet("{id:int}/history")]
    [Authorize(Roles = "Staff,Admin,Doctor")]
    [ProducesResponseType(typeof(List<AppointmentStatusHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusHistory(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id, userId.Value, GetCurrentUserRole());
            if (appointment == null) return NotFound(new { message = "Appointment not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        var history = await _appointmentService.GetStatusHistoryAsync(id);
        return Ok(history);
    }

    // ── POST /api/appointments/{id}/confirm-emergency ─────────────────────────

    /// <summary>Confirms a Scheduled appointment. Only Staff or Admin may call this endpoint.</summary>
    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAppointment(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var result = await _appointmentService.ConfirmAppointmentAsync(id, userId.Value);
            await BroadcastAppointmentEventAsync("AppointmentUpdated", result);
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
    }

    /// <summary>
    /// Confirms an Emergency-priority appointment.
    /// Only Staff or Admin may call this endpoint.
    /// </summary>
    [HttpPost("{id:int}/confirm-emergency")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmergency(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            var result = await _appointmentService.ConfirmEmergencyAsync(id, userId.Value);
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
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    private Task BroadcastAppointmentEventAsync(string eventName, AppointmentResponse appointment) =>
        _hub.Clients.Groups(new[] { $"user:{appointment.PatientId}", $"user:{appointment.DoctorId}", $"doctor:{appointment.DoctorId}", "staff", "admin" })
            .SendAsync(eventName, appointment);
}

