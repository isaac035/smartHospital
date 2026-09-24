using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Appointments;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _context;
    private readonly IAvailabilityService _availabilityService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AppointmentService> _logger;

    // ── Approved status-transition map ────────────────────────────────────────
    private static readonly Dictionary<AppointmentStatus, HashSet<AppointmentStatus>> AllowedTransitions =
        new()
        {
            [AppointmentStatus.Scheduled]    = new() { AppointmentStatus.Confirmed, AppointmentStatus.Cancelled, AppointmentStatus.NoShow, AppointmentStatus.Rescheduled },
            [AppointmentStatus.Confirmed]    = new() { AppointmentStatus.CheckedIn, AppointmentStatus.Cancelled, AppointmentStatus.NoShow, AppointmentStatus.Rescheduled },
            [AppointmentStatus.CheckedIn]    = new() { AppointmentStatus.InProgress, AppointmentStatus.NoShow },
            [AppointmentStatus.InProgress]   = new() { AppointmentStatus.Completed, AppointmentStatus.NoShow },
            [AppointmentStatus.Completed]    = new HashSet<AppointmentStatus>(),   // terminal
            [AppointmentStatus.Cancelled]    = new HashSet<AppointmentStatus>(),   // terminal
            [AppointmentStatus.NoShow]       = new HashSet<AppointmentStatus>(),   // terminal
            [AppointmentStatus.Rescheduled]  = new HashSet<AppointmentStatus>(),   // terminal
        };

    public AppointmentService(
        AppDbContext context,
        IAvailabilityService availabilityService,
        INotificationService notificationService,
        ILogger<AppointmentService> logger)
    {
        _context = context;
        _availabilityService = availabilityService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ── Booking ───────────────────────────────────────────────────────────────

    public async Task<AppointmentResponse> BookAppointmentAsync(
        int requestingUserId,
        CreateAppointmentRequest request)
    {
        // 1. Validate patient
        var patient = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.Role == UserRole.Patient && u.Status == UserStatus.Active)
            ?? throw new InvalidOperationException("Active patient not found.");

        // 2. Validate doctor
        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.DoctorId && u.Role == UserRole.Doctor && u.Status == UserStatus.Active)
            ?? throw new InvalidOperationException("Active doctor not found.");

        // 3. Validate department (if provided)
        if (request.DepartmentId.HasValue)
        {
            var deptExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId && d.IsActive);
            if (!deptExists)
                throw new InvalidOperationException("Department not found or inactive.");
        }

        // 4. Slot must be in the future
        if (request.ScheduledStart <= DateTime.UtcNow)
            throw new InvalidOperationException("Appointment must be scheduled in the future.");

        // 5. Daily capacity check
        if (await _availabilityService.IsDailyCapacityReachedAsync(request.DoctorId, request.ScheduledStart))
            throw new InvalidOperationException("Doctor has reached maximum patient capacity for this day.");

        // 6. Double-booking prevention — atomic via EF transaction
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var slotFree = await _availabilityService.IsSlotAvailableAsync(
                request.DoctorId,
                request.ScheduledStart,
                request.EstimatedDurationMinutes);

            if (!slotFree)
                throw new InvalidOperationException(
                    "The requested slot is already booked. Please choose another time.");

            // 7. Generate unique reference number
            string referenceNumber;
            do
            {
                referenceNumber = $"APT-{request.ScheduledStart:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            } while (await _context.Appointments.AnyAsync(a => a.ReferenceNumber == referenceNumber));

            // 8. Generate daily queue number for this doctor
            var date = request.ScheduledStart.Date;
            var maxQueue = await _context.Appointments
                .Where(a => a.DoctorId == request.DoctorId
                         && a.ScheduledStart.Date == date
                         && a.Status != AppointmentStatus.Cancelled
                         && a.Status != AppointmentStatus.Rescheduled)
                .MaxAsync(a => (int?)a.QueueNumber) ?? 0;
            var queueNumber = maxQueue + 1;

            var now = DateTime.UtcNow;

            var appointment = new Appointment
            {
                PatientId               = request.PatientId,
                DoctorId                = request.DoctorId,
                DepartmentId            = request.DepartmentId,
                AppointmentType         = request.AppointmentType,
                ScheduledStart          = request.ScheduledStart,
                EstimatedDurationMinutes = request.EstimatedDurationMinutes,
                Status                  = AppointmentStatus.Scheduled,
                Priority                = request.Priority,
                ReferenceNumber         = referenceNumber,
                QueueNumber             = queueNumber,
                Notes                   = request.Notes?.Trim(),
                EmergencyConfirmed      = false,
                CreatedAt               = now,
                UpdatedAt               = now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // 9. Create queue entry
            var queueEntry = new QueueEntry
            {
                AppointmentId = appointment.Id,
                DoctorId      = request.DoctorId,
                QueueNumber   = queueNumber,
                Status        = QueueEntryStatus.Waiting,
                Priority      = request.Priority
            };
            _context.QueueEntries.Add(queueEntry);

            // 10. Initial status history
            _context.AppointmentStatusHistories.Add(new AppointmentStatusHistory
            {
                AppointmentId = appointment.Id,
                OldStatus     = AppointmentStatus.Scheduled,
                NewStatus     = AppointmentStatus.Scheduled,
                ChangedBy     = requestingUserId,
                ChangedAt     = now,
                Reason        = "Appointment booked."
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Appointment {RefNum} booked for patient {PatientId} with doctor {DoctorId}.",
                referenceNumber, request.PatientId, request.DoctorId);

            // 11. Notify patient
            await _notificationService.CreateAppointmentNotificationAsync(
                request.PatientId,
                appointment.Id,
                "Appointment Confirmed",
                $"Your appointment {referenceNumber} has been scheduled for {request.ScheduledStart:f} UTC. Queue #{queueNumber}.");

            return await GetResponseAsync(appointment.Id)
                   ?? throw new InvalidOperationException("Appointment not found after creation.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<AppointmentResponse>> GetAppointmentsAsync(
        int requestingUserId,
        string requestingUserRole,
        AppointmentQueryFilter filter)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .AsQueryable();

        // Patients can only see their own appointments
        if (requestingUserRole == nameof(UserRole.Patient))
            query = query.Where(a => a.PatientId == requestingUserId);
        else
        {
            if (filter.PatientId.HasValue) query = query.Where(a => a.PatientId == filter.PatientId);
            if (filter.DoctorId.HasValue)  query = query.Where(a => a.DoctorId  == filter.DoctorId);
        }

        if (filter.DepartmentId.HasValue) query = query.Where(a => a.DepartmentId == filter.DepartmentId);
        if (filter.Status.HasValue)        query = query.Where(a => a.Status   == filter.Status);
        if (filter.Priority.HasValue)      query = query.Where(a => a.Priority == filter.Priority);
        if (filter.FromDate.HasValue)      query = query.Where(a => a.ScheduledStart >= filter.FromDate);
        if (filter.ToDate.HasValue)        query = query.Where(a => a.ScheduledStart <= filter.ToDate);

        var appointments = await query.OrderBy(a => a.ScheduledStart).ToListAsync();
        return appointments.Select(MapToResponse).ToList();
    }

    public async Task<AppointmentResponse?> GetAppointmentByIdAsync(
        int id,
        int requestingUserId,
        string requestingUserRole)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null) return null;

        // Patients can only view their own appointments
        if (requestingUserRole == nameof(UserRole.Patient) && appointment.PatientId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorised to view this appointment.");

        return MapToResponse(appointment);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<AppointmentResponse?> UpdateAppointmentAsync(
        int id,
        UpdateAppointmentRequest request,
        int requestingUserId,
        string requestingUserRole)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (IsTerminalStatus(appointment.Status))
            throw new InvalidOperationException($"Cannot update a {appointment.Status} appointment.");

        if (requestingUserRole == nameof(UserRole.Patient) && appointment.PatientId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorised to update this appointment.");

        if (request.AppointmentType.HasValue)     appointment.AppointmentType = request.AppointmentType.Value;
        if (request.EstimatedDurationMinutes.HasValue) appointment.EstimatedDurationMinutes = request.EstimatedDurationMinutes.Value;
        if (request.Notes != null)                appointment.Notes = request.Notes.Trim();

        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponse(appointment);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<AppointmentResponse> CancelAppointmentAsync(
        int id,
        CancelAppointmentRequest request,
        int requestingUserId,
        string requestingUserRole)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .Include(a => a.QueueEntry)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (requestingUserRole == nameof(UserRole.Patient) && appointment.PatientId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorised to cancel this appointment.");

        ValidateTransition(appointment.Status, AppointmentStatus.Cancelled);

        var now = DateTime.UtcNow;
        var oldStatus = appointment.Status;

        appointment.Status          = AppointmentStatus.Cancelled;
        appointment.CancelledReason = request.Reason.Trim();
        appointment.UpdatedAt       = now;

        // Cancel the queue entry too
        if (appointment.QueueEntry != null && !IsTerminalQueueStatus(appointment.QueueEntry.Status))
            appointment.QueueEntry.Status = QueueEntryStatus.NoShow;

        _context.AppointmentStatusHistories.Add(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            OldStatus     = oldStatus,
            NewStatus     = AppointmentStatus.Cancelled,
            ChangedBy     = requestingUserId,
            ChangedAt     = now,
            Reason        = request.Reason
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment {Id} cancelled by user {UserId}.", id, requestingUserId);

        await _notificationService.CreateAppointmentNotificationAsync(
            appointment.PatientId,
            appointment.Id,
            "Appointment Cancelled",
            $"Your appointment {appointment.ReferenceNumber} has been cancelled. Reason: {request.Reason}");

        return MapToResponse(appointment);
    }

    // ── Reschedule ────────────────────────────────────────────────────────────

    public async Task<AppointmentResponse> RescheduleAppointmentAsync(
        int id,
        RescheduleAppointmentRequest request,
        int requestingUserId,
        string requestingUserRole)
    {
        var old = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .Include(a => a.QueueEntry)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (requestingUserRole == nameof(UserRole.Patient) && old.PatientId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorised to reschedule this appointment.");

        ValidateTransition(old.Status, AppointmentStatus.Rescheduled);

        if (request.NewScheduledStart <= DateTime.UtcNow)
            throw new InvalidOperationException("New appointment slot must be in the future.");

        int newDuration = request.NewEstimatedDurationMinutes ?? old.EstimatedDurationMinutes;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var slotFree = await _availabilityService.IsSlotAvailableAsync(
                old.DoctorId,
                request.NewScheduledStart,
                newDuration);

            if (!slotFree)
                throw new InvalidOperationException(
                    "The requested slot is already booked. Please choose another time.");

            var now = DateTime.UtcNow;

            // Mark old as Rescheduled
            var oldStatus = old.Status;
            old.Status    = AppointmentStatus.Rescheduled;
            old.UpdatedAt = now;

            if (old.QueueEntry != null && !IsTerminalQueueStatus(old.QueueEntry.Status))
                old.QueueEntry.Status = QueueEntryStatus.Skipped;

            _context.AppointmentStatusHistories.Add(new AppointmentStatusHistory
            {
                AppointmentId = old.Id,
                OldStatus     = oldStatus,
                NewStatus     = AppointmentStatus.Rescheduled,
                ChangedBy     = requestingUserId,
                ChangedAt     = now,
                Reason        = request.Reason ?? "Rescheduled by user."
            });

            // Generate new reference number
            string referenceNumber;
            do
            {
                referenceNumber = $"APT-{request.NewScheduledStart:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            } while (await _context.Appointments.AnyAsync(a => a.ReferenceNumber == referenceNumber));

            var date        = request.NewScheduledStart.Date;
            var maxQueue    = await _context.Appointments
                .Where(a => a.DoctorId == old.DoctorId
                         && a.ScheduledStart.Date == date
                         && a.Status != AppointmentStatus.Cancelled
                         && a.Status != AppointmentStatus.Rescheduled)
                .MaxAsync(a => (int?)a.QueueNumber) ?? 0;

            var newAppointment = new Appointment
            {
                PatientId                = old.PatientId,
                DoctorId                 = old.DoctorId,
                DepartmentId             = old.DepartmentId,
                AppointmentType          = old.AppointmentType,
                ScheduledStart           = request.NewScheduledStart,
                EstimatedDurationMinutes = newDuration,
                Status                   = AppointmentStatus.Scheduled,
                Priority                 = old.Priority,
                ReferenceNumber          = referenceNumber,
                QueueNumber              = maxQueue + 1,
                Notes                    = old.Notes,
                RescheduledFromId        = old.Id,
                EmergencyConfirmed       = false,
                CreatedAt                = now,
                UpdatedAt                = now
            };

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            _context.QueueEntries.Add(new QueueEntry
            {
                AppointmentId = newAppointment.Id,
                DoctorId      = old.DoctorId,
                QueueNumber   = maxQueue + 1,
                Status        = QueueEntryStatus.Waiting,
                Priority      = old.Priority
            });

            _context.AppointmentStatusHistories.Add(new AppointmentStatusHistory
            {
                AppointmentId = newAppointment.Id,
                OldStatus     = AppointmentStatus.Scheduled,
                NewStatus     = AppointmentStatus.Scheduled,
                ChangedBy     = requestingUserId,
                ChangedAt     = now,
                Reason        = $"Rescheduled from appointment #{old.ReferenceNumber}."
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Appointment {OldRef} rescheduled → {NewRef} by user {UserId}.",
                old.ReferenceNumber, referenceNumber, requestingUserId);

            await _notificationService.CreateAppointmentNotificationAsync(
                old.PatientId,
                newAppointment.Id,
                "Appointment Rescheduled",
                $"Your appointment has been rescheduled to {request.NewScheduledStart:f} UTC. New ref: {referenceNumber}.");

            return await GetResponseAsync(newAppointment.Id)
                   ?? throw new InvalidOperationException("Rescheduled appointment not found.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── Status history ────────────────────────────────────────────────────────

    public async Task<List<AppointmentStatusHistoryResponse>> GetStatusHistoryAsync(int appointmentId)
    {
        return await _context.AppointmentStatusHistories
            .Include(h => h.ChangedByUser)
            .Where(h => h.AppointmentId == appointmentId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new AppointmentStatusHistoryResponse
            {
                Id              = h.Id,
                AppointmentId   = h.AppointmentId,
                OldStatus       = h.OldStatus.ToString(),
                NewStatus       = h.NewStatus.ToString(),
                ChangedBy       = h.ChangedBy,
                ChangedByName   = h.ChangedByUser != null
                    ? $"{h.ChangedByUser.FirstName} {h.ChangedByUser.LastName}"
                    : "Unknown",
                ChangedAt       = h.ChangedAt,
                Reason          = h.Reason
            })
            .ToListAsync();
    }

    // ── Emergency confirmation ────────────────────────────────────────────────

    public async Task<AppointmentResponse> ConfirmEmergencyAsync(int id, int staffUserId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (appointment.Priority != AppointmentPriority.Emergency)
            throw new InvalidOperationException("Appointment is not marked as Emergency priority.");

        if (appointment.EmergencyConfirmed)
            throw new InvalidOperationException("Emergency already confirmed.");

        appointment.EmergencyConfirmed = true;
        appointment.Status             = AppointmentStatus.Confirmed;
        appointment.UpdatedAt          = DateTime.UtcNow;

        _context.AppointmentStatusHistories.Add(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            OldStatus     = AppointmentStatus.Scheduled,
            NewStatus     = AppointmentStatus.Confirmed,
            ChangedBy     = staffUserId,
            ChangedAt     = DateTime.UtcNow,
            Reason        = "Emergency appointment confirmed by staff."
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Emergency appointment {Id} confirmed by staff {StaffId}.", id, staffUserId);

        await _notificationService.CreateAppointmentNotificationAsync(
            appointment.PatientId,
            appointment.Id,
            "Emergency Appointment Confirmed",
            $"Your emergency appointment {appointment.ReferenceNumber} has been confirmed by hospital staff.");

        return MapToResponse(appointment);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AppointmentResponse?> GetResponseAsync(int id)
    {
        var a = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id);
        return a == null ? null : MapToResponse(a);
    }

    private static AppointmentResponse MapToResponse(Appointment a) => new()
    {
        Id                       = a.Id,
        ReferenceNumber          = a.ReferenceNumber,
        PatientId                = a.PatientId,
        PatientName              = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : string.Empty,
        PatientEmail             = a.Patient?.Email ?? string.Empty,
        DoctorId                 = a.DoctorId,
        DoctorName               = a.Doctor != null ? $"{a.Doctor.FirstName} {a.Doctor.LastName}" : string.Empty,
        DepartmentId             = a.DepartmentId,
        DepartmentName           = a.Department?.Name,
        AppointmentType          = a.AppointmentType.ToString(),
        ScheduledStart           = a.ScheduledStart,
        EstimatedDurationMinutes = a.EstimatedDurationMinutes,
        Status                   = a.Status.ToString(),
        Priority                 = a.Priority.ToString(),
        QueueNumber              = a.QueueNumber,
        Notes                    = a.Notes,
        CancelledReason          = a.CancelledReason,
        RescheduledFromId        = a.RescheduledFromId,
        EmergencyConfirmed       = a.EmergencyConfirmed,
        CreatedAt                = a.CreatedAt,
        UpdatedAt                = a.UpdatedAt
    };

    private static void ValidateTransition(AppointmentStatus current, AppointmentStatus next)
    {
        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidOperationException(
                $"Cannot transition appointment from {current} to {next}.");
    }

    private static bool IsTerminalStatus(AppointmentStatus status) =>
        status is AppointmentStatus.Completed
               or AppointmentStatus.Cancelled
               or AppointmentStatus.NoShow
               or AppointmentStatus.Rescheduled;

    private static bool IsTerminalQueueStatus(QueueEntryStatus status) =>
        status is QueueEntryStatus.Completed
               or QueueEntryStatus.NoShow
               or QueueEntryStatus.Skipped;
}
