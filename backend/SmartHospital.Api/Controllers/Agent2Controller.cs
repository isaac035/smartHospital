using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartHospital.Api.DTOs.Agent2;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/agent2")]
[Authorize(Roles = "Patient")]
public class Agent2Controller : ControllerBase
{
    public const string RateLimitPolicy = "agent2-per-patient";
    private readonly IAgent2OptimizationService _optimizationService;
    private readonly ILogger<Agent2Controller> _logger;

    public Agent2Controller(IAgent2OptimizationService optimizationService, ILogger<Agent2Controller> logger)
    {
        _optimizationService = optimizationService;
        _logger = logger;
    }

    [HttpPost("optimize-appointment")]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType(typeof(OptimizeAppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> OptimizeAppointment(
        [FromBody] OptimizeAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var patientId))
            return Unauthorized(new { message = "User is not authenticated." });

        try
        {
            return Ok(await _optimizationService.OptimizeAsync(patientId, request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Appointment optimization failed for patient {PatientId}.", patientId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new OptimizeAppointmentResponse
            {
                PatientId = patientId,
                Message = "Appointment suggestions are unavailable right now. You can browse doctors and slots manually.",
            });
        }
    }
}
