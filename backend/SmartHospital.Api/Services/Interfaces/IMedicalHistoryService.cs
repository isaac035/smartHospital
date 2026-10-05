using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface IMedicalHistoryService
{
    Task<PatientMedicalTimelineResponse> GetPatientTimelineAsync(int patientId);
}
