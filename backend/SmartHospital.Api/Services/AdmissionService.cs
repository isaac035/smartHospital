using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Admissions;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class AdmissionService : IAdmissionService
{
    private readonly AppDbContext _context;

    public AdmissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdmissionResponse> CreateAdmissionAsync(CreateAdmissionRequest request)
    {
        var patient = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.Role == UserRole.Patient);

        if (patient == null)
        {
            throw new InvalidOperationException("Patient user not found.");
        }

        var hasActiveAdmission = await _context.Admissions
            .AnyAsync(a => a.PatientId == request.PatientId && a.Status == AdmissionStatus.Admitted);

        if (hasActiveAdmission)
        {
            throw new InvalidOperationException("This patient already has an active inpatient admission.");
        }


        User? doctor = null;
        if (request.AdmittingDoctorId.HasValue)
        {
            doctor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.AdmittingDoctorId.Value && u.Role == UserRole.Doctor);

            if (doctor == null)
            {
                throw new InvalidOperationException("Admitting doctor not found.");
            }
        }

        string admissionNumber;
        do
        {
            admissionNumber = $"ADM-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        } while (await _context.Admissions.AnyAsync(a => a.AdmissionNumber == admissionNumber));

        var admission = new Admission
        {
            AdmissionNumber = admissionNumber,
            PatientId = request.PatientId,
            AdmittingDoctorId = request.AdmittingDoctorId,
            AdmissionDate = DateTime.UtcNow,
            Status = AdmissionStatus.Admitted,
            Priority = request.Priority,
            ReasonForAdmission = request.ReasonForAdmission.Trim(),
            Diagnosis = request.Diagnosis?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Admissions.Add(admission);
        await _context.SaveChangesAsync();

        return new AdmissionResponse
        {
            Id = admission.Id,
            AdmissionNumber = admission.AdmissionNumber,
            PatientId = patient.Id,
            PatientName = $"{patient.FirstName} {patient.LastName}".Trim(),
            PatientEmail = patient.Email,
            PatientPhone = patient.PhoneNumber,
            AdmittingDoctorId = doctor?.Id,
            AdmittingDoctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : null,
            AdmissionDate = admission.AdmissionDate,
            DischargeDate = admission.DischargeDate,
            Status = admission.Status.ToString(),
            Priority = admission.Priority.ToString(),
            ReasonForAdmission = admission.ReasonForAdmission,
            Diagnosis = admission.Diagnosis,
            DischargeSummary = admission.DischargeSummary,
            CreatedAt = admission.CreatedAt,
            UpdatedAt = admission.UpdatedAt
        };
    }

    public async Task<List<AdmissionResponse>> GetAdmissionsAsync(AdmissionQueryFilter filter)
    {
        var query = _context.Admissions
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.PatientSearch))
        {
            var term = filter.PatientSearch.Trim().ToLower();
            query = query.Where(a =>
                a.Patient!.FirstName.ToLower().Contains(term) ||
                a.Patient.LastName.ToLower().Contains(term) ||
                a.Patient.Email.ToLower().Contains(term) ||
                a.Patient.PhoneNumber.ToLower().Contains(term));
        }

        if (filter.DoctorId.HasValue)
        {
            query = query.Where(a => a.AdmittingDoctorId == filter.DoctorId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(a => a.Priority == filter.Priority.Value);
        }

        if (filter.WardId.HasValue)
        {
            query = query.Where(a => a.BedAllocations.Any(ba =>
                ba.Status == BedAllocationStatus.Active &&
                ba.Bed!.Room!.WardId == filter.WardId.Value));
        }

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var admissions = await query
            .OrderByDescending(a => a.AdmissionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return admissions.Select(MapToAdmissionResponse).ToList();
    }

    public async Task<AdmissionResponse?> GetAdmissionByIdAsync(int id)
    {
        var admission = await _context.Admissions
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .FirstOrDefaultAsync(a => a.Id == id);

        return admission == null ? null : MapToAdmissionResponse(admission);
    }

    public async Task<PatientAdmissionSummaryResponse?> GetActiveAdmissionByPatientIdAsync(int patientId)
    {
        var admission = await _context.Admissions
            .AsNoTracking()
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .Where(a => a.PatientId == patientId && a.Status == AdmissionStatus.Admitted)
            .OrderByDescending(a => a.AdmissionDate)
            .FirstOrDefaultAsync();

        if (admission == null)
        {
            return null;
        }

        var activeAlloc = admission.BedAllocations.FirstOrDefault(ba => ba.Status == BedAllocationStatus.Active);

        return new PatientAdmissionSummaryResponse
        {
            AdmissionId = admission.Id,
            AdmissionNumber = admission.AdmissionNumber,
            Status = admission.Status.ToString(),
            Priority = admission.Priority.ToString(),
            AdmissionDate = admission.AdmissionDate,
            DischargeDate = admission.DischargeDate,
            DoctorName = admission.AdmittingDoctor != null
                ? $"{admission.AdmittingDoctor.FirstName} {admission.AdmittingDoctor.LastName}".Trim()
                : null,
            WardName = activeAlloc?.Bed?.Room?.Ward?.Name,
            WardFloor = activeAlloc?.Bed?.Room?.Ward?.Floor,
            RoomNumber = activeAlloc?.Bed?.Room?.RoomNumber,
            BedNumber = activeAlloc?.Bed?.BedNumber,
            AllocatedAt = activeAlloc?.AllocatedAt,
            ReasonForAdmission = admission.ReasonForAdmission
        };
    }

    public async Task<List<PatientAdmissionSummaryResponse>> GetPatientAdmissionHistoryAsync(int patientId)
    {
        var admissions = await _context.Admissions
            .AsNoTracking()
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AdmissionDate)
            .ToListAsync();

        return admissions.Select(a =>
        {
            var lastAlloc = a.BedAllocations.OrderByDescending(ba => ba.AllocatedAt).FirstOrDefault();

            return new PatientAdmissionSummaryResponse
            {
                AdmissionId = a.Id,
                AdmissionNumber = a.AdmissionNumber,
                Status = a.Status.ToString(),
                Priority = a.Priority.ToString(),
                AdmissionDate = a.AdmissionDate,
                DischargeDate = a.DischargeDate,
                DoctorName = a.AdmittingDoctor != null
                    ? $"{a.AdmittingDoctor.FirstName} {a.AdmittingDoctor.LastName}".Trim()
                    : null,
                WardName = lastAlloc?.Bed?.Room?.Ward?.Name,
                WardFloor = lastAlloc?.Bed?.Room?.Ward?.Floor,
                RoomNumber = lastAlloc?.Bed?.Room?.RoomNumber,
                BedNumber = lastAlloc?.Bed?.BedNumber,
                AllocatedAt = lastAlloc?.AllocatedAt,
                ReasonForAdmission = a.ReasonForAdmission
            };
        }).ToList();
    }

    public async Task<AdmissionResponse?> UpdateAdmissionAsync(int id, UpdateAdmissionRequest request)
    {
        var admission = await _context.Admissions
            .Include(a => a.Patient)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (admission == null)
        {
            return null;
        }

        if (request.AdmittingDoctorId.HasValue && request.AdmittingDoctorId != admission.AdmittingDoctorId)
        {
            var doctorExists = await _context.Users
                .AnyAsync(u => u.Id == request.AdmittingDoctorId.Value && u.Role == UserRole.Doctor);

            if (!doctorExists)
            {
                throw new InvalidOperationException("Admitting doctor user not found.");
            }

            admission.AdmittingDoctorId = request.AdmittingDoctorId.Value;
        }

        admission.Priority = request.Priority;
        admission.ReasonForAdmission = request.ReasonForAdmission.Trim();
        admission.Diagnosis = request.Diagnosis?.Trim();
        admission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToAdmissionResponse(admission);
    }

    public async Task<AdmissionResponse> AllocateBedAsync(AllocateBedRequest request, int? staffUserId = null)
    {
        var admission = await _context.Admissions
            .Include(a => a.Patient)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .FirstOrDefaultAsync(a => a.Id == request.AdmissionId);

        if (admission == null || admission.Status != AdmissionStatus.Admitted)
        {
            throw new InvalidOperationException("Active admitted patient stay not found.");
        }

        var patientHasActiveBed = await _context.BedAllocations
            .AnyAsync(ba => ba.Admission!.PatientId == admission.PatientId && ba.Status == BedAllocationStatus.Active);

        if (patientHasActiveBed)
        {
            throw new InvalidOperationException("This patient already has an active bed allocation.");
        }

        var bed = await _context.Beds
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .FirstOrDefaultAsync(b => b.Id == request.BedId);

        if (bed == null || !bed.IsActive || bed.Status != BedStatus.Available)
        {
            throw new InvalidOperationException("Selected bed is not available for allocation.");
        }

        var bedHasActiveAllocation = await _context.BedAllocations
            .AnyAsync(ba => ba.BedId == request.BedId && ba.Status == BedAllocationStatus.Active);

        if (bedHasActiveAllocation)
        {
            throw new InvalidOperationException("Selected bed already has an active allocation.");
        }

        var wardCapacity = bed.Room?.Ward?.Capacity ?? 0;
        var wardName = bed.Room?.Ward?.Name ?? "Ward";
        var wardId = bed.Room?.WardId ?? 0;

        var occupiedInWard = await _context.Beds
            .CountAsync(b => b.Room!.WardId == wardId && b.Status == BedStatus.Occupied);

        if (occupiedInWard >= wardCapacity)
        {
            throw new InvalidOperationException($"Ward '{wardName}' capacity limit of {wardCapacity} has been reached.");
        }

        var allocation = new BedAllocation
        {
            AdmissionId = admission.Id,
            BedId = bed.Id,
            AllocatedByStaffId = staffUserId,
            AllocatedAt = DateTime.UtcNow,
            Status = BedAllocationStatus.Active,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        bed.Status = BedStatus.Occupied;
        bed.UpdatedAt = DateTime.UtcNow;

        _context.BedAllocations.Add(allocation);
        await _context.SaveChangesAsync();

        return MapToAdmissionResponse(admission);
    }

    public async Task<AdmissionResponse> TransferPatientAsync(int admissionId, TransferPatientRequest request, int? staffUserId = null)
    {
        using var tx = await _context.Database.BeginTransactionAsync();

        var admission = await _context.Admissions
            .Include(a => a.Patient)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .FirstOrDefaultAsync(a => a.Id == admissionId);

        if (admission == null || admission.Status != AdmissionStatus.Admitted)
        {
            throw new InvalidOperationException("Active admitted patient stay not found.");
        }

        var currentAlloc = admission.BedAllocations.FirstOrDefault(ba => ba.Status == BedAllocationStatus.Active);
        if (currentAlloc == null)
        {
            throw new InvalidOperationException("The patient currently has no active bed allocation to transfer from.");
        }

        if (currentAlloc.BedId == request.NewBedId)
        {
            throw new InvalidOperationException("The destination bed cannot be the same as the current bed.");
        }

        var newBed = await _context.Beds
            .Include(b => b.Room)
                .ThenInclude(r => r!.Ward)
            .FirstOrDefaultAsync(b => b.Id == request.NewBedId);

        if (newBed == null || !newBed.IsActive || newBed.Status != BedStatus.Available)
        {
            throw new InvalidOperationException("Destination bed is not available for transfer.");
        }

        var currentWardId = currentAlloc.Bed?.Room?.WardId;
        var newWardId = newBed.Room?.WardId;

        if (newWardId.HasValue && newWardId != currentWardId)
        {
            var occupiedInNewWard = await _context.Beds
                .CountAsync(b => b.Room!.WardId == newWardId.Value && b.Status == BedStatus.Occupied);

            var newWardCapacity = newBed.Room?.Ward?.Capacity ?? 0;
            if (occupiedInNewWard >= newWardCapacity)
            {
                throw new InvalidOperationException($"Destination ward '{newBed.Room?.Ward?.Name}' capacity limit has been reached.");
            }
        }

        currentAlloc.Status = BedAllocationStatus.Transferred;
        currentAlloc.ReleasedAt = DateTime.UtcNow;
        currentAlloc.UpdatedAt = DateTime.UtcNow;

        if (currentAlloc.Bed != null)
        {
            currentAlloc.Bed.Status = BedStatus.Available;
            currentAlloc.Bed.UpdatedAt = DateTime.UtcNow;
        }

        var newAlloc = new BedAllocation
        {
            AdmissionId = admission.Id,
            BedId = newBed.Id,
            AllocatedByStaffId = staffUserId,
            AllocatedAt = DateTime.UtcNow,
            Status = BedAllocationStatus.Active,
            Notes = request.TransferReason.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        newBed.Status = BedStatus.Occupied;
        newBed.UpdatedAt = DateTime.UtcNow;

        _context.BedAllocations.Add(newAlloc);
        admission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return MapToAdmissionResponse(admission);
    }

    public async Task<AdmissionResponse> DischargePatientAsync(int admissionId, DischargePatientRequest request, int? staffUserId = null)
    {
        using var tx = await _context.Database.BeginTransactionAsync();

        var admission = await _context.Admissions
            .Include(a => a.Patient)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .FirstOrDefaultAsync(a => a.Id == admissionId);

        if (admission == null || admission.Status != AdmissionStatus.Admitted)
        {
            throw new InvalidOperationException("Active admitted patient stay not found.");
        }

        var activeAlloc = admission.BedAllocations.FirstOrDefault(ba => ba.Status == BedAllocationStatus.Active);
        if (activeAlloc != null)
        {
            activeAlloc.Status = BedAllocationStatus.Released;
            activeAlloc.ReleasedAt = DateTime.UtcNow;
            activeAlloc.UpdatedAt = DateTime.UtcNow;

            var bed = activeAlloc.Bed ?? await _context.Beds.FindAsync(activeAlloc.BedId);
            if (bed != null)
            {
                bed.Status = BedStatus.Available;
                bed.UpdatedAt = DateTime.UtcNow;
            }
        }

        admission.Status = AdmissionStatus.Discharged;
        admission.DischargeDate = DateTime.UtcNow;
        admission.DischargeSummary = request.DischargeSummary.Trim();
        admission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return MapToAdmissionResponse(admission);
    }

    private static AdmissionResponse MapToAdmissionResponse(Admission admission)
    {
        var activeAlloc = admission.BedAllocations.FirstOrDefault(ba => ba.Status == BedAllocationStatus.Active);

        return new AdmissionResponse
        {
            Id = admission.Id,
            AdmissionNumber = admission.AdmissionNumber,
            PatientId = admission.PatientId,
            PatientName = admission.Patient != null ? $"{admission.Patient.FirstName} {admission.Patient.LastName}".Trim() : string.Empty,
            PatientEmail = admission.Patient?.Email ?? string.Empty,
            PatientPhone = admission.Patient?.PhoneNumber ?? string.Empty,
            AdmittingDoctorId = admission.AdmittingDoctorId,
            AdmittingDoctorName = admission.AdmittingDoctor != null
                ? $"{admission.AdmittingDoctor.FirstName} {admission.AdmittingDoctor.LastName}".Trim()
                : null,
            AdmissionDate = admission.AdmissionDate,
            DischargeDate = admission.DischargeDate,
            Status = admission.Status.ToString(),
            Priority = admission.Priority.ToString(),
            ReasonForAdmission = admission.ReasonForAdmission,
            Diagnosis = admission.Diagnosis,
            DischargeSummary = admission.DischargeSummary,
            ActiveBedId = activeAlloc?.BedId,
            ActiveBedNumber = activeAlloc?.Bed?.BedNumber,
            ActiveRoomNumber = activeAlloc?.Bed?.Room?.RoomNumber,
            ActiveWardName = activeAlloc?.Bed?.Room?.Ward?.Name,
            CreatedAt = admission.CreatedAt,
            UpdatedAt = admission.UpdatedAt
        };
    }
}
