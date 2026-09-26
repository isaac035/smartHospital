using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Doctors;

public class UpdateMyDoctorProfileRequest
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Bio { get; set; } = string.Empty;
}
