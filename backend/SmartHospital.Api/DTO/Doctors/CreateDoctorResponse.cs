namespace SmartHospital.Api.DTOs.Doctors;

/// <summary>Creation-only response. The initial password is returned once to the admin.</summary>
public class CreateDoctorResponse
{
    public DoctorResponse Doctor { get; set; } = new();

    public string TemporaryPassword { get; set; } = string.Empty;
}
