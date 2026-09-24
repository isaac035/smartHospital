namespace SmartHospital.Api.DTOs.Queues;

public class QueueEntryResponse
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public string AppointmentReferenceNumber { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int QueueNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public int? EstimatedWaitMinutes { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public DateTime? CalledAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    /// <summary>Position in the active queue (1 = next to be called).</summary>
    public int QueuePosition { get; set; }
}
