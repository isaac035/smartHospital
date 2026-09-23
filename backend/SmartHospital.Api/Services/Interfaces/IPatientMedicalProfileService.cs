using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface IPatientMedicalProfileService
{
    Task<PatientMedicalProfileResponse?> GetByPatientIdAsync(int patientId);

    Task<PatientMedicalProfileResponse> UpsertAsync(int patientId, UpsertPatientMedicalProfileRequest request);
}
