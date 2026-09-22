namespace SmartHospital.Api.Models;

public class ResourceMaintenance
{
    public int Id { get; set; }

    public string MaintenanceCode { get; set; } = string.Empty;

    // Maintenance targets either a Bed or a MedicalResource (mutually exclusive)
    public int? BedId { get; set; }

    public int? MedicalResourceId { get; set; }

    public MaintenanceType Type { get; set; } = MaintenanceType.RoutineInspection;

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

    public string Description { get; set; } = string.Empty;

    public DateTime ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public DateTime? ActualCompletedAt { get; set; }

    public int? PerformedByStaffId { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Bed? Bed { get; set; }

    public MedicalResource? MedicalResource { get; set; }

    public User? PerformedByStaff { get; set; }
}
