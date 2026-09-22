using SmartHospital.Api.DTOs.Occupancy;
using SmartHospital.Api.DTOs.Wards;

namespace SmartHospital.Api.Services.Interfaces;

public interface IWardService
{
    Task<WardResponse> CreateWardAsync(CreateWardRequest request);
    Task<List<WardResponse>> GetAllWardsAsync(bool? isActive = null);
    Task<WardDetailResponse?> GetWardByIdAsync(int id);
    Task<WardResponse?> UpdateWardAsync(int id, UpdateWardRequest request);
    Task<bool> DeactivateWardAsync(int id);
    Task<WardOccupancyResponse?> GetWardOccupancyAsync(int id);
}
