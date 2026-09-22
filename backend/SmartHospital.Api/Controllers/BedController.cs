using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Beds;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/beds")]
[Authorize]
public class BedController : ControllerBase
{
    private readonly IBedService _bedService;

    public BedController(IBedService bedService)
    {
        _bedService = bedService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateBed([FromBody] CreateBedRequest request)
    {
        try
        {
            var result = await _bedService.CreateBedAsync(request);
            return Created($"/api/beds/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBeds([FromQuery] BedQueryFilter filter)
    {
        var beds = await _bedService.GetBedsAsync(filter);
        return Ok(beds);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableBeds(
        [FromQuery] int? wardId = null,
        [FromQuery] int? roomId = null,
        [FromQuery] BedType? type = null)
    {
        var beds = await _bedService.GetAvailableBedsAsync(wardId, roomId, type);
        return Ok(beds);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBedById(int id)
    {
        var bed = await _bedService.GetBedByIdAsync(id);
        if (bed == null)
        {
            return NotFound(new { message = "Bed not found." });
        }

        return Ok(bed);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateBed(int id, [FromBody] UpdateBedRequest request)
    {
        try
        {
            var bed = await _bedService.UpdateBedAsync(id, request);
            if (bed == null)
            {
                return NotFound(new { message = "Bed not found." });
            }

            return Ok(bed);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateBedStatus(int id, [FromBody] UpdateBedStatusRequest request)
    {
        try
        {
            var bed = await _bedService.UpdateBedStatusAsync(id, request);
            if (bed == null)
            {
                return NotFound(new { message = "Bed not found." });
            }

            return Ok(bed);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateBed(int id)
    {
        try
        {
            var success = await _bedService.DeactivateBedAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Bed not found." });
            }

            return Ok(new { message = "Bed deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
