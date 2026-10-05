namespace SmartHospital.Api.DTOs.Availability;

/// <summary>
/// A suggested alternative slot returned when an automatic rescheduling
/// suggestion is requested (e.g. after the original slot becomes unavailable).
/// </summary>
public class RescheduleSuggestionResponse
{
    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public DateTime SuggestedStart { get; set; }

    public DateTime SuggestedEnd { get; set; }

    public int DurationMinutes { get; set; }
}
