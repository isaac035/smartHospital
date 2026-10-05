namespace SmartHospital.Api.DTOs.Rooms;

public class RoomResponse
{
    public int Id { get; set; }

    public int WardId { get; set; }

    public string WardName { get; set; } = string.Empty;

    public string WardCode { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsActive { get; set; }

    public int TotalBeds { get; set; }

    public int OccupiedBeds { get; set; }

    public int AvailableBeds { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
