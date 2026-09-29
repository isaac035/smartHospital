using System.Security.Claims;
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
    private readonly IEmrAuditService? _auditService;

    public PatientProfilesController(
        IPatientMedicalProfileService profileService,
        IEmrAuditService? auditService = null)
    {
        _profileService = profileService;
        _auditService = auditService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatientId(int patientId)
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
                await _auditService.LogAsync(currentUserId, "View", "PatientMedicalProfile", null, patientId, "Unauthorized access attempt by patient", isSuccess: false);
            }
            return Forbid();
        }

        try
        {
            var profile = await _profileService.GetByPatientIdAsync(patientId);
            if (profile == null)
            {
                return NotFound(new { message = "Medical profile not found for this patient." });
            }

            return Ok(profile);
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

    [HttpPut("patient/{patientId:int}")]
    [Authorize(Roles = "Doctor,Staff,Admin")]
    public async Task<IActionResult> Upsert(int patientId, [FromBody] UpsertPatientMedicalProfileRequest request)
    {
        if (patientId <= 0)
        {
            return BadRequest(new { message = "Invalid patient identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(currentUserIdStr, out var currentUserId);

        try
        {
            var result = await _profileService.UpsertAsync(patientId, request);
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "UpdateProfile", "PatientMedicalProfile", result.Id, patientId, $"BloodGroup: {result.BloodGroup}", isSuccess: true);
            }
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "UpdateProfile", "PatientMedicalProfile", null, patientId, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "UpdateProfile", "PatientMedicalProfile", null, patientId, ex.Message, isSuccess: false);
            }
            return NotFound(new { message = ex.Message });
        }
    }
}
