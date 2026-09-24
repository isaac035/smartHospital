using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Beds;

public class BedQueryFilter
{
    public int? WardId { get; set; }

    public int? RoomId { get; set; }

    public string? Floor { get; set; }

    public string? BedNumber { get; set; }

    public BedStatus? Status { get; set; }

    public BedType? Type { get; set; }

    public bool? IsAvailable { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
