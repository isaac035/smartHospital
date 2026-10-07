using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Emr;

public class UpsertPatientMedicalProfileRequest
{
    [DateOfBirth]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    [NoHtml]
    public string Gender { get; set; } = string.Empty;

    [DefinedEnum]
    public BloodGroup BloodGroup { get; set; } = BloodGroup.Unknown;

    [MaxLength(500)]
    [NoHtml]
    public string Allergies { get; set; } = string.Empty;

    [MaxLength(500)]
    [NoHtml]
    public string ChronicDiseases { get; set; } = string.Empty;

    [MaxLength(100)]
    [NoHtml]
    public string EmergencyContactName { get; set; } = string.Empty;

    [MaxLength(20)]
    [PhoneNumber]
    public string EmergencyContactPhone { get; set; } = string.Empty;
}
