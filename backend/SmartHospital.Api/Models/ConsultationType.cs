namespace SmartHospital.Api.Models;

public class ConsultationType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public string Description { get; set; } = string.Empty;

    public ConsultationTypeStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
