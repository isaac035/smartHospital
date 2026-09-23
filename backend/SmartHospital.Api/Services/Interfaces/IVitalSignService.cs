using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface IVitalSignService
{
    Task<List<VitalSignResponse>> GetByPatientIdAsync(int patientId);

    Task<VitalSignResponse> RecordAsync(int recordedByUserId, RecordVitalSignRequest request);
}
