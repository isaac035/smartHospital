using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.MedicalResources;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class MedicalResourceService : IMedicalResourceService
{
    private readonly AppDbContext _context;

    public MedicalResourceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalResourceResponse> CreateResourceAsync(CreateResourceRequest request)
    {
        var code = request.ResourceCode.Trim().ToUpperInvariant();
        var exists = await _context.MedicalResources.AnyAsync(mr => mr.ResourceCode == code);
        if (exists)
        {
            throw new InvalidOperationException($"Medical resource with code '{code}' already exists.");
        }

        await ValidateLocationHierarchyAsync(request.WardId, request.RoomId, request.BedId);

        var resource = new MedicalResource
        {
            ResourceCode = code,
            Name = request.Name.Trim(),
            Category = request.Category,
            Status = ResourceStatus.Available,
            LocationDescription = request.LocationDescription.Trim(),
            SerialNumber = request.SerialNumber?.Trim(),
            Manufacturer = request.Manufacturer?.Trim(),
            ModelNumber = request.ModelNumber?.Trim(),
            WardId = request.WardId,
            RoomId = request.RoomId,
            BedId = request.BedId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MedicalResources.Add(resource);
        await _context.SaveChangesAsync();

        return await GetResourceByIdAsync(resource.Id) ?? MapToResourceResponse(resource);
    }

    public async Task<List<MedicalResourceResponse>> GetResourcesAsync(ResourceQueryFilter filter)
    {
        var query = _context.MedicalResources
            .AsNoTracking()
            .Include(mr => mr.Ward)
            .Include(mr => mr.Room)
            .Include(mr => mr.Bed)
            .AsQueryable();

        if (filter.Category.HasValue)
        {
            query = query.Where(mr => mr.Category == filter.Category.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(mr => mr.Status == filter.Status.Value);
        }

        if (filter.WardId.HasValue)
        {
            query = query.Where(mr => mr.WardId == filter.WardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(mr =>
                mr.ResourceCode.ToLower().Contains(term) ||
                mr.Name.ToLower().Contains(term) ||
                mr.LocationDescription.ToLower().Contains(term));
        }

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var items = await query
            .OrderBy(mr => mr.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return items.Select(MapToResourceResponse).ToList();
    }

    public async Task<MedicalResourceResponse?> GetResourceByIdAsync(int id)
    {
        var resource = await _context.MedicalResources
            .AsNoTracking()
            .Include(mr => mr.Ward)
            .Include(mr => mr.Room)
            .Include(mr => mr.Bed)
            .FirstOrDefaultAsync(mr => mr.Id == id);

        return resource == null ? null : MapToResourceResponse(resource);
    }

    public async Task<MedicalResourceResponse?> UpdateResourceAsync(int id, UpdateResourceRequest request)
    {
        var resource = await _context.MedicalResources
            .Include(mr => mr.Ward)
            .Include(mr => mr.Room)
            .Include(mr => mr.Bed)
            .FirstOrDefaultAsync(mr => mr.Id == id);

        if (resource == null)
        {
            return null;
        }

        await ValidateLocationHierarchyAsync(request.WardId, request.RoomId, request.BedId);

        resource.Name = request.Name.Trim();
        resource.Category = request.Category;
        resource.Status = request.Status;
        resource.LocationDescription = request.LocationDescription.Trim();
        resource.SerialNumber = request.SerialNumber?.Trim();
        resource.Manufacturer = request.Manufacturer?.Trim();
        resource.ModelNumber = request.ModelNumber?.Trim();
        resource.WardId = request.WardId;
        resource.RoomId = request.RoomId;
        resource.BedId = request.BedId;
        resource.IsActive = request.IsActive;
        resource.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetResourceByIdAsync(resource.Id) ?? MapToResourceResponse(resource);
    }

    public async Task<MedicalResourceResponse?> AssignResourceAsync(int id, AssignResourceRequest request)
    {
        var resource = await _context.MedicalResources
            .Include(mr => mr.Ward)
            .Include(mr => mr.Room)
            .Include(mr => mr.Bed)
            .FirstOrDefaultAsync(mr => mr.Id == id);

        if (resource == null)
        {
            return null;
        }

        await ValidateLocationHierarchyAsync(request.WardId, request.RoomId, request.BedId);

        resource.WardId = request.WardId;
        resource.RoomId = request.RoomId;
        resource.BedId = request.BedId;

        if (!string.IsNullOrWhiteSpace(request.LocationDescription))
        {
            resource.LocationDescription = request.LocationDescription.Trim();
        }

        resource.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetResourceByIdAsync(resource.Id) ?? MapToResourceResponse(resource);
    }

    public async Task<bool> DeactivateResourceAsync(int id)
    {
        var resource = await _context.MedicalResources.FindAsync(id);
        if (resource == null)
        {
            return false;
        }

        if (resource.Status == ResourceStatus.InUse)
        {
            throw new InvalidOperationException("Cannot deactivate a medical resource that is currently in use.");
        }

        resource.IsActive = false;
        resource.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ValidateLocationHierarchyAsync(int? wardId, int? roomId, int? bedId)
    {
        if (bedId.HasValue)
        {
            var bed = await _context.Beds
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bedId.Value);

            if (bed == null)
            {
                throw new InvalidOperationException("Specified bed does not exist.");
            }

            if (roomId.HasValue && roomId.Value != bed.RoomId)
            {
                throw new InvalidOperationException("Specified bed does not belong to the selected room.");
            }

            if (wardId.HasValue && bed.Room != null && wardId.Value != bed.Room.WardId)
            {
                throw new InvalidOperationException("Specified bed does not belong to the selected ward.");
            }
        }
        else if (roomId.HasValue)
        {
            var room = await _context.Rooms.FindAsync(roomId.Value);
            if (room == null)
            {
                throw new InvalidOperationException("Specified room does not exist.");
            }

            if (wardId.HasValue && wardId.Value != room.WardId)
            {
                throw new InvalidOperationException("Specified room does not belong to the selected ward.");
            }
        }
        else if (wardId.HasValue)
        {
            var wardExists = await _context.Wards.AnyAsync(w => w.Id == wardId.Value);
            if (!wardExists)
            {
                throw new InvalidOperationException("Specified ward does not exist.");
            }
        }
    }

    private static MedicalResourceResponse MapToResourceResponse(MedicalResource mr)
    {
        return new MedicalResourceResponse
        {
            Id = mr.Id,
            ResourceCode = mr.ResourceCode,
            Name = mr.Name,
            Category = mr.Category.ToString(),
            Status = mr.Status.ToString(),
            LocationDescription = mr.LocationDescription,
            SerialNumber = mr.SerialNumber,
            Manufacturer = mr.Manufacturer,
            ModelNumber = mr.ModelNumber,
            WardId = mr.WardId,
            WardName = mr.Ward?.Name,
            RoomId = mr.RoomId,
            RoomNumber = mr.Room?.RoomNumber,
            BedId = mr.BedId,
            BedNumber = mr.Bed?.BedNumber,
            IsActive = mr.IsActive,
            CreatedAt = mr.CreatedAt,
            UpdatedAt = mr.UpdatedAt
        };
    }
}
