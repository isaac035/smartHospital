namespace SmartHospital.Api.Models;

public class Bed
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public string BedNumber { get; set; } = string.Empty;

    public BedType Type { get; set; } = BedType.Standard;

    public BedStatus Status { get; set; } = BedStatus.Available;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Room? Room { get; set; }

    public ICollection<BedAllocation> Allocations { get; set; } = new List<BedAllocation>();
}
