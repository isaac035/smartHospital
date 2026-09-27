using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface IEmrAuditService
{
    Task<EmrAuditLogResponse> LogAsync(
        int userId,
        string action,
        string entityType,
        int? entityId,
        int? patientId = null,
        string? metadata = null,
        bool isSuccess = true);

    Task<List<EmrAuditLogResponse>> GetAuditLogsAsync(
        int? patientId = null,
        string? entityType = null,
        int? userId = null,
        int limit = 100);

    Task<EmrAuditLogResponse?> GetByIdAsync(int id);

    Task<List<EmrAuditLogResponse>> GetByPatientIdAsync(int patientId, int limit = 100);
}
