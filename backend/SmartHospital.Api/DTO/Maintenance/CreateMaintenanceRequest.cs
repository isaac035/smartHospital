using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Maintenance;

public class CreateMaintenanceRequest
{
    public int? BedId { get; set; }

    public int? MedicalResourceId { get; set; }

    [Required]
    public MaintenanceType Type { get; set; } = MaintenanceType.RoutineInspection;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }
}
