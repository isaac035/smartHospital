using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Maintenance;

public class CancelMaintenanceRequest
{
    [MaxLength(1000)]
    [NoHtml]
    public string? Reason { get; set; }
}
