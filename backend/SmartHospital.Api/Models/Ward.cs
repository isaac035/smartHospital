namespace SmartHospital.Api.Models;

public class Ward
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Floor { get; set; } = string.Empty;

    public string BuildingBlock { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public WardType Type { get; set; } = WardType.General;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
