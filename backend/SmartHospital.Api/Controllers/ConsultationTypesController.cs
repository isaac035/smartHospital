using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.ConsultationTypes;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/consultation-types")]
[Authorize]
public class ConsultationTypesController : ControllerBase
{
    private readonly IConsultationTypeService _consultationTypeService;

    public ConsultationTypesController(IConsultationTypeService consultationTypeService)
    {
        _consultationTypeService = consultationTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var consultationTypes = await _consultationTypeService.GetAllAsync();

        return Ok(consultationTypes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consultationType = await _consultationTypeService.GetByIdAsync(id);

        if (consultationType == null)
        {
            return NotFound(new
            {
                message = "Consultation type not found."
            });
        }

        return Ok(consultationType);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateConsultationTypeRequest request)
    {
        try
        {
            var result = await _consultationTypeService.CreateAsync(request);

            return Created($"/api/consultation-types/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UpdateConsultationTypeRequest request)
    {
        try
        {
            var consultationType = await _consultationTypeService.UpdateAsync(id, request);

            if (consultationType == null)
            {
                return NotFound(new
                {
                    message = "Consultation type not found."
                });
            }

            return Ok(consultationType);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var success = await _consultationTypeService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "Consultation type not found."
            });
        }

        return Ok(new
        {
            message = "Consultation type deactivated successfully."
        });
    }
}
