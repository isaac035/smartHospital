namespace SmartHospital.Api.DTOs.Appointments;

public class AppointmentResponse
{
    public int Id { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string PatientEmail { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public string AppointmentType { get; set; } = string.Empty;

    public DateTime ScheduledStart { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public int? QueueNumber { get; set; }

    public string? Notes { get; set; }

    public string? CancelledReason { get; set; }

    public int? RescheduledFromId { get; set; }

    public bool EmergencyConfirmed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
