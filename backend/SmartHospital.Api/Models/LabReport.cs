namespace SmartHospital.Api.Models;

public class LabReport
{
    public int Id { get; set; }

    public int LabOrderId { get; set; }

    public int ConductedByUserId { get; set; }

    public DateTime ReportDate { get; set; }

    public string ResultSummary { get; set; } = string.Empty;

    public string Findings { get; set; } = string.Empty;

    public string ReferenceRange { get; set; } = string.Empty;

    public string DoctorRemarks { get; set; } = string.Empty;

    public string AttachmentUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public LabOrder? LabOrder { get; set; }

    public User? ConductedByUser { get; set; }
}
