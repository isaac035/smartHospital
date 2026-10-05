namespace SmartHospital.Api.DTOs.Occupancy;

public class WardOccupancyResponse
{
    public int WardId { get; set; }

    public string WardName { get; set; } = string.Empty;

    public string WardCode { get; set; } = string.Empty;

    public string WardType { get; set; } = string.Empty;

    public string Floor { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int TotalBeds { get; set; }

    public int AvailableBeds { get; set; }

    public int OccupiedBeds { get; set; }

    public int ReservedBeds { get; set; }

    public int MaintenanceBeds { get; set; }

    public int BlockedBeds { get; set; }

    public double OccupancyRate { get; set; }

    public bool IsLowAvailability { get; set; }
}
