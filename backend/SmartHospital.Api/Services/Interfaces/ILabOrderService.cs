using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface ILabOrderService
{
    Task<List<LabOrderResponse>> GetByPatientIdAsync(int patientId);

    Task<LabOrderResponse?> GetByIdAsync(int id);

    Task<LabOrderResponse> CreateOrderAsync(int doctorId, CreateLabOrderRequest request);

    Task<LabReportResponse?> RecordReportAsync(int labOrderId, int conductedByUserId, RecordLabReportRequest request);
}
