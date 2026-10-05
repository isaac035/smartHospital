namespace SmartHospital.Api.Models;

public class Doctor
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int DepartmentId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string LicenseNumber { get; set; } = string.Empty;

    public int YearsOfExperience { get; set; }

    public string Bio { get; set; } = string.Empty;

    public DoctorStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Department? Department { get; set; }
}
