using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.DTOs.Emr;
using Xunit;

namespace SmartHospital.Tests.Unit.Validation;

public class MedicalRecordValidationTests
{
    [Fact]
    public void CreateMedicalRecordRequest_WithValidFields_PassesValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            AppointmentId = 5,
            AdmissionId = 10,
            ChiefComplaint = "Severe headache and sensitivity to light",
            Symptoms = "Nausea, throbbing pain on left side of head",
            ExaminationNotes = "Pupils reactive, no focal neurological deficits",
            Diagnosis = "Migraine with aura",
            TreatmentPlan = "Prescribed sumatriptan and dark room rest",
            FollowUpDate = DateTime.UtcNow.AddDays(7)
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithMinimalValidFields_PassesValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 2,
            ChiefComplaint = "Cough and sore throat",
            Diagnosis = "Upper respiratory tract infection"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreateMedicalRecordRequest_WithInvalidPatientId_FailsValidation(int invalidPatientId)
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = invalidPatientId,
            ChiefComplaint = "Fever and chills",
            Diagnosis = "Influenza"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.PatientId)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateMedicalRecordRequest_WithEmptyOrWhitespaceChiefComplaint_FailsValidation(string? chiefComplaint)
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = chiefComplaint!,
            Diagnosis = "Hypertension"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.ChiefComplaint)));
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithExcessivelyLongChiefComplaint_FailsValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = new string('A', 501),
            Diagnosis = "Hypertension"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.ChiefComplaint)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateMedicalRecordRequest_WithEmptyOrWhitespaceDiagnosis_FailsValidation(string? diagnosis)
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Abdominal pain",
            Diagnosis = diagnosis!
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.Diagnosis)));
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithExcessivelyLongDiagnosis_FailsValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Abdominal pain",
            Diagnosis = new string('D', 501)
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.Diagnosis)));
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithExcessivelyLongSymptoms_FailsValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Joint pain",
            Symptoms = new string('S', 1001),
            Diagnosis = "Osteoarthritis"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.Symptoms)));
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithExcessivelyLongExaminationNotes_FailsValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Back pain",
            ExaminationNotes = new string('E', 2001),
            Diagnosis = "Lumbar strain"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.ExaminationNotes)));
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithExcessivelyLongTreatmentPlan_FailsValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Skin rash",
            TreatmentPlan = new string('T', 2001),
            Diagnosis = "Contact dermatitis"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.TreatmentPlan)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateMedicalRecordRequest_WithInvalidAppointmentId_FailsValidation(int invalidAppointmentId)
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            AppointmentId = invalidAppointmentId,
            ChiefComplaint = "Routine follow-up",
            Diagnosis = "Controlled diabetes"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.AppointmentId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CreateMedicalRecordRequest_WithInvalidAdmissionId_FailsValidation(int invalidAdmissionId)
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            AdmissionId = invalidAdmissionId,
            ChiefComplaint = "Post-op review",
            Diagnosis = "Appendectomy recovery"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.AdmissionId)));
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithPastFollowUpDate_FailsValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Fever",
            Diagnosis = "Viral flu",
            FollowUpDate = DateTime.UtcNow.AddDays(-2)
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.FollowUpDate)));
    }

    [Fact]
    public void CreateMedicalRecordRequest_WithDistantFollowUpDate_FailsValidation()
    {
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Fever",
            Diagnosis = "Viral flu",
            FollowUpDate = DateTime.UtcNow.AddYears(6)
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateMedicalRecordRequest.FollowUpDate)));
    }

    [Fact]
    public void UpdateMedicalRecordRequest_WithValidFields_PassesValidation()
    {
        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Updated complaint: resolving migraine",
            Symptoms = "Mild residual headache",
            ExaminationNotes = "Normal cranial nerves",
            Diagnosis = "Migraine resolving",
            TreatmentPlan = "Reduce medication as symptoms subside",
            FollowUpDate = DateTime.UtcNow.AddDays(14),
            AppointmentId = 8,
            AdmissionId = 12
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateMedicalRecordRequest_WithEmptyOrWhitespaceChiefComplaint_FailsValidation(string? chiefComplaint)
    {
        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = chiefComplaint!,
            Diagnosis = "Bronchitis"
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.ChiefComplaint)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateMedicalRecordRequest_WithEmptyOrWhitespaceDiagnosis_FailsValidation(string? diagnosis)
    {
        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Persistent cough",
            Diagnosis = diagnosis!
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.Diagnosis)));
    }

    [Fact]
    public void UpdateMedicalRecordRequest_WithExcessivelyLongFields_FailsValidation()
    {
        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = new string('C', 501),
            Symptoms = new string('S', 1001),
            ExaminationNotes = new string('E', 2001),
            Diagnosis = new string('D', 501),
            TreatmentPlan = new string('T', 2001)
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.ChiefComplaint)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.Symptoms)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.ExaminationNotes)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.Diagnosis)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.TreatmentPlan)));
    }

    [Fact]
    public void UpdateMedicalRecordRequest_WithPastFollowUpDate_FailsValidation()
    {
        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Chest congestion",
            Diagnosis = "Acute bronchitis",
            FollowUpDate = DateTime.UtcNow.AddDays(-1)
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.FollowUpDate)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void UpdateMedicalRecordRequest_WithInvalidAppointmentOrAdmissionId_FailsValidation(int invalidId)
    {
        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Follow up",
            Diagnosis = "Stable",
            AppointmentId = invalidId,
            AdmissionId = invalidId
        };

        var context = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.AppointmentId)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UpdateMedicalRecordRequest.AdmissionId)));
    }
}
