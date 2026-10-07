using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Users;

public class CreateAppointmentManagerRequest
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
