using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.MedicalResources;

public class CreateResourceRequest
{
    [Required]
    [MaxLength(50)]
    public string ResourceCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ResourceCategory Category { get; set; } = ResourceCategory.Other;

    [Required]
    [MaxLength(200)]
    public string LocationDescription { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? ModelNumber { get; set; }

    public int? WardId { get; set; }

    public int? RoomId { get; set; }

    public int? BedId { get; set; }
}
