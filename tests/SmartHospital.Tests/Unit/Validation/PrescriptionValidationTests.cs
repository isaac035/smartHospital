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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void CreatePrescriptionRequest_WithInvalidPatientId_FailsValidation(int invalidPatientId)
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Paracetamol",
            Dosage = "500mg",
            Frequency = "Twice daily",
            DurationDays = 3
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = invalidPatientId,
            Items = new List<CreatePrescriptionItemRequest> { item }
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionRequest.PatientId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreatePrescriptionRequest_WithInvalidMedicalRecordId_FailsValidation(int invalidRecordId)
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Ibuprofen",
            Dosage = "400mg",
            Frequency = "As needed",
            DurationDays = 5
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            MedicalRecordId = invalidRecordId,
            Items = new List<CreatePrescriptionItemRequest> { item }
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionRequest.MedicalRecordId)));
    }

    [Fact]
    public void CreatePrescriptionRequest_WithValidMedicalRecordId_PassesValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Ibuprofen",
            Dosage = "400mg",
            Frequency = "As needed",
            DurationDays = 5
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            MedicalRecordId = 10,
            Items = new List<CreatePrescriptionItemRequest> { item }
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePrescriptionRequest_WithPastExpiryDate_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Metformin",
            Dosage = "500mg",
            Frequency = "Daily",
            DurationDays = 30
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(-1),
            Items = new List<CreatePrescriptionItemRequest> { item }
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionRequest.ExpiryDate)));
    }

    [Fact]
    public void CreatePrescriptionRequest_WithDistantExpiryDate_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Metformin",
            Dosage = "500mg",
            Frequency = "Daily",
            DurationDays = 30
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            Items = new List<CreatePrescriptionItemRequest> { item }
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionRequest.ExpiryDate)));
    }

    [Fact]
    public void CreatePrescriptionRequest_WithExcessivelyLongGeneralInstructions_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Omeprazole",
            Dosage = "20mg",
            Frequency = "Before breakfast",
            DurationDays = 14
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            GeneralInstructions = new string('G', 1001),
            Items = new List<CreatePrescriptionItemRequest> { item }
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionRequest.GeneralInstructions)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePrescriptionItemRequest_WithEmptyMedicineName_FailsValidation(string? medicineName)
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = medicineName!,
            Dosage = "500mg",
            Frequency = "Once daily",
            DurationDays = 7
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.MedicineName)));
    }

    [Fact]
    public void CreatePrescriptionItemRequest_WithExcessivelyLongMedicineName_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = new string('M', 151),
            Dosage = "500mg",
            Frequency = "Once daily",
            DurationDays = 7
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.MedicineName)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePrescriptionItemRequest_WithEmptyDosage_FailsValidation(string? dosage)
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Aspirin",
            Dosage = dosage!,
            Frequency = "Once daily",
            DurationDays = 7
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.Dosage)));
    }

    [Fact]
    public void CreatePrescriptionItemRequest_WithExcessivelyLongDosage_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Aspirin",
            Dosage = new string('D', 51),
            Frequency = "Once daily",
            DurationDays = 7
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.Dosage)));
    }

    [Fact]
    public void CreatePrescriptionItemRequest_WithExcessivelyLongRoute_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Aspirin",
            Dosage = "100mg",
            Route = new string('R', 51),
            Frequency = "Once daily",
            DurationDays = 7
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.Route)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePrescriptionItemRequest_WithEmptyFrequency_FailsValidation(string? frequency)
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Aspirin",
            Dosage = "100mg",
            Frequency = frequency!,
            DurationDays = 7
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.Frequency)));
    }

    [Fact]
    public void CreatePrescriptionItemRequest_WithExcessivelyLongFrequency_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Aspirin",
            Dosage = "100mg",
            Frequency = new string('F', 51),
            DurationDays = 7
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.Frequency)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(366)]
    [InlineData(1000)]
    public void CreatePrescriptionItemRequest_WithInvalidDurationDays_FailsValidation(int invalidDuration)
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Aspirin",
            Dosage = "100mg",
            Frequency = "Once daily",
            DurationDays = invalidDuration
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.DurationDays)));
    }

    [Fact]
    public void CreatePrescriptionItemRequest_WithExcessivelyLongSpecialInstructions_FailsValidation()
    {
        var item = new CreatePrescriptionItemRequest
        {
            MedicineName = "Aspirin",
            Dosage = "100mg",
            Frequency = "Once daily",
            DurationDays = 7,
            SpecialInstructions = new string('S', 501)
        };

        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreatePrescriptionItemRequest.SpecialInstructions)));
    }

    [Fact]
    public void CreatePrescriptionRequest_WithInvalidNestedItem_FailsValidation()
    {
        var invalidItem = new CreatePrescriptionItemRequest
        {
            MedicineName = "   ", // Invalid whitespace
            Dosage = "500mg",
            Frequency = "Daily",
            DurationDays = 0 // Invalid duration
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            Items = new List<CreatePrescriptionItemRequest> { invalidItem }
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.NotEmpty(validationResults);
    }
}
