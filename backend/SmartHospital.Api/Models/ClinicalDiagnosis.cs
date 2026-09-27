using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.Models;

public class ClinicalDiagnosis
{
    public int Id { get; set; }

    [Required]
    public int MedicalRecordId { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [MaxLength(50)]
    public string? Code { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DiagnosisType Type { get; set; } = DiagnosisType.Primary;

    public DiagnosisStatus Status { get; set; } = DiagnosisStatus.Active;

    public DiagnosisSeverity Severity { get; set; } = DiagnosisSeverity.Moderate;

    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;

    public DateTime DiagnosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public MedicalRecord? MedicalRecord { get; set; }

    public User? Patient { get; set; }

    public User? Doctor { get; set; }
}
