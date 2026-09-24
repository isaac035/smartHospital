namespace SmartHospital.Api.DTOs.Queues;

/// <summary>
/// Snapshot of a doctor's queue state at a point in time.
/// Designed to be polled by a frontend (or to back a future SignalR hub).
/// </summary>
public class QueueStatusResponse
{
    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    /// <summary>Queue entry ID currently being served (null if nobody is in-progress).</summary>
    public int? CurrentlyServingQueueEntryId { get; set; }

    public int? CurrentlyServingQueueNumber { get; set; }

    public string? CurrentlyServingPatientName { get; set; }

    /// <summary>Total number of entries still waiting or called.</summary>
    public int TotalWaiting { get; set; }

    public List<QueueEntryResponse> Queue { get; set; } = new();
}
