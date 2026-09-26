using SmartHospital.Api.DTOs.Queues;

namespace SmartHospital.Api.Services.Interfaces;

public interface IQueueService
{
    /// <summary>
    /// Returns the full queue for a doctor ordered by priority (Emergency first)
    /// then FIFO within the same priority, for active entries only.
    /// </summary>
    Task<List<QueueEntryResponse>> GetQueueForDoctorAsync(int doctorId);

    /// <summary>
    /// Returns the real-time queue status snapshot for a doctor —
    /// who is currently being served, how many are waiting, full ordered list.
    /// Designed to be polled by a frontend or to back a SignalR hub.
    /// </summary>
    Task<QueueStatusResponse> GetQueueStatusForDoctorAsync(int doctorId);

    /// <summary>Returns the status of a specific queue entry (by queue entry ID).</summary>
    Task<QueueEntryResponse?> GetQueueEntryStatusAsync(int queueEntryId);

    /// <summary>
    /// Marks the specified queue entry as Called (status → Called, sets CalledAt).
    /// Recalculates estimated wait times for all subsequent entries.
    /// </summary>
    Task<QueueEntryResponse> CallQueueEntryAsync(int queueEntryId, int staffUserId);

    /// <summary>
    /// Checks in a patient for their appointment (status → CheckedIn on Appointment,
    /// sets CheckedInAt on QueueEntry).
    /// </summary>
    Task<QueueEntryResponse> CheckInAsync(int appointmentId, int staffUserId);

    /// <summary>Marks a queue entry as NoShow and updates the related appointment.</summary>
    Task<QueueEntryResponse> MarkNoShowAsync(int queueEntryId, int staffUserId);

    /// <summary>Marks a queue entry as Completed and updates the related appointment.</summary>
    Task<QueueEntryResponse> MarkCompletedAsync(int queueEntryId, int staffUserId);
}
