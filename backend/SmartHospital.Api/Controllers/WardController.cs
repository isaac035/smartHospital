using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Wards;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/wards")]
[Authorize]
public class WardController : ControllerBase
{
    private readonly IWardService _wardService;

    public WardController(IWardService wardService)
    {
        _wardService = wardService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateWard([FromBody] CreateWardRequest request)
    {
        try
        {
            var result = await _wardService.CreateWardAsync(request);
            return Created($"/api/wards/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllWards([FromQuery] bool? isActive = null)
    {
        var wards = await _wardService.GetAllWardsAsync(isActive);
        return Ok(wards);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetWardById(int id)
    {
        var ward = await _wardService.GetWardByIdAsync(id);
        if (ward == null)
        {
            return NotFound(new { message = "Ward not found." });
        }

        return Ok(ward);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateWard(int id, [FromBody] UpdateWardRequest request)
    {
        try
        {
            var ward = await _wardService.UpdateWardAsync(id, request);
            if (ward == null)
            {
                return NotFound(new { message = "Ward not found." });
            }

            return Ok(ward);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateWard(int id)
    {
        try
        {
            var success = await _wardService.DeactivateWardAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Ward not found." });
            }

            return Ok(new { message = "Ward deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/occupancy")]
    public async Task<IActionResult> GetWardOccupancy(int id)
    {
        var occupancy = await _wardService.GetWardOccupancyAsync(id);
        if (occupancy == null)
        {
            return NotFound(new { message = "Ward not found." });
        }

        return Ok(occupancy);
    }
}
