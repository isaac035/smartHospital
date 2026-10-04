using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Users;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserResponse>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public Task<UserResponse> CreateAppointmentManagerAsync(CreateAppointmentManagerRequest request) =>
        CreateManagerAccountAsync(
            request.FirstName, request.LastName, request.Email, request.Password, request.PhoneNumber,
            UserRole.AppointmentManager, "appointment manager");

    public Task<UserResponse> CreateDoctorManagerAsync(CreateDoctorManagerRequest request) =>
        CreateManagerAccountAsync(
            request.FirstName, request.LastName, request.Email, request.Password, request.PhoneNumber,
            UserRole.DoctorManager, "doctor manager");

    private async Task<UserResponse> CreateManagerAccountAsync(
        string firstName, string lastName, string email, string password, string phoneNumber,
        UserRole role, string roleLabel)
    {
        email = email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("A user with this email already exists.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PhoneNumber = phoneNumber.Trim(),
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException($"Failed to create {roleLabel} account.");
    }

    public async Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return null;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return false;
        }

        user.Status = UserStatus.Inactive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}
