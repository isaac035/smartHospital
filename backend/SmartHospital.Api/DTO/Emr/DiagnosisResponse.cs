namespace SmartHospital.Api.DTOs.Emr;

public class DiagnosisResponse
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime DiagnosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
