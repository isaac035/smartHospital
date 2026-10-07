using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Appointments;

public class UpdateAppointmentPriorityRequest
{
    [Required]
    [DefinedEnum]
    public AppointmentPriority? Priority { get; set; }
}
