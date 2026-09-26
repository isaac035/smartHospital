using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface IMedicalRecordService
{
    Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId);

    Task<MedicalRecordResponse?> GetByIdAsync(int id);

    Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request);

    Task<MedicalRecordResponse?> UpdateAsync(int id, int doctorId, UpdateMedicalRecordRequest request);
}
