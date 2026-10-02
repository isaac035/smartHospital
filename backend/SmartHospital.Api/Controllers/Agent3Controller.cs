using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Agent3;
using SmartHospital.Api.Services.Agent3;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/agent3")]
[Authorize(Roles = "Patient")]
public class Agent3Controller : ControllerBase
{
    private readonly IAgent3ResourceAllocationService _service;
    private readonly ILogger<Agent3Controller> _logger;
    public Agent3Controller(IAgent3ResourceAllocationService service, ILogger<Agent3Controller> logger) { _service = service; _logger = logger; }

    [HttpPost("recommendations")]
    public async Task<IActionResult> Recommend([FromBody] RecommendResourcesRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var patientId)) return Unauthorized();
        try { return Ok(await _service.RecommendAsync(patientId, request.AppointmentId, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogError(ex, "Agent 3 recommendation failed for appointment {AppointmentId}", request.AppointmentId); return StatusCode(503, new { message = "Resource recommendations are unavailable. Your appointment remains confirmed." }); }
    }

    [HttpPost("allocations")]
    public async Task<IActionResult> Allocate([FromBody] SelectResourceRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var patientId)) return Unauthorized();
        try { return Ok(await _service.AllocateAsync(patientId, request.AppointmentId, request.SelectedResourceId, request.Kind, request.AdmissionDate!.Value, ct)); }
        catch (ResourceUnavailableException ex)
        {
            _logger.LogInformation("Agent 3 resource {ResourceId} ({Kind}) became unavailable for appointment {AppointmentId}.", request.SelectedResourceId, request.Kind, request.AppointmentId);
            return Conflict(new { code = "RESOURCE_UNAVAILABLE", message = ex.Message, refreshRecommendations = true });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState is "40001" or "23505")
        {
            _logger.LogInformation(ex, "Agent 3 database allocation conflict for appointment {AppointmentId}, resource {ResourceId} ({Kind}).", request.AppointmentId, request.SelectedResourceId, request.Kind);
            return Conflict(new { code = "RESOURCE_UNAVAILABLE", message = "The selected resource is no longer available. Please select another available resource.", refreshRecommendations = true });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pg && pg.SqlState is "40001" or "23505")
        {
            _logger.LogInformation(ex, "Agent 3 database allocation conflict for appointment {AppointmentId}, resource {ResourceId} ({Kind}).", request.AppointmentId, request.SelectedResourceId, request.Kind);
            return Conflict(new { code = "RESOURCE_UNAVAILABLE", message = "The selected resource is no longer available. Please select another available resource.", refreshRecommendations = true });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { code = "NOT_FOUND", message = ex.Message, refreshRecommendations = false }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { code = "INVALID_REQUEST", message = ex.Message, refreshRecommendations = false }); }
        catch (InvalidOperationException ex)
        {
            var code = GetConflictCode(ex.Message);
            _logger.LogWarning("Agent 3 allocation rejected with {ErrorCode} for appointment {AppointmentId}, resource {ResourceId} ({Kind}): {Reason}", code, request.AppointmentId, request.SelectedResourceId, request.Kind, ex.Message);
            return Conflict(new { code, message = ex.Message, refreshRecommendations = code is "RESOURCE_UNAVAILABLE" or "RESOURCE_INACTIVE" or "WARD_CAPACITY_REACHED" });
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogError(ex, "Resource allocation failed for appointment {AppointmentId}", request.AppointmentId); return StatusCode(503, new { code = "ALLOCATION_UNAVAILABLE", message = "Resource reservation could not be completed right now. Your appointment remains confirmed.", refreshRecommendations = false }); }
    }

    private static string GetConflictCode(string message)
    {
        var normalized = message.ToLowerInvariant();
        if (normalized.Contains("not available") || normalized.Contains("already has an active allocation")) return "RESOURCE_UNAVAILABLE";
        if (normalized.Contains("not active")) return "RESOURCE_INACTIVE";
        if (normalized.Contains("capacity limit")) return "WARD_CAPACITY_REACHED";
        if (normalized.Contains("active bed allocation")) return "PATIENT_ALREADY_HAS_BED";
        if (normalized.Contains("active inpatient admission")) return "ACTIVE_ADMISSION_EXISTS";
        return "ALLOCATION_REJECTED";
    }
}
