using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    [PersonName]
    [NoHtml]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [PersonName]
    [NoHtml]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    [StrongPassword]
    public string Password { get; set; } = string.Empty;

    [Required]
    [PhoneNumber]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}