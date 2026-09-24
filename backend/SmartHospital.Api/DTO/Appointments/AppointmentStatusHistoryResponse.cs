namespace SmartHospital.Api.DTOs.Appointments;

public class AppointmentStatusHistoryResponse
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public string OldStatus { get; set; } = string.Empty;

    public string NewStatus { get; set; } = string.Empty;

    public int ChangedBy { get; set; }

    public string ChangedByName { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    public string? Reason { get; set; }
}
