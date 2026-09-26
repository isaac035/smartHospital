using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Doctors;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _context;

    public DoctorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorResponse>> GetAllAsync(DoctorFilterRequest filter)
    {
        var query = _context.Doctors.AsNoTracking();

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(d => d.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Specialization))
        {
            var specialization = filter.Specialization.Trim();
            query = query.Where(d => EF.Functions.ILike(d.Specialization, $"%{specialization}%"));
        }

        if (filter.MinExperience.HasValue)
        {
            query = query.Where(d => d.YearsOfExperience >= filter.MinExperience.Value);
        }

        if (filter.ConsultationTypeId.HasValue)
        {
            query = query.Where(d => _context.DoctorSchedules.Any(s =>
                s.DoctorId == d.Id &&
                s.Status == ScheduleStatus.Active &&
                s.ConsultationTypeId == filter.ConsultationTypeId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(d =>
                EF.Functions.ILike(d.FirstName, $"%{term}%") ||
                EF.Functions.ILike(d.LastName, $"%{term}%") ||
                EF.Functions.ILike((d.FirstName + " " + d.LastName), $"%{term}%") ||
                d.Id.ToString().Contains(term));
        }

        return await query
            .OrderBy(d => d.LastName)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                UserId = d.UserId,
                DepartmentId = d.DepartmentId,
                DepartmentName = d.Department!.Name,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                Specialization = d.Specialization,
                LicenseNumber = d.LicenseNumber,
                YearsOfExperience = d.YearsOfExperience,
                Bio = d.Bio,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<DoctorResponse?> GetByIdAsync(int id)
    {
        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                UserId = d.UserId,
                DepartmentId = d.DepartmentId,
                DepartmentName = d.Department!.Name,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                Specialization = d.Specialization,
                LicenseNumber = d.LicenseNumber,
                YearsOfExperience = d.YearsOfExperience,
                Bio = d.Bio,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DoctorResponse> CreateAsync(CreateDoctorRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existing = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Email == email);

        if (existing != null)
        {
            throw new InvalidOperationException(
                "A doctor with this email already exists."
            );
        }

        var departmentExists = await _context.Departments
            .AnyAsync(dept => dept.Id == request.DepartmentId);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "The specified department does not exist."
            );
        }

        var doctor = new Doctor
        {
            UserId = request.UserId,
            DepartmentId = request.DepartmentId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            Specialization = request.Specialization.Trim(),
            LicenseNumber = request.LicenseNumber.Trim(),
            YearsOfExperience = request.YearsOfExperience,
            Bio = request.Bio.Trim(),
            Status = DoctorStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Doctors.Add(doctor);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(doctor.Id)
            ?? throw new InvalidOperationException("Failed to create doctor.");
    }

    public async Task<DoctorResponse?> UpdateAsync(
        int id,
        UpdateDoctorRequest request)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _context.Doctors
            .AnyAsync(d => d.Id != id && d.Email == email);

        if (emailTaken)
        {
            throw new InvalidOperationException(
                "A doctor with this email already exists."
            );
        }

        var departmentExists = await _context.Departments
            .AnyAsync(dept => dept.Id == request.DepartmentId);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "The specified department does not exist."
            );
        }

        doctor.FirstName = request.FirstName.Trim();
        doctor.LastName = request.LastName.Trim();
        doctor.Email = email;
        doctor.PhoneNumber = request.PhoneNumber.Trim();
        doctor.DepartmentId = request.DepartmentId;
        doctor.Specialization = request.Specialization.Trim();
        doctor.LicenseNumber = request.LicenseNumber.Trim();
        doctor.YearsOfExperience = request.YearsOfExperience;
        doctor.Bio = request.Bio.Trim();
        doctor.UserId = request.UserId;
        doctor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
        {
            return false;
        }

        doctor.Status = DoctorStatus.Inactive;
        doctor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<DoctorResponse>> GetAvailableAsync(AvailableDoctorFilterRequest filter)
    {
        var query = _context.Doctors
            .AsNoTracking()
            .Where(d => d.Status == DoctorStatus.Active);

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(d => d.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.ConsultationTypeId.HasValue)
        {
            query = query.Where(d => _context.DoctorSchedules.Any(s =>
                s.DoctorId == d.Id &&
                s.Status == ScheduleStatus.Active &&
                s.ConsultationTypeId == filter.ConsultationTypeId.Value));
        }

        if (filter.Date.HasValue)
        {
            var systemDayOfWeek = (int)filter.Date.Value.DayOfWeek;
            var dayOfWeek = (Models.DayOfWeek)(systemDayOfWeek == 0 ? 7 : systemDayOfWeek);
            var date = filter.Date.Value;

            query = query
                .Where(d => _context.DoctorSchedules.Any(s =>
                    s.DoctorId == d.Id &&
                    s.Status == ScheduleStatus.Active &&
                    s.DayOfWeek == dayOfWeek))
                .Where(d => !_context.DoctorLeaves.Any(l =>
                    l.DoctorId == d.Id &&
                    l.Status == LeaveStatus.Approved &&
                    l.StartDate <= date &&
                    l.EndDate >= date));
        }

        return await query
            .OrderBy(d => d.LastName)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                UserId = d.UserId,
                DepartmentId = d.DepartmentId,
                DepartmentName = d.Department!.Name,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                Specialization = d.Specialization,
                LicenseNumber = d.LicenseNumber,
                YearsOfExperience = d.YearsOfExperience,
                Bio = d.Bio,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<DoctorResponse?> GetByUserIdAsync(int userId)
    {
        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                UserId = d.UserId,
                DepartmentId = d.DepartmentId,
                DepartmentName = d.Department!.Name,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                Specialization = d.Specialization,
                LicenseNumber = d.LicenseNumber,
                YearsOfExperience = d.YearsOfExperience,
                Bio = d.Bio,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DoctorResponse?> UpdateMyProfileAsync(
        int userId,
        UpdateMyDoctorProfileRequest request)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return null;
        }

        doctor.PhoneNumber = request.PhoneNumber.Trim();
        doctor.Bio = request.Bio.Trim();
        doctor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByUserIdAsync(userId);
    }
}
