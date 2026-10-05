namespace SmartHospital.Api.DTOs.Appointments;

public class AppointmentDoctorOptionResponse
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
}
