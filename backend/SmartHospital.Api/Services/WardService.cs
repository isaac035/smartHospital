using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Occupancy;
using SmartHospital.Api.DTOs.Rooms;
using SmartHospital.Api.DTOs.Wards;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class WardService : IWardService
{
    private readonly AppDbContext _context;

    public WardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WardResponse> CreateWardAsync(CreateWardRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _context.Wards.AnyAsync(w => w.Code == code);
        if (codeExists)
        {
            throw new InvalidOperationException($"A ward with code '{code}' already exists.");
        }

        var ward = new Ward
        {
            Name = request.Name.Trim(),
            Code = code,
            Floor = request.Floor.Trim(),
            BuildingBlock = request.BuildingBlock?.Trim() ?? string.Empty,
            Capacity = request.Capacity,
            Type = request.Type,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Wards.Add(ward);
        await _context.SaveChangesAsync();

        return MapToWardResponse(ward, totalRooms: 0, totalBeds: 0, occupiedBeds: 0, availableBeds: 0);
    }

    public async Task<List<WardResponse>> GetAllWardsAsync(bool? isActive = null)
    {
        var query = _context.Wards.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        var wards = await query
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                Ward = w,
                TotalRooms = w.Rooms.Count(r => r.IsActive),
                TotalBeds = w.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive),
                OccupiedBeds = w.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive && b.Status == BedStatus.Occupied),
                AvailableBeds = w.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive && b.Status == BedStatus.Available)
            })
            .ToListAsync();

        return wards.Select(x => MapToWardResponse(
            x.Ward,
            x.TotalRooms,
            x.TotalBeds,
            x.OccupiedBeds,
            x.AvailableBeds
        )).ToList();
    }

    public async Task<WardDetailResponse?> GetWardByIdAsync(int id)
    {
        var ward = await _context.Wards
            .AsNoTracking()
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ward == null)
        {
            return null;
        }

        var totalRooms = ward.Rooms.Count(r => r.IsActive);
        var totalBeds = ward.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive);
        var occupiedBeds = ward.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive && b.Status == BedStatus.Occupied);
        var availableBeds = ward.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive && b.Status == BedStatus.Available);

        var wardResponse = MapToWardResponse(ward, totalRooms, totalBeds, occupiedBeds, availableBeds);

        var roomResponses = ward.Rooms.Select(r => new RoomResponse
        {
            Id = r.Id,
            WardId = r.WardId,
            WardName = ward.Name,
            WardCode = ward.Code,
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

        return new WardDetailResponse
        {
            Ward = wardResponse,
            Rooms = roomResponses
        };
    }

    public async Task<WardResponse?> UpdateWardAsync(int id, UpdateWardRequest request)
    {
        var ward = await _context.Wards
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ward == null)
        {
            return null;
        }

        var activeBeds = ward.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive);
        if (request.Capacity < activeBeds)
        {
            throw new InvalidOperationException($"Ward capacity cannot be less than current active beds ({activeBeds}).");
        }

        if (!request.IsActive && ward.Rooms.SelectMany(r => r.Beds).Any(b => b.Status == BedStatus.Occupied))
        {
            throw new InvalidOperationException("Cannot deactivate a ward containing occupied beds. Please transfer or discharge patients first.");
        }

        ward.Name = request.Name.Trim();
        ward.Floor = request.Floor.Trim();
        ward.BuildingBlock = request.BuildingBlock?.Trim() ?? string.Empty;
        ward.Capacity = request.Capacity;
        ward.Type = request.Type;
        ward.IsActive = request.IsActive;
        ward.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var totalRooms = ward.Rooms.Count(r => r.IsActive);
        var totalBeds = activeBeds;
        var occupiedBeds = ward.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive && b.Status == BedStatus.Occupied);
        var availableBeds = ward.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive && b.Status == BedStatus.Available);

        return MapToWardResponse(ward, totalRooms, totalBeds, occupiedBeds, availableBeds);
    }

    public async Task<bool> DeactivateWardAsync(int id)
    {
        var ward = await _context.Wards
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ward == null)
        {
            return false;
        }

        var hasOccupiedBeds = ward.Rooms
            .SelectMany(r => r.Beds)
            .Any(b => b.Status == BedStatus.Occupied);

        if (hasOccupiedBeds)
        {
            throw new InvalidOperationException("Cannot deactivate a ward containing occupied beds. Please transfer or discharge patients first.");
        }

        ward.IsActive = false;
        ward.UpdatedAt = DateTime.UtcNow;

        foreach (var room in ward.Rooms)
        {
            room.IsActive = false;
            room.UpdatedAt = DateTime.UtcNow;
            foreach (var bed in room.Beds)
            {
                bed.IsActive = false;
                bed.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<WardOccupancyResponse?> GetWardOccupancyAsync(int id)
    {
        var ward = await _context.Wards
            .AsNoTracking()
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ward == null)
        {
            return null;
        }

        var beds = ward.Rooms.SelectMany(r => r.Beds).Where(b => b.IsActive).ToList();
        var totalBeds = beds.Count;
        var occupiedBeds = beds.Count(b => b.Status == BedStatus.Occupied);
        var availableBeds = beds.Count(b => b.Status == BedStatus.Available);
        var reservedBeds = beds.Count(b => b.Status == BedStatus.Reserved);
        var maintenanceBeds = beds.Count(b => b.Status == BedStatus.Maintenance);
        var blockedBeds = beds.Count(b => b.Status == BedStatus.Blocked);
        var rate = totalBeds > 0 ? Math.Round((double)occupiedBeds / totalBeds * 100, 2) : 0;
        var isLow = totalBeds > 0 && ((double)availableBeds / totalBeds) <= 0.20;

        return new WardOccupancyResponse
        {
            WardId = ward.Id,
            WardName = ward.Name,
            WardCode = ward.Code,
            WardType = ward.Type.ToString(),
            Floor = ward.Floor,
            Capacity = ward.Capacity,
            TotalBeds = totalBeds,
            AvailableBeds = availableBeds,
            OccupiedBeds = occupiedBeds,
            ReservedBeds = reservedBeds,
            MaintenanceBeds = maintenanceBeds,
            BlockedBeds = blockedBeds,
            OccupancyRate = rate,
            IsLowAvailability = isLow
        };
    }

    private static WardResponse MapToWardResponse(Ward ward, int totalRooms, int totalBeds, int occupiedBeds, int availableBeds)
    {
        var rate = totalBeds > 0 ? Math.Round((double)occupiedBeds / totalBeds * 100, 2) : 0;

        return new WardResponse
        {
            Id = ward.Id,
            Name = ward.Name,
            Code = ward.Code,
            Floor = ward.Floor,
            BuildingBlock = ward.BuildingBlock,
            Capacity = ward.Capacity,
            Type = ward.Type.ToString(),
            IsActive = ward.IsActive,
            TotalRooms = totalRooms,
            TotalBeds = totalBeds,
            OccupiedBeds = occupiedBeds,
            AvailableBeds = availableBeds,
            OccupancyRate = rate,
            CreatedAt = ward.CreatedAt,
            UpdatedAt = ward.UpdatedAt
        };
    }
}
