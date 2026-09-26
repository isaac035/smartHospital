using SmartHospital.Api.DTOs.Appointments;

namespace SmartHospital.Api.Services.Interfaces;

public interface IAppointmentService
{
    /// <summary>Books a new appointment, assigns reference/queue numbers, and creates a queue entry.</summary>
    Task<AppointmentResponse> BookAppointmentAsync(int requestingUserId, CreateAppointmentRequest request);

    /// <summary>
    /// Returns appointments filtered by the query.
    /// Patients receive only their own records; Staff/Doctor/Admin receive all.
    /// </summary>
    Task<List<AppointmentResponse>> GetAppointmentsAsync(
        int requestingUserId,
        string requestingUserRole,
        AppointmentQueryFilter filter);

    /// <summary>Returns a single appointment, enforcing patient-ownership rules.</summary>
    Task<AppointmentResponse?> GetAppointmentByIdAsync(
        int id,
        int requestingUserId,
        string requestingUserRole);

    /// <summary>Updates mutable fields (notes, type, duration). Does NOT change status.</summary>
    Task<AppointmentResponse?> UpdateAppointmentAsync(
        int id,
        UpdateAppointmentRequest request,
        int requestingUserId,
        string requestingUserRole);

    /// <summary>Cancels an appointment and records the reason in the status history.</summary>
    Task<AppointmentResponse> CancelAppointmentAsync(
        int id,
        CancelAppointmentRequest request,
        int requestingUserId,
        string requestingUserRole);

    /// <summary>
    /// Reschedules an appointment to a new future slot.
    /// Marks the old appointment as Rescheduled and creates a new Appointment.
    /// </summary>
    Task<AppointmentResponse> RescheduleAppointmentAsync(
        int id,
        RescheduleAppointmentRequest request,
        int requestingUserId,
        string requestingUserRole);

    /// <summary>Returns the full status-change audit trail for an appointment.</summary>
    Task<List<AppointmentStatusHistoryResponse>> GetStatusHistoryAsync(int appointmentId);

    /// <summary>
    /// Confirms an emergency-priority appointment.
    /// Only Staff or Admin may call this.
    /// </summary>
    Task<AppointmentResponse> ConfirmEmergencyAsync(int id, int staffUserId);
}
