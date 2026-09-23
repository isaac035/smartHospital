using SmartHospital.Api.DTOs.Rooms;

namespace SmartHospital.Api.DTOs.Wards;

public class WardDetailResponse
{
    public WardResponse Ward { get; set; } = new();

    public List<RoomResponse> Rooms { get; set; } = new();
}
