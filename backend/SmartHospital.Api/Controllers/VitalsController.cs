using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VitalsController : ControllerBase
{
    private readonly IVitalSignService _vitalSignService;
    private readonly IEmrAuditService? _auditService;

    public VitalsController(
        IVitalSignService vitalSignService,
        IEmrAuditService? auditService = null)
    {
        _vitalSignService = vitalSignService;
        _auditService = auditService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        if (patientId <= 0)
        {
            return BadRequest(new { message = "Invalid patient identifier." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(currentUserIdStr, out var currentUserId);

        if (role == "Patient" && currentUserId != patientId)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "ViewList", "VitalSign", null, patientId, "Unauthorized access attempt by patient", isSuccess: false);
            }
            return Forbid();
        }

        try
        {
            var vitals = await _vitalSignService.GetByPatientIdAsync(patientId);
            return Ok(vitals);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Staff,Admin")]
    public async Task<IActionResult> Record([FromBody] RecordVitalSignRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
        {
            return Unauthorized(new { message = "Invalid user identification." });
        }

        try
        {
            var created = await _vitalSignService.RecordAsync(userId, request);
            if (_auditService != null)
            {
                await _auditService.LogAsync(userId, "RecordVitals", "VitalSign", created.Id, created.PatientId, $"Temp: {created.TemperatureCelsius}, BP: {created.SystolicBloodPressure}/{created.DiastolicBloodPressure}", isSuccess: true);
            }
            return Created($"/api/vitals/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(userId, "RecordVitals", "VitalSign", null, request.PatientId, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(userId, "RecordVitals", "VitalSign", null, request.PatientId, ex.Message, isSuccess: false);
            }
            return NotFound(new { message = ex.Message });
        }
    }
}
