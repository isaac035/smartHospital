using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SmartHospital.Api.DTOs.Agent4;

public class GenerateMedicalReportRequest
{
    public int? PatientId { get; set; }
    [Range(1, int.MaxValue)] public int? AppointmentId { get; set; }
    public Guid? GenerationId { get; set; }
    public bool? CheckupRequested { get; set; }
}

public class MedicalReportListItem
{
    public Guid ReportId { get; set; }
    public Guid? GenerationId { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AppointmentType { get; set; }
    public string? DoctorName { get; set; }
    public string? Priority { get; set; }
}

public class MedicalReportResponse : MedicalReportListItem
{
    public JsonElement Content { get; set; }
}

public class Agent4ReasonRequest
{
    public JsonElement RecordedData { get; set; }
}

public class Agent4ReasonResponse
{
    public string AiSummary { get; set; } = string.Empty;
    public List<string> AiRecommendations { get; set; } = new();
    public string Mode { get; set; } = "non_ai_fallback";
}
