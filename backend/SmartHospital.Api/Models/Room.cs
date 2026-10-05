namespace SmartHospital.Api.Models;

public class Room
{
    public int Id { get; set; }

    public int WardId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public RoomType Type { get; set; } = RoomType.Standard;

    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Ward? Ward { get; set; }

    public ICollection<Bed> Beds { get; set; } = new List<Bed>();
}
