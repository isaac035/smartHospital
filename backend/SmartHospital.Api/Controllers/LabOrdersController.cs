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
        var orders = await _labOrderService.GetByPatientIdAsync(patientId);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _labOrderService.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound(new { message = "Lab order not found." });
        }

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateOrder(CreateLabOrderRequest request)
    {
        var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId))
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        var created = await _labOrderService.CreateOrderAsync(doctorId, request);
        return Created($"/api/laborders/{created.Id}", created);
    }

    [HttpPost("{id:int}/report")]
    [Authorize(Roles = "Staff,Doctor,Admin")]
    public async Task<IActionResult> RecordReport(int id, RecordLabReportRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid user identification." });
        }

        var report = await _labOrderService.RecordReportAsync(id, userId, request);
        if (report == null)
        {
            return NotFound(new { message = "Lab order not found." });
        }

        return Ok(report);
    }
}
