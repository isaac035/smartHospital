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

    public async Task<AdmissionResponse> CreateAdmissionAsync(CreateAdmissionRequest request, int? appointmentId = null, bool reserveStatus = false, DateTime? checkupDateUtc = null)
    {
        var patient = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.Role == UserRole.Patient);

        if (patient == null)
        {
            throw new InvalidOperationException("Patient user not found.");
        }

        Appointment? appointment = null;
        if (appointmentId.HasValue)
        {
            appointment = await _context.Appointments.Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId.Value && a.PatientId == request.PatientId);
            if (appointment == null)
            {
                throw new InvalidOperationException("Appointment not found for this patient.");
            }
            if (request.AdmittingDoctorId.HasValue && request.AdmittingDoctorId.Value != appointment.DoctorId)
            {
                throw new InvalidOperationException("Admitting doctor must match the selected appointment's doctor.");
            }
        }

        var existingForAppointment = appointmentId.HasValue
            ? await _context.Admissions.Include(a => a.Patient).Include(a => a.AdmittingDoctor)
                .Include(a => a.Appointment).ThenInclude(a => a!.Doctor)
                .Include(a => a.BedAllocations).ThenInclude(ba => ba.Bed!).ThenInclude(b => b.Room!).ThenInclude(r => r.Ward)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId.Value)
            : null;

        var hasActiveAdmission = await _context.Admissions
            .AnyAsync(a => a.PatientId == request.PatientId &&
                (a.Status == AdmissionStatus.Admitted || a.Status == AdmissionStatus.Reserved) &&
                (!appointmentId.HasValue || a.AppointmentId != appointmentId.Value));

        if (hasActiveAdmission)
        {
            throw new InvalidOperationException("This patient already has an active inpatient admission.");
        }

        if (existingForAppointment != null)
        {
            existingForAppointment.Status = reserveStatus ? AdmissionStatus.Reserved : AdmissionStatus.Admitted;
            existingForAppointment.AdmittingDoctorId = request.AdmittingDoctorId;
            existingForAppointment.Priority = request.Priority;
            existingForAppointment.ReasonForAdmission = request.ReasonForAdmission.Trim();
            existingForAppointment.Diagnosis = request.Diagnosis?.Trim();
            existingForAppointment.DischargeDate = null;
            existingForAppointment.DischargeSummary = null;
            if (checkupDateUtc.HasValue) existingForAppointment.CheckupDate = checkupDateUtc.Value;
            existingForAppointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToAdmissionResponse(existingForAppointment);
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
            CheckupDate = checkupDateUtc,
            AppointmentId = appointmentId,
            Appointment = appointment,
            Status = reserveStatus ? AdmissionStatus.Reserved : AdmissionStatus.Admitted,
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
            DoctorName = appointment?.Doctor != null ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}".Trim() : (doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : null),
            AppointmentId = appointmentId,
            AdmissionDate = admission.AdmissionDate,
            CheckupDate = admission.CheckupDate,
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
            .Include(a => a.Appointment).ThenInclude(a => a!.Doctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.PatientSearch))
        {
            var rawTerm = filter.PatientSearch.Trim();
            var term = System.Text.RegularExpressions.Regex.Replace(rawTerm, @"\s+", " ").ToLower();
            query = query.Where(a =>
                a.Patient!.FirstName.ToLower().Contains(term) ||
                a.Patient.LastName.ToLower().Contains(term) ||
                (a.Patient.FirstName.ToLower() + " " + a.Patient.LastName.ToLower()).Contains(term) ||
                (a.Patient.LastName.ToLower() + " " + a.Patient.FirstName.ToLower()).Contains(term) ||
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
            var targetWardId = filter.WardId.Value;
            query = query.Where(a =>
                ((a.Status == AdmissionStatus.Admitted || a.Status == AdmissionStatus.Reserved) && a.BedAllocations.Any(ba =>
                    ba.Status == BedAllocationStatus.Active &&
                    ba.Bed!.Room!.WardId == targetWardId))
                ||
                (a.Status != AdmissionStatus.Admitted && a.Status != AdmissionStatus.Reserved && a.BedAllocations
                    .OrderByDescending(ba => ba.ReleasedAt ?? ba.AllocatedAt)
                    .ThenByDescending(ba => ba.AllocatedAt)
                    .ThenByDescending(ba => ba.Id)
                    .Select(ba => (int?)ba.Bed!.Room!.WardId)
                    .FirstOrDefault() == targetWardId));
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
            .Include(a => a.Appointment).ThenInclude(a => a!.Doctor)
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
            .Where(a => a.PatientId == patientId && a.Status == AdmissionStatus.Discharged)
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
            .Include(a => a.Appointment).ThenInclude(a => a!.Doctor)
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

    public async Task<AdmissionResponse> AllocateBedAsync(AllocateBedRequest request, int? staffUserId = null, bool reserveBed = false)
    {
        var admission = await _context.Admissions
            .Include(a => a.Patient)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.Appointment).ThenInclude(a => a!.Doctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .FirstOrDefaultAsync(a => a.Id == request.AdmissionId);

        if (admission == null || (admission.Status != AdmissionStatus.Admitted && !(reserveBed && admission.Status == AdmissionStatus.Reserved)))
        {
            throw new InvalidOperationException("Active admitted patient stay not found.");
        }

        var existingAdmissionAllocation = admission.BedAllocations.FirstOrDefault(ba => ba.Status == BedAllocationStatus.Active);
        if (!reserveBed && existingAdmissionAllocation != null)
        {
            throw new InvalidOperationException("This admission already has an active bed allocation. Use the patient transfer workflow to move the patient to a different bed.");
        }

        if (reserveBed && existingAdmissionAllocation?.BedId == request.BedId && existingAdmissionAllocation.Bed?.Status == BedStatus.Reserved)
        {
            return MapToAdmissionResponse(admission);
        }

        if (reserveBed && existingAdmissionAllocation != null)
        {
            existingAdmissionAllocation.Status = BedAllocationStatus.Released;
            existingAdmissionAllocation.ReleasedAt = DateTime.UtcNow;
            existingAdmissionAllocation.UpdatedAt = DateTime.UtcNow;
            if (existingAdmissionAllocation.Bed != null)
            {
                existingAdmissionAllocation.Bed.Status = BedStatus.Available;
                existingAdmissionAllocation.Bed.UpdatedAt = DateTime.UtcNow;
            }
        }

        var patientHasActiveBed = await _context.BedAllocations
            .AnyAsync(ba => ba.Admission!.PatientId == admission.PatientId && ba.Status == BedAllocationStatus.Active && ba.AdmissionId != admission.Id);

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
            .CountAsync(b => b.Room!.WardId == wardId &&
                (b.Status == BedStatus.Occupied || (reserveBed && b.Status == BedStatus.Reserved)));

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

        bed.Status = reserveBed ? BedStatus.Reserved : BedStatus.Occupied;
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
            .Include(a => a.Appointment).ThenInclude(a => a!.Doctor)
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
            .Include(a => a.Appointment).ThenInclude(a => a!.Doctor)
            .Include(a => a.BedAllocations)
                .ThenInclude(ba => ba.Bed!)
                    .ThenInclude(b => b.Room!)
                        .ThenInclude(r => r.Ward)
            .FirstOrDefaultAsync(a => a.Id == admissionId);

        if (admission == null || (admission.Status != AdmissionStatus.Admitted && admission.Status != AdmissionStatus.Reserved))
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

        if (admission.AppointmentId.HasValue)
        {
            var appointmentAllocations = await _context.AppointmentResourceAllocations
                .Where(a => a.AppointmentId == admission.AppointmentId.Value && a.BedId.HasValue && a.IsActive)
                .ToListAsync();
            foreach (var resourceAllocation in appointmentAllocations)
            {
                resourceAllocation.IsActive = false;
                resourceAllocation.ReleasedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return MapToAdmissionResponse(admission);
    }

    private static AdmissionResponse MapToAdmissionResponse(Admission admission)
    {
        var activeAlloc = admission.Status == AdmissionStatus.Admitted || admission.Status == AdmissionStatus.Reserved
            ? admission.BedAllocations.FirstOrDefault(ba => ba.Status == BedAllocationStatus.Active)
            : null;

        var resolvedAlloc = activeAlloc ?? admission.BedAllocations
            .OrderByDescending(ba => ba.ReleasedAt ?? ba.AllocatedAt)
            .ThenByDescending(ba => ba.AllocatedAt)
            .ThenByDescending(ba => ba.Id)
            .FirstOrDefault();

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
            DoctorName = admission.Appointment?.Doctor != null
                ? $"{admission.Appointment.Doctor.FirstName} {admission.Appointment.Doctor.LastName}".Trim()
                : admission.AdmittingDoctor != null ? $"{admission.AdmittingDoctor.FirstName} {admission.AdmittingDoctor.LastName}".Trim() : null,
            AppointmentId = admission.AppointmentId,
            AdmissionDate = admission.AdmissionDate,
            CheckupDate = admission.CheckupDate,
            DischargeDate = admission.DischargeDate,
            Status = admission.Status.ToString(),
            Priority = admission.Priority.ToString(),
            ReasonForAdmission = admission.ReasonForAdmission,
            Diagnosis = admission.Diagnosis,
            DischargeSummary = admission.DischargeSummary,
            ActiveBedId = resolvedAlloc?.BedId,
            ActiveBedNumber = resolvedAlloc?.Bed?.BedNumber,
            ActiveRoomNumber = resolvedAlloc?.Bed?.Room?.RoomNumber,
            ActiveWardName = resolvedAlloc?.Bed?.Room?.Ward?.Name,
            CreatedAt = admission.CreatedAt,
            UpdatedAt = admission.UpdatedAt
        };
    }
}
