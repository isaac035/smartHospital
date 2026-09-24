using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Rooms;

public class UpdateRoomRequest
{
    [Required]
    [MaxLength(30)]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    public RoomType Type { get; set; } = RoomType.Standard;

    [Required]
    [Range(1, 20)]
    public int Capacity { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;
}
