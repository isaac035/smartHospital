namespace SmartHospital.Api.DTOs.Availability;

/// <summary>A single bookable time slot returned by the availability service.</summary>
public class AvailableSlotResponse
{
    /// <summary>User ID used by the appointment booking endpoint.</summary>
    public int DoctorId { get; set; }

    /// <summary>Doctor profile ID used by schedule management.</summary>
    public int DoctorProfileId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    /// <summary>UTC start of the available slot.</summary>
    public DateTime SlotStart { get; set; }

    /// <summary>UTC end of the available slot.</summary>
    public DateTime SlotEnd { get; set; }

    /// <summary>Duration of the slot in minutes.</summary>
    public int DurationMinutes { get; set; }

    public string Status { get; set; } = "Available";
}
