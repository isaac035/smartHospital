using SmartHospital.Api.DTOs.Agent3;

namespace SmartHospital.Api.Services.Interfaces;

public interface IAgent3ResourceAllocationService
{
    Task<RecommendResourcesResponse> RecommendAsync(int patientId, int appointmentId, CancellationToken cancellationToken);
    Task<AllocationResponse> AllocateAsync(int patientId, int appointmentId, int resourceId, string kind, DateOnly checkupDate, CancellationToken cancellationToken);
    Task<List<AllocationResponse>> GetForAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);
}
