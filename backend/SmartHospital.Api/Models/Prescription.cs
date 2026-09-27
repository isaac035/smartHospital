using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.Models;

public class Prescription
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string PrescriptionNumber { get; set; } = string.Empty;

    public int? MedicalRecordId { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Required]
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;

    [MaxLength(1000)]
    public string GeneralInstructions { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User? Patient { get; set; }

    public User? Doctor { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
