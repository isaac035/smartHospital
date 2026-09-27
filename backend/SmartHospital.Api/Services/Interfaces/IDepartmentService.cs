using SmartHospital.Api.DTOs.Departments;

namespace SmartHospital.Api.Services.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentResponse>> GetAllAsync();

    Task<DepartmentResponse?> GetByIdAsync(int id);

    Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request);

    Task<DepartmentResponse?> UpdateAsync(
        int id,
        UpdateDepartmentRequest request);

    Task<bool> DeactivateAsync(int id);
}
