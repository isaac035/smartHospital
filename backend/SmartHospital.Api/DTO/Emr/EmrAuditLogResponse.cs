namespace SmartHospital.Api.DTOs.Emr;

public class EmrAuditLogResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public int? PatientId { get; set; }

    public DateTime Timestamp { get; set; }

    public string? Metadata { get; set; }

    public bool IsSuccess { get; set; }
}
