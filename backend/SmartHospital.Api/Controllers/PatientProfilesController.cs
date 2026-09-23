using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientProfilesController : ControllerBase
{
    private readonly IPatientMedicalProfileService _profileService;

    public PatientProfilesController(IPatientMedicalProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatientId(int patientId)
    {
        var profile = await _profileService.GetByPatientIdAsync(patientId);
        if (profile == null)
        {
            return NotFound(new { message = "Medical profile not found for this patient." });
        }

        return Ok(profile);
    }

    [HttpPut("patient/{patientId:int}")]
    [Authorize(Roles = "Doctor,Staff,Admin")]
    public async Task<IActionResult> Upsert(int patientId, UpsertPatientMedicalProfileRequest request)
    {
        var result = await _profileService.UpsertAsync(patientId, request);
        return Ok(result);
    }
}
