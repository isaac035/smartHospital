using SmartHospital.Api.DTOs.Beds;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Services.Interfaces;

public interface IBedService
{
    Task<BedResponse> CreateBedAsync(CreateBedRequest request);
    Task<List<BedResponse>> GetBedsAsync(BedQueryFilter filter);
    Task<List<BedResponse>> GetAvailableBedsAsync(int? wardId = null, int? roomId = null, BedType? type = null);
    Task<BedResponse?> GetBedByIdAsync(int id);
    Task<BedResponse?> UpdateBedAsync(int id, UpdateBedRequest request);
    Task<BedResponse?> UpdateBedStatusAsync(int id, UpdateBedStatusRequest request);
    Task<bool> DeactivateBedAsync(int id);
}
