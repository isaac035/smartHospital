using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Maintenance;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/resource-maintenances")]
[Authorize(Roles = "Admin,Staff")]
public class ResourceMaintenanceController : ControllerBase
{
    private readonly IResourceMaintenanceService _maintenanceService;

    public ResourceMaintenanceController(IResourceMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    [HttpPost]
    public async Task<IActionResult> ScheduleMaintenance([FromBody] CreateMaintenanceRequest request)
    {
        try
        {
            var result = await _maintenanceService.ScheduleMaintenanceAsync(request, GetCurrentUserId());
            return Created($"/api/resource-maintenances/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMaintenanceRecords(
        [FromQuery] int? bedId = null,
        [FromQuery] int? resourceId = null,
        [FromQuery] MaintenanceStatus? status = null)
    {
        var records = await _maintenanceService.GetMaintenanceRecordsAsync(bedId, resourceId, status);
        return Ok(records);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMaintenanceById(int id)
    {
        var record = await _maintenanceService.GetMaintenanceByIdAsync(id);
        if (record == null)
        {
            return NotFound(new { message = "Maintenance record not found." });
        }

        return Ok(record);
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> StartMaintenance(int id)
    {
        try
        {
            var record = await _maintenanceService.StartMaintenanceAsync(id, GetCurrentUserId());
            if (record == null)
            {
                return NotFound(new { message = "Maintenance record not found." });
            }

            return Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> CompleteMaintenance(int id, [FromBody] CompleteMaintenanceRequest request)
    {
        try
        {
            var record = await _maintenanceService.CompleteMaintenanceAsync(id, request, GetCurrentUserId());
            if (record == null)
            {
                return NotFound(new { message = "Maintenance record not found." });
            }

            return Ok(record);
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
