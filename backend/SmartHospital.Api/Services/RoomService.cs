using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Rooms;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _context;

    public RoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request)
    {
        var ward = await _context.Wards.FindAsync(request.WardId);
        if (ward == null || !ward.IsActive)
        {
            throw new InvalidOperationException("Target ward does not exist or is inactive.");
        }

        var roomNumber = request.RoomNumber.Trim();
        var roomExists = await _context.Rooms.AnyAsync(r => r.WardId == request.WardId && r.RoomNumber == roomNumber);
        if (roomExists)
        {
            throw new InvalidOperationException($"Room number '{roomNumber}' already exists in this ward.");
        }

        var room = new Room
        {
            WardId = request.WardId,
            RoomNumber = roomNumber,
            Type = request.Type,
            Capacity = request.Capacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return new RoomResponse
        {
            Id = room.Id,
            WardId = room.WardId,
            WardName = ward.Name,
            WardCode = ward.Code,
            RoomNumber = room.RoomNumber,
            Type = room.Type.ToString(),
            Capacity = room.Capacity,
            IsActive = room.IsActive,
            TotalBeds = 0,
            OccupiedBeds = 0,
            AvailableBeds = 0,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }

    public async Task<List<RoomResponse>> GetRoomsByWardAsync(int wardId, bool? isActive = null)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(r => r.Ward)
            .Include(r => r.Beds)
            .Where(r => r.WardId == wardId);

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rooms = await query.OrderBy(r => r.RoomNumber).ToListAsync();

        return rooms.Select(r => new RoomResponse
        {
            Id = r.Id,
            WardId = r.WardId,
            WardName = r.Ward?.Name ?? string.Empty,
            WardCode = r.Ward?.Code ?? string.Empty,
            RoomNumber = r.RoomNumber,
            Type = r.Type.ToString(),
            Capacity = r.Capacity,
            IsActive = r.IsActive,
            TotalBeds = r.Beds.Count(b => b.IsActive),
            OccupiedBeds = r.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),
            AvailableBeds = r.Beds.Count(b => b.IsActive && b.Status == BedStatus.Available),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }

    public async Task<RoomResponse?> GetRoomByIdAsync(int id)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.Ward)
            .Include(r => r.Beds)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            return null;
        }

        return new RoomResponse
        {
            Id = room.Id,
            WardId = room.WardId,
            WardName = room.Ward?.Name ?? string.Empty,
            WardCode = room.Ward?.Code ?? string.Empty,
            RoomNumber = room.RoomNumber,
            Type = room.Type.ToString(),
            Capacity = room.Capacity,
            IsActive = room.IsActive,
            TotalBeds = room.Beds.Count(b => b.IsActive),
            OccupiedBeds = room.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),
            AvailableBeds = room.Beds.Count(b => b.IsActive && b.Status == BedStatus.Available),
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }

    public async Task<RoomResponse?> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        var room = await _context.Rooms
            .Include(r => r.Ward)
            .Include(r => r.Beds)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            return null;
        }

        var activeBeds = room.Beds.Count(b => b.IsActive);
        if (request.Capacity < activeBeds)
        {
            throw new InvalidOperationException($"Room capacity cannot be less than current active beds ({activeBeds}).");
        }

        var roomNumber = request.RoomNumber.Trim();
        var duplicateExists = await _context.Rooms.AnyAsync(r => r.WardId == room.WardId && r.RoomNumber == roomNumber && r.Id != id);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"Room number '{roomNumber}' already exists in this ward.");
        }

        if (!request.IsActive && room.Beds.Any(b => b.Status == BedStatus.Occupied))
        {
            throw new InvalidOperationException("Cannot deactivate a room containing occupied beds. Please transfer or discharge patients first.");
        }

        room.RoomNumber = roomNumber;
        room.Type = request.Type;
        room.Capacity = request.Capacity;
        room.IsActive = request.IsActive;
        room.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new RoomResponse
        {
            Id = room.Id,
            WardId = room.WardId,
            WardName = room.Ward?.Name ?? string.Empty,
            WardCode = room.Ward?.Code ?? string.Empty,
            RoomNumber = room.RoomNumber,
            Type = room.Type.ToString(),
            Capacity = room.Capacity,
            IsActive = room.IsActive,
            TotalBeds = activeBeds,
            OccupiedBeds = room.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),
            AvailableBeds = room.Beds.Count(b => b.IsActive && b.Status == BedStatus.Available),
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }

    public async Task<bool> DeactivateRoomAsync(int id)
    {
        var room = await _context.Rooms
            .Include(r => r.Beds)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            return false;
        }

        var hasOccupiedBeds = room.Beds.Any(b => b.Status == BedStatus.Occupied);
        if (hasOccupiedBeds)
        {
            throw new InvalidOperationException("Cannot deactivate a room containing occupied beds. Please transfer or discharge patients first.");
        }

        room.IsActive = false;
        room.UpdatedAt = DateTime.UtcNow;

        foreach (var bed in room.Beds)
        {
            bed.IsActive = false;
            bed.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
