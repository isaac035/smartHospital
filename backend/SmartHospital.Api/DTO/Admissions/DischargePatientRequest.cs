using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Admissions;

public class DischargePatientRequest
{
    [Required]
    [MaxLength(1000)]
    public string DischargeSummary { get; set; } = string.Empty;
}
