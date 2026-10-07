using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;
namespace SmartHospital.Api.DTOs.Doctors;

public class DoctorFilterRequest
{
    public bool IncludeInactive { get; set; }

    public int? DepartmentId { get; set; }

    [MaxLength(150)]
    [NoHtml]
    public string? Specialization { get; set; }

    public int? MinExperience { get; set; }

    public int? ConsultationTypeId { get; set; }

    [MaxLength(100)]
    [NoHtml]
    public string? SearchTerm { get; set; }
}
