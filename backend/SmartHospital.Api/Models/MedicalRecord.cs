namespace SmartHospital.Api.Models;

public class MedicalRecord
{
    public int Id { get; set; }

    public string RecordNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

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

    // Navigation properties
    public User? Patient { get; set; }

    public User? Doctor { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public ICollection<VitalSign> VitalSigns { get; set; } = new List<VitalSign>();

    public ICollection<LabOrder> LabOrders { get; set; } = new List<LabOrder>();
}
