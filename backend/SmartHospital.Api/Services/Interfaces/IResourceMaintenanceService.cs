using SmartHospital.Api.DTOs.Maintenance;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Services.Interfaces;

public interface IResourceMaintenanceService
{
    Task<MaintenanceResponse> ScheduleMaintenanceAsync(CreateMaintenanceRequest request, int? staffUserId = null);
    Task<List<MaintenanceResponse>> GetMaintenanceRecordsAsync(int? bedId = null, int? resourceId = null, MaintenanceStatus? status = null);
    Task<MaintenanceResponse?> GetMaintenanceByIdAsync(int id);
    Task<MaintenanceResponse?> StartMaintenanceAsync(int id, int? staffUserId = null);
    Task<MaintenanceResponse?> CompleteMaintenanceAsync(int id, CompleteMaintenanceRequest request, int? staffUserId = null);
}
