using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Beds;

public class UpdateBedRequest
{
    [Required]
    [MaxLength(30)]
    [NoHtml]
    public string BedNumber { get; set; } = string.Empty;

    [Required]
    [DefinedEnum]
    public BedType Type { get; set; } = BedType.Standard;

    [Required]
    public bool IsActive { get; set; } = true;
}
