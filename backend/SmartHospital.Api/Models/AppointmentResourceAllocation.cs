namespace SmartHospital.Api.Models;

public class AppointmentResourceAllocation
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int? BedId { get; set; }
    public int? MedicalResourceId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int? DepartmentId { get; set; }
    public string ClinicalSpecialty { get; set; } = string.Empty;
    public AppointmentPriority Priority { get; set; }
    public DateTime AllocatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Appointment? Appointment { get; set; }
    public Bed? Bed { get; set; }
    public MedicalResource? MedicalResource { get; set; }
}
