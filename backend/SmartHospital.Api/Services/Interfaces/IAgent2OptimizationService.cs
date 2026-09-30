using SmartHospital.Api.DTOs.Agent2;

namespace SmartHospital.Api.Services.Interfaces;

public interface IAgent2OptimizationService
{
    Task<OptimizeAppointmentResponse> OptimizeAsync(
        int patientId, OptimizeAppointmentRequest request, CancellationToken cancellationToken = default);
}
