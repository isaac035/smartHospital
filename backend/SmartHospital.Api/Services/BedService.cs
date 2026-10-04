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
            if (filter.Status.Value == BedStatus.Available)
            {
                query = query.Where(b => b.Status == BedStatus.Available
                    && !b.Allocations.Any(a => a.Status == BedAllocationStatus.Active)
                    && !_context.AppointmentResourceAllocations.Any(a => a.BedId == b.Id && a.IsActive));
            }
            else if (filter.Status.Value == BedStatus.Reserved)
            {
                query = query.Where(b => b.Status == BedStatus.Reserved
                    || b.Allocations.Any(a => a.Status == BedAllocationStatus.Active && a.Admission!.Status == AdmissionStatus.Reserved)
                    || _context.AppointmentResourceAllocations.Any(a => a.BedId == b.Id && a.IsActive));
            }
            else if (filter.Status.Value == BedStatus.Occupied)
            {
                query = query.Where(b => b.Status == BedStatus.Occupied
                    || b.Allocations.Any(a => a.Status == BedAllocationStatus.Active));
            }
            else
            {
                query = query.Where(b => b.Status == filter.Status.Value);
            }
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(b => b.Type == filter.Type.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = filter.IsAvailable.Value
                ? query.Where(b => b.IsActive && b.Status == BedStatus.Available
                    && !b.Allocations.Any(a => a.Status == BedAllocationStatus.Active)
                    && !_context.AppointmentResourceAllocations.Any(a => a.BedId == b.Id && a.IsActive))
                : query.Where(b => b.Status != BedStatus.Available
                    || b.Allocations.Any(a => a.Status == BedAllocationStatus.Active)
                    || _context.AppointmentResourceAllocations.Any(a => a.BedId == b.Id && a.IsActive));
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

        var agentReservations = await GetActiveAgentReservations(beds.Select(b => b.Id));
        return beds.Select(b => MapToBedResponse(b, agentReservations.GetValueOrDefault(b.Id))).ToList();
    }

    public async Task<List<BedResponse>> GetAvailableBedsAsync(int? wardId = null, int? roomId = null, BedType? type = null)
    {
        var query = _context.Beds
            .AsNoTracking()
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .Where(b => b.IsActive && b.Status == BedStatus.Available
                && !b.Allocations.Any(a => a.Status == BedAllocationStatus.Active)
                && !_context.AppointmentResourceAllocations.Any(a => a.BedId == b.Id && a.IsActive));

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

        return beds.Select(b => MapToBedResponse(b)).ToList();
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

        if (bed == null) return null;
        var agentReservations = await GetActiveAgentReservations(new[] { bed.Id });
        return MapToBedResponse(bed, agentReservations.GetValueOrDefault(bed.Id));
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

        if (!request.IsActive &&
            (await _context.BedAllocations.AnyAsync(a => a.BedId == id && a.Status == BedAllocationStatus.Active)
                || await _context.AppointmentResourceAllocations.AnyAsync(a => a.BedId == id && a.IsActive)))
        {
            throw new InvalidOperationException("Cannot deactivate a bed while an active reservation is linked to it.");
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

        var activeAgentReservations = await GetActiveAgentReservations(new[] { bed.Id });
        return MapToBedResponse(bed, activeAgentReservations.GetValueOrDefault(bed.Id));
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

        if (await _context.BedAllocations.AnyAsync(a => a.BedId == id && a.Status == BedAllocationStatus.Active)
            || await _context.AppointmentResourceAllocations.AnyAsync(a => a.BedId == id && a.IsActive))
        {
            throw new InvalidOperationException("This bed has an active reservation or allocation. Release it from Admissions before changing its status.");
        }

        if (request.Status == BedStatus.Occupied)
        {
            throw new InvalidOperationException("A bed cannot be manually marked as Occupied. Occupancy is managed through patient bed allocation.");
        }

        bed.Status = request.Status;
        bed.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var agentReservations = await GetActiveAgentReservations(new[] { bed.Id });
        return MapToBedResponse(bed, agentReservations.GetValueOrDefault(bed.Id));
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

        if (await _context.BedAllocations.AnyAsync(a => a.BedId == id && a.Status == BedAllocationStatus.Active)
            || await _context.AppointmentResourceAllocations.AnyAsync(a => a.BedId == id && a.IsActive))
        {
            throw new InvalidOperationException("This bed has an active reservation or allocation. Release it from Admissions before deactivating the bed.");
        }

        bed.IsActive = false;
        bed.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<Dictionary<int, AgentBedReservationPatient>> GetActiveAgentReservations(IEnumerable<int> bedIds)
    {
        var ids = bedIds.ToArray();
        if (ids.Length == 0) return new Dictionary<int, AgentBedReservationPatient>();
        var reservations = await (
            from allocation in _context.AppointmentResourceAllocations.AsNoTracking()
            join patient in _context.Users.AsNoTracking() on allocation.PatientId equals patient.Id
            where allocation.IsActive && allocation.BedId.HasValue && ids.Contains(allocation.BedId.Value)
            select new
            {
                BedId = allocation.BedId!.Value,
                PatientId = patient.Id,
                PatientName = (patient.FirstName + " " + patient.LastName).Trim()
            })
            .ToListAsync();
        return reservations.GroupBy(r => r.BedId).ToDictionary(
            group => group.Key,
            group => new AgentBedReservationPatient(group.First().PatientId, group.First().PatientName));
    }

    private static BedResponse MapToBedResponse(Bed bed, AgentBedReservationPatient? appointmentReservation = null)
    {
        var activeAlloc = bed.Allocations.FirstOrDefault(a => a.Status == BedAllocationStatus.Active);
        var status = bed.Status;
        if (status == BedStatus.Available && activeAlloc != null)
        {
            status = activeAlloc.Admission?.Status == AdmissionStatus.Reserved ? BedStatus.Reserved : BedStatus.Occupied;
        }
        else if (status == BedStatus.Available && appointmentReservation != null)
        {
            status = BedStatus.Reserved;
        }

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
            Status = status.ToString(),
            IsActive = bed.IsActive,
            CurrentPatientId = activeAlloc?.Admission?.PatientId ?? appointmentReservation?.PatientId,
            CurrentPatientName = activeAlloc?.Admission?.Patient != null
                ? $"{activeAlloc.Admission.Patient.FirstName} {activeAlloc.Admission.Patient.LastName}".Trim()
                : appointmentReservation?.PatientName,
            CurrentAdmissionId = activeAlloc?.AdmissionId,
            CreatedAt = bed.CreatedAt,
            UpdatedAt = bed.UpdatedAt
        };
    }

    private sealed record AgentBedReservationPatient(int PatientId, string PatientName);
}
