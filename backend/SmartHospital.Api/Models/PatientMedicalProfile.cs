namespace SmartHospital.Api.Models;

public class PatientMedicalProfile
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public BloodGroup BloodGroup { get; set; } = BloodGroup.Unknown;

    public string Allergies { get; set; } = string.Empty;

    public string ChronicDiseases { get; set; } = string.Empty;

    public string EmergencyContactName { get; set; } = string.Empty;

    public string EmergencyContactPhone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public User? Patient { get; set; }
}
