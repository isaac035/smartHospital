using SmartHospital.Api.DTOs.Admissions;

namespace SmartHospital.Api.Services.Interfaces;

public interface IAdmissionService
{
    Task<AdmissionResponse> CreateAdmissionAsync(CreateAdmissionRequest request);
    Task<List<AdmissionResponse>> GetAdmissionsAsync(AdmissionQueryFilter filter);
    Task<AdmissionResponse?> GetAdmissionByIdAsync(int id);
    Task<PatientAdmissionSummaryResponse?> GetActiveAdmissionByPatientIdAsync(int patientId);
    Task<List<PatientAdmissionSummaryResponse>> GetPatientAdmissionHistoryAsync(int patientId);
    Task<AdmissionResponse?> UpdateAdmissionAsync(int id, UpdateAdmissionRequest request);
    Task<AdmissionResponse> AllocateBedAsync(AllocateBedRequest request, int? staffUserId = null);
    Task<AdmissionResponse> TransferPatientAsync(int admissionId, TransferPatientRequest request, int? staffUserId = null);
    Task<AdmissionResponse> DischargePatientAsync(int admissionId, DischargePatientRequest request, int? staffUserId = null);
}
