using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Rooms;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        try
        {
            var result = await _roomService.CreateRoomAsync(request);
            return Created($"/api/rooms/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-ward/{wardId:int}")]
    public async Task<IActionResult> GetRoomsByWard(int wardId, [FromQuery] bool? isActive = null)
    {
        var rooms = await _roomService.GetRoomsByWardAsync(wardId, isActive);
        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRoomById(int id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room == null)
        {
            return NotFound(new { message = "Room not found." });
        }

        return Ok(room);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
        try
        {
            var room = await _roomService.UpdateRoomAsync(id, request);
            if (room == null)
            {
                return NotFound(new { message = "Room not found." });
            }

            return Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateRoom(int id)
    {
        try
        {
            var success = await _roomService.DeactivateRoomAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Room not found." });
            }

            return Ok(new { message = "Room deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
