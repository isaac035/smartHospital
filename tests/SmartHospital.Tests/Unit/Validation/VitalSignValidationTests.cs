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
}
