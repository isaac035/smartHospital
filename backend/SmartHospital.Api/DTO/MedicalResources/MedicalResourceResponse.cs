namespace SmartHospital.Api.DTOs.MedicalResources;

public class MedicalResourceResponse
{
    public int Id { get; set; }

    public string ResourceCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string LocationDescription { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? Manufacturer { get; set; }

    public string? ModelNumber { get; set; }

    public int? WardId { get; set; }

    public string? WardName { get; set; }

    public int? RoomId { get; set; }

    public string? RoomNumber { get; set; }

    public int? BedId { get; set; }

    public string? BedNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
