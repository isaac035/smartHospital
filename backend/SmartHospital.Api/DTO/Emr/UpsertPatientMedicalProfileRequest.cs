using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Emr;

public class UpsertPatientMedicalProfileRequest
{
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    public BloodGroup BloodGroup { get; set; } = BloodGroup.Unknown;

    [MaxLength(500)]
    public string Allergies { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ChronicDiseases { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EmergencyContactName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string EmergencyContactPhone { get; set; } = string.Empty;
}
