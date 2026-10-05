using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Emr;

public class RecordVitalSignRequest
{
    [Required]
    public int PatientId { get; set; }

    public int? MedicalRecordId { get; set; }

    [Range(30.0, 45.0, ErrorMessage = "Temperature must be between 30.0 and 45.0 Celsius.")]
    public decimal? TemperatureCelsius { get; set; }

    [Range(50, 250, ErrorMessage = "Systolic blood pressure must be between 50 and 250 mmHg.")]
    public int? SystolicBloodPressure { get; set; }

    [Range(30, 150, ErrorMessage = "Diastolic blood pressure must be between 30 and 150 mmHg.")]
    public int? DiastolicBloodPressure { get; set; }

    [Range(30, 250, ErrorMessage = "Heart rate must be between 30 and 250 bpm.")]
    public int? HeartRateBpm { get; set; }

    [Range(5, 60, ErrorMessage = "Respiratory rate must be between 5 and 60 breaths/min.")]
    public int? RespiratoryRateBpm { get; set; }

    [Range(50.0, 100.0, ErrorMessage = "SpO2 must be between 50.0% and 100.0%.")]
    public decimal? OxygenSaturationSpO2 { get; set; }

    [Range(1.0, 500.0, ErrorMessage = "Weight must be between 1.0 and 500.0 kg.")]
    public decimal? WeightKg { get; set; }

    [Range(30.0, 300.0, ErrorMessage = "Height must be between 30.0 and 300.0 cm.")]
    public decimal? HeightCm { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;
}
