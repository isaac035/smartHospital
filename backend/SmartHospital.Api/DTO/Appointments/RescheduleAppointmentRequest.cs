using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Appointments;

public class RescheduleAppointmentRequest
{
    /// <summary>New UTC slot date-time. Must be in the future.</summary>
    [Required]
    public DateTime NewScheduledStart { get; set; }

    [Range(10, 480, ErrorMessage = "Duration must be between 10 and 480 minutes.")]
    public int? NewEstimatedDurationMinutes { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}
