using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Availability;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

/// <summary>Exposes free booking slots derived from doctor schedules minus existing appointments.</summary>
[ApiController]
[Route("api/availability")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    // ── GET /api/availability/slots ───────────────────────────────────────────

    /// <summary>
    /// Returns available bookable time slots for a doctor and/or department on a specific date.
    /// At least one of <paramref name="doctorId"/> or <paramref name="departmentId"/> is recommended.
    /// </summary>
    [HttpGet("slots")]
    [ProducesResponseType(typeof(List<AvailableSlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int? doctorId,
        [FromQuery] int? departmentId,
        [FromQuery] DateTime date)
    {
        if (date == default)
            return BadRequest(new { message = "A valid date is required." });

        try
        {
            var slots = await _availabilityService.GetAvailableSlotsAsync(doctorId, departmentId, date);
            return Ok(slots);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── GET /api/availability/suggestions ─────────────────────────────────────

    /// <summary>
    /// Returns up to N rescheduling suggestions (future free slots) for a doctor,
    /// scanning forward from the preferred date.
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(List<RescheduleSuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRescheduleSuggestions(
        [FromQuery] int doctorId,
        [FromQuery] DateTime preferredDate,
        [FromQuery] int durationMinutes = 30,
        [FromQuery] int count = 5)
    {
        if (preferredDate == default)
            return BadRequest(new { message = "A valid preferred date is required." });

        try
        {
            var suggestions = await _availabilityService.GetRescheduleSuggestionsAsync(
                doctorId, preferredDate, durationMinutes, count);
            return Ok(suggestions);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
