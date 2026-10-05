using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Wards;

public class CreateWardRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Floor { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? BuildingBlock { get; set; }

    [Required]
    [Range(1, 500)]
    public int Capacity { get; set; }

    [Required]
    public WardType Type { get; set; } = WardType.General;
}
