using SmartHospital.Api.DTOs.Users;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Services.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync(UserRole? role = null);

    Task<List<PatientSearchResult>> SearchPatientsAsync(string query, int limit = 10);

    Task<UserResponse?> GetByIdAsync(int id);

    Task<UserResponse> CreateAppointmentManagerAsync(CreateAppointmentManagerRequest request);
    Task<UserResponse> CreateDoctorManagerAsync(CreateDoctorManagerRequest request);

    Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request);

    Task<bool> DeactivateAsync(int id);
}
