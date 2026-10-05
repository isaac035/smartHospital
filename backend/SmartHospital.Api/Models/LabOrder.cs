namespace SmartHospital.Api.Models;

public class LabOrder
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int? MedicalRecordId { get; set; }

    public string TestName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public LabOrderPriority Priority { get; set; } = LabOrderPriority.Routine;

    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;

    public DateTime OrderedAt { get; set; }

    public string ClinicalNotes { get; set; } = string.Empty;

    // Navigation properties
    public User? Patient { get; set; }

    public User? Doctor { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }

    public LabReport? Report { get; set; }
}
