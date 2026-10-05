using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Leaves;

public class UpdateLeaveRequest
{
    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;
}
