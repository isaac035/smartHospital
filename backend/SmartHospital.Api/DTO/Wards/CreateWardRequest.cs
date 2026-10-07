using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Wards;

public class CreateWardRequest
{
    [Required]
    [MaxLength(100)]
    [NoHtml]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [NoHtml]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [NoHtml]
    public string Floor { get; set; } = string.Empty;

    [MaxLength(50)]
    [NoHtml]
    public string? BuildingBlock { get; set; }

    [Required]
    [Range(1, 500)]
    public int Capacity { get; set; }

    [Required]
    [DefinedEnum]
    public WardType Type { get; set; } = WardType.General;
}
