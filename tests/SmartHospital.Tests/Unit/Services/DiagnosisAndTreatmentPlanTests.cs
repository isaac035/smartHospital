using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class DiagnosisAndTreatmentPlanTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private (User patient, User doctor, MedicalRecord record) SeedDefaultData(AppDbContext context, int patientId = 1, int doctorId = 2)
    {
        var patient = new User
        {
            Id = patientId,
            FirstName = "Alice",
            LastName = "Brown",
            Email = $"patient{patientId}@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor = new User
        {
            Id = doctorId,
            FirstName = "Gregory",
            LastName = "House",
            Email = $"doctor{doctorId}@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        var record = new MedicalRecord
        {
            Id = 100,
            RecordNumber = "REC-100",
            PatientId = patientId,
            DoctorId = doctorId,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Severe headache and dizziness",
            Diagnosis = "Migraine with aura",
            TreatmentPlan = "Triptans as needed",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            Patient = patient,
            Doctor = doctor
        };

        context.Users.AddRange(patient, doctor);
        context.MedicalRecords.Add(record);
        context.SaveChanges();

        return (patient, doctor, record);
    }

    [Fact]
    public async Task AddDiagnosisAsync_WithValidDetails_AssociatesWithRecordPatientDoctorAndUpdatesMedicalRecord()
    {
        using var context = CreateInMemoryDbContext();
        var (patient, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Code = "G43.109",
            Description = "Migraine with aura, not intractable",
            Type = DiagnosisType.Primary,
            Status = DiagnosisStatus.Active,
            Severity = DiagnosisSeverity.Severe,
            Notes = "Patient reports visual disturbances before headache onset",
            DiagnosedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        var result = await service.AddDiagnosisAsync(record.Id, doctor.Id, request);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(record.Id, result.MedicalRecordId);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal("Alice Brown", result.PatientName);
        Assert.Equal(doctor.Id, result.DoctorId);
        Assert.Equal("Dr. Gregory House", result.DoctorName);
        Assert.Equal("G43.109", result.Code);
        Assert.Equal("Migraine with aura, not intractable", result.Description);
        Assert.Equal("Primary", result.Type);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Severe", result.Severity);
        Assert.Equal("Patient reports visual disturbances before headache onset", result.Notes);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);

        // Verify medical record diagnosis synchronized
        var updatedRecord = await context.MedicalRecords.FindAsync(record.Id);
        Assert.NotNull(updatedRecord);
        Assert.Equal("Migraine with aura, not intractable", updatedRecord.Diagnosis);
        Assert.True(updatedRecord.UpdatedAt >= result.CreatedAt);
    }

    [Fact]
    public async Task AddDiagnosisAsync_WithNonPrimaryType_DoesNotOverwriteMedicalRecordPrimaryDiagnosis()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Code = "I10",
            Description = "Essential hypertension",
            Type = DiagnosisType.Secondary,
            Status = DiagnosisStatus.Active,
            Severity = DiagnosisSeverity.Mild
        };

        var result = await service.AddDiagnosisAsync(record.Id, doctor.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Secondary", result.Type);

        var updatedRecord = await context.MedicalRecords.FindAsync(record.Id);
        Assert.NotNull(updatedRecord);
        Assert.Equal("Migraine with aura", updatedRecord.Diagnosis); // Unchanged primary
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AddDiagnosisAsync_WithEmptyDescription_ThrowsArgumentException(string? description)
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Description = description!
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddDiagnosisAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task AddDiagnosisAsync_WithExceedingDescriptionLength_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Description = new string('D', 501)
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddDiagnosisAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task AddDiagnosisAsync_WithExceedingCodeLength_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Code = new string('C', 51),
            Description = "Valid description"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddDiagnosisAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task AddDiagnosisAsync_WithFutureDate_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Description = "Valid description",
            DiagnosedAt = DateTime.UtcNow.AddDays(5)
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddDiagnosisAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task AddDiagnosisAsync_WhenMedicalRecordNotFound_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, _) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Description = "Valid diagnosis"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddDiagnosisAsync(999, doctor.Id, request));
    }

    [Fact]
    public async Task AddDiagnosisAsync_WhenDoctorNotAuthorizedForRecord_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, _, record) = SeedDefaultData(context);

        var otherDoctor = new User
        {
            Id = 99,
            FirstName = "Other",
            LastName = "Doctor",
            Email = "other@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };
        context.Users.Add(otherDoctor);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Description = "Unauthorized Diagnosis"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AddDiagnosisAsync(record.Id, otherDoctor.Id, request));
    }

    [Fact]
    public async Task AddDiagnosisAsync_WhenDoctorInactive_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);

        doctor.Status = UserStatus.Inactive;
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new AddDiagnosisRequest
        {
            Description = "Valid description"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AddDiagnosisAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task UpdateDiagnosisAsync_WithValidUpdates_UpdatesDiagnosisAndTimestamps()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var addRequest = new AddDiagnosisRequest
        {
            Description = "Initial Diagnosis",
            Type = DiagnosisType.Primary,
            Status = DiagnosisStatus.Active,
            Severity = DiagnosisSeverity.Moderate
        };
        var added = await service.AddDiagnosisAsync(record.Id, doctor.Id, addRequest);

        var updateRequest = new UpdateDiagnosisRequest
        {
            Code = "G43.0",
            Description = "Migraine without aura, controlled",
            Type = DiagnosisType.Primary,
            Status = DiagnosisStatus.Resolved,
            Severity = DiagnosisSeverity.Mild,
            Notes = "Symptoms fully resolved after therapeutic cycle"
        };

        var updated = await service.UpdateDiagnosisAsync(record.Id, added.Id, doctor.Id, updateRequest);

        Assert.NotNull(updated);
        Assert.Equal(added.Id, updated.Id);
        Assert.Equal("Migraine without aura, controlled", updated.Description);
        Assert.Equal("Resolved", updated.Status);
        Assert.Equal("Mild", updated.Severity);
        Assert.Equal("G43.0", updated.Code);

        // Verify medical record was updated
        var updatedRecord = await context.MedicalRecords.FindAsync(record.Id);
        Assert.NotNull(updatedRecord);
        Assert.Equal("Migraine without aura, controlled", updatedRecord.Diagnosis);
    }

    [Fact]
    public async Task UpdateDiagnosisAsync_WhenDiagnosisNotFound_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var updateRequest = new UpdateDiagnosisRequest
        {
            Description = "Some diagnosis"
        };

        var result = await service.UpdateDiagnosisAsync(record.Id, 9999, doctor.Id, updateRequest);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePrimaryDiagnosisAsync_WhenNoExistingDiagnosis_CreatesPrimaryDiagnosis()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new UpdateDiagnosisRequest
        {
            Code = "J06.9",
            Description = "Acute upper respiratory tract infection",
            Type = DiagnosisType.Primary,
            Status = DiagnosisStatus.Active,
            Severity = DiagnosisSeverity.Moderate
        };

        var result = await service.UpdatePrimaryDiagnosisAsync(record.Id, doctor.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Acute upper respiratory tract infection", result.Description);
        Assert.Equal("Primary", result.Type);

        var recordInDb = await context.MedicalRecords.FindAsync(record.Id);
        Assert.NotNull(recordInDb);
        Assert.Equal("Acute upper respiratory tract infection", recordInDb.Diagnosis);
    }

    [Fact]
    public async Task GetDiagnosesAsync_ReturnsAllDiagnosesForRecord_OrderedCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        await service.AddDiagnosisAsync(record.Id, doctor.Id, new AddDiagnosisRequest
        {
            Description = "Secondary Asthma",
            Type = DiagnosisType.Secondary
        });

        await service.AddDiagnosisAsync(record.Id, doctor.Id, new AddDiagnosisRequest
        {
            Description = "Primary Bronchitis",
            Type = DiagnosisType.Primary
        });

        var diagnoses = await service.GetDiagnosesAsync(record.Id);

        Assert.Equal(2, diagnoses.Count);
        Assert.Equal("Primary", diagnoses[0].Type);
        Assert.Equal("Secondary", diagnoses[1].Type);
    }

    [Fact]
    public async Task RecordTreatmentPlanAsync_WithValidDetails_AssociatesWithRecordPatientDoctorAndSetsTimestamps()
    {
        using var context = CreateInMemoryDbContext();
        var (patient, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var start = DateTime.UtcNow;
        var target = start.AddMonths(3);

        var request = new RecordTreatmentPlanRequest
        {
            Title = "Hypertension Step-Care Protocol",
            Category = TreatmentPlanCategory.Pharmacological,
            Description = "Initiate low-dose Lisinopril 10mg daily and monitor serum potassium",
            Goals = "Maintain systolic BP under 130 mmHg",
            Interventions = "Blood pressure self-monitoring twice daily, low sodium diet",
            Status = TreatmentPlanStatus.Active,
            StartDate = start,
            TargetDate = target
        };

        var result = await service.RecordTreatmentPlanAsync(record.Id, doctor.Id, request);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(record.Id, result.MedicalRecordId);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal("Alice Brown", result.PatientName);
        Assert.Equal(doctor.Id, result.DoctorId);
        Assert.Equal("Dr. Gregory House", result.DoctorName);
        Assert.Equal("Hypertension Step-Care Protocol", result.Title);
        Assert.Equal("Pharmacological", result.Category);
        Assert.Equal("Initiate low-dose Lisinopril 10mg daily and monitor serum potassium", result.Description);
        Assert.Equal("Maintain systolic BP under 130 mmHg", result.Goals);
        Assert.Equal("Blood pressure self-monitoring twice daily, low sodium diet", result.Interventions);
        Assert.Equal("Active", result.Status);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);

        // Verify medical record treatment plan updated
        var updatedRecord = await context.MedicalRecords.FindAsync(record.Id);
        Assert.NotNull(updatedRecord);
        Assert.Equal(request.Description, updatedRecord.TreatmentPlan);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RecordTreatmentPlanAsync_WithEmptyTitle_ThrowsArgumentException(string? title)
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new RecordTreatmentPlanRequest
        {
            Title = title!,
            Description = "Valid description"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordTreatmentPlanAsync(record.Id, doctor.Id, request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RecordTreatmentPlanAsync_WithEmptyDescription_ThrowsArgumentException(string? description)
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new RecordTreatmentPlanRequest
        {
            Title = "Valid title",
            Description = description!
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordTreatmentPlanAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task RecordTreatmentPlanAsync_WhenTargetDateBeforeStartDate_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new RecordTreatmentPlanRequest
        {
            Title = "Valid title",
            Description = "Valid description",
            StartDate = DateTime.UtcNow.AddDays(5),
            TargetDate = DateTime.UtcNow.AddDays(2) // Invalid: before start date
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordTreatmentPlanAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task RecordTreatmentPlanAsync_WhenEndDateBeforeStartDate_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new RecordTreatmentPlanRequest
        {
            Title = "Valid title",
            Description = "Valid description",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(2) // Invalid: before start date
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordTreatmentPlanAsync(record.Id, doctor.Id, request));
    }

    [Fact]
    public async Task RecordTreatmentPlanAsync_WhenDoctorNotAuthorized_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateInMemoryDbContext();
        var (_, _, record) = SeedDefaultData(context);

        var otherDoctor = new User
        {
            Id = 88,
            FirstName = "Other",
            LastName = "Physician",
            Email = "physician@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };
        context.Users.Add(otherDoctor);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new RecordTreatmentPlanRequest
        {
            Title = "Valid Title",
            Description = "Valid Description"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RecordTreatmentPlanAsync(record.Id, otherDoctor.Id, request));
    }

    [Fact]
    public async Task UpdateTreatmentPlanAsync_WithValidDetails_UpdatesPlanAndTimestamps()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var created = await service.RecordTreatmentPlanAsync(record.Id, doctor.Id, new RecordTreatmentPlanRequest
        {
            Title = "Initial Plan",
            Description = "Initial plan details"
        });

        var updateRequest = new UpdateTreatmentPlanRequest
        {
            Title = "Updated Rehabilitation Plan",
            Category = TreatmentPlanCategory.Rehabilitative,
            Description = "Physiotherapy twice weekly for knee rehabilitation",
            Goals = "Regain full range of motion",
            Interventions = "Strengthening exercises, ultrasound therapy",
            Status = TreatmentPlanStatus.InProgress,
            StartDate = DateTime.UtcNow
        };

        var updated = await service.UpdateTreatmentPlanAsync(record.Id, created.Id, doctor.Id, updateRequest);

        Assert.NotNull(updated);
        Assert.Equal("Updated Rehabilitation Plan", updated.Title);
        Assert.Equal("Rehabilitative", updated.Category);
        Assert.Equal("Physiotherapy twice weekly for knee rehabilitation", updated.Description);
        Assert.Equal("InProgress", updated.Status);
        Assert.True(updated.UpdatedAt >= created.CreatedAt);

        var updatedRecord = await context.MedicalRecords.FindAsync(record.Id);
        Assert.NotNull(updatedRecord);
        Assert.Equal("Physiotherapy twice weekly for knee rehabilitation", updatedRecord.TreatmentPlan);
    }

    [Fact]
    public async Task UpdateTreatmentPlanAsync_WhenPlanNotFound_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var updateRequest = new UpdateTreatmentPlanRequest
        {
            Title = "Plan",
            Description = "Description"
        };

        var result = await service.UpdateTreatmentPlanAsync(record.Id, 9999, doctor.Id, updateRequest);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePrimaryTreatmentPlanAsync_WhenNoPlanExists_CreatesPlan()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        var request = new UpdateTreatmentPlanRequest
        {
            Title = "Cardiac Care Plan",
            Category = TreatmentPlanCategory.Monitoring,
            Description = "Holter monitor and cardiology review in 2 weeks",
            Status = TreatmentPlanStatus.Active
        };

        var result = await service.UpdatePrimaryTreatmentPlanAsync(record.Id, doctor.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Cardiac Care Plan", result.Title);
        Assert.Equal("Monitoring", result.Category);

        var plans = await service.GetTreatmentPlansAsync(record.Id);
        Assert.Single(plans);
    }

    [Fact]
    public async Task GetTreatmentPlansAsync_ReturnsAllPlansForRecord_OrderedDescending()
    {
        using var context = CreateInMemoryDbContext();
        var (_, doctor, record) = SeedDefaultData(context);
        var service = new MedicalRecordService(context);

        await service.RecordTreatmentPlanAsync(record.Id, doctor.Id, new RecordTreatmentPlanRequest
        {
            Title = "Plan 1",
            Description = "Description 1"
        });

        await service.RecordTreatmentPlanAsync(record.Id, doctor.Id, new RecordTreatmentPlanRequest
        {
            Title = "Plan 2",
            Description = "Description 2"
        });

        var plans = await service.GetTreatmentPlansAsync(record.Id);

        Assert.Equal(2, plans.Count);
    }

    [Fact]
    public async Task MedicalRecord_CreateAsync_InitializesStructuredDiagnosisAndTreatmentPlan()
    {
        using var context = CreateInMemoryDbContext();
        var patient = new User
        {
            Id = 10,
            FirstName = "Bob",
            LastName = "Smith",
            Email = "bob@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor = new User
        {
            Id = 20,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var createRequest = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Chronic cough",
            Diagnosis = "Acute Bronchitis",
            TreatmentPlan = "Bronchodilator inhaler and warm fluids"
        };

        var recordResponse = await service.CreateAsync(20, createRequest);

        Assert.NotNull(recordResponse);
        Assert.Equal("Acute Bronchitis", recordResponse.Diagnosis);
        Assert.Equal("Bronchodilator inhaler and warm fluids", recordResponse.TreatmentPlan);

        // Verify structured entities were created
        var diagnoses = await service.GetDiagnosesAsync(recordResponse.Id);
        Assert.Single(diagnoses);
        Assert.Equal("Acute Bronchitis", diagnoses[0].Description);
        Assert.Equal("Primary", diagnoses[0].Type);

        var plans = await service.GetTreatmentPlansAsync(recordResponse.Id);
        Assert.Single(plans);
        Assert.Equal("Bronchodilator inhaler and warm fluids", plans[0].Description);

        // Verify GetByIdAsync includes them
        var fetched = await service.GetByIdAsync(recordResponse.Id);
        Assert.NotNull(fetched);
        Assert.Single(fetched.Diagnoses);
        Assert.Single(fetched.TreatmentPlans);
    }
}
