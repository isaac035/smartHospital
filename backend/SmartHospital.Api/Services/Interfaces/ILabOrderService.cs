using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Services.Interfaces;

public interface ILabOrderService
{
    Task<List<LabOrderResponse>> GetByPatientIdAsync(int patientId);

    Task<LabOrderResponse?> GetByIdAsync(int id);

    Task<LabOrderResponse> CreateOrderAsync(int doctorId, CreateLabOrderRequest request);

    Task<LabOrderResponse?> UpdateStatusAsync(int id, UpdateLabOrderStatusRequest request);

    Task<LabOrderResponse?> UpdateStatusAsync(int id, LabOrderStatus newStatus);

    Task<LabReportResponse?> RecordReportAsync(int labOrderId, int conductedByUserId, RecordLabReportRequest request);
}
