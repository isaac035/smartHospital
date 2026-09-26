using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Leaves;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class LeaveService : ILeaveService
{
    private readonly AppDbContext _context;

    public LeaveService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveResponse>> GetAllAsync(LeaveFilterRequest filter)
    {
        var query = _context.DoctorLeaves.AsNoTracking();

        if (filter.DoctorId.HasValue)
        {
            query = query.Where(l => l.DoctorId == filter.DoctorId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.EndDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(l => l.StartDate <= filter.ToDate.Value);
        }

        return await query
            .OrderByDescending(l => l.StartDate)
            .Select(l => new LeaveResponse
            {
                Id = l.Id,
                DoctorId = l.DoctorId,
                DoctorName = l.Doctor!.FirstName + " " + l.Doctor.LastName,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Reason = l.Reason,
                Status = l.Status.ToString()
            })
            .ToListAsync();
    }

    public async Task<LeaveResponse?> GetByIdAsync(int id)
    {
        return await _context.DoctorLeaves
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LeaveResponse
            {
                Id = l.Id,
                DoctorId = l.DoctorId,
                DoctorName = l.Doctor!.FirstName + " " + l.Doctor.LastName,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Reason = l.Reason,
                Status = l.Status.ToString()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<LeaveResponse> CreateAsync(
        CreateLeaveRequest request,
        LeaveStatus initialStatus)
    {
        if (request.EndDate < request.StartDate)
        {
            throw new InvalidOperationException("End date must be on or after start date.");
        }

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId);

        if (doctor == null)
        {
            throw new InvalidOperationException("The specified doctor does not exist.");
        }

        var leave = new DoctorLeave
        {
            DoctorId = request.DoctorId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason.Trim(),
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DoctorLeaves.Add(leave);

        ApplyOnLeaveStatusIfActive(doctor, leave.StartDate, leave.EndDate, initialStatus);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(leave.Id)
            ?? throw new InvalidOperationException("Failed to create leave record.");
    }

    private static void ApplyOnLeaveStatusIfActive(
        Doctor doctor,
        DateOnly startDate,
        DateOnly endDate,
        LeaveStatus status)
    {
        if (status != LeaveStatus.Approved)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (startDate <= today && endDate >= today)
        {
            doctor.Status = DoctorStatus.OnLeave;
            doctor.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<LeaveResponse?> UpdateAsync(
        int id,
        UpdateLeaveRequest request)
    {
        var leave = await _context.DoctorLeaves
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leave == null)
        {
            return null;
        }

        if (request.EndDate < request.StartDate)
        {
            throw new InvalidOperationException("End date must be on or after start date.");
        }

        if (!Enum.TryParse<LeaveStatus>(request.Status, true, out var status))
        {
            throw new InvalidOperationException("Invalid leave status.");
        }

        leave.StartDate = request.StartDate;
        leave.EndDate = request.EndDate;
        leave.Reason = request.Reason.Trim();
        leave.Status = status;
        leave.UpdatedAt = DateTime.UtcNow;

        if (status == LeaveStatus.Approved)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == leave.DoctorId);

            if (doctor != null)
            {
                ApplyOnLeaveStatusIfActive(doctor, leave.StartDate, leave.EndDate, status);
            }
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> CancelAsync(int id)
    {
        var leave = await _context.DoctorLeaves
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leave == null)
        {
            return false;
        }

        leave.Status = LeaveStatus.Cancelled;
        leave.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}
