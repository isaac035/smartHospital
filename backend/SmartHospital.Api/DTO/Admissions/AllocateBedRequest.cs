using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Admissions;

public class AllocateBedRequest
{
    [Required]
    public int AdmissionId { get; set; }

    [Required]
    public int BedId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
