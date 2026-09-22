using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Beds;

public class CreateBedRequest
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    [MaxLength(30)]
    public string BedNumber { get; set; } = string.Empty;

    [Required]
    public BedType Type { get; set; } = BedType.Standard;
}
