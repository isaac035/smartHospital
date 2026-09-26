using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Schedules;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;

    public SchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ScheduleFilterRequest filter)
    {
        var schedules = await _scheduleService.GetAllAsync(filter);

        return Ok(schedules);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _scheduleService.GetByIdAsync(id);

        if (schedule == null)
        {
            return NotFound(new
            {
                message = "Schedule not found."
            });
        }

        return Ok(schedule);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(CreateScheduleRequest request)
    {
        try
        {
            var result = await _scheduleService.CreateAsync(request);

            return Created($"/api/schedules/{result.Id}", result);
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
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(int id, UpdateScheduleRequest request)
    {
        try
        {
            var schedule = await _scheduleService.UpdateAsync(id, request);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message = "Schedule not found."
                });
            }

            return Ok(schedule);
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
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Remove(int id)
    {
        var success = await _scheduleService.RemoveAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "Schedule not found."
            });
        }

        return Ok(new
        {
            message = "Schedule removed successfully."
        });
    }
}
