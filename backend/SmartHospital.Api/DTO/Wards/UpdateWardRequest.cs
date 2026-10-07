using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Wards;

public class UpdateWardRequest
{
    [Required]
    [MaxLength(100)]
    [NoHtml]
    public string Name { get; set; } = string.Empty;

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

    [Required]
    public bool IsActive { get; set; } = true;
}
