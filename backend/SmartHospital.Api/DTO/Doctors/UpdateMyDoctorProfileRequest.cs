using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Doctors;

public class UpdateMyDoctorProfileRequest
{
    [Required]
    [PhoneNumber]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(2000)]
    [NoHtml]
    public string Bio { get; set; } = string.Empty;
}
