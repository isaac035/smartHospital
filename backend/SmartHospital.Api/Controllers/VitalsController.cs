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

    public VitalsController(IVitalSignService vitalSignService)
    {
        _vitalSignService = vitalSignService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var vitals = await _vitalSignService.GetByPatientIdAsync(patientId);
        return Ok(vitals);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Staff,Admin")]
    public async Task<IActionResult> Record(RecordVitalSignRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid user identification." });
        }

        var created = await _vitalSignService.RecordAsync(userId, request);
        return Created($"/api/vitals/{created.Id}", created);
    }
}
