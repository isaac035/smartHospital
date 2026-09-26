using SmartHospital.Api.DTOs.Schedules;

namespace SmartHospital.Api.Services.Interfaces;

public interface IScheduleService
{
    Task<List<ScheduleResponse>> GetAllAsync(ScheduleFilterRequest filter);

    Task<ScheduleResponse?> GetByIdAsync(int id);

    Task<ScheduleResponse> CreateAsync(CreateScheduleRequest request);

    Task<ScheduleResponse?> UpdateAsync(
        int id,
        UpdateScheduleRequest request);

    Task<bool> RemoveAsync(int id);
}
