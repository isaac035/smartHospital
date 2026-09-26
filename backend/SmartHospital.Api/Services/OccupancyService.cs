using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Occupancy;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class OccupancyService : IOccupancyService
{
    private readonly AppDbContext _context;

    public OccupancyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OccupancyOverviewResponse> GetOccupancyOverviewAsync()
    {
        var totalBeds = await _context.Beds.CountAsync(b => b.IsActive);
        var availableBeds = await _context.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Available);
        var occupiedBeds = await _context.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied);
        var reservedBeds = await _context.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Reserved);
        var maintenanceBeds = await _context.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Maintenance);
        var blockedBeds = await _context.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Blocked);

        var hospitalRate = totalBeds > 0 ? Math.Round((double)occupiedBeds / totalBeds * 100, 2) : 0;

        var totalWards = await _context.Wards.CountAsync();
        var activeWards = await _context.Wards.CountAsync(w => w.IsActive);

        var wards = await _context.Wards
            .AsNoTracking()
            .Where(w => w.IsActive)
            .Select(w => new
            {
                Total = w.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive),
                Available = w.Rooms.SelectMany(r => r.Beds).Count(b => b.IsActive && b.Status == BedStatus.Available)
            })
            .ToListAsync();

        var lowAvailabilityWardCount = wards.Count(w =>
            w.Total > 0 && ((double)w.Available / w.Total) <= 0.20);

        var totalResources = await _context.MedicalResources.CountAsync(mr => mr.IsActive);
        var availableResources = await _context.MedicalResources.CountAsync(mr => mr.IsActive && mr.Status == ResourceStatus.Available);
        var inUseResources = await _context.MedicalResources.CountAsync(mr => mr.IsActive && mr.Status == ResourceStatus.InUse);
        var maintenanceResources = await _context.MedicalResources.CountAsync(mr => mr.IsActive && mr.Status == ResourceStatus.Maintenance);

        return new OccupancyOverviewResponse
        {
            TotalBeds = totalBeds,
            AvailableBeds = availableBeds,
            OccupiedBeds = occupiedBeds,
            ReservedBeds = reservedBeds,
            MaintenanceBeds = maintenanceBeds,
            BlockedBeds = blockedBeds,
            HospitalOccupancyRate = hospitalRate,
            TotalWards = totalWards,
            ActiveWards = activeWards,
            LowAvailabilityWardCount = lowAvailabilityWardCount,
            TotalMedicalResources = totalResources,
            AvailableMedicalResources = availableResources,
            InUseMedicalResources = inUseResources,
            MaintenanceMedicalResources = maintenanceResources
        };
    }

    public async Task<List<WardOccupancyResponse>> GetWardOccupanciesAsync()
    {
        var wards = await _context.Wards
            .AsNoTracking()
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .OrderBy(w => w.Name)
            .ToListAsync();

        return wards.Select(MapToWardOccupancy).ToList();
    }

    public async Task<WardOccupancyResponse?> GetWardOccupancyByIdAsync(int wardId)
    {
        var ward = await _context.Wards
            .AsNoTracking()
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .FirstOrDefaultAsync(w => w.Id == wardId);

        return ward == null ? null : MapToWardOccupancy(ward);
    }

    private static WardOccupancyResponse MapToWardOccupancy(Ward ward)
    {
        var beds = ward.Rooms.SelectMany(r => r.Beds).Where(b => b.IsActive).ToList();
        var total = beds.Count;
        var occupied = beds.Count(b => b.Status == BedStatus.Occupied);
        var available = beds.Count(b => b.Status == BedStatus.Available);
        var reserved = beds.Count(b => b.Status == BedStatus.Reserved);
        var maintenance = beds.Count(b => b.Status == BedStatus.Maintenance);
        var blocked = beds.Count(b => b.Status == BedStatus.Blocked);

        var rate = total > 0 ? Math.Round((double)occupied / total * 100, 2) : 0;
        var isLow = total > 0 && ((double)available / total) <= 0.20;

        return new WardOccupancyResponse
        {
            WardId = ward.Id,
            WardName = ward.Name,
            WardCode = ward.Code,
            WardType = ward.Type.ToString(),
            Floor = ward.Floor,
            Capacity = ward.Capacity,
            TotalBeds = total,
            AvailableBeds = available,
            OccupiedBeds = occupied,
            ReservedBeds = reserved,
            MaintenanceBeds = maintenance,
            BlockedBeds = blocked,
            OccupancyRate = rate,
            IsLowAvailability = isLow
        };
    }
}
