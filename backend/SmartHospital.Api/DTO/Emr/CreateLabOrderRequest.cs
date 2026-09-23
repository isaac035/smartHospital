using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Emr;

public class CreateLabOrderRequest
{
    [Required]
    public int PatientId { get; set; }

    public int? MedicalRecordId { get; set; }

    [Required]
    [MaxLength(120)]
    public string TestName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public LabOrderPriority Priority { get; set; } = LabOrderPriority.Routine;

    [MaxLength(1000)]
    public string ClinicalNotes { get; set; } = string.Empty;
}
