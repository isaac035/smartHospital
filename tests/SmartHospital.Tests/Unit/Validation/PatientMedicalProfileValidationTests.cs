using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using Xunit;

namespace SmartHospital.Tests.Unit.Validation;

public class PatientMedicalProfileValidationTests
{
    [Fact]
    public void UpsertPatientMedicalProfileRequest_WithValidValues_PassesValidation()
    {
        // Arrange
        var request = new UpsertPatientMedicalProfileRequest
        {
            DateOfBirth = new DateTime(1990, 5, 20),
            Gender = "Female",
            BloodGroup = BloodGroup.OPositive,
            Allergies = "Peanuts, Penicillin",
            ChronicDiseases = "Mild Asthma",
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "+1-555-1234"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void UpsertPatientMedicalProfileRequest_WithGenderExceeding20Chars_FailsValidation()
    {
        // Arrange
        var request = new UpsertPatientMedicalProfileRequest
        {
            Gender = new string('A', 21) // Max is 20
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpsertPatientMedicalProfileRequest.Gender)));
    }

    [Fact]
    public void UpsertPatientMedicalProfileRequest_WithAllergiesExceeding500Chars_FailsValidation()
    {
        // Arrange
        var request = new UpsertPatientMedicalProfileRequest
        {
            Allergies = new string('X', 501) // Max is 500
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpsertPatientMedicalProfileRequest.Allergies)));
    }

    [Fact]
    public void UpsertPatientMedicalProfileRequest_WithChronicDiseasesExceeding500Chars_FailsValidation()
    {
        // Arrange
        var request = new UpsertPatientMedicalProfileRequest
        {
            ChronicDiseases = new string('Y', 501) // Max is 500
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpsertPatientMedicalProfileRequest.ChronicDiseases)));
    }

    [Fact]
    public void UpsertPatientMedicalProfileRequest_WithEmergencyContactNameExceeding100Chars_FailsValidation()
    {
        // Arrange
        var request = new UpsertPatientMedicalProfileRequest
        {
            EmergencyContactName = new string('N', 101) // Max is 100
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpsertPatientMedicalProfileRequest.EmergencyContactName)));
    }

    [Fact]
    public void UpsertPatientMedicalProfileRequest_WithEmergencyContactPhoneExceeding20Chars_FailsValidation()
    {
        // Arrange
        var request = new UpsertPatientMedicalProfileRequest
        {
            EmergencyContactPhone = new string('1', 21) // Max is 20
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpsertPatientMedicalProfileRequest.EmergencyContactPhone)));
    }

    [Fact]
    public void UpsertPatientMedicalProfileRequest_WithEmptyOptionalFields_PassesValidation()
    {
        // Arrange
        var request = new UpsertPatientMedicalProfileRequest
        {
            DateOfBirth = null,
            Gender = "",
            BloodGroup = BloodGroup.Unknown,
            Allergies = "",
            ChronicDiseases = "",
            EmergencyContactName = "",
            EmergencyContactPhone = ""
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }
}
