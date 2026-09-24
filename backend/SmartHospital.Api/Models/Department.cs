namespace SmartHospital.Api.Models;

// TODO: owned by Doctor & Clinical Schedule Management module.
// Minimal stub created by Appointment & Queue module for FK reference only.
// Do NOT add clinical/scheduling logic here.
public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
