using SmartHospital.Api.DTOs.Leaves;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Services.Interfaces;

public interface ILeaveService
{
    Task<List<LeaveResponse>> GetAllAsync(LeaveFilterRequest filter);

    Task<LeaveResponse?> GetByIdAsync(int id);

    Task<LeaveResponse> CreateAsync(
        CreateLeaveRequest request,
        LeaveStatus initialStatus);

    Task<LeaveResponse?> UpdateAsync(
        int id,
        UpdateLeaveRequest request);

    Task<bool> CancelAsync(int id);
}
