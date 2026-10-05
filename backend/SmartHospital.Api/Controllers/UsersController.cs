using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Users;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("appointment-manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAppointmentManager(CreateAppointmentManagerRequest request)
    {
        try
        {
            var user = await _userService.CreateAppointmentManagerAsync(request);
            return Created($"/api/users/{user.Id}", user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("doctor-manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDoctorManager(CreateDoctorManagerRequest request)
    {
        try
        {
            var user = await _userService.CreateDoctorManagerAsync(request);
            return Created($"/api/users/{user.Id}", user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff,ResourceAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] UserRole? role = null)
    {
        if (User.IsInRole("ResourceAdmin"))
        {
            role = UserRole.Patient;
        }

        var users = await _userService.GetAllAsync(role);
        return Ok(users);
    }

    [HttpGet("patients/search")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> SearchPatients([FromQuery] string query, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query)) return Ok(Array.Empty<PatientSearchResult>());
        var patients = await _userService.SearchPatientsAsync(query, limit);
        return Ok(patients);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(user);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateUserRequest request)
    {
        var user = await _userService.UpdateAsync(id, request);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var success = await _userService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(new
        {
            message = "User deactivated successfully."
        });
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id)
    {
        var success = await _userService.ActivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(new
        {
            message = "User activated successfully."
        });
    }
}
