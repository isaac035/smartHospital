using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Admissions;

public class CreateAdmissionRequest
{
    [Required]
    [SelectedId]
    public int PatientId { get; set; }

    public int? AdmittingDoctorId { get; set; }

    [Required]
    [DefinedEnum]
    public AdmissionPriority Priority { get; set; } = AdmissionPriority.Normal;

    [Required]
    [MaxLength(500)]
    [NoHtml]
    public string ReasonForAdmission { get; set; } = string.Empty;

    [MaxLength(1000)]
    [NoHtml]
    public string? Diagnosis { get; set; }
}
