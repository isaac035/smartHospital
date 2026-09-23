using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class CreatePrescriptionRequest
{
    [Required]
    public int PatientId { get; set; }

    public int? MedicalRecordId { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(1000)]
    public string GeneralInstructions { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one prescription item is required.")]
    public List<CreatePrescriptionItemRequest> Items { get; set; } = new();
}

public class CreatePrescriptionItemRequest
{
    [Required]
    [MaxLength(150)]
    public string MedicineName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Dosage { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Route { get; set; } = "Oral";

    [Required]
    [MaxLength(50)]
    public string Frequency { get; set; } = string.Empty;

    [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days.")]
    public int DurationDays { get; set; } = 1;

    [MaxLength(500)]
    public string SpecialInstructions { get; set; } = string.Empty;
}
