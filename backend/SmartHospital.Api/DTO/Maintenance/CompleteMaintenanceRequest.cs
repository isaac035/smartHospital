using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Maintenance;

public class CompleteMaintenanceRequest
{
    [Required]
    [MaxLength(1000)]
    [NoHtml]
    public string ResolutionNotes { get; set; } = string.Empty;
}
