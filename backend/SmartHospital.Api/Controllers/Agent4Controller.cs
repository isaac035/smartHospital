using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Agent4;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/agent4/reports")]
[Authorize(Roles = "Patient,Admin,Staff,Doctor")]
public class Agent4Controller : ControllerBase
{
    private readonly IAgent4MedicalReportService _service;
    private readonly ILogger<Agent4Controller> _logger;
    public Agent4Controller(IAgent4MedicalReportService service, ILogger<Agent4Controller> logger) { _service = service; _logger = logger; }

    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] GenerateMedicalReportRequest request, CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId)) return Unauthorized();
        var patientId = role == "Patient" ? callerId : request.PatientId;
        if (!patientId.HasValue || patientId <= 0) return BadRequest(new { message = "PatientId is required for staff report generation." });
        try { return Ok(await _service.GenerateAsync(patientId.Value, request.AppointmentId, request.GenerationId, request.CheckupRequested, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Agent 4 report generation failed for patient {PatientId} and appointment {AppointmentId}.", patientId.Value, request.AppointmentId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { code = "REPORT_GENERATION_FAILED", message = "The report could not be generated right now. Please retry; any report already saved will be available in your report list." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPatientReports([FromQuery] int? patientId, CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId)) return Unauthorized();
        var requestedPatientId = role == "Patient" ? callerId : patientId;
        if (!requestedPatientId.HasValue || requestedPatientId <= 0) return BadRequest(new { message = "A valid patientId is required." });
        return Ok(await _service.GetPatientReportsAsync(requestedPatientId.Value, ct));
    }

    [HttpGet("{reportId:guid}")]
    public async Task<IActionResult> GetById(Guid reportId, CancellationToken ct)
    {
        var report = await _service.GetByReportIdAsync(reportId, ct);
        if (report == null) return NotFound(new { message = "Medical report was not found." });
        if (User.IsInRole("Patient"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var patientId)) return Unauthorized();
            if (report.PatientId != patientId) return Forbid();
        }
        return Ok(report);
    }
}
