namespace SmartHospital.Api.Models;

public class EmrAuditLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public int? PatientId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? Metadata { get; set; }

    public bool IsSuccess { get; set; } = true;
}
