using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required]
    [SelectedId]
    public int PatientId { get; set; }

    [Required]
    [SelectedId]
    public int DoctorId { get; set; }

    public int? DepartmentId { get; set; }

    [Required]
    [DefinedEnum]
    public AppointmentType AppointmentType { get; set; } = AppointmentType.General;

    /// <summary>UTC date-time of the requested slot.</summary>
    [Required]
    public DateTime ScheduledStart { get; set; }

    [Range(10, 480, ErrorMessage = "Duration must be between 10 and 480 minutes.")]
    public int EstimatedDurationMinutes { get; set; } = 30;

    [DefinedEnum]
    public AppointmentPriority Priority { get; set; } = AppointmentPriority.Normal;

    [Range(1, int.MaxValue)]
    public int? TriageResultId { get; set; }

    [MaxLength(1000)]
    [NoHtml]
    public string? Notes { get; set; }
}
