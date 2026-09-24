using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Queues;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class QueueService : IQueueService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<QueueService> _logger;

    public QueueService(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<QueueService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ── Doctor queue (ordered: Emergency > Urgent > Normal, then FIFO) ────────

    public async Task<List<QueueEntryResponse>> GetQueueForDoctorAsync(int doctorId)
    {
        var entries = await GetActiveQueueQuery(doctorId).ToListAsync();
        return ProjectWithPosition(entries);
    }

    public async Task<QueueStatusResponse> GetQueueStatusForDoctorAsync(int doctorId)
    {
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == doctorId)
            ?? throw new KeyNotFoundException("Doctor not found.");

        var active = await GetActiveQueueQuery(doctorId).ToListAsync();

        var inProgress = active.FirstOrDefault(e => e.Status == QueueEntryStatus.InProgress);
        var projected  = ProjectWithPosition(active);

        return new QueueStatusResponse
        {
            DoctorId                         = doctorId,
            DoctorName                       = $"{doctor.FirstName} {doctor.LastName}",
            CurrentlyServingQueueEntryId     = inProgress?.Id,
            CurrentlyServingQueueNumber      = inProgress?.QueueNumber,
            CurrentlyServingPatientName      = inProgress?.Appointment?.Patient != null
                ? $"{inProgress.Appointment.Patient.FirstName} {inProgress.Appointment.Patient.LastName}"
                : null,
            TotalWaiting = active.Count(e => e.Status == QueueEntryStatus.Waiting),
            Queue        = projected
        };
    }

    public async Task<QueueEntryResponse?> GetQueueEntryStatusAsync(int queueEntryId)
    {
        var allForDoctor = await GetAllActiveEntriesForEntry(queueEntryId);
        var entry = allForDoctor.FirstOrDefault(e => e.Id == queueEntryId);
        if (entry == null) return null;

        var ordered  = OrderByPriority(allForDoctor);
        var position = ordered.ToList().FindIndex(e => e.Id == queueEntryId) + 1;

        return MapToResponse(entry, position);
    }

    // ── Call next ─────────────────────────────────────────────────────────────

    public async Task<QueueEntryResponse> CallQueueEntryAsync(int queueEntryId, int staffUserId)
    {
        var entry = await _context.QueueEntries
            .Include(q => q.Appointment).ThenInclude(a => a!.Patient)
            .FirstOrDefaultAsync(q => q.Id == queueEntryId)
            ?? throw new KeyNotFoundException("Queue entry not found.");

        if (entry.Status != QueueEntryStatus.Waiting)
            throw new InvalidOperationException($"Cannot call a queue entry with status {entry.Status}.");

        entry.Status   = QueueEntryStatus.Called;
        entry.CalledAt = DateTime.UtcNow;

        if (entry.Appointment != null)
        {
            entry.Appointment.Status    = AppointmentStatus.Confirmed;
            entry.Appointment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Recalculate estimated wait for all waiting entries behind this one
        await RecalculateWaitTimesAsync(entry.DoctorId);

        _logger.LogInformation("Queue entry {Id} called by staff {StaffId}.", queueEntryId, staffUserId);

        if (entry.Appointment?.PatientId != null)
        {
            await _notificationService.CreateAppointmentNotificationAsync(
                entry.Appointment.PatientId,
                entry.AppointmentId,
                "You Are Being Called",
                $"Please proceed to the doctor's room. Queue #{entry.QueueNumber}.");
        }

        return MapToResponse(entry, 0);
    }

    // ── Check-in ──────────────────────────────────────────────────────────────

    public async Task<QueueEntryResponse> CheckInAsync(int appointmentId, int staffUserId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed
            or AppointmentStatus.NoShow or AppointmentStatus.Rescheduled)
            throw new InvalidOperationException($"Cannot check in a {appointment.Status} appointment.");

        var entry = await _context.QueueEntries
            .Include(q => q.Appointment).ThenInclude(a => a!.Patient)
            .FirstOrDefaultAsync(q => q.AppointmentId == appointmentId)
            ?? throw new KeyNotFoundException("Queue entry not found for this appointment.");

        if (entry.Status == QueueEntryStatus.Completed || entry.Status == QueueEntryStatus.NoShow)
            throw new InvalidOperationException("Patient has already been processed.");

        var now = DateTime.UtcNow;

        entry.Status      = QueueEntryStatus.Waiting;
        entry.CheckedInAt = now;

        appointment.Status    = AppointmentStatus.CheckedIn;
        appointment.UpdatedAt = now;

        await _context.SaveChangesAsync();
        await RecalculateWaitTimesAsync(entry.DoctorId);

        _logger.LogInformation("Patient checked in for appointment {Id} by staff {StaffId}.", appointmentId, staffUserId);

        return MapToResponse(entry, 0);
    }

    // ── No-show ───────────────────────────────────────────────────────────────

    public async Task<QueueEntryResponse> MarkNoShowAsync(int queueEntryId, int staffUserId)
    {
        var entry = await _context.QueueEntries
            .Include(q => q.Appointment)
            .FirstOrDefaultAsync(q => q.Id == queueEntryId)
            ?? throw new KeyNotFoundException("Queue entry not found.");

        if (entry.Status == QueueEntryStatus.Completed)
            throw new InvalidOperationException("Cannot mark a completed entry as no-show.");

        entry.Status      = QueueEntryStatus.NoShow;
        entry.CompletedAt = DateTime.UtcNow;

        if (entry.Appointment != null)
        {
            entry.Appointment.Status    = AppointmentStatus.NoShow;
            entry.Appointment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await RecalculateWaitTimesAsync(entry.DoctorId);

        _logger.LogInformation("Queue entry {Id} marked as NoShow by staff {StaffId}.", queueEntryId, staffUserId);

        return MapToResponse(entry, 0);
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    public async Task<QueueEntryResponse> MarkCompletedAsync(int queueEntryId, int staffUserId)
    {
        var entry = await _context.QueueEntries
            .Include(q => q.Appointment)
            .FirstOrDefaultAsync(q => q.Id == queueEntryId)
            ?? throw new KeyNotFoundException("Queue entry not found.");

        if (entry.Status == QueueEntryStatus.Completed)
            throw new InvalidOperationException("Queue entry is already completed.");

        var now = DateTime.UtcNow;
        entry.Status      = QueueEntryStatus.Completed;
        entry.CompletedAt = now;

        if (entry.Appointment != null)
        {
            entry.Appointment.Status    = AppointmentStatus.Completed;
            entry.Appointment.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
        await RecalculateWaitTimesAsync(entry.DoctorId);

        _logger.LogInformation("Queue entry {Id} completed by staff {StaffId}.", queueEntryId, staffUserId);

        return MapToResponse(entry, 0);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns active queue entries for a doctor ordered by
    /// Priority descending (Emergency=3 first) then QueueNumber ascending (FIFO).
    /// </summary>
    private IQueryable<QueueEntry> GetActiveQueueQuery(int doctorId) =>
        _context.QueueEntries
            .Include(q => q.Appointment).ThenInclude(a => a!.Patient)
            .Include(q => q.Doctor)
            .Where(q => q.DoctorId == doctorId
                     && (q.Status == QueueEntryStatus.Waiting
                      || q.Status == QueueEntryStatus.Called
                      || q.Status == QueueEntryStatus.InProgress))
            .OrderByDescending(q => (int)q.Priority)
            .ThenBy(q => q.QueueNumber);

    private async Task<List<QueueEntry>> GetAllActiveEntriesForEntry(int queueEntryId)
    {
        var entry = await _context.QueueEntries.FindAsync(queueEntryId);
        if (entry == null) return new List<QueueEntry>();
        return await GetActiveQueueQuery(entry.DoctorId).ToListAsync();
    }

    private static IEnumerable<QueueEntry> OrderByPriority(IEnumerable<QueueEntry> entries) =>
        entries.OrderByDescending(e => (int)e.Priority).ThenBy(e => e.QueueNumber);

    /// <summary>
    /// Recalculates EstimatedWaitMinutes for all Waiting entries for a doctor.
    /// Estimation: each waiting entry before the current one adds its appointment's duration.
    /// </summary>
    private async Task RecalculateWaitTimesAsync(int doctorId)
    {
        var waiting = await _context.QueueEntries
            .Include(q => q.Appointment)
            .Where(q => q.DoctorId == doctorId && q.Status == QueueEntryStatus.Waiting)
            .OrderByDescending(q => (int)q.Priority)
            .ThenBy(q => q.QueueNumber)
            .ToListAsync();

        int cumulativeMinutes = 0;
        foreach (var entry in waiting)
        {
            entry.EstimatedWaitMinutes = cumulativeMinutes;
            cumulativeMinutes += entry.Appointment?.EstimatedDurationMinutes ?? 30;
        }

        await _context.SaveChangesAsync();
    }

    private static List<QueueEntryResponse> ProjectWithPosition(IEnumerable<QueueEntry> entries)
    {
        var list     = entries.ToList();
        var result   = new List<QueueEntryResponse>();
        int position = 1;
        foreach (var e in list)
        {
            result.Add(MapToResponse(e, position));
            position++;
        }
        return result;
    }

    private static QueueEntryResponse MapToResponse(QueueEntry e, int position) => new()
    {
        Id                         = e.Id,
        AppointmentId              = e.AppointmentId,
        AppointmentReferenceNumber = e.Appointment?.ReferenceNumber ?? string.Empty,
        DoctorId                   = e.DoctorId,
        DoctorName                 = e.Doctor != null ? $"{e.Doctor.FirstName} {e.Doctor.LastName}" : string.Empty,
        PatientId                  = e.Appointment?.PatientId ?? 0,
        PatientName                = e.Appointment?.Patient != null
            ? $"{e.Appointment.Patient.FirstName} {e.Appointment.Patient.LastName}"
            : string.Empty,
        QueueNumber                = e.QueueNumber,
        Status                     = e.Status.ToString(),
        Priority                   = e.Priority.ToString(),
        EstimatedWaitMinutes       = e.EstimatedWaitMinutes,
        CheckedInAt                = e.CheckedInAt,
        CalledAt                   = e.CalledAt,
        CompletedAt                = e.CompletedAt,
        QueuePosition              = position
    };
}
