namespace SmartHospital.Api.Models;

/// <summary>
/// Priority ordering for appointment and queue management.
/// Emergency (3) is served first, then Urgent (2), then Normal (1) — FIFO within same priority.
/// </summary>
public enum AppointmentPriority
{
    Normal    = 1,
    Urgent    = 2,
    Emergency = 3
}
