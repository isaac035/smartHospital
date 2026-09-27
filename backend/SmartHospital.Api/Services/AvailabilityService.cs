using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Availability;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _context;

    public AvailabilityService(AppDbContext context)
    {
        _context = context;
    }

    // ── Free-slot computation ─────────────────────────────────────────────────

    public async Task<List<AvailableSlotResponse>> GetAvailableSlotsAsync(
        int? doctorId,
        int? departmentId,
        DateTime date)
    {
        // Models.DayOfWeek uses Monday=1..Sunday=7, unlike System.DayOfWeek's Sunday=0..Saturday=6.
        var systemDayOfWeek = (int)date.DayOfWeek;
        var dayOfWeek = (Models.DayOfWeek)(systemDayOfWeek == 0 ? 7 : systemDayOfWeek);
        var dateOnly  = date.Date;

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
                     && s.Doctor.UserId != null);

        if (doctorId.HasValue)     schedulesQuery = schedulesQuery.Where(s => s.Doctor!.UserId     == doctorId);
        if (departmentId.HasValue) schedulesQuery = schedulesQuery.Where(s => s.Doctor!.DepartmentId == departmentId);

        var schedules = await schedulesQuery.ToListAsync();

        // Load all appointments for that day that are still active (not cancelled/rescheduled)
        var existingAppointments = await _context.Appointments
            .Where(a => a.ScheduledStart.Date == dateOnly
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.Rescheduled)
            .ToListAsync();

        var result = new List<AvailableSlotResponse>();

        foreach (var schedule in schedules)
        {
            var slotStart = dateOnly + schedule.StartTime.ToTimeSpan();
            var schedEnd  = dateOnly + schedule.EndTime.ToTimeSpan();
            int duration  = schedule.SlotDurationMinutes;

            while (slotStart.AddMinutes(duration) <= schedEnd)
            {
                var slotEnd = slotStart.AddMinutes(duration);

                // Check overlap with any existing appointment for this doctor
                var scheduleDoctorUserId = schedule.Doctor!.UserId!.Value;
                bool overlaps = existingAppointments.Any(a =>
                    a.DoctorId == scheduleDoctorUserId &&
                    a.ScheduledStart < slotEnd &&
                    a.ScheduledStart.AddMinutes(a.EstimatedDurationMinutes) > slotStart);

                if (!overlaps && slotStart > DateTime.UtcNow)
                {
                    result.Add(new AvailableSlotResponse
                    {
                        DoctorId       = scheduleDoctorUserId,
                        DoctorName     = $"{schedule.Doctor.FirstName} {schedule.Doctor.LastName}",
                        DepartmentId   = schedule.Doctor.DepartmentId,
                        DepartmentName = schedule.Doctor.Department?.Name,
                        SlotStart      = slotStart,
                        SlotEnd        = slotEnd,
                        DurationMinutes = duration
                    });
                }

                slotStart = slotEnd;
            }
        }

        return result.OrderBy(s => s.DoctorId).ThenBy(s => s.SlotStart).ToList();
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
        // Get the schedule for this day to know MaxPatientsPerDay.
        // doctorId here is a Users.Id (Appointment convention) — bridge via Doctor.UserId.
        var systemDayOfWeek = (int)date.DayOfWeek;
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
                          && a.ScheduledStart.Date == date.Date
                          && a.Status != AppointmentStatus.Cancelled
                          && a.Status != AppointmentStatus.Rescheduled);

        return activeCount >= schedule.MaxPatientsPerDay;
    }
}
