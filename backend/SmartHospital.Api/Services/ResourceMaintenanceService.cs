using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Maintenance;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class ResourceMaintenanceService : IResourceMaintenanceService
{
    private readonly AppDbContext _context;

    public ResourceMaintenanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenanceResponse> ScheduleMaintenanceAsync(CreateMaintenanceRequest request, int? staffUserId = null)
    {
        if (request.BedId.HasValue && request.MedicalResourceId.HasValue)
        {
            throw new ArgumentException("A maintenance task cannot target both a bed and a medical resource.");
        }

        if (!request.BedId.HasValue && !request.MedicalResourceId.HasValue)
        {
            throw new ArgumentException("A maintenance task must target either a bed or a medical resource.");
        }

        Bed? bed = null;
        if (request.BedId.HasValue)
        {
            bed = await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Ward)
                .FirstOrDefaultAsync(b => b.Id == request.BedId.Value);

            if (bed == null)
            {
                throw new InvalidOperationException("Specified bed does not exist.");
            }
        }

        MedicalResource? resource = null;
        if (request.MedicalResourceId.HasValue)
        {
            resource = await _context.MedicalResources.FindAsync(request.MedicalResourceId.Value);
            if (resource == null)
            {
                throw new InvalidOperationException("Specified medical resource does not exist.");
            }
        }

        string maintenanceCode;
        do
        {
            maintenanceCode = $"MNT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        } while (await _context.ResourceMaintenances.AnyAsync(m => m.MaintenanceCode == maintenanceCode));

        var maintenance = new ResourceMaintenance
        {
            MaintenanceCode = maintenanceCode,
            BedId = request.BedId,
            MedicalResourceId = request.MedicalResourceId,
            Type = request.Type,
            Status = MaintenanceStatus.Scheduled,
            Description = request.Description.Trim(),
            ScheduledStart = request.ScheduledStart,
            ScheduledEnd = request.ScheduledEnd,
            PerformedByStaffId = staffUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ResourceMaintenances.Add(maintenance);
        await _context.SaveChangesAsync();

        return MapToMaintenanceResponse(maintenance, bed, resource, null);
    }

    public async Task<List<MaintenanceResponse>> GetMaintenanceRecordsAsync(int? bedId = null, int? resourceId = null, MaintenanceStatus? status = null)
    {
        var query = _context.ResourceMaintenances
            .AsNoTracking()
            .Include(m => m.Bed)
                .ThenInclude(b => b!.Room)
                    .ThenInclude(r => r!.Ward)
            .Include(m => m.MedicalResource)
            .Include(m => m.PerformedByStaff)
            .AsQueryable();

        if (bedId.HasValue)
        {
            query = query.Where(m => m.BedId == bedId.Value);
        }

        if (resourceId.HasValue)
        {
            query = query.Where(m => m.MedicalResourceId == resourceId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        var records = await query.OrderByDescending(m => m.ScheduledStart).ToListAsync();

        return records.Select(m => MapToMaintenanceResponse(m, m.Bed, m.MedicalResource, m.PerformedByStaff)).ToList();
    }

    public async Task<MaintenanceResponse?> GetMaintenanceByIdAsync(int id)
    {
        var m = await _context.ResourceMaintenances
            .AsNoTracking()
            .Include(x => x.Bed)
                .ThenInclude(b => b!.Room)
                    .ThenInclude(r => r!.Ward)
            .Include(x => x.MedicalResource)
            .Include(x => x.PerformedByStaff)
            .FirstOrDefaultAsync(x => x.Id == id);

        return m == null ? null : MapToMaintenanceResponse(m, m.Bed, m.MedicalResource, m.PerformedByStaff);
    }

    public async Task<MaintenanceResponse?> StartMaintenanceAsync(int id, int? staffUserId = null)
    {
        var m = await _context.ResourceMaintenances
            .Include(x => x.Bed)
            .Include(x => x.MedicalResource)
            .Include(x => x.PerformedByStaff)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (m == null)
        {
            return null;
        }

        if (m.Status != MaintenanceStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot start maintenance with status '{m.Status}'. It must be Scheduled.");
        }

        if (m.BedId.HasValue && m.Bed != null)
        {
            var hasActiveAlloc = await _context.BedAllocations
                .AnyAsync(ba => ba.BedId == m.BedId.Value && ba.Status == BedAllocationStatus.Active);

            if (hasActiveAlloc || m.Bed.Status == BedStatus.Occupied)
            {
                throw new InvalidOperationException("Cannot start maintenance on a bed with an active patient allocation.");
            }

            m.Bed.Status = BedStatus.Maintenance;
            m.Bed.UpdatedAt = DateTime.UtcNow;
        }

        if (m.MedicalResourceId.HasValue && m.MedicalResource != null)
        {
            if (m.MedicalResource.Status == ResourceStatus.InUse)
            {
                throw new InvalidOperationException("Cannot start maintenance on equipment that is currently in use.");
            }

            m.MedicalResource.Status = ResourceStatus.Maintenance;
            m.MedicalResource.UpdatedAt = DateTime.UtcNow;
        }

        m.Status = MaintenanceStatus.InProgress;
        if (staffUserId.HasValue)
        {
            m.PerformedByStaffId = staffUserId.Value;
        }
        m.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToMaintenanceResponse(m, m.Bed, m.MedicalResource, m.PerformedByStaff);
    }

    public async Task<MaintenanceResponse?> CompleteMaintenanceAsync(int id, CompleteMaintenanceRequest request, int? staffUserId = null)
    {
        var m = await _context.ResourceMaintenances
            .Include(x => x.Bed)
            .Include(x => x.MedicalResource)
            .Include(x => x.PerformedByStaff)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (m == null)
        {
            return null;
        }

        if (m.Status != MaintenanceStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete maintenance with status '{m.Status}'. It must be InProgress.");
        }

        if (m.BedId.HasValue)
        {
            var bed = m.Bed ?? await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Ward)
                .FirstOrDefaultAsync(b => b.Id == m.BedId.Value);

            if (bed == null)
            {
                throw new InvalidOperationException("Safety check failed: target bed record not found.");
            }

            if (!bed.IsActive)
            {
                throw new InvalidOperationException("Safety check failed: cannot restore a deactivated bed to Available.");
            }

            if (bed.Room != null && (!bed.Room.IsActive || (bed.Room.Ward != null && !bed.Room.Ward.IsActive)))
            {
                throw new InvalidOperationException("Safety check failed: bed belongs to an inactive room or ward.");
            }

            var hasActiveAlloc = await _context.BedAllocations
                .AnyAsync(ba => ba.BedId == m.BedId.Value && ba.Status == BedAllocationStatus.Active);

            if (hasActiveAlloc)
            {
                throw new InvalidOperationException("Safety check failed: bed has an active patient allocation and cannot be marked Available.");
            }

            bed.Status = BedStatus.Available;
            bed.UpdatedAt = DateTime.UtcNow;
        }

        if (m.MedicalResourceId.HasValue)
        {
            var resource = m.MedicalResource ?? await _context.MedicalResources
                .FirstOrDefaultAsync(r => r.Id == m.MedicalResourceId.Value);

            if (resource == null)
            {
                throw new InvalidOperationException("Safety check failed: target medical resource not found.");
            }

            if (!resource.IsActive)
            {
                throw new InvalidOperationException("Safety check failed: cannot restore a deactivated medical resource to Available.");
            }

            if (resource.Status == ResourceStatus.InUse)
            {
                throw new InvalidOperationException("Safety check failed: medical resource is currently in use.");
            }

            resource.Status = ResourceStatus.Available;
            resource.UpdatedAt = DateTime.UtcNow;
        }

        m.Status = MaintenanceStatus.Completed;
        m.ActualCompletedAt = DateTime.UtcNow;
        m.ResolutionNotes = request.ResolutionNotes.Trim();
        if (staffUserId.HasValue)
        {
            m.PerformedByStaffId = staffUserId.Value;
        }
        m.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToMaintenanceResponse(m, m.Bed, m.MedicalResource, m.PerformedByStaff);
    }

    private static MaintenanceResponse MapToMaintenanceResponse(
        ResourceMaintenance m,
        Bed? bed,
        MedicalResource? resource,
        User? staff)
    {
        var targetType = m.BedId.HasValue ? "Bed" : "MedicalResource";
        var targetName = m.BedId.HasValue
            ? $"Bed {bed?.BedNumber ?? m.BedId.ToString()}"
            : (resource != null ? $"{resource.Name} ({resource.ResourceCode})" : "Medical Resource");

        return new MaintenanceResponse
        {
            Id = m.Id,
            MaintenanceCode = m.MaintenanceCode,
            TargetType = targetType,
            TargetName = targetName,
            BedId = m.BedId,
            BedNumber = bed?.BedNumber,
            MedicalResourceId = m.MedicalResourceId,
            MedicalResourceCode = resource?.ResourceCode,
            MedicalResourceName = resource?.Name,
            Type = m.Type.ToString(),
            Status = m.Status.ToString(),
            Description = m.Description,
            ScheduledStart = m.ScheduledStart,
            ScheduledEnd = m.ScheduledEnd,
            ActualCompletedAt = m.ActualCompletedAt,
            PerformedByStaffId = m.PerformedByStaffId,
            PerformedByStaffName = staff != null ? $"{staff.FirstName} {staff.LastName}".Trim() : null,
            ResolutionNotes = m.ResolutionNotes,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };
    }
}
