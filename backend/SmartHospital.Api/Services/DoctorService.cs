using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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

        if (!filter.IncludeInactive)
        {
            query = query.Where(d => d.Status != DoctorStatus.Inactive);
        }

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

    public async Task<CreateDoctorResponse> CreateAsync(CreateDoctorRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            throw new InvalidOperationException("This email is already registered to a user.");
        }

        if (await _context.Doctors.AnyAsync(d => d.Email.ToLower() == email))
            throw new InvalidOperationException("A doctor profile with this email already exists.");

        var departmentExists = await _context.Departments
            .AnyAsync(dept => dept.Id == request.DepartmentId);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "The specified department does not exist."
            );
        }

        var temporaryPassword = GenerateTemporaryPassword();
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
            : null;

        try
        {
            var now = DateTime.UtcNow;
            var user = CreateDoctorUser(
                request.FirstName, request.LastName, email, request.PhoneNumber, temporaryPassword, now);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var doctor = new Doctor
            {
                UserId = user.Id,
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
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            var doctorResponse = await GetByIdAsync(doctor.Id)
                ?? throw new InvalidOperationException("Failed to create doctor profile.");
            if (transaction != null) await transaction.CommitAsync();

            return new CreateDoctorResponse
            {
                Doctor = doctorResponse,
                TemporaryPassword = temporaryPassword
            };
        }
        catch (DbUpdateException)
        {
            if (transaction != null) await transaction.RollbackAsync();

            if (await _context.Users.AsNoTracking().AnyAsync(u => u.Email.ToLower() == email))
                throw new InvalidOperationException("This email is already registered to a user.");
            if (await _context.Doctors.AsNoTracking().AnyAsync(d => d.Email.ToLower() == email))
                throw new InvalidOperationException("A doctor profile with this email already exists.");

            throw;
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
    }

    private static string GenerateTemporaryPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "!";
    }

    public async Task<CreateDoctorResponse> CreateAccountForExistingAsync(int doctorId)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
            : null;
        string? backfillEmail = null;

        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId)
                ?? throw new KeyNotFoundException("Doctor profile not found.");

            if (doctor.UserId.HasValue)
                throw new InvalidOperationException("This doctor profile already has a linked login account.");
            if (doctor.Status == DoctorStatus.Inactive)
                throw new InvalidOperationException("Inactive doctor profiles cannot receive a login account. Activate this profile first.");

            var email = doctor.Email.Trim().ToLowerInvariant();
            backfillEmail = email;
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
                throw new InvalidOperationException("A user account already uses this email. Review that account before linking this doctor profile.");

            var temporaryPassword = GenerateTemporaryPassword();
            var now = DateTime.UtcNow;
            var user = CreateDoctorUser(doctor.FirstName, doctor.LastName, email, doctor.PhoneNumber, temporaryPassword, now);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            doctor.UserId = user.Id;
            doctor.UpdatedAt = now;
            await _context.SaveChangesAsync();

            var doctorResponse = await GetByIdAsync(doctor.Id)
                ?? throw new InvalidOperationException("Failed to reload the linked doctor profile.");
            if (transaction != null) await transaction.CommitAsync();

            return new CreateDoctorResponse
            {
                Doctor = doctorResponse,
                TemporaryPassword = temporaryPassword
            };
        }
        catch (DbUpdateException)
        {
            if (transaction != null) await transaction.RollbackAsync();
            if (backfillEmail != null && await _context.Users.AsNoTracking().AnyAsync(u => u.Email.ToLower() == backfillEmail))
                throw new InvalidOperationException("A user account already uses this email. Review that account before linking this doctor profile.");
            throw new InvalidOperationException("The account could not be created because its email or doctor link changed. Refresh and review the profile.");
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
    }

    private static User CreateDoctorUser(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string temporaryPassword,
        DateTime now) => new()
    {
        FirstName = firstName.Trim(),
        LastName = lastName.Trim(),
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
        PhoneNumber = phoneNumber.Trim(),
        Role = UserRole.Doctor,
        Status = UserStatus.Active,
        CreatedAt = now,
        UpdatedAt = now
    };

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
        doctor.UserId = request.UserId ?? doctor.UserId;
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

    public async Task<bool> DeletePermanentlyAsync(int id)
    {
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null)
        {
            return false;
        }

        if (doctor.UserId.HasValue)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == doctor.UserId.Value);
            var anotherDoctorUsesAccount = await _context.Doctors.AnyAsync(d => d.Id != id && d.UserId == doctor.UserId);
            if (user != null && !anotherDoctorUsesAccount)
            {
                // Keep the login row for historical appointment references, but prevent further sign-in.
                user.Status = UserStatus.Inactive;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.Doctors.Remove(doctor);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
        {
            throw new KeyNotFoundException("Doctor not found.");
        }

        if (doctor.Status != DoctorStatus.Inactive)
        {
            throw new InvalidOperationException("Only an inactive doctor can be activated.");
        }

        // Doctor status is the existing source of truth for booking eligibility.
        // Activation intentionally leaves users, schedules, slots, availability,
        // and appointments unchanged.
        doctor.Status = DoctorStatus.Active;
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
