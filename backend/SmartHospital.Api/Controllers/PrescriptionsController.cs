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
        var prescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);
        return Ok(prescriptions);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
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
        var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId))
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        var created = await _prescriptionService.CreateAsync(doctorId, request);
        return Created($"/api/prescriptions/{created.Id}", created);
    }
}
