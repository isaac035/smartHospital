namespace SmartHospital.Api.DTOs.Admissions;

public class AdmissionResponse
{
    public int Id { get; set; }

    public string AdmissionNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string PatientEmail { get; set; } = string.Empty;

    public string PatientPhone { get; set; } = string.Empty;

    public int? AdmittingDoctorId { get; set; }

    public string? AdmittingDoctorName { get; set; }

    public DateTime AdmissionDate { get; set; }

    public DateTime? DischargeDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string ReasonForAdmission { get; set; } = string.Empty;

    public string? Diagnosis { get; set; }

    public string? DischargeSummary { get; set; }

    public int? ActiveBedId { get; set; }

    public string? ActiveBedNumber { get; set; }

    public string? ActiveRoomNumber { get; set; }

    public string? ActiveWardName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
