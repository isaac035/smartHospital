using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.DTOs.Emr;
using Xunit;

namespace SmartHospital.Tests.Unit.Validation;

public class VitalSignValidationTests
{
    [Fact]
    public void RecordVitalSignRequest_WithValidValues_PassesValidation()
    {
        // Arrange
        var request = new RecordVitalSignRequest
        {
            PatientId = 1,
            TemperatureCelsius = 37.0m,
            SystolicBloodPressure = 120,
            DiastolicBloodPressure = 80,
            HeartRateBpm = 72,
            RespiratoryRateBpm = 16,
            OxygenSaturationSpO2 = 98.5m,
            WeightKg = 70.0m,
            HeightCm = 175.0m,
            Notes = "Patient resting comfortably"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(25.0)] // Below minimum 30.0
    [InlineData(29.9)] // Just below minimum
    [InlineData(45.1)] // Just above maximum
    [InlineData(50.0)] // Above maximum 45.0
    public void RecordVitalSignRequest_WithInvalidTemperature_FailsValidation(double temp)
    {
        // Arrange
        var request = new RecordVitalSignRequest
        {
            PatientId = 1,
            TemperatureCelsius = (decimal)temp
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.TemperatureCelsius)));
    }

    [Theory]
    [InlineData(30.0)] // Min boundary
    [InlineData(37.0)] // Normal
    [InlineData(45.0)] // Max boundary
    public void RecordVitalSignRequest_WithBoundaryTemperature_PassesValidation(double temp)
    {
        // Arrange
        var request = new RecordVitalSignRequest
        {
            PatientId = 1,
            TemperatureCelsius = (decimal)temp
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(49)]  // Below min 50
    [InlineData(251)] // Above max 250
    public void RecordVitalSignRequest_WithInvalidSystolicBP_FailsValidation(int bp)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, SystolicBloodPressure = bp };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.SystolicBloodPressure)));
    }

    [Theory]
    [InlineData(50)]  // Min boundary
    [InlineData(120)] // Normal
    [InlineData(250)] // Max boundary
    public void RecordVitalSignRequest_WithBoundarySystolicBP_PassesValidation(int bp)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, SystolicBloodPressure = bp };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(29)]  // Below min 30
    [InlineData(151)] // Above max 150
    public void RecordVitalSignRequest_WithInvalidDiastolicBP_FailsValidation(int bp)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, DiastolicBloodPressure = bp };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.DiastolicBloodPressure)));
    }

    [Theory]
    [InlineData(29)]  // Below min 30
    [InlineData(251)] // Above max 250
    public void RecordVitalSignRequest_WithInvalidHeartRate_FailsValidation(int hr)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, HeartRateBpm = hr };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.HeartRateBpm)));
    }

    [Theory]
    [InlineData(4)]  // Below min 5
    [InlineData(61)] // Above max 60
    public void RecordVitalSignRequest_WithInvalidRespiratoryRate_FailsValidation(int rr)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, RespiratoryRateBpm = rr };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.RespiratoryRateBpm)));
    }

    [Theory]
    [InlineData(49.9)]  // Below min 50.0
    [InlineData(100.1)] // Above max 100.0
    public void RecordVitalSignRequest_WithInvalidSpO2_FailsValidation(double spo2)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, OxygenSaturationSpO2 = (decimal)spo2 };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.OxygenSaturationSpO2)));
    }

    [Theory]
    [InlineData(0.9)]   // Below min 1.0
    [InlineData(500.1)] // Above max 500.0
    public void RecordVitalSignRequest_WithInvalidWeight_FailsValidation(double weight)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, WeightKg = (decimal)weight };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.WeightKg)));
    }

    [Theory]
    [InlineData(29.9)]  // Below min 30.0
    [InlineData(300.1)] // Above max 300.0
    public void RecordVitalSignRequest_WithInvalidHeight_FailsValidation(double height)
    {
        var request = new RecordVitalSignRequest { PatientId = 1, HeightCm = (decimal)height };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.HeightCm)));
    }

    [Fact]
    public void RecordVitalSignRequest_WithNotesExceeding500Chars_FailsValidation()
    {
        var request = new RecordVitalSignRequest
        {
            PatientId = 1,
            Notes = new string('A', 501)
        };
        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RecordVitalSignRequest.Notes)));
    }
}
