using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Schedules;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class ScheduleService : IScheduleService
{
    private readonly AppDbContext _context;

    public ScheduleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScheduleResponse>> GetAllAsync(ScheduleFilterRequest filter)
    {
        var query = _context.DoctorSchedules.AsNoTracking();

        if (filter.DoctorId.HasValue)
        {
            query = query.Where(s => s.DoctorId == filter.DoctorId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(s => s.Doctor!.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.DayOfWeek))
        {
            if (!Enum.TryParse<Models.DayOfWeek>(filter.DayOfWeek, true, out var dayOfWeek))
            {
                throw new InvalidOperationException("Invalid day of week.");
            }

            query = query.Where(s => s.DayOfWeek == dayOfWeek);
        }

        if (filter.SpecificDate.HasValue)
        {
            var date = filter.SpecificDate.Value;
            var systemDayOfWeek = (int)date.DayOfWeek;
            var dateDayOfWeek = (Models.DayOfWeek)(systemDayOfWeek == 0 ? 7 : systemDayOfWeek);
            query = query.Where(s => s.SpecificDate == date ||
                (s.SpecificDate == null && s.DayOfWeek == dateDayOfWeek));
        }
        else
        {
            // The weekly availability calendar lists recurring sessions only.
            query = query.Where(s => s.SpecificDate == null);
        }

        return await query
            .Where(s => s.Status == ScheduleStatus.Active)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new ScheduleResponse
            {
                Id = s.Id,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor!.FirstName + " " + s.Doctor.LastName,
                ConsultationTypeId = s.ConsultationTypeId,
                ConsultationTypeName = s.ConsultationType!.Name,
                DayOfWeek = s.DayOfWeek.ToString(),
                SpecificDate = s.SpecificDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SlotDurationMinutes = s.SlotDurationMinutes,
                Status = s.Status.ToString()
            })
            .ToListAsync();
    }

    public async Task<ScheduleResponse?> GetByIdAsync(int id)
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ScheduleResponse
            {
                Id = s.Id,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor!.FirstName + " " + s.Doctor.LastName,
                ConsultationTypeId = s.ConsultationTypeId,
                ConsultationTypeName = s.ConsultationType!.Name,
                DayOfWeek = s.DayOfWeek.ToString(),
                SpecificDate = s.SpecificDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SlotDurationMinutes = s.SlotDurationMinutes,
                Status = s.Status.ToString()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ScheduleResponse> CreateAsync(CreateScheduleRequest request)
    {
        if (!Enum.TryParse<Models.DayOfWeek>(request.DayOfWeek, true, out var dayOfWeek))
        {
            throw new InvalidOperationException("Invalid day of week.");
        }

        if (request.EndTime <= request.StartTime)
        {
            throw new InvalidOperationException("End time must be after start time.");
        }

        if (request.SpecificDate.HasValue)
        {
            var systemDayOfWeek = (int)request.SpecificDate.Value.DayOfWeek;
            var dateDayOfWeek = (Models.DayOfWeek)(systemDayOfWeek == 0 ? 7 : systemDayOfWeek);
            if (dateDayOfWeek != dayOfWeek)
                throw new InvalidOperationException("The selected day does not match the specific date.");
        }

        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == request.DoctorId);

        if (!doctorExists)
        {
            throw new InvalidOperationException("The specified doctor does not exist.");
        }

        if (request.SpecificDate.HasValue)
        {
            var specificDate = request.SpecificDate.Value;
            if (await _context.DoctorLeaves.AnyAsync(l => l.DoctorId == request.DoctorId
                    && l.Status == LeaveStatus.Approved
                    && l.StartDate <= specificDate && l.EndDate >= specificDate))
                throw new InvalidOperationException("The doctor is on approved leave for this date.");
        }

        var consultationType = await _context.ConsultationTypes
            .FirstOrDefaultAsync(c => c.Id == request.ConsultationTypeId);

        if (consultationType == null)
        {
            throw new InvalidOperationException("The specified consultation type does not exist.");
        }

        var hasOverlap = await _context.DoctorSchedules
            .Where(s => s.DoctorId == request.DoctorId && s.Status == ScheduleStatus.Active)
            .Where(s => request.SpecificDate.HasValue
                ? s.SpecificDate == request.SpecificDate.Value
                : s.SpecificDate == null && s.DayOfWeek == dayOfWeek)
            .AnyAsync(s => request.StartTime < s.EndTime && request.EndTime > s.StartTime);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "This doctor already has an overlapping availability session on this day."
            );
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = request.DoctorId,
            ConsultationTypeId = request.ConsultationTypeId,
            DayOfWeek = dayOfWeek,
            SpecificDate = request.SpecificDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SlotDurationMinutes = consultationType.DurationMinutes,
            Status = ScheduleStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DoctorSchedules.Add(schedule);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(schedule.Id)
            ?? throw new InvalidOperationException("Failed to create schedule.");
    }

    public async Task<ScheduleResponse?> UpdateAsync(
        int id,
        UpdateScheduleRequest request)
    {
        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return null;
        }

        if (!Enum.TryParse<Models.DayOfWeek>(request.DayOfWeek, true, out var dayOfWeek))
        {
            throw new InvalidOperationException("Invalid day of week.");
        }

        if (request.EndTime <= request.StartTime)
        {
            throw new InvalidOperationException("End time must be after start time.");
        }

        if (schedule.SpecificDate.HasValue)
        {
            var systemDayOfWeek = (int)schedule.SpecificDate.Value.DayOfWeek;
            var dateDayOfWeek = (Models.DayOfWeek)(systemDayOfWeek == 0 ? 7 : systemDayOfWeek);
            if (dateDayOfWeek != dayOfWeek)
                throw new InvalidOperationException("The selected day does not match the specific date.");
        }

        var consultationType = await _context.ConsultationTypes
            .FirstOrDefaultAsync(c => c.Id == request.ConsultationTypeId);

        if (consultationType == null)
        {
            throw new InvalidOperationException("The specified consultation type does not exist.");
        }

        var hasOverlap = await _context.DoctorSchedules
            .Where(s => s.Id != id && s.DoctorId == schedule.DoctorId && s.Status == ScheduleStatus.Active)
            .Where(s => schedule.SpecificDate.HasValue
                ? s.SpecificDate == schedule.SpecificDate.Value
                : s.SpecificDate == null && s.DayOfWeek == dayOfWeek)
            .AnyAsync(s => request.StartTime < s.EndTime && request.EndTime > s.StartTime);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "This doctor already has an overlapping availability session on this day."
            );
        }

        schedule.ConsultationTypeId = request.ConsultationTypeId;
        schedule.SlotDurationMinutes = consultationType.DurationMinutes;
        schedule.DayOfWeek = dayOfWeek;
        schedule.StartTime = request.StartTime;
        schedule.EndTime = request.EndTime;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> RemoveAsync(int id)
    {
        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return false;
        }

        schedule.Status = ScheduleStatus.Inactive;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ScheduleResponse?> AddSlotsAsync(int scheduleId, DateOnly date, int additionalSlotCount)
    {
        if (additionalSlotCount is < 1 or > 100)
            throw new InvalidOperationException("Add between 1 and 100 slots at a time.");

        var zone = ResolveHospitalTimeZone();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        if (date < today)
            throw new InvalidOperationException("Slots can only be added to today or a future date.");

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
            : null;
        try
        {
            var source = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .Include(s => s.ConsultationType)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Status == ScheduleStatus.Active);
            if (source == null) return null;

            if (source.Doctor?.Status != DoctorStatus.Active)
                throw new InvalidOperationException("Slots cannot be added because this doctor is not active.");

            var dayOfWeek = ToScheduleDay(date);
            if (source.DayOfWeek != dayOfWeek || (source.SpecificDate.HasValue && source.SpecificDate.Value != date))
                throw new InvalidOperationException("This schedule does not apply to the selected date.");

            var onLeave = await _context.DoctorLeaves.AnyAsync(l => l.DoctorId == source.DoctorId
                && l.Status == LeaveStatus.Approved && l.StartDate <= date && l.EndDate >= date);
            if (onLeave)
                throw new InvalidOperationException("Slots cannot be added while the doctor is on approved leave.");

            var dateSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == source.DoctorId && s.SpecificDate == date && s.Status == ScheduleStatus.Active)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            DoctorSchedule selectedDateSchedule;
            if (source.SpecificDate == date)
            {
                selectedDateSchedule = source;
            }
            else
            {
                if (dateSchedules.Count > 0)
                    throw new InvalidOperationException("This recurring schedule is overridden by date-specific availability.");

                // A date-specific schedule overrides every weekly schedule for that day.
                // Snapshot all of that day's active sessions so extending one does not hide the others.
                var recurring = await _context.DoctorSchedules
                    .Include(s => s.ConsultationType)
                    .Where(s => s.DoctorId == source.DoctorId && s.SpecificDate == null
                        && s.DayOfWeek == dayOfWeek && s.Status == ScheduleStatus.Active)
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();

                selectedDateSchedule = null!;
                foreach (var weekly in recurring)
                {
                    var endTime = weekly.Id == source.Id
                        ? ExtendEndTime(weekly.EndTime, weekly.SlotDurationMinutes, additionalSlotCount)
                        : weekly.EndTime;
                    var created = await CreateAsync(new CreateScheduleRequest
                    {
                        DoctorId = weekly.DoctorId,
                        ConsultationTypeId = weekly.ConsultationTypeId,
                        DayOfWeek = dayOfWeek.ToString(),
                        SpecificDate = date,
                        StartTime = weekly.StartTime,
                        EndTime = endTime
                    });
                    if (weekly.Id == source.Id)
                        selectedDateSchedule = await _context.DoctorSchedules.FirstAsync(s => s.Id == created.Id);
                }
            }

            if (source.SpecificDate == date)
            {
                var newEnd = ExtendEndTime(selectedDateSchedule.EndTime, selectedDateSchedule.SlotDurationMinutes, additionalSlotCount);
                await EnsureNoAppointmentsInExtensionAsync(selectedDateSchedule, date, newEnd);
                await UpdateAsync(selectedDateSchedule.Id, new UpdateScheduleRequest
                {
                    ConsultationTypeId = selectedDateSchedule.ConsultationTypeId,
                    DayOfWeek = dayOfWeek.ToString(),
                    StartTime = selectedDateSchedule.StartTime,
                    EndTime = newEnd
                });
            }
            else
            {
                // Validate the added segment before committing the newly snapshotted one-day schedules.
                var oldEnd = source.EndTime;
                var addedSchedule = selectedDateSchedule;
                // CreateAsync saved the expanded row already; compare only the appended interval.
                await EnsureNoAppointmentsInExtensionAsync(addedSchedule, date, addedSchedule.EndTime, oldEnd);
            }

            if (transaction != null) await transaction.CommitAsync();
            return await GetByIdAsync(selectedDateSchedule.Id);
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task EnsureNoAppointmentsInExtensionAsync(
        DoctorSchedule schedule, DateOnly date, TimeOnly newEnd, TimeOnly? previousEnd = null)
    {
        var startLocal = date.ToDateTime(previousEnd ?? schedule.EndTime, DateTimeKind.Unspecified);
        var endLocal = date.ToDateTime(newEnd, DateTimeKind.Unspecified);
        var zone = ResolveHospitalTimeZone();
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, zone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, zone);
        var userId = schedule.Doctor?.UserId ?? await _context.Doctors
            .Where(d => d.Id == schedule.DoctorId).Select(d => d.UserId).FirstOrDefaultAsync();
        if (!userId.HasValue) return;

        var candidates = await _context.Appointments
            .Where(a => a.DoctorId == userId.Value && a.ScheduledStart < endUtc
                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Rescheduled)
            .ToListAsync();
        if (candidates.Any(a => a.ScheduledStart.AddMinutes(a.EstimatedDurationMinutes) > startUtc))
            throw new InvalidOperationException("The added time overlaps an existing appointment. Choose a different schedule window.");
    }

    private static TimeOnly ExtendEndTime(TimeOnly endTime, int durationMinutes, int count)
    {
        var totalMinutes = endTime.Hour * 60 + endTime.Minute + durationMinutes * count;
        if (durationMinutes <= 0 || totalMinutes >= 24 * 60)
            throw new InvalidOperationException("Added slots must end before midnight.");
        return new TimeOnly(totalMinutes / 60, totalMinutes % 60);
    }

    private static Models.DayOfWeek ToScheduleDay(DateOnly date)
    {
        var systemDay = (int)date.DayOfWeek;
        return (Models.DayOfWeek)(systemDay == 0 ? 7 : systemDay);
    }

    private static TimeZoneInfo ResolveHospitalTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"); }
    }
}
