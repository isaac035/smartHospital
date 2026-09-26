using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalHistoryController : ControllerBase
{
    private readonly IMedicalHistoryService _medicalHistoryService;

    public MedicalHistoryController(IMedicalHistoryService medicalHistoryService)
    {
        _medicalHistoryService = medicalHistoryService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetPatientTimeline(int patientId)
    {
        if (patientId <= 0)
        {
            return BadRequest(new { message = "Invalid patient identifier." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (role == "Patient" && int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId != patientId)
        {
            return Forbid();
        }

        try
        {
            var timeline = await _medicalHistoryService.GetPatientTimelineAsync(patientId);
            return Ok(timeline);
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

    [HttpGet("{patientId:int}")]
    public Task<IActionResult> GetPatientTimelineAlias(int patientId)
    {
        return GetPatientTimeline(patientId);
    }
}
