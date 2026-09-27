namespace SmartHospital.Api.DTOs.Doctors;

public class AvailableDoctorFilterRequest
{
    public int? DepartmentId { get; set; }

    public int? ConsultationTypeId { get; set; }

    public DateOnly? Date { get; set; }
}
