using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Beds;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class BedService : IBedService
{
    private readonly AppDbContext _context;

    public BedService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BedResponse> CreateBedAsync(CreateBedRequest request)
    {
        var room = await _context.Rooms
            .Include(r => r.Ward)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId);

        if (room == null || !room.IsActive)
        {
            throw new InvalidOperationException("Target room does not exist or is inactive.");
        }

        var currentBeds = await _context.Beds.CountAsync(b => b.RoomId == request.RoomId && b.IsActive);
        if (currentBeds >= room.Capacity)
        {
            throw new InvalidOperationException($"Room '{room.RoomNumber}' has reached its capacity of {room.Capacity} beds.");
        }

        var bedNumber = request.BedNumber.Trim().ToUpperInvariant();
        var bedExists = await _context.Beds.AnyAsync(b => b.RoomId == request.RoomId && b.BedNumber == bedNumber);
        if (bedExists)
        {
            throw new InvalidOperationException($"Bed '{bedNumber}' already exists in room '{room.RoomNumber}'.");
        }

        var bed = new Bed
        {
            RoomId = request.RoomId,
            BedNumber = bedNumber,
            Type = request.Type,
            Status = BedStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Beds.Add(bed);
        await _context.SaveChangesAsync();

        return new BedResponse
        {
            Id = bed.Id,
            RoomId = bed.RoomId,
            RoomNumber = room.RoomNumber,
            WardId = room.WardId,
            WardName = room.Ward?.Name ?? string.Empty,
            WardCode = room.Ward?.Code ?? string.Empty,
            Floor = room.Ward?.Floor ?? string.Empty,
            BedNumber = bed.BedNumber,
            Type = bed.Type.ToString(),
            Status = bed.Status.ToString(),
            IsActive = bed.IsActive,
            CreatedAt = bed.CreatedAt,
            UpdatedAt = bed.UpdatedAt
        };
    }

    public async Task<List<BedResponse>> GetBedsAsync(BedQueryFilter filter)
    {
        var query = _context.Beds
            .AsNoTracking()
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .Include(b => b.Allocations)
                .ThenInclude(a => a!.Admission)
                    .ThenInclude(adm => adm!.Patient)
            .AsQueryable();

        if (filter.WardId.HasValue)
        {
            query = query.Where(b => b.Room!.WardId == filter.WardId.Value);
        }

        if (filter.RoomId.HasValue)
        {
            query = query.Where(b => b.RoomId == filter.RoomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Floor))
        {
            query = query.Where(b => b.Room!.Ward!.Floor.ToLower().Contains(filter.Floor.Trim().ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.BedNumber))
        {
            query = query.Where(b => b.BedNumber.ToLower().Contains(filter.BedNumber.Trim().ToLower()));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(b => b.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(b => b.Type == filter.Type.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = filter.IsAvailable.Value
                ? query.Where(b => b.IsActive && b.Status == BedStatus.Available)
                : query.Where(b => b.Status != BedStatus.Available);
        }

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var beds = await query
            .OrderBy(b => b.Room!.Ward!.Name)
            .ThenBy(b => b.Room!.RoomNumber)
            .ThenBy(b => b.BedNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return beds.Select(MapToBedResponse).ToList();
    }

    public async Task<List<BedResponse>> GetAvailableBedsAsync(int? wardId = null, int? roomId = null, BedType? type = null)
    {
        var query = _context.Beds
            .AsNoTracking()
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .Where(b => b.IsActive && b.Status == BedStatus.Available);

        if (wardId.HasValue)
        {
            query = query.Where(b => b.Room!.WardId == wardId.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(b => b.RoomId == roomId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(b => b.Type == type.Value);
        }

        var beds = await query
            .OrderBy(b => b.Room!.Ward!.Name)
            .ThenBy(b => b.Room!.RoomNumber)
            .ThenBy(b => b.BedNumber)
            .ToListAsync();

        return beds.Select(MapToBedResponse).ToList();
    }

    public async Task<BedResponse?> GetBedByIdAsync(int id)
    {
        var bed = await _context.Beds
            .AsNoTracking()
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .Include(b => b.Allocations)
                .ThenInclude(a => a!.Admission)
                    .ThenInclude(adm => adm!.Patient)
            .FirstOrDefaultAsync(b => b.Id == id);

        return bed == null ? null : MapToBedResponse(bed);
    }

    public async Task<BedResponse?> UpdateBedAsync(int id, UpdateBedRequest request)
    {
        var bed = await _context.Beds
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .Include(b => b.Allocations)
                .ThenInclude(a => a!.Admission)
                    .ThenInclude(adm => adm!.Patient)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bed == null)
        {
            return null;
        }

        if (bed.Status == BedStatus.Occupied && !request.IsActive)
        {
            throw new InvalidOperationException("Cannot deactivate a bed that is currently occupied.");
        }

        var bedNumber = request.BedNumber.Trim().ToUpperInvariant();
        var duplicateExists = await _context.Beds.AnyAsync(b => b.RoomId == bed.RoomId && b.BedNumber == bedNumber && b.Id != id);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"Bed number '{bedNumber}' already exists in this room.");
        }

        bed.BedNumber = bedNumber;
        bed.Type = request.Type;
        bed.IsActive = request.IsActive;
        bed.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToBedResponse(bed);
    }

    public async Task<BedResponse?> UpdateBedStatusAsync(int id, UpdateBedStatusRequest request)
    {
        var bed = await _context.Beds
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .Include(b => b.Allocations)
                .ThenInclude(a => a!.Admission)
                    .ThenInclude(adm => adm!.Patient)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bed == null)
        {
            return null;
        }

        if (bed.Status == BedStatus.Occupied)
        {
            throw new InvalidOperationException("An occupied bed cannot be manually updated. Please transfer or discharge the patient instead.");
        }

        if (request.Status == BedStatus.Occupied)
        {
            throw new InvalidOperationException("A bed cannot be manually marked as Occupied. Occupancy is managed through patient bed allocation.");
        }

        bed.Status = request.Status;
        bed.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToBedResponse(bed);
    }

    public async Task<bool> DeactivateBedAsync(int id)
    {
        var bed = await _context.Beds.FindAsync(id);
        if (bed == null)
        {
            return false;
        }

        if (bed.Status == BedStatus.Occupied)
        {
            throw new InvalidOperationException("Cannot deactivate an occupied bed. Please transfer or discharge the patient first.");
        }

        bed.IsActive = false;
        bed.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static BedResponse MapToBedResponse(Bed bed)
    {
        var activeAlloc = bed.Allocations.FirstOrDefault(a => a.Status == BedAllocationStatus.Active);

        return new BedResponse
        {
            Id = bed.Id,
            RoomId = bed.RoomId,
            RoomNumber = bed.Room?.RoomNumber ?? string.Empty,
            WardId = bed.Room?.WardId ?? 0,
            WardName = bed.Room?.Ward?.Name ?? string.Empty,
            WardCode = bed.Room?.Ward?.Code ?? string.Empty,
            Floor = bed.Room?.Ward?.Floor ?? string.Empty,
            BedNumber = bed.BedNumber,
            Type = bed.Type.ToString(),
            Status = bed.Status.ToString(),
            IsActive = bed.IsActive,
            CurrentPatientId = activeAlloc?.Admission?.PatientId,
            CurrentPatientName = activeAlloc?.Admission?.Patient != null
                ? $"{activeAlloc.Admission.Patient.FirstName} {activeAlloc.Admission.Patient.LastName}".Trim()
                : null,
            CurrentAdmissionId = activeAlloc?.AdmissionId,
            CreatedAt = bed.CreatedAt,
            UpdatedAt = bed.UpdatedAt
        };
    }
}
