using SmartHospital.Api.DTOs.Availability;

namespace SmartHospital.Api.Services.Interfaces;

public interface IAvailabilityService
{
    /// <summary>
    /// Computes free bookable slots for a doctor (and optionally department) on a given UTC date.
    /// Slots are derived from DoctorSchedule minus existing confirmed/scheduled appointments.
    /// </summary>
    Task<List<AvailableSlotResponse>> GetAvailableSlotsAsync(
        int? doctorId,
        int? departmentId,
        DateTime date);

    /// <summary>
    /// Determines whether a specific slot is free for a doctor.
    /// Thread-safe: relies on the caller to hold a DB transaction when used in booking.
    /// </summary>
    Task<bool> IsSlotAvailableAsync(
        int doctorId,
        DateTime scheduledStart,
        int durationMinutes,
        int? excludeAppointmentId = null);

    /// <summary>
    /// Returns up to <paramref name="count"/> suggested future slots for rescheduling,
    /// starting from <paramref name="preferredDate"/> and scanning forward day-by-day.
    /// </summary>
    Task<List<RescheduleSuggestionResponse>> GetRescheduleSuggestionsAsync(
        int doctorId,
        DateTime preferredDate,
        int durationMinutes,
        int count = 5);

    /// <summary>
    /// Checks whether the doctor has reached their daily maximum patient capacity
    /// for the given UTC date.
    /// </summary>
    Task<bool> IsDailyCapacityReachedAsync(int doctorId, DateTime date);
}
