using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Users;

/// <summary>Creates a Doctor Manager: an admin limited to doctors, availability and leave.</summary>
public class CreateDoctorManagerRequest
{
    [Required, StringLength(100)]
    [PersonName]
    [NoHtml]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [PersonName]
    [NoHtml]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Email must be a valid email address."), StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    [StrongPassword]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    [PhoneNumber]
    public string PhoneNumber { get; set; } = string.Empty;
}
