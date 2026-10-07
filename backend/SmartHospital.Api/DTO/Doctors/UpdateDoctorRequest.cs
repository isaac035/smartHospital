using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Doctors;

public class UpdateDoctorRequest
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
    [PhoneNumber]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [SelectedId]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(150)]
    [NoHtml]
    public string Specialization { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [NoHtml]
    public string LicenseNumber { get; set; } = string.Empty;

    [Range(0, 80)]
    public int YearsOfExperience { get; set; }

    [MaxLength(2000)]
    [NoHtml]
    public string Bio { get; set; } = string.Empty;

    public int? UserId { get; set; }
}
