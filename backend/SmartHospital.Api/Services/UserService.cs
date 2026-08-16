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