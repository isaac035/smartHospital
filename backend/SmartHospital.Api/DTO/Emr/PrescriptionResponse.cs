namespace SmartHospital.Api.DTOs.Emr;

public class PrescriptionResponse
{
    public int Id { get; set; }

    public string PrescriptionNumber { get; set; } = string.Empty;

    public int? MedicalRecordId { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string GeneralInstructions { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<PrescriptionItemResponse> Items { get; set; } = new();
}

public class PrescriptionItemResponse
{
    public int Id { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public int DurationDays { get; set; }

    public string SpecialInstructions { get; set; } = string.Empty;
}
