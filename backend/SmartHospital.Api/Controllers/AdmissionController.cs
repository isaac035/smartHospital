using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Admissions;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/admissions")]
[Authorize]
public class AdmissionController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public AdmissionController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> CreateAdmission([FromBody] CreateAdmissionRequest request)
    {
        try
        {
            var result = await _admissionService.CreateAdmissionAsync(request);
            return Created($"/api/admissions/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> GetAdmissions([FromQuery] AdmissionQueryFilter filter)
    {
        var admissions = await _admissionService.GetAdmissionsAsync(filter);
        return Ok(admissions);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAdmissionById(int id)
    {
        var admission = await _admissionService.GetAdmissionByIdAsync(id);
        if (admission == null)
        {
            return NotFound(new { message = "Admission not found." });
        }

        return Ok(admission);
    }

    [HttpGet("my/active")]
    public async Task<IActionResult> GetMyActiveAdmission()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        var admission = await _admissionService.GetActiveAdmissionByPatientIdAsync(currentUserId.Value);
        if (admission == null)
        {
            return NotFound(new { message = "No active admission found." });
        }

        return Ok(admission);
    }

    [HttpGet("my/history")]
    public async Task<IActionResult> GetMyAdmissionHistory()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        var history = await _admissionService.GetPatientAdmissionHistoryAsync(currentUserId.Value);
        return Ok(history);
    }

    [HttpGet("patient/{patientId:int}/active")]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> GetPatientActiveAdmission(int patientId)
    {
        var admission = await _admissionService.GetActiveAdmissionByPatientIdAsync(patientId);
        if (admission == null)
        {
            return NotFound(new { message = "No active admission found for this patient." });
        }

        return Ok(admission);
    }

    [HttpGet("patient/{patientId:int}/history")]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> GetPatientAdmissionHistory(int patientId)
    {
        var history = await _admissionService.GetPatientAdmissionHistoryAsync(patientId);
        return Ok(history);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> UpdateAdmission(int id, [FromBody] UpdateAdmissionRequest request)
    {
        try
        {
            var admission = await _admissionService.UpdateAdmissionAsync(id, request);
            if (admission == null)
            {
                return NotFound(new { message = "Admission not found." });
            }

            return Ok(admission);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/allocate-bed")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> AllocateBed(int id, [FromBody] AllocateBedRequest request)
    {
        try
        {
            request.AdmissionId = id;
            var result = await _admissionService.AllocateBedAsync(request, GetCurrentUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/transfer")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> TransferPatient(int id, [FromBody] TransferPatientRequest request)
    {
        try
        {
            var result = await _admissionService.TransferPatientAsync(id, request, GetCurrentUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/discharge")]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> DischargePatient(int id, [FromBody] DischargePatientRequest request)
    {
        try
        {
            var result = await _admissionService.DischargePatientAsync(id, request, GetCurrentUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
