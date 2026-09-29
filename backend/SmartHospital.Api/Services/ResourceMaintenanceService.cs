using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Maintenance;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class ResourceMaintenanceService : IResourceMaintenanceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ResourceMaintenanceService> _logger;

    public ResourceMaintenanceService(AppDbContext context, ILogger<ResourceMaintenanceService> logger)
    {
        _context = context;
        _logger = logger;
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

    public async Task<int> ProcessScheduledMaintenanceAutoStartAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        // Query maintenance records that are Scheduled and whose ScheduledStart has arrived or passed
        var dueRecordIds = await _context.ResourceMaintenances
            .AsNoTracking()
            .Where(m => m.Status == MaintenanceStatus.Scheduled && m.ScheduledStart <= nowUtc)
            .OrderBy(m => m.ScheduledStart)
            .Select(m => new { m.Id, m.MaintenanceCode, m.BedId, m.MedicalResourceId })
            .ToListAsync(cancellationToken);

        if (dueRecordIds.Count == 0)
        {
            return 0;
        }

        var startedCount = 0;

        foreach (var item in dueRecordIds)
        {
            try
            {
                _context.ChangeTracker.Clear();
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                // 1. Concurrency safety: Atomically transition status from Scheduled -> InProgress
                // Only one concurrent process can update the row if status is still Scheduled
                var rowsUpdated = await _context.ResourceMaintenances
                    .Where(m => m.Id == item.Id && m.Status == MaintenanceStatus.Scheduled)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.Status, MaintenanceStatus.InProgress)
                        .SetProperty(m => m.UpdatedAt, nowUtc),
                        cancellationToken);

                if (rowsUpdated == 0)
                {
                    // Another instance or thread already transitioned or cancelled this record
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                // 2. Validate target safety rules (identical to StartMaintenanceAsync)
                if (item.BedId.HasValue)
                {
                    var bed = await _context.Beds
                        .FirstOrDefaultAsync(b => b.Id == item.BedId.Value, cancellationToken);

                    if (bed == null)
                    {
                        _logger.LogWarning("Maintenance {Code}: Target bed ID {BedId} not found. Reverting to Scheduled.", item.MaintenanceCode, item.BedId.Value);
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    var hasActiveAlloc = await _context.BedAllocations
                        .AnyAsync(ba => ba.BedId == bed.Id && ba.Status == BedAllocationStatus.Active, cancellationToken);

                    if (bed.Status == BedStatus.Occupied || hasActiveAlloc)
                    {
                        _logger.LogInformation("Maintenance {Code}: Bed {BedNumber} is currently occupied or allocated. Keeping Scheduled until vacated.", item.MaintenanceCode, bed.BedNumber);
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    bed.Status = BedStatus.Maintenance;
                    bed.UpdatedAt = nowUtc;
                }
                else if (item.MedicalResourceId.HasValue)
                {
                    var resource = await _context.MedicalResources
                        .FirstOrDefaultAsync(r => r.Id == item.MedicalResourceId.Value, cancellationToken);

                    if (resource == null)
                    {
                        _logger.LogWarning("Maintenance {Code}: Target medical resource ID {ResId} not found. Reverting to Scheduled.", item.MaintenanceCode, item.MedicalResourceId.Value);
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    if (resource.Status == ResourceStatus.InUse)
                    {
                        _logger.LogInformation("Maintenance {Code}: Medical resource {Name} ({Code}) is currently InUse. Keeping Scheduled until released.", item.MaintenanceCode, resource.Name, resource.ResourceCode);
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    resource.Status = ResourceStatus.Maintenance;
                    resource.UpdatedAt = nowUtc;
                }

                // 3. Save target status change and commit transaction
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                startedCount++;
                _logger.LogInformation("Maintenance {Code} automatically transitioned to InProgress. Target asset marked Maintenance.", item.MaintenanceCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-starting maintenance record ID {Id} ({Code}).", item.Id, item.MaintenanceCode);
            }
        }

        return startedCount;
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
