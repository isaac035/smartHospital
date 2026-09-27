namespace SmartHospital.Api.DTOs.Doctors;

public class DoctorFilterRequest
{
    public int? DepartmentId { get; set; }

    public string? Specialization { get; set; }

    public int? MinExperience { get; set; }

    public int? ConsultationTypeId { get; set; }

    public string? SearchTerm { get; set; }
}
