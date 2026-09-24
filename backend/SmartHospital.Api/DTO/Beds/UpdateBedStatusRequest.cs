using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Beds;

public class UpdateBedStatusRequest
{
    [Required]
    public BedStatus Status { get; set; }
}
