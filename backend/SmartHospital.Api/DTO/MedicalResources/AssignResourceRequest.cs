using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.MedicalResources;

/// <summary>
/// Request to assign or relocate a medical resource.
/// Note: WardId, RoomId, and BedId are optional, but service-layer validation
/// must verify consistent hierarchy (e.g., if BedId is provided, it must belong
/// to RoomId and WardId; or if unassigned, all three may be cleared).
/// </summary>
public class AssignResourceRequest
{
    public int? WardId { get; set; }

    public int? RoomId { get; set; }

    public int? BedId { get; set; }

    [MaxLength(200)]
    public string? LocationDescription { get; set; }
}
