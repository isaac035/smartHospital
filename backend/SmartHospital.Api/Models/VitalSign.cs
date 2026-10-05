namespace SmartHospital.Api.Models;

public class VitalSign
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int RecordedByUserId { get; set; }

    public int? MedicalRecordId { get; set; }

    public DateTime RecordedAt { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public int? SystolicBloodPressure { get; set; }

    public int? DiastolicBloodPressure { get; set; }

    public int? HeartRateBpm { get; set; }

    public int? RespiratoryRateBpm { get; set; }

    public decimal? OxygenSaturationSpO2 { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? Bmi { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? Patient { get; set; }

    public User? RecordedByUser { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }
}
