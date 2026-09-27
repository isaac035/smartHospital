using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using Xunit;

namespace SmartHospital.Tests.Unit.Validation;

public class LabOrderValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, validationResults, validateAllProperties: true);
        return validationResults;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateLabOrderRequest Validation Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateLabOrderRequest_WithValidData_PassesValidation()
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            MedicalRecordId = 5,
            TestName = "Complete Blood Count",
            Category = "Hematology",
            Priority = LabOrderPriority.Routine,
            ClinicalNotes = "Routine pre-op test"
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateLabOrderRequest_WithInvalidPatientId_FailsValidation(int invalidPatientId)
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = invalidPatientId,
            TestName = "Blood Glucose"
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("positive integer"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateLabOrderRequest_WithInvalidMedicalRecordId_FailsValidation(int invalidRecordId)
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            MedicalRecordId = invalidRecordId,
            TestName = "Blood Glucose"
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("positive integer"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateLabOrderRequest_WithEmptyTestName_FailsValidation(string? testName)
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            TestName = testName!
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void CreateLabOrderRequest_WithTestNameExceeding120Chars_FailsValidation()
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            TestName = new string('X', 121)
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("120 characters"));
    }

    [Fact]
    public void CreateLabOrderRequest_WithCategoryExceeding100Chars_FailsValidation()
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            TestName = "CBC",
            Category = new string('C', 101)
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public void CreateLabOrderRequest_WithClinicalNotesExceeding1000Chars_FailsValidation()
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            TestName = "CBC",
            ClinicalNotes = new string('N', 1001)
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("1000 characters"));
    }

    [Fact]
    public void CreateLabOrderRequest_WithInvalidPriorityEnum_FailsValidation()
    {
        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            TestName = "CBC",
            Priority = (LabOrderPriority)99
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Invalid lab order priority"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RecordLabReportRequest Validation Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RecordLabReportRequest_WithValidData_PassesValidation()
    {
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Normal hemoglobin level",
            Findings = "Hb 14.5 g/dL, Platelets 250 x10^3/uL",
            ReferenceRange = "13.5-17.5 g/dL",
            DoctorRemarks = "No further intervention needed",
            AttachmentUrl = "https://hospital.local/reports/lab-123.pdf",
            ReportDate = DateTime.UtcNow.AddMinutes(-10)
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void RecordLabReportRequest_WithEmptyResultSummary_FailsValidation(string? summary)
    {
        var request = new RecordLabReportRequest
        {
            ResultSummary = summary!,
            Findings = "Normal findings"
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void RecordLabReportRequest_WithEmptyFindings_FailsValidation(string? findings)
    {
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Normal summary",
            Findings = findings!
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void RecordLabReportRequest_WithResultSummaryExceeding500Chars_FailsValidation()
    {
        var request = new RecordLabReportRequest
        {
            ResultSummary = new string('S', 501),
            Findings = "Valid findings"
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("500 characters"));
    }

    [Fact]
    public void RecordLabReportRequest_WithFindingsExceeding2000Chars_FailsValidation()
    {
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Valid summary",
            Findings = new string('F', 2001)
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("2000 characters"));
    }

    [Fact]
    public void RecordLabReportRequest_WithFutureReportDate_FailsValidation()
    {
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Valid summary",
            Findings = "Valid findings",
            ReportDate = DateTime.UtcNow.AddDays(2)
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("future"));
    }

    [Theory]
    [InlineData("ftp://files.example.com/report.pdf")]
    [InlineData("not-a-valid-url")]
    public void RecordLabReportRequest_WithInvalidAttachmentUrl_FailsValidation(string invalidUrl)
    {
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Valid summary",
            Findings = "Valid findings",
            AttachmentUrl = invalidUrl
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Attachment URL must be a valid HTTP or HTTPS URL"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateLabOrderStatusRequest Validation Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateLabOrderStatusRequest_WithValidStatus_PassesValidation()
    {
        var request = new UpdateLabOrderStatusRequest
        {
            Status = LabOrderStatus.SampleCollected
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateLabOrderStatusRequest_WithInvalidStatusEnum_FailsValidation()
    {
        var request = new UpdateLabOrderStatusRequest
        {
            Status = (LabOrderStatus)999
        };

        var results = ValidateModel(request);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Invalid lab order status"));
    }
}
