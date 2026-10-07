using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Appointments;

public class CancelAppointmentRequest
{
    [Required]
    [MaxLength(500)]
    [NoHtml]
    public string Reason { get; set; } = string.Empty;
}
