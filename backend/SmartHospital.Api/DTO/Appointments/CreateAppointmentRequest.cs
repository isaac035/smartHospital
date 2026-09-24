using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    public int? DepartmentId { get; set; }

    [Required]
    public AppointmentType AppointmentType { get; set; } = AppointmentType.General;

    /// <summary>UTC date-time of the requested slot.</summary>
    [Required]
    public DateTime ScheduledStart { get; set; }

    [Range(10, 480, ErrorMessage = "Duration must be between 10 and 480 minutes.")]
    public int EstimatedDurationMinutes { get; set; } = 30;

    public AppointmentPriority Priority { get; set; } = AppointmentPriority.Normal;

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
