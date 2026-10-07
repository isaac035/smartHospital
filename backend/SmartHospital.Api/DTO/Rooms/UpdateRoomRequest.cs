using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Rooms;

public class UpdateRoomRequest
{
    [Required]
    [MaxLength(30)]
    [NoHtml]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    [DefinedEnum]
    public RoomType Type { get; set; } = RoomType.Standard;

    [Required]
    [Range(1, 20)]
    public int Capacity { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;
}
