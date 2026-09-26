using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Leaves;

public class CreateLeaveRequest
{
    [Required]
    public int DoctorId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
