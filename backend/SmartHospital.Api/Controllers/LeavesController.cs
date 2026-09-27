using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Leaves;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/leaves")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly IDoctorService _doctorService;

    public LeavesController(
        ILeaveService leaveService,
        IDoctorService doctorService)
    {
        _leaveService = leaveService;
        _doctorService = doctorService;
    }

    private async Task<int?> GetMyDoctorIdAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var doctor = await _doctorService.GetByUserIdAsync(userId);

        return doctor?.Id;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LeaveFilterRequest filter)
    {
        var leaves = await _leaveService.GetAllAsync(filter);

        return Ok(leaves);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var leave = await _leaveService.GetByIdAsync(id);

        if (leave == null)
        {
            return NotFound(new
            {
                message = "Leave record not found."
            });
        }

        return Ok(leave);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> Create(CreateLeaveRequest request)
    {
        var isDoctor = User.IsInRole("Doctor");

        if (isDoctor)
        {
            var myDoctorId = await GetMyDoctorIdAsync();

            if (myDoctorId == null || myDoctorId != request.DoctorId)
            {
                return Forbid();
            }
        }

        // A doctor requesting their own leave needs Admin/Staff approval first;
        // Admin/Staff recording leave directly is treated as an already-decided record.
        var initialStatus = isDoctor
            ? LeaveStatus.Pending
            : LeaveStatus.Approved;

        try
        {
            var result = await _leaveService.CreateAsync(request, initialStatus);

            return Created($"/api/leaves/{result.Id}", result);
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
    public async Task<IActionResult> Update(int id, UpdateLeaveRequest request)
    {
        try
        {
            var leave = await _leaveService.UpdateAsync(id, request);

            if (leave == null)
            {
                return NotFound(new
                {
                    message = "Leave record not found."
                });
            }

            return Ok(leave);
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
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (User.IsInRole("Doctor"))
        {
            var leave = await _leaveService.GetByIdAsync(id);

            if (leave == null)
            {
                return NotFound(new
                {
                    message = "Leave record not found."
                });
            }

            var myDoctorId = await GetMyDoctorIdAsync();

            if (myDoctorId == null || myDoctorId != leave.DoctorId)
            {
                return Forbid();
            }
        }

        var success = await _leaveService.CancelAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "Leave record not found."
            });
        }

        return Ok(new
        {
            message = "Leave cancelled successfully."
        });
    }
}
