using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Users;

public class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
}