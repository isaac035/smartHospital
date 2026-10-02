using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Agent3;

public class RecommendResourcesRequest
{
    [Range(1, int.MaxValue)] public int AppointmentId { get; set; }
}

public class SelectResourceRequest
{
    [Range(1, int.MaxValue)] public int AppointmentId { get; set; }
    [Range(1, int.MaxValue)] public int SelectedResourceId { get; set; }
    [Required] public string Kind { get; set; } = string.Empty;
    // A date-only value preserves the day selected in the patient's local calendar.
    [Required] public DateOnly? AdmissionDate { get; set; }
}

public class ResourceCandidateDto
{
    public int ResourceId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SpecialtyText { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RecommendResourcesResponse
{
    public int AppointmentId { get; set; }
    public string ClinicalSpecialty { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public List<ResourceCandidateDto> Recommendations { get; set; } = new();
    public string? Message { get; set; }
}

public class Agent3ServiceRequest
{
    public int AppointmentId { get; set; }
    public string ClinicalSpecialty { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public List<ResourceCandidateDto> Candidates { get; set; } = new();
}

public class AllocationResponse
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AllocatedAt { get; set; }
}
