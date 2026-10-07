using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Admissions;

public class AllocateBedRequest
{
    [Required]
    [SelectedId]
    public int AdmissionId { get; set; }

    [Required]
    [SelectedId]
    public int BedId { get; set; }

    [MaxLength(500)]
    [NoHtml]
    public string? Notes { get; set; }
}
