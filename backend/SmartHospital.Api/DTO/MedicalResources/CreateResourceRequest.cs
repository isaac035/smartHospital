using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.MedicalResources;

public class CreateResourceRequest
{
    [Required]
    [MaxLength(50)]
    [NoHtml]
    public string ResourceCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    [NoHtml]
    public string Name { get; set; } = string.Empty;

    [Required]
    [DefinedEnum]
    public ResourceCategory Category { get; set; } = ResourceCategory.Other;

    [Required]
    [MaxLength(200)]
    [NoHtml]
    public string LocationDescription { get; set; } = string.Empty;

    [MaxLength(100)]
    [NoHtml]
    public string? SerialNumber { get; set; }

    [MaxLength(100)]
    [NoHtml]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    [NoHtml]
    public string? ModelNumber { get; set; }

    public int? WardId { get; set; }

    public int? RoomId { get; set; }

    public int? BedId { get; set; }
}
