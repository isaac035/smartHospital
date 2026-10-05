using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.ConsultationTypes;

public class UpdateConsultationTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 480)]
    public int DurationMinutes { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
