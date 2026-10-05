namespace SmartHospital.Api.DTOs.Wards;

public class WardResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Floor { get; set; } = string.Empty;

    public string? BuildingBlock { get; set; }

    public int Capacity { get; set; }

    public string Type { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int TotalRooms { get; set; }

    public int TotalBeds { get; set; }

    public int OccupiedBeds { get; set; }

    public int AvailableBeds { get; set; }

    public double OccupancyRate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
