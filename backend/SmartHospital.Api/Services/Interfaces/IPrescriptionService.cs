using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface IPrescriptionService
{
    Task<List<PrescriptionResponse>> GetByPatientIdAsync(int patientId);

    Task<PrescriptionResponse?> GetByIdAsync(int id);

    Task<PrescriptionResponse> CreateAsync(int doctorId, CreatePrescriptionRequest request);
}
