using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.Models;

public class MedicalRecord
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string RecordNumber { get; set; } = string.Empty;

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    public int? AppointmentId { get; set; }

    public int? AdmissionId { get; set; }

    [Required]
    public DateTime VisitDate { get; set; }

    [Required]
    [MaxLength(500)]
    public string ChiefComplaint { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Symptoms { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ExaminationNotes { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(2000)]
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

    public ICollection<ClinicalDiagnosis> Diagnoses { get; set; } = new List<ClinicalDiagnosis>();

    public ICollection<ClinicalTreatmentPlan> TreatmentPlans { get; set; } = new List<ClinicalTreatmentPlan>();

    public ICollection<MedicalRecordVersion> Versions { get; set; } = new List<MedicalRecordVersion>();
}
