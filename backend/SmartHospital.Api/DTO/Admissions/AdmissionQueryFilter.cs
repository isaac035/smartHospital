using SmartHospital.Api.Models;

namespace SmartHospital.Api.DTOs.Admissions;

public class AdmissionQueryFilter
{
    public string? PatientSearch { get; set; }

    public int? WardId { get; set; }

    public int? DoctorId { get; set; }

    public AdmissionStatus? Status { get; set; }

    public AdmissionPriority? Priority { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
