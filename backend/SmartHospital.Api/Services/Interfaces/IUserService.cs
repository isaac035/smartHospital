using SmartHospital.Api.DTOs.Users;

namespace SmartHospital.Api.Services.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync();

    Task<UserResponse?> GetByIdAsync(int id);

    Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request);

    Task<bool> DeactivateAsync(int id);
}