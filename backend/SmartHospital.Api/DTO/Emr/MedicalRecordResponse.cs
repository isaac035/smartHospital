namespace SmartHospital.Api.DTOs.Emr;

public class MedicalRecordResponse
{
    public int Id { get; set; }

    public string RecordNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int? AppointmentId { get; set; }

    public int? AdmissionId { get; set; }

    public DateTime VisitDate { get; set; }

    public string ChiefComplaint { get; set; } = string.Empty;

    public string Symptoms { get; set; } = string.Empty;

    public string ExaminationNotes { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string TreatmentPlan { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<PrescriptionResponse> Prescriptions { get; set; } = new();

    public List<VitalSignResponse> VitalSigns { get; set; } = new();

    public List<LabOrderResponse> LabOrders { get; set; } = new();

    public List<DiagnosisResponse> Diagnoses { get; set; } = new();

    public List<TreatmentPlanResponse> TreatmentPlans { get; set; } = new();
}
