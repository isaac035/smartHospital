using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Appointments;

public class CancelAppointmentRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
