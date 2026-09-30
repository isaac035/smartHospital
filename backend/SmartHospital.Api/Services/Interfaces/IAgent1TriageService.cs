using SmartHospital.Api.DTOs.Agent1;

namespace SmartHospital.Api.Services.Interfaces;

/// <summary>Agent 1: clinical triage + doctor matching. Recommends only - never books.</summary>
public interface IAgent1TriageService
{
    Task<TriageDoctorMatchResponse> TriageAndMatchAsync(int patientId, string symptoms, CancellationToken cancellationToken = default);
}
