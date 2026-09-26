using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Doctors;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DoctorFilterRequest filter)
    {
        var doctors = await _doctorService.GetAllAsync(filter);

        return Ok(doctors);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var doctor = await _doctorService.GetByUserIdAsync(userId);

        if (doctor == null)
        {
            return NotFound(new
            {
                message = "No doctor profile is linked to your account."
            });
        }

        return Ok(doctor);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> UpdateMyProfile(UpdateMyDoctorProfileRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var doctor = await _doctorService.UpdateMyProfileAsync(userId, request);

        if (doctor == null)
        {
            return NotFound(new
            {
                message = "No doctor profile is linked to your account."
            });
        }

        return Ok(doctor);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] AvailableDoctorFilterRequest filter)
    {
        var doctors = await _doctorService.GetAvailableAsync(filter);

        return Ok(doctors);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var doctor = await _doctorService.GetByIdAsync(id);

        if (doctor == null)
        {
            return NotFound(new
            {
                message = "Doctor not found."
            });
        }

        return Ok(doctor);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(CreateDoctorRequest request)
    {
        try
        {
            var result = await _doctorService.CreateAsync(request);

            return Created($"/api/doctors/{result.Id}", result);
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
    public async Task<IActionResult> Update(int id, UpdateDoctorRequest request)
    {
        try
        {
            var doctor = await _doctorService.UpdateAsync(id, request);

            if (doctor == null)
            {
                return NotFound(new
                {
                    message = "Doctor not found."
                });
            }

            return Ok(doctor);
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
        var success = await _doctorService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "Doctor not found."
            });
        }

        return Ok(new
        {
            message = "Doctor deactivated successfully."
        });
    }
}
