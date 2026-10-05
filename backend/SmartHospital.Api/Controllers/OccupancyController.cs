using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/occupancy")]
[Authorize]
public class OccupancyController : ControllerBase
{
    private readonly IOccupancyService _occupancyService;

    public OccupancyController(IOccupancyService occupancyService)
    {
        _occupancyService = occupancyService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOccupancyOverview()
    {
        var overview = await _occupancyService.GetOccupancyOverviewAsync();
        return Ok(overview);
    }

    [HttpGet("wards")]
    public async Task<IActionResult> GetWardOccupancies()
    {
        var wardOccupancies = await _occupancyService.GetWardOccupanciesAsync();
        return Ok(wardOccupancies);
    }

    [HttpGet("wards/{wardId:int}")]
    public async Task<IActionResult> GetWardOccupancyById(int wardId)
    {
        var occupancy = await _occupancyService.GetWardOccupancyByIdAsync(wardId);
        if (occupancy == null)
        {
            return NotFound(new { message = "Ward not found." });
        }

        return Ok(occupancy);
    }
}
