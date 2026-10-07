using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}