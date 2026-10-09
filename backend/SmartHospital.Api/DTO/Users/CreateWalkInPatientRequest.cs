using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Users;

/// <summary>Creates a patient record from a typed name when booking for someone not yet registered.</summary>
public class CreateWalkInPatientRequest
{
    [Required, StringLength(100)]
    [PersonName]
    [NoHtml]
    public string FullName { get; set; } = string.Empty;
}
