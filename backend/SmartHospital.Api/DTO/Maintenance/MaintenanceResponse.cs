namespace SmartHospital.Api.DTOs.Maintenance;

public class MaintenanceResponse
{
    public int Id { get; set; }

    public string MaintenanceCode { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public int? BedId { get; set; }

    public string? BedNumber { get; set; }

    public int? MedicalResourceId { get; set; }

    public string? MedicalResourceCode { get; set; }

    public string? MedicalResourceName { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public DateTime? ActualCompletedAt { get; set; }

    public int? PerformedByStaffId { get; set; }

    public string? PerformedByStaffName { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
