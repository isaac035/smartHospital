using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Admissions;

public class UpdateAdmissionRequest
{
    public int? AdmittingDoctorId { get; set; }

    [Required]
    public AdmissionPriority Priority { get; set; } = AdmissionPriority.Normal;

    [Required]
    [MaxLength(500)]
    public string ReasonForAdmission { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Diagnosis { get; set; }
}
