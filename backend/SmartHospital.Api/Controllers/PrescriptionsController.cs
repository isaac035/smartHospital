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

    public PrescriptionsController(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        if (patientId <= 0)
        {
            return BadRequest(new { message = "Invalid patient identifier." });
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

        return Ok(prescription);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Create(CreatePrescriptionRequest request)
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
            return Created($"/api/prescriptions/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
