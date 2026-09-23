using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.DTOs.Emr;
using Xunit;

namespace SmartHospital.Tests.Unit.Validation;

public class PrescriptionValidationTests
{
    [Fact]
    public void CreatePrescriptionRequest_WithoutItems_FailsValidation()
    {
        // Arrange
        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            GeneralInstructions = "Take after meals",
            Items = new List<CreatePrescriptionItemRequest>()
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionRequest.Items)));
    }

    [Fact]
    public void CreatePrescriptionRequest_WithValidItem_PassesValidation()
    {
        // Arrange
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Amoxicillin",
            Dosage = "500mg",
            Route = "Oral",
            Frequency = "Three times a day",
            DurationDays = 7,
            SpecialInstructions = "Complete full course"
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            GeneralInstructions = "Take with water",
            Items = new List<CreatePrescriptionItemRequest> { item }
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
