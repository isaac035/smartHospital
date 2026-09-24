using SmartHospital.Api.DTOs.Rooms;

namespace SmartHospital.Api.Services.Interfaces;

public interface IRoomService
{
    Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request);
    Task<List<RoomResponse>> GetRoomsByWardAsync(int wardId, bool? isActive = null);
    Task<RoomResponse?> GetRoomByIdAsync(int id);
    Task<RoomResponse?> UpdateRoomAsync(int id, UpdateRoomRequest request);
    Task<bool> DeactivateRoomAsync(int id);
}
