using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.ConsultationTypes;

public class CreateConsultationTypeRequest
{
    [Required]
    [MaxLength(100)]
    [NoHtml]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 480)]
    public int DurationMinutes { get; set; }

    [MaxLength(500)]
    [NoHtml]
    public string Description { get; set; } = string.Empty;
}
