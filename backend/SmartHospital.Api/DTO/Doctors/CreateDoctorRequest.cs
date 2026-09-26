using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Doctors;

public class CreateDoctorRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Specialization { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Range(0, 80)]
    public int YearsOfExperience { get; set; }

    [MaxLength(2000)]
    public string Bio { get; set; } = string.Empty;

    public int? UserId { get; set; }
}
