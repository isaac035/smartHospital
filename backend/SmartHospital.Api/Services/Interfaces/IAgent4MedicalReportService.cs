using SmartHospital.Api.DTOs.Agent4;

namespace SmartHospital.Api.Services.Interfaces;

public interface IAgent4MedicalReportService
{
    Task<MedicalReportResponse> GenerateAsync(int patientId, int? appointmentId, Guid? generationId, bool? checkupRequested, CancellationToken ct);
    Task<List<MedicalReportListItem>> GetPatientReportsAsync(int patientId, CancellationToken ct);
    Task<MedicalReportResponse?> GetByReportIdAsync(Guid reportId, CancellationToken ct);
}
