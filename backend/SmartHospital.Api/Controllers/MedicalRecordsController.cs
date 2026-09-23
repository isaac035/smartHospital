using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordService _medicalRecordService;

    public MedicalRecordsController(IMedicalRecordService medicalRecordService)
    {
        _medicalRecordService = medicalRecordService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var records = await _medicalRecordService.GetByPatientIdAsync(patientId);
        return Ok(records);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound(new { message = "Medical record not found." });
        }

        return Ok(record);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Create(CreateMedicalRecordRequest request)
    {
        var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId))
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        var created = await _medicalRecordService.CreateAsync(doctorId, request);
        return Created($"/api/medicalrecords/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Update(int id, UpdateMedicalRecordRequest request)
    {
        var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId))
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        var updated = await _medicalRecordService.UpdateAsync(id, doctorId, request);
        if (updated == null)
        {
            return NotFound(new { message = "Medical record not found or not editable by this doctor." });
        }

        return Ok(updated);
    }
}
