using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Users;

public class UpdateUserRequest
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
    [PhoneNumber]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}