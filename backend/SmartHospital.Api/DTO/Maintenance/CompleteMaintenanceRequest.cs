using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Maintenance;

public class CompleteMaintenanceRequest
{
    [Required]
    [MaxLength(1000)]
    public string ResolutionNotes { get; set; } = string.Empty;
}
