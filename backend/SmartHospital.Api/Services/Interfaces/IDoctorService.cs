using SmartHospital.Api.DTOs.Doctors;

namespace SmartHospital.Api.Services.Interfaces;

public interface IDoctorService
{
    Task<List<DoctorResponse>> GetAllAsync(DoctorFilterRequest filter);

    Task<DoctorResponse?> GetByIdAsync(int id);

    Task<DoctorResponse> CreateAsync(CreateDoctorRequest request);

    Task<DoctorResponse?> UpdateAsync(
        int id,
        UpdateDoctorRequest request);

    Task<bool> DeactivateAsync(int id);

    Task<List<DoctorResponse>> GetAvailableAsync(AvailableDoctorFilterRequest filter);

    Task<DoctorResponse?> GetByUserIdAsync(int userId);

    Task<DoctorResponse?> UpdateMyProfileAsync(
        int userId,
        UpdateMyDoctorProfileRequest request);
}
