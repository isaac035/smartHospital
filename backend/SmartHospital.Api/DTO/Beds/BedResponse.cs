namespace SmartHospital.Api.DTOs.Beds;

public class BedResponse
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public int WardId { get; set; }

    public string WardName { get; set; } = string.Empty;

    public string WardCode { get; set; } = string.Empty;

    public string Floor { get; set; } = string.Empty;

    public string BedNumber { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int? CurrentPatientId { get; set; }

    public string? CurrentPatientName { get; set; }

    public int? CurrentAdmissionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
