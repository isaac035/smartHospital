using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor,Staff")]
public class AuditLogsController : ControllerBase
{
    private readonly IEmrAuditService _auditService;

    public AuditLogsController(IEmrAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? patientId,
        [FromQuery] string? entityType,
        [FromQuery] int? userId,
        [FromQuery] int limit = 100)
    {
        var role = User?.FindFirstValue(ClaimTypes.Role);
        if (role == "Patient")
        {
            return Forbid();
        }

        if (limit <= 0)
        {
            limit = 100;
        }

        var logs = await _auditService.GetAuditLogsAsync(patientId, entityType, userId, limit);
        return Ok(logs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = User?.FindFirstValue(ClaimTypes.Role);
        if (role == "Patient")
        {
            return Forbid();
        }

        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid audit log identifier." });
        }

        var log = await _auditService.GetByIdAsync(id);
        if (log == null)
        {
            return NotFound(new { message = "Audit log entry not found." });
        }

        return Ok(log);
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId, [FromQuery] int limit = 100)
    {
        var role = User?.FindFirstValue(ClaimTypes.Role);
        if (role == "Patient")
        {
            return Forbid();
        }

        if (patientId <= 0)
        {
            return BadRequest(new { message = "Invalid patient identifier." });
        }

        var logs = await _auditService.GetByPatientIdAsync(patientId, limit);
        return Ok(logs);
    }
}
