namespace SmartHospital.Api.Models;

public class MedicalResource
{
    public int Id { get; set; }

    public string ResourceCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ResourceCategory Category { get; set; } = ResourceCategory.Other;

    public ResourceStatus Status { get; set; } = ResourceStatus.Available;

    public string LocationDescription { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? Manufacturer { get; set; }

    public string? ModelNumber { get; set; }

    // Optional location relationships (nullable)
    public int? WardId { get; set; }

    public int? RoomId { get; set; }

    public int? BedId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Ward? Ward { get; set; }

    public Room? Room { get; set; }

    public Bed? Bed { get; set; }

    public ICollection<ResourceMaintenance> Maintenances { get; set; } = new List<ResourceMaintenance>();
}
