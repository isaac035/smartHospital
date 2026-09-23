using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class RecordLabReportRequest
{
    [Required]
    [MaxLength(500)]
    public string ResultSummary { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Findings { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ReferenceRange { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string DoctorRemarks { get; set; } = string.Empty;

    [MaxLength(500)]
    public string AttachmentUrl { get; set; } = string.Empty;
}
