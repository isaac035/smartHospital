using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Admissions;

public class TransferPatientRequest
{
    [Required]
    public int NewBedId { get; set; }

    [Required]
    [MaxLength(500)]
    public string TransferReason { get; set; } = string.Empty;
}
