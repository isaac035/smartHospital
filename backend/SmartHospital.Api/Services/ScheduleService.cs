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
                StartTime = s.StartTime,
                EndTime = s.EndTime,
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
                StartTime = s.StartTime,
                EndTime = s.EndTime,
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

        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == request.DoctorId);

        if (!doctorExists)
        {
            throw new InvalidOperationException("The specified doctor does not exist.");
        }

        var consultationTypeExists = await _context.ConsultationTypes
            .AnyAsync(c => c.Id == request.ConsultationTypeId);

        if (!consultationTypeExists)
        {
            throw new InvalidOperationException("The specified consultation type does not exist.");
        }

        var hasOverlap = await _context.DoctorSchedules
            .Where(s =>
                s.DoctorId == request.DoctorId &&
                s.DayOfWeek == dayOfWeek &&
                s.Status == ScheduleStatus.Active)
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
            StartTime = request.StartTime,
            EndTime = request.EndTime,
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

        var consultationTypeExists = await _context.ConsultationTypes
            .AnyAsync(c => c.Id == request.ConsultationTypeId);

        if (!consultationTypeExists)
        {
            throw new InvalidOperationException("The specified consultation type does not exist.");
        }

        var hasOverlap = await _context.DoctorSchedules
            .Where(s =>
                s.Id != id &&
                s.DoctorId == schedule.DoctorId &&
                s.DayOfWeek == dayOfWeek &&
                s.Status == ScheduleStatus.Active)
            .AnyAsync(s => request.StartTime < s.EndTime && request.EndTime > s.StartTime);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "This doctor already has an overlapping availability session on this day."
            );
        }

        schedule.ConsultationTypeId = request.ConsultationTypeId;
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
}
