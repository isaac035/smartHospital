using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.MedicalResources;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/medical-resources")]
[Authorize]
public class MedicalResourceController : ControllerBase
{
    private readonly IMedicalResourceService _medicalResourceService;

    public MedicalResourceController(IMedicalResourceService medicalResourceService)
    {
        _medicalResourceService = medicalResourceService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateResource([FromBody] CreateResourceRequest request)
    {
        try
        {
            var result = await _medicalResourceService.CreateResourceAsync(request);
            return Created($"/api/medical-resources/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetResources([FromQuery] ResourceQueryFilter filter)
    {
        var resources = await _medicalResourceService.GetResourcesAsync(filter);
        return Ok(resources);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetResourceById(int id)
    {
        var resource = await _medicalResourceService.GetResourceByIdAsync(id);
        if (resource == null)
        {
            return NotFound(new { message = "Medical resource not found." });
        }

        return Ok(resource);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateResource(int id, [FromBody] UpdateResourceRequest request)
    {
        try
        {
            var resource = await _medicalResourceService.UpdateResourceAsync(id, request);
            if (resource == null)
            {
                return NotFound(new { message = "Medical resource not found." });
            }

            return Ok(resource);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> AssignResource(int id, [FromBody] AssignResourceRequest request)
    {
        try
        {
            var resource = await _medicalResourceService.AssignResourceAsync(id, request);
            if (resource == null)
            {
                return NotFound(new { message = "Medical resource not found." });
            }

            return Ok(resource);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateResource(int id)
    {
        try
        {
            var success = await _medicalResourceService.DeactivateResourceAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Medical resource not found." });
            }

            return Ok(new { message = "Medical resource deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
