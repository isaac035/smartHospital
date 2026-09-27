using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Schedules;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly AppDbContext _context;
    private readonly IHubContext<Hubs.HospitalHub> _hub;

    public SchedulesController(IScheduleService scheduleService, AppDbContext context, IHubContext<Hubs.HospitalHub> hub)
    {
        _scheduleService = scheduleService;
        _context = context;
        _hub = hub;
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
            await BroadcastSlotUpdateAsync(result.DoctorId, result);

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

            await BroadcastSlotUpdateAsync(schedule.DoctorId, schedule);
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
        var existing = await _scheduleService.GetByIdAsync(id);
        var success = await _scheduleService.RemoveAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "Schedule not found."
            });
        }

        if (existing != null) await BroadcastSlotUpdateAsync(existing.DoctorId, existing);
        return Ok(new
        {
            message = "Schedule removed successfully."
        });
    }

    private async Task BroadcastSlotUpdateAsync(int doctorProfileId, object payload)
    {
        var userId = await _context.Doctors.Where(d => d.Id == doctorProfileId)
            .Select(d => d.UserId).FirstOrDefaultAsync();
        var groups = new List<string> { "staff", "admin" };
        if (userId.HasValue)
        {
            groups.Add($"doctor:{userId.Value}");
            groups.Add($"user:{userId.Value}");
        }
        await _hub.Clients.Groups(groups).SendAsync("SlotUpdated", payload);
    }
}
