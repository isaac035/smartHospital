using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IEmrAuditService? _auditService;

    public PrescriptionsController(
        IPrescriptionService prescriptionService,
        IEmrAuditService? auditService = null)
    {
        _prescriptionService = prescriptionService;
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
        if (role == "Patient" && int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId != patientId)
        {
            return Forbid();
        }

        try
        {
            var prescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);
            return Ok(prescriptions);
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid prescription identifier." });
        }

        var prescription = await _prescriptionService.GetByIdAsync(id);
        if (prescription == null)
        {
            return NotFound(new { message = "Prescription not found." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(currentUserIdStr, out var currentUserId);

        if (role == "Patient" && prescription.PatientId != currentUserId)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "View", "Prescription", id, prescription.PatientId, "Unauthorized access attempt by patient", isSuccess: false);
            }
            return Forbid();
        }

        if (_auditService != null && currentUserId > 0)
        {
            await _auditService.LogAsync(currentUserId, "View", "Prescription", prescription.Id, prescription.PatientId, $"PrescriptionNumber: {prescription.PrescriptionNumber}", isSuccess: true);
        }

        return Ok(prescription);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var created = await _prescriptionService.CreateAsync(doctorId, request);
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Create", "Prescription", created.Id, created.PatientId, $"PrescriptionNumber: {created.PrescriptionNumber}", isSuccess: true);
            }
            return Created($"/api/prescriptions/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Create", "Prescription", null, request.PatientId, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Create", "Prescription", null, request.PatientId, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
    }
}
