using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartHospital.Api.DTOs.Agent1;
using SmartHospital.Api.Services.Agent1;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

/// <summary>
/// Agent 1 - Clinical Triage + Doctor Matching ("Smart Care" in the patient app).
/// Recommends a specialty and real bookable doctors. It never books: the patient continues
/// through the existing appointment flow and POST /api/appointments.
/// </summary>
[ApiController]
[Route("api/agent1")]
[Authorize(Roles = "Patient")]
public class Agent1Controller : ControllerBase
{
    public const string RateLimitPolicy = "agent1-per-patient";

    private readonly IAgent1TriageService _triageService;
    private readonly ILogger<Agent1Controller> _logger;

    public Agent1Controller(IAgent1TriageService triageService, ILogger<Agent1Controller> logger)
    {
        _triageService = triageService;
        _logger = logger;
    }

    // ── POST /api/agent1/triage-doctor-match ──────────────────────────────────

    [HttpPost("triage-doctor-match")]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType(typeof(TriageDoctorMatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> TriageDoctorMatch(
        [FromBody] TriageDoctorMatchRequest request, CancellationToken cancellationToken)
    {
        // Same pattern as AppointmentController: the JWT is the only source of patient identity.
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var patientId))
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        var symptoms = request.Symptoms?.Trim() ?? string.Empty;
        if (symptoms.Length < 3)
        {
            return BadRequest(new { message = "Please describe your symptoms or what you need help with." });
        }

        try
        {
            var result = await _triageService.TriageAndMatchAsync(patientId, symptoms, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Smart Care triage failed for patient {PatientId}.", patientId);
            var emergency = EmergencyRedFlags.Matches(symptoms);
            return StatusCode(StatusCodes.Status500InternalServerError, new TriageDoctorMatchResponse
            {
                Status = TriageStatus.AiUnavailable,
                PatientId = patientId,
                PossibleEmergency = emergency,
                EmergencyNotice = emergency ? EmergencyRedFlags.Notice : null,
                Message = "Smart Care couldn't complete your request. You can still browse doctors and book an appointment yourself.",
            });
        }
    }
}
