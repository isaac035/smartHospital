using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Admissions;

public class DischargePatientRequest
{
    [Required]
    [MaxLength(1000)]
    [NoHtml]
    public string DischargeSummary { get; set; } = string.Empty;
}
