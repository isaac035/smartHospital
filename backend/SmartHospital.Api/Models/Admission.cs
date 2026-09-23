namespace SmartHospital.Api.Models;

public class Admission
{
    public int Id { get; set; }

    public string AdmissionNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public int? AdmittingDoctorId { get; set; }

    public DateTime AdmissionDate { get; set; }

    public DateTime? DischargeDate { get; set; }

    public AdmissionStatus Status { get; set; } = AdmissionStatus.Admitted;

    public AdmissionPriority Priority { get; set; } = AdmissionPriority.Normal;

    public string ReasonForAdmission { get; set; } = string.Empty;

    public string? Diagnosis { get; set; }

    public string? DischargeSummary { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User? Patient { get; set; }

    public User? AdmittingDoctor { get; set; }

    public ICollection<BedAllocation> BedAllocations { get; set; } = new List<BedAllocation>();
}
