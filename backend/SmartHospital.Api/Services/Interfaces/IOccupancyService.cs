using SmartHospital.Api.DTOs.Occupancy;

namespace SmartHospital.Api.Services.Interfaces;

public interface IOccupancyService
{
    Task<OccupancyOverviewResponse> GetOccupancyOverviewAsync();
    Task<List<WardOccupancyResponse>> GetWardOccupanciesAsync();
    Task<WardOccupancyResponse?> GetWardOccupancyByIdAsync(int wardId);
}
