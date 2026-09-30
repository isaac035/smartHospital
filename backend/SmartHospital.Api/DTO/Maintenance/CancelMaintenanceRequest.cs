using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Maintenance;

public class CancelMaintenanceRequest
{
    [MaxLength(1000)]
    public string? Reason { get; set; }
}
