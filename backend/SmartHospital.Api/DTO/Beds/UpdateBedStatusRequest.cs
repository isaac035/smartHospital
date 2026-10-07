using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Beds;

public class UpdateBedStatusRequest
{
    [Required]
    [DefinedEnum]
    public BedStatus Status { get; set; }
}
