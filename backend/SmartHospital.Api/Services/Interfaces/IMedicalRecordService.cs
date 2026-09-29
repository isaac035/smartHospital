using SmartHospital.Api.DTOs.Emr;

namespace SmartHospital.Api.Services.Interfaces;

public interface IMedicalRecordService
{
    Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId);

    Task<MedicalRecordResponse?> GetByIdAsync(int id);

    Task<MedicalRecordResponse?> GetByAppointmentIdAsync(int appointmentId);

    Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request);

    Task<MedicalRecordResponse?> UpdateAsync(int id, int doctorId, UpdateMedicalRecordRequest request);

    Task<DiagnosisResponse> AddDiagnosisAsync(int medicalRecordId, int doctorId, AddDiagnosisRequest request);

    Task<DiagnosisResponse?> UpdateDiagnosisAsync(int medicalRecordId, int diagnosisId, int doctorId, UpdateDiagnosisRequest request);

    Task<DiagnosisResponse?> UpdatePrimaryDiagnosisAsync(int medicalRecordId, int doctorId, UpdateDiagnosisRequest request);

    Task<List<DiagnosisResponse>> GetDiagnosesAsync(int medicalRecordId);

    Task<TreatmentPlanResponse> RecordTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request);

    Task<TreatmentPlanResponse?> UpdateTreatmentPlanAsync(int medicalRecordId, int treatmentPlanId, int doctorId, UpdateTreatmentPlanRequest request);

    Task<TreatmentPlanResponse?> UpdatePrimaryTreatmentPlanAsync(int medicalRecordId, int doctorId, UpdateTreatmentPlanRequest request);

    Task<List<TreatmentPlanResponse>> GetTreatmentPlansAsync(int medicalRecordId);

    Task<List<MedicalRecordVersionResponse>> GetVersionHistoryAsync(int medicalRecordId);

    Task<MedicalRecordVersionResponse?> GetVersionAsync(int medicalRecordId, int versionNumber);

    Task<PagedMedicalRecordResult> SearchAsync(MedicalRecordQueryFilter filter);
}
