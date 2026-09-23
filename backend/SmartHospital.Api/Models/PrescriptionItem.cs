namespace SmartHospital.Api.Models;

public class PrescriptionItem
{
    public int Id { get; set; }

    public int PrescriptionId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public int DurationDays { get; set; }

    public string SpecialInstructions { get; set; } = string.Empty;

    // Navigation property
    public Prescription? Prescription { get; set; }
}
