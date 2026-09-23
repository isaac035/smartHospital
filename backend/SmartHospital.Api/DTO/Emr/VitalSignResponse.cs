namespace SmartHospital.Api.DTOs.Emr;

public class VitalSignResponse
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int RecordedByUserId { get; set; }

    public string RecordedByUserName { get; set; } = string.Empty;

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
}
