using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Appointments;

public class UpdateAppointmentPriorityRequest
{
    [Required]
    public AppointmentPriority? Priority { get; set; }
}
