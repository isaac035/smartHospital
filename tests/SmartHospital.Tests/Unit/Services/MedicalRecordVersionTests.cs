using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class MedicalRecordVersionTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private (User patient, User doctor) SeedUsers(AppDbContext context, int patientId = 10, int doctorId = 20)
    {
        var patient = new User
        {
            Id = patientId,
            FirstName = "Alice",
            LastName = "Patient",
            Email = $"patient{patientId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor = new User
        {
            Id = doctorId,
            FirstName = "Robert",
            LastName = "House",
            Email = $"doctor{doctorId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor);
        return (patient, doctor);
    }

    [Fact]
    public async Task CreateAsync_CreatesInitialVersion1_WithSnapshotValuesAndNullPreviousValues()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Severe headache and light sensitivity",
            Symptoms = "Nausea, photophobia, throbbing head pain",
            ExaminationNotes = "Normal cranial nerve exam, photophobia noted",
            Diagnosis = "Acute Migraine",
            TreatmentPlan = "Sumatriptan 50mg, dark room rest, hydration",
            FollowUpDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var created = await service.CreateAsync(20, request);
        var versions = await service.GetVersionHistoryAsync(created.Id);

        // Assert
        Assert.Single(versions);
        var v1 = versions[0];
        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal(created.Id, v1.MedicalRecordId);
        Assert.Equal(20, v1.ChangedByUserId);
        Assert.Equal("Initial", v1.ChangeType);
        Assert.Contains("Initial medical record created", v1.ChangeSummary);

        // Current snapshot clinical values
        Assert.Equal("Severe headache and light sensitivity", v1.ChiefComplaint);
        Assert.Equal("Nausea, photophobia, throbbing head pain", v1.Symptoms);
        Assert.Equal("Normal cranial nerve exam, photophobia noted", v1.ExaminationNotes);
        Assert.Equal("Acute Migraine", v1.Diagnosis);
        Assert.Equal("Sumatriptan 50mg, dark room rest, hydration", v1.TreatmentPlan);

        // Previous values are null for initial version
        Assert.Null(v1.PreviousChiefComplaint);
        Assert.Null(v1.PreviousSymptoms);
        Assert.Null(v1.PreviousExaminationNotes);
        Assert.Null(v1.PreviousDiagnosis);
        Assert.Null(v1.PreviousTreatmentPlan);
        Assert.Null(v1.PreviousFollowUpDate);
    }

    [Fact]
    public async Task UpdateAsync_CreatesNewVersion_IncrementsVersionNumber_RecordsPreviousValues()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var initialRequest = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Cough and mild fever",
            Symptoms = "Dry cough, low fever",
            ExaminationNotes = "Mild pharyngeal erythema",
            Diagnosis = "Upper Respiratory Tract Infection",
            TreatmentPlan = "Symptomatic treatment, paracetamol 500mg"
        };
        var created = await service.CreateAsync(20, initialRequest);

        var updateRequest = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Worsening productive cough and high fever",
            Symptoms = "Purulent sputum, fever 39C, chills",
            ExaminationNotes = "Rales in right lower lung field",
            Diagnosis = "Community-Acquired Pneumonia",
            TreatmentPlan = "Amoxicillin/Clavulanate 875/125mg BD for 7 days, chest X-ray",
            FollowUpDate = DateTime.UtcNow.AddDays(3)
        };

        // Act
        var updated = await service.UpdateAsync(created.Id, 20, updateRequest);
        var versions = await service.GetVersionHistoryAsync(created.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(2, versions.Count);

        var v2 = versions.First(v => v.VersionNumber == 2);
        Assert.Equal(2, v2.VersionNumber);
        Assert.Equal(created.Id, v2.MedicalRecordId);
        Assert.Equal(20, v2.ChangedByUserId);
        Assert.Equal("Update", v2.ChangeType);
        Assert.Contains("Chief complaint updated", v2.ChangeSummary);
        Assert.Contains("Diagnosis updated", v2.ChangeSummary);

        // Previous values correspond to initial values
        Assert.Equal("Cough and mild fever", v2.PreviousChiefComplaint);
        Assert.Equal("Dry cough, low fever", v2.PreviousSymptoms);
        Assert.Equal("Mild pharyngeal erythema", v2.PreviousExaminationNotes);
        Assert.Equal("Upper Respiratory Tract Infection", v2.PreviousDiagnosis);
        Assert.Equal("Symptomatic treatment, paracetamol 500mg", v2.PreviousTreatmentPlan);
        Assert.Null(v2.PreviousFollowUpDate);

        // Current values reflect the update
        Assert.Equal("Worsening productive cough and high fever", v2.ChiefComplaint);
        Assert.Equal("Community-Acquired Pneumonia", v2.Diagnosis);
        Assert.Equal("Amoxicillin/Clavulanate 875/125mg BD for 7 days, chest X-ray", v2.TreatmentPlan);
    }

    [Fact]
    public async Task GetVersionHistoryAsync_OrdersVersionsDescending_NewestFirst()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var created = await service.CreateAsync(20, new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Initial complaint v1",
            Diagnosis = "Initial diagnosis v1"
        });

        await service.UpdateAsync(created.Id, 20, new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Updated complaint v2",
            Diagnosis = "Updated diagnosis v2"
        });

        await service.UpdateAsync(created.Id, 20, new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Updated complaint v3",
            Diagnosis = "Updated diagnosis v3"
        });

        // Act
        var versions = await service.GetVersionHistoryAsync(created.Id);

        // Assert
        Assert.Equal(3, versions.Count);
        Assert.Equal(3, versions[0].VersionNumber);
        Assert.Equal(2, versions[1].VersionNumber);
        Assert.Equal(1, versions[2].VersionNumber);
        Assert.True(versions[0].ChangedAt >= versions[1].ChangedAt);
        Assert.True(versions[1].ChangedAt >= versions[2].ChangedAt);
    }

    [Fact]
    public async Task UpdateAsync_HistoricalValuesRemainUnchanged_Immutable()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var created = await service.CreateAsync(20, new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Immutable Complaint V1",
            Symptoms = "Symptoms V1",
            ExaminationNotes = "Exam V1",
            Diagnosis = "Diagnosis V1",
            TreatmentPlan = "Plan V1"
        });

        // First update -> Creates v2
        await service.UpdateAsync(created.Id, 20, new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Complaint V2",
            Symptoms = "Symptoms V2",
            ExaminationNotes = "Exam V2",
            Diagnosis = "Diagnosis V2",
            TreatmentPlan = "Plan V2"
        });

        // Second update -> Creates v3
        await service.UpdateAsync(created.Id, 20, new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Complaint V3",
            Symptoms = "Symptoms V3",
            ExaminationNotes = "Exam V3",
            Diagnosis = "Diagnosis V3",
            TreatmentPlan = "Plan V3"
        });

        // Act: Fetch each version individually
        var v1 = await service.GetVersionAsync(created.Id, 1);
        var v2 = await service.GetVersionAsync(created.Id, 2);
        var v3 = await service.GetVersionAsync(created.Id, 3);

        // Assert: Version 1 values MUST be identical to the original creation values
        Assert.NotNull(v1);
        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal("Immutable Complaint V1", v1.ChiefComplaint);
        Assert.Equal("Symptoms V1", v1.Symptoms);
        Assert.Equal("Exam V1", v1.ExaminationNotes);
        Assert.Equal("Diagnosis V1", v1.Diagnosis);
        Assert.Equal("Plan V1", v1.TreatmentPlan);
        Assert.Null(v1.PreviousChiefComplaint);
        Assert.Null(v1.PreviousDiagnosis);

        // Version 2 values MUST be preserved as they were at v2
        Assert.NotNull(v2);
        Assert.Equal(2, v2.VersionNumber);
        Assert.Equal("Complaint V2", v2.ChiefComplaint);
        Assert.Equal("Diagnosis V2", v2.Diagnosis);
        Assert.Equal("Immutable Complaint V1", v2.PreviousChiefComplaint);
        Assert.Equal("Diagnosis V1", v2.PreviousDiagnosis);

        // Version 3 values MUST reflect v3
        Assert.NotNull(v3);
        Assert.Equal(3, v3.VersionNumber);
        Assert.Equal("Complaint V3", v3.ChiefComplaint);
        Assert.Equal("Diagnosis V3", v3.Diagnosis);
        Assert.Equal("Complaint V2", v3.PreviousChiefComplaint);
        Assert.Equal("Diagnosis V2", v3.PreviousDiagnosis);

        // Verify total row count in MedicalRecordVersions table equals 3
        var totalVersionsInDb = await context.MedicalRecordVersions.CountAsync(v => v.MedicalRecordId == created.Id);
        Assert.Equal(3, totalVersionsInDb);
    }

    [Fact]
    public async Task GetVersionHistoryAsync_ThrowsInvalidOperationException_WhenRecordDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MedicalRecordService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetVersionHistoryAsync(9999));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetVersionHistoryAsync_ThrowsArgumentException_WhenIdIsInvalid(int invalidId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MedicalRecordService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetVersionHistoryAsync(invalidId));
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsNull_WhenVersionDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var created = await service.CreateAsync(20, new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        });

        // Act
        var version = await service.GetVersionAsync(created.Id, 99);

        // Assert
        Assert.Null(version);
    }

    [Fact]
    public async Task UpdateAsync_BackfillsVersion1_IfLegacyRecordHadNoVersion()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);

        var legacyRecord = new MedicalRecord
        {
            Id = 50,
            RecordNumber = "REC-LEGACY-001",
            PatientId = 10,
            DoctorId = 20,
            VisitDate = DateTime.UtcNow.AddDays(-10),
            ChiefComplaint = "Legacy Complaint",
            Diagnosis = "Legacy Diagnosis",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
        context.MedicalRecords.Add(legacyRecord);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        // Act
        var updateResult = await service.UpdateAsync(50, 20, new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Updated After Migration",
            Diagnosis = "Updated Diagnosis"
        });

        // Assert
        Assert.NotNull(updateResult);
        var versions = await service.GetVersionHistoryAsync(50);
        Assert.Equal(2, versions.Count);

        var v1 = versions.First(v => v.VersionNumber == 1);
        Assert.Equal("Legacy Complaint", v1.ChiefComplaint);
        Assert.Equal("Legacy Diagnosis", v1.Diagnosis);

        var v2 = versions.First(v => v.VersionNumber == 2);
        Assert.Equal("Updated After Migration", v2.ChiefComplaint);
        Assert.Equal("Legacy Complaint", v2.PreviousChiefComplaint);
    }
}
