using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class EmrAuditServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private (User patient, User doctor, User staff) SeedUsers(AppDbContext context, int patientId = 10, int doctorId = 20, int staffId = 30)
    {
        var patient = new User
        {
            Id = patientId,
            FirstName = "John",
            LastName = "Doe",
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

        var staff = new User
        {
            Id = staffId,
            FirstName = "Lisa",
            LastName = "Cuddy",
            Email = $"staff{staffId}@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Staff,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor, staff);
        return (patient, doctor, staff);
    }

    [Fact]
    public async Task LogAsync_SuccessfullyCreatesAuditLog_WithAllRequiredFields()
    {
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new EmrAuditService(context);

        var result = await service.LogAsync(
            userId: 20,
            action: "Create",
            entityType: "MedicalRecord",
            entityId: 101,
            patientId: 10,
            metadata: "RecordNumber: REC-2026-001",
            isSuccess: true
        );

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(20, result.UserId);
        Assert.Equal("Gregory House", result.UserName);
        Assert.Equal("Doctor", result.UserRole);
        Assert.Equal("Create", result.Action);
        Assert.Equal("MedicalRecord", result.EntityType);
        Assert.Equal(101, result.EntityId);
        Assert.Equal(10, result.PatientId);
        Assert.Equal("RecordNumber: REC-2026-001", result.Metadata);
        Assert.True(result.IsSuccess);
        Assert.True(result.Timestamp <= DateTime.UtcNow);

        // Verify persisted in DB
        var persisted = await context.EmrAuditLogs.FindAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Create", persisted.Action);
    }

    [Fact]
    public async Task LogAsync_WithFailureFlag_PersistsFailureStateAndReason()
    {
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new EmrAuditService(context);

        var result = await service.LogAsync(
            userId: 20,
            action: "Update",
            entityType: "MedicalRecord",
            entityId: 101,
            patientId: 10,
            metadata: "Validation failed: Diagnosis cannot be empty",
            isSuccess: false
        );

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation failed: Diagnosis cannot be empty", result.Metadata);

        var persisted = await context.EmrAuditLogs.FindAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.False(persisted.IsSuccess);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByPatientId_ReturnsMatchingLogsDescending()
    {
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new EmrAuditService(context);

        // Create log 1 for patient 10
        await service.LogAsync(20, "Create", "MedicalRecord", 101, 10, "Record 1");
        await Task.Delay(10);

        // Create log 2 for patient 25
        await service.LogAsync(20, "Create", "MedicalRecord", 102, 25, "Record 2");
        await Task.Delay(10);

        // Create log 3 for patient 10
        await service.LogAsync(20, "Update", "MedicalRecord", 101, 10, "Record 1 Updated");

        var logs = await service.GetAuditLogsAsync(patientId: 10);

        Assert.Equal(2, logs.Count);
        Assert.All(logs, l => Assert.Equal(10, l.PatientId));
        // Chronological order: newest first
        Assert.Equal("Update", logs[0].Action);
        Assert.Equal("Create", logs[1].Action);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByEntityType_ReturnsMatchingLogs()
    {
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new EmrAuditService(context);

        await service.LogAsync(20, "Create", "MedicalRecord", 101, 10);
        await service.LogAsync(20, "Create", "Prescription", 201, 10);
        await service.LogAsync(20, "Create", "LabOrder", 301, 10);

        var prescriptionLogs = await service.GetAuditLogsAsync(entityType: "Prescription");

        Assert.Single(prescriptionLogs);
        Assert.Equal("Prescription", prescriptionLogs[0].EntityType);
        Assert.Equal(201, prescriptionLogs[0].EntityId);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByUserId_ReturnsMatchingLogs()
    {
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20, 30);
        await context.SaveChangesAsync();

        var service = new EmrAuditService(context);

        await service.LogAsync(20, "Create", "MedicalRecord", 101, 10); // Doctor 20
        await service.LogAsync(30, "RecordVitals", "VitalSign", 401, 10); // Staff 30

        var staffLogs = await service.GetAuditLogsAsync(userId: 30);

        Assert.Single(staffLogs);
        Assert.Equal(30, staffLogs[0].UserId);
        Assert.Equal("Lisa Cuddy", staffLogs[0].UserName);
        Assert.Equal("Staff", staffLogs[0].UserRole);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectAuditEntry()
    {
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new EmrAuditService(context);

        var logged = await service.LogAsync(20, "RecordReport", "LabReport", 501, 10, "LabOrderId: 301");

        var retrieved = await service.GetByIdAsync(logged.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(logged.Id, retrieved.Id);
        Assert.Equal("RecordReport", retrieved.Action);
        Assert.Equal("LabReport", retrieved.EntityType);
        Assert.Equal(501, retrieved.EntityId);
        Assert.Equal("Gregory House", retrieved.UserName);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidOrNonExistentId_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new EmrAuditService(context);

        Assert.Null(await service.GetByIdAsync(0));
        Assert.Null(await service.GetByIdAsync(-1));
        Assert.Null(await service.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsOnlyLogsForTargetPatient()
    {
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new EmrAuditService(context);

        await service.LogAsync(20, "Create", "MedicalRecord", 101, 10);
        await service.LogAsync(20, "Create", "MedicalRecord", 102, 99);

        var patientLogs = await service.GetByPatientIdAsync(10);

        Assert.Single(patientLogs);
        Assert.Equal(10, patientLogs[0].PatientId);
    }

    [Fact]
    public async Task LogAsync_WithoutUserInDatabase_GracefullyHandlesNullUser()
    {
        using var context = CreateInMemoryDbContext();
        var service = new EmrAuditService(context);

        // User 999 does not exist in Users table
        var result = await service.LogAsync(
            userId: 999,
            action: "SystemEvent",
            entityType: "MedicalRecord",
            entityId: 101,
            metadata: "System clean up"
        );

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.UserName);
        Assert.Equal(string.Empty, result.UserRole);
        Assert.Equal(999, result.UserId);
    }
}
