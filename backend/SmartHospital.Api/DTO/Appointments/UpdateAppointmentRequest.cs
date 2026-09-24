using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Appointments;

public class UpdateAppointmentRequest
{
    public AppointmentType? AppointmentType { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Range(10, 480, ErrorMessage = "Duration must be between 10 and 480 minutes.")]
    public int? EstimatedDurationMinutes { get; set; }
}
