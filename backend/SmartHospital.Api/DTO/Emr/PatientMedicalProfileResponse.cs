namespace SmartHospital.Api.DTOs.Emr;

public class PatientMedicalProfileResponse
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string BloodGroup { get; set; } = string.Empty;

    public string Allergies { get; set; } = string.Empty;

    public string ChronicDiseases { get; set; } = string.Empty;

    public string EmergencyContactName { get; set; } = string.Empty;

    public string EmergencyContactPhone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
