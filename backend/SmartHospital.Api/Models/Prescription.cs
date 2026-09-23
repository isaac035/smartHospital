namespace SmartHospital.Api.Models;

public class Prescription
{
    public int Id { get; set; }

    public string PrescriptionNumber { get; set; } = string.Empty;

    public int? MedicalRecordId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;

    public string GeneralInstructions { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User? Patient { get; set; }

    public User? Doctor { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
