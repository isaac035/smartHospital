using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabOrdersController : ControllerBase
{
    private readonly ILabOrderService _labOrderService;

    public LabOrdersController(ILabOrderService labOrderService)
    {
        _labOrderService = labOrderService;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        if (patientId <= 0)
        {
            return BadRequest(new { message = "Invalid patient identifier." });
        }

        try
        {
            var orders = await _labOrderService.GetByPatientIdAsync(patientId);
            return Ok(orders);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid lab order identifier." });
        }

        var order = await _labOrderService.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound(new { message = "Lab order not found." });
        }

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateLabOrderRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var created = await _labOrderService.CreateOrderAsync(doctorId, request);
            return Created($"/api/laborders/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Staff,Doctor,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLabOrderStatusRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid lab order identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _labOrderService.UpdateStatusAsync(id, request);
            if (updated == null)
            {
                return NotFound(new { message = "Lab order not found." });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/report")]
    [Authorize(Roles = "Staff,Doctor,Admin")]
    public async Task<IActionResult> RecordReport(int id, [FromBody] RecordLabReportRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid lab order identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
        {
            return Unauthorized(new { message = "Invalid user identification." });
        }

        try
        {
            var report = await _labOrderService.RecordReportAsync(id, userId, request);
            if (report == null)
            {
                return NotFound(new { message = "Lab order not found." });
            }

            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
