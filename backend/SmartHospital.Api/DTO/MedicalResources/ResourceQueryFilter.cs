using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.MedicalResources;

public class ResourceQueryFilter
{
    public ResourceCategory? Category { get; set; }

    public ResourceStatus? Status { get; set; }

    public int? WardId { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
