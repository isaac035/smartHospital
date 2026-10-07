using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Admissions;

public class TransferPatientRequest
{
    [Required]
    [SelectedId]
    public int NewBedId { get; set; }

    [Required]
    [MaxLength(500)]
    [NoHtml]
    public string TransferReason { get; set; } = string.Empty;
}
