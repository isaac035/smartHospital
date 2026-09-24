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
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    // ── GET /api/appointments ─────────────────────────────────────────────────

    /// <summary>
    /// Lists appointments. Patients see only their own; Staff/Doctor/Admin see all.
    /// Supports filtering by status, priority, doctor, department, and date range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments([FromQuery] AppointmentQueryFilter filter)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "User is not authenticated." });

        var role = GetCurrentUserRole();
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
        catch (UnauthorizedAccessException ex)
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

        // Patients may only book for themselves
        var role = GetCurrentUserRole();
        if (role == "Patient" && request.PatientId != userId.Value)
            return Forbid();

        try
        {
            var result = await _appointmentService.BookAppointmentAsync(userId.Value, request);
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
        var history = await _appointmentService.GetStatusHistoryAsync(id);
        return Ok(history);
    }

    // ── POST /api/appointments/{id}/confirm-emergency ─────────────────────────

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
}
