using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.Models;

public class PrescriptionItem
{
    public int Id { get; set; }

    public int PrescriptionId { get; set; }

    [Required]
    [MaxLength(150)]
    public string MedicineName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Dosage { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Route { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Frequency { get; set; } = string.Empty;

    [Range(1, 365)]
    public int DurationDays { get; set; }

    [MaxLength(500)]
    public string SpecialInstructions { get; set; } = string.Empty;

    // Navigation property
    public Prescription? Prescription { get; set; }
}
