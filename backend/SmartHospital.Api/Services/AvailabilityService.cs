using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Availability;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _context;
    private static readonly TimeZoneInfo HospitalTimeZone = ResolveHospitalTimeZone();

    public AvailabilityService(AppDbContext context)
    {
        _context = context;
    }

    // ── Free-slot computation ─────────────────────────────────────────────────

    public async Task<List<AvailableSlotResponse>> GetAvailableSlotsAsync(
        int? doctorId,
        int? departmentId,
        DateTime date,
        int? doctorProfileId = null,
        bool includePast = false)
    {
        // Models.DayOfWeek uses Monday=1..Sunday=7, unlike System.DayOfWeek's Sunday=0..Saturday=6.
        var systemDayOfWeek = (int)date.DayOfWeek;
        var dayOfWeek = (Models.DayOfWeek)(systemDayOfWeek == 0 ? 7 : systemDayOfWeek);
        // Schedule dates/times are hospital-local wall time; appointment instants are UTC.
        var dateOnlyValue = DateOnly.FromDateTime(date);
        var localStartOfDay = dateOnlyValue.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEndOfDay = localStartOfDay.AddDays(1);
        var utcStartOfDay = TimeZoneInfo.ConvertTimeToUtc(localStartOfDay, HospitalTimeZone);
        var utcEndOfDay = TimeZoneInfo.ConvertTimeToUtc(localEndOfDay, HospitalTimeZone);

        // Load active schedules matching the filters.
        // NOTE: DoctorSchedule.DoctorId references Doctors.Id (the Doctor & Clinical
        // Schedule Management module's own profile table), whereas Appointment.DoctorId
        // references Users.Id directly. Only doctors with a linked User account
        // (Doctor.UserId) can be booked through the Appointment module, so we bridge
        // through that link and exclude schedules for doctors without one.
        var schedulesQuery = _context.DoctorSchedules
            .Include(s => s.Doctor)
                .ThenInclude(d => d!.Department)
            .Where(s => s.Status == ScheduleStatus.Active
                     && s.DayOfWeek == dayOfWeek
                     && s.Doctor != null
                     && s.Doctor.Status == DoctorStatus.Active
                     && s.Doctor.UserId != null);

        // A one-date session overrides the doctor's weekly template for that
        // date. If no one-date session exists, use the recurring weekly rows.
        schedulesQuery = schedulesQuery.Where(s =>
            s.SpecificDate == dateOnlyValue ||
            (s.SpecificDate == null && !_context.DoctorSchedules.Any(dateSchedule =>
                dateSchedule.DoctorId == s.DoctorId &&
                dateSchedule.SpecificDate == dateOnlyValue &&
                dateSchedule.Status == ScheduleStatus.Active)));

        if (doctorProfileId.HasValue)
            schedulesQuery = schedulesQuery.Where(s => s.DoctorId == doctorProfileId.Value);
        if (doctorId.HasValue)
            schedulesQuery = schedulesQuery.Where(s => s.Doctor!.UserId == doctorId.Value);
        if (departmentId.HasValue) schedulesQuery = schedulesQuery.Where(s => s.Doctor!.DepartmentId == departmentId);

        var schedules = await schedulesQuery.ToListAsync();

        // Approved leave blocks all schedules for that doctor on the selected date.
        var doctorIds = schedules.Select(s => s.DoctorId).Distinct().ToList();
        var doctorsOnLeave = await _context.DoctorLeaves
            .Where(l => doctorIds.Contains(l.DoctorId)
                     && l.Status == LeaveStatus.Approved
                     && l.StartDate <= dateOnlyValue
                     && l.EndDate >= dateOnlyValue)
            .Select(l => l.DoctorId)
            .Distinct()
            .ToListAsync();
        var doctorsOnLeaveSet = doctorsOnLeave.ToHashSet();

        // Load all appointments for that day that are still active (not cancelled/rescheduled)
        var existingAppointments = await _context.Appointments
            .Where(a => a.ScheduledStart >= utcStartOfDay && a.ScheduledStart < utcEndOfDay
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.Rescheduled)
            .ToListAsync();

        var result = new List<AvailableSlotResponse>();
        var nowUtc = DateTime.UtcNow;

        foreach (var schedule in schedules)
        {
            if (doctorsOnLeaveSet.Contains(schedule.DoctorId))
                continue;

            var slotStartLocal = dateOnlyValue.ToDateTime(schedule.StartTime, DateTimeKind.Unspecified);
            var schedEndLocal = dateOnlyValue.ToDateTime(schedule.EndTime, DateTimeKind.Unspecified);
            int duration  = schedule.SlotDurationMinutes;

            while (slotStartLocal.AddMinutes(duration) <= schedEndLocal)
            {
                var slotEndLocal = slotStartLocal.AddMinutes(duration);
                var slotStart = TimeZoneInfo.ConvertTimeToUtc(slotStartLocal, HospitalTimeZone);
                var slotEnd = TimeZoneInfo.ConvertTimeToUtc(slotEndLocal, HospitalTimeZone);

                // Check overlap with any existing appointment for this doctor
                var scheduleDoctorUserId = schedule.Doctor!.UserId!.Value;
                bool overlaps = existingAppointments.Any(a =>
                    a.DoctorId == scheduleDoctorUserId &&
                    a.ScheduledStart < slotEnd &&
                    a.ScheduledStart.AddMinutes(a.EstimatedDurationMinutes) > slotStart);

                if (includePast || slotStart > nowUtc)
                {
                    result.Add(new AvailableSlotResponse
                    {
                        DoctorId       = scheduleDoctorUserId,
                        DoctorProfileId = schedule.DoctorId,
                        DoctorName     = $"{schedule.Doctor.FirstName} {schedule.Doctor.LastName}",
                        DepartmentId   = schedule.Doctor.DepartmentId,
                        DepartmentName = schedule.Doctor.Department?.Name,
                        SlotStart      = slotStart,
                        SlotEnd        = slotEnd,
                        DurationMinutes = duration,
                        Status = overlaps ? "Booked" : "Available"
                    });
                }

                slotStartLocal = slotEndLocal;
            }
        }

        // Historical/imported data can contain overlapping schedule rows even though
        // ScheduleService prevents creating them. A slot is identified by profile + start.
        return result
            .GroupBy(s => new { s.DoctorId, s.SlotStart })
            .Select(group => group.First())
            .OrderBy(s => s.DoctorId)
            .ThenBy(s => s.SlotStart)
            .ToList();
    }

    private static TimeZoneInfo ResolveHospitalTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"); }
    }

    // ── Conflict detection ────────────────────────────────────────────────────

    public async Task<bool> IsSlotAvailableAsync(
        int doctorId,
        DateTime scheduledStart,
        int durationMinutes,
        int? excludeAppointmentId = null)
    {
        var slotEnd = scheduledStart.AddMinutes(durationMinutes);

        var query = _context.Appointments
            .Where(a => a.DoctorId == doctorId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.Rescheduled
                     && a.ScheduledStart < slotEnd
                     && a.ScheduledStart.AddMinutes(a.EstimatedDurationMinutes) > scheduledStart);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return !await query.AnyAsync();
    }

    // ── Rescheduling suggestions ──────────────────────────────────────────────

    public async Task<List<RescheduleSuggestionResponse>> GetRescheduleSuggestionsAsync(
        int doctorId,
        DateTime preferredDate,
        int durationMinutes,
        int count = 5)
    {
        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId)
            ?? throw new KeyNotFoundException("Doctor not found.");

        var suggestions = new List<RescheduleSuggestionResponse>();
        var scanDate    = preferredDate.Date;

        // Scan up to 30 days forward to find enough suggestions
        for (int day = 0; day < 30 && suggestions.Count < count; day++)
        {
            var currentDate = scanDate.AddDays(day);
            var slots       = await GetAvailableSlotsAsync(doctorId, null, currentDate);

            foreach (var slot in slots)
            {
                if (slot.DurationMinutes >= durationMinutes && suggestions.Count < count)
                {
                    suggestions.Add(new RescheduleSuggestionResponse
                    {
                        DoctorId       = doctorId,
                        DoctorName     = $"{doctor.FirstName} {doctor.LastName}",
                        SuggestedStart = slot.SlotStart,
                        SuggestedEnd   = slot.SlotStart.AddMinutes(durationMinutes),
                        DurationMinutes = durationMinutes
                    });
                }
            }
        }

        return suggestions;
    }

    // ── Daily capacity ────────────────────────────────────────────────────────

    public async Task<bool> IsDailyCapacityReachedAsync(int doctorId, DateTime date)
    {
        var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(date, DateTimeKind.Utc), HospitalTimeZone);
        var localDate = DateOnly.FromDateTime(localDateTime);
        var localDayStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utcDayStart = TimeZoneInfo.ConvertTimeToUtc(localDayStart, HospitalTimeZone);
        var utcDayEnd = TimeZoneInfo.ConvertTimeToUtc(localDayStart.AddDays(1), HospitalTimeZone);
        // Get the schedule for this day to know MaxPatientsPerDay.
        // doctorId here is a Users.Id (Appointment convention) — bridge via Doctor.UserId.
        var systemDayOfWeek = (int)localDateTime.DayOfWeek;
        var dayOfWeek = (Models.DayOfWeek)(systemDayOfWeek == 0 ? 7 : systemDayOfWeek);
        var schedule = await _context.DoctorSchedules
            .Include(s => s.Doctor)
            .Where(s => s.Doctor != null
                     && s.Doctor.UserId == doctorId
                     && s.DayOfWeek == dayOfWeek
                     && s.Status == ScheduleStatus.Active)
            .FirstOrDefaultAsync();

        if (schedule == null) return false; // No schedule = no capacity limit enforced here

        var activeCount = await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId
                          && a.ScheduledStart >= utcDayStart && a.ScheduledStart < utcDayEnd
                          && a.Status != AppointmentStatus.Cancelled
                          && a.Status != AppointmentStatus.Rescheduled);

        return activeCount >= schedule.MaxPatientsPerDay;
    }
}
