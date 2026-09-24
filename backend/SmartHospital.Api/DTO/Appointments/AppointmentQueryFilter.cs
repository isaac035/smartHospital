using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Appointments;

/// <summary>Query parameters for filtering the appointments list endpoint.</summary>
public class AppointmentQueryFilter
{
    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    public int? DepartmentId { get; set; }

    public AppointmentStatus? Status { get; set; }

    public AppointmentPriority? Priority { get; set; }

    /// <summary>Filter appointments on or after this UTC date.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Filter appointments on or before this UTC date.</summary>
    public DateTime? ToDate { get; set; }
}
