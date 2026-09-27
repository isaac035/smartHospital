using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class LabOrderServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static (User patient, User doctor, User staff) SeedDefaultUsers(
        AppDbContext context,
        int patientId = 10,
        int doctorId = 20,
        int staffId = 30)
    {
        var patient = new User
        {
            Id = patientId,
            FirstName = "Alice",
            LastName = "Johnson",
            Email = $"patient{patientId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor = new User
        {
            Id = doctorId,
            FirstName = "Robert",
            LastName = "Chen",
            Email = $"doctor{doctorId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        var staff = new User
        {
            Id = staffId,
            FirstName = "Sarah",
            LastName = "Connor",
            Email = $"staff{staffId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Staff,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor, staff);
        return (patient, doctor, staff);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Lab Order Creation Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_CreatesOrderSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = "Full Blood Count",
            Category = "Hematology",
            Priority = LabOrderPriority.Urgent,
            ClinicalNotes = "Suspected anemia"
        };

        var result = await service.CreateOrderAsync(20, request);

        Assert.NotNull(result);
        Assert.StartsWith("LAB-", result.OrderNumber);
        Assert.Equal(10, result.PatientId);
        Assert.Equal("Alice Johnson", result.PatientName);
        Assert.Equal(20, result.DoctorId);
        Assert.Equal("Dr. Robert Chen", result.DoctorName);
        Assert.Equal("Full Blood Count", result.TestName);
        Assert.Equal("Hematology", result.Category);
        Assert.Equal("Urgent", result.Priority);
        Assert.Equal("Ordered", result.Status);
        Assert.Equal("Suspected anemia", result.ClinicalNotes);
        Assert.Null(result.Report);
        Assert.True(result.OrderedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateOrderAsync_WithLinkedMedicalRecord_CreatesOrderSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);

        var record = new MedicalRecord
        {
            Id = 50,
            RecordNumber = "REC-20260901-001",
            PatientId = 10,
            DoctorId = 20,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Fatigue and dizziness",
            Diagnosis = "Iron deficiency anemia",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            MedicalRecordId = 50,
            TestName = "Serum Ferritin",
            Category = "Biochemistry",
            Priority = LabOrderPriority.Routine
        };

        var result = await service.CreateOrderAsync(20, request);

        Assert.NotNull(result);
        Assert.Equal(50, result.MedicalRecordId);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentPatient_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 999, // non-existent
            TestName = "Lipid Profile"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(20, request));
        Assert.Contains("Active patient not found", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInactivePatient_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var inactivePatient = new User
        {
            Id = 15,
            FirstName = "Inactive",
            LastName = "Patient",
            Email = "inactive@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Inactive
        };
        context.Users.Add(inactivePatient);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 15,
            TestName = "Lipid Profile"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(20, request));
        Assert.Contains("Active patient not found", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithUserNotPatientRole_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context); // Doctor is ID 20
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 20, // Passing doctor ID as patient
            TestName = "Lipid Profile"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(20, request));
        Assert.Contains("Active patient not found", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentDoctor_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = "Electrolytes"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(999, request));
        Assert.Contains("Active ordering doctor not found", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInactiveDoctor_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var inactiveDoctor = new User
        {
            Id = 25,
            FirstName = "Inactive",
            LastName = "Doctor",
            Email = "inactivedoctor@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Inactive
        };
        context.Users.Add(inactiveDoctor);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = "Electrolytes"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(25, request));
        Assert.Contains("Active ordering doctor not found", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithUserNotDoctorRole_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context); // Patient is ID 10
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = "Electrolytes"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(10, request)); // Patient cannot order
        Assert.Contains("Active ordering doctor not found", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentMedicalRecord_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            MedicalRecordId = 9999,
            TestName = "Urinalysis"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(20, request));
        Assert.Contains("Medical record not found", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithMedicalRecordForDifferentPatient_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);

        var otherPatient = new User
        {
            Id = 11,
            FirstName = "Bob",
            LastName = "Brown",
            Email = "bob@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };
        context.Users.Add(otherPatient);

        var record = new MedicalRecord
        {
            Id = 55,
            RecordNumber = "REC-20260901-002",
            PatientId = 11, // Belongs to Bob
            DoctorId = 20,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Cough",
            Diagnosis = "Bronchitis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10, // Ordering for Alice with Bob's record
            MedicalRecordId = 55,
            TestName = "Chest X-Ray"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(20, request));
        Assert.Contains("Medical record does not belong to the specified patient", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public async Task CreateOrderAsync_WithEmptyTestName_ThrowsArgumentException(string? testName)
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = testName!
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(20, request));
    }

    [Fact]
    public async Task CreateOrderAsync_WithTestNameExceeding120Chars_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = new string('A', 121)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(20, request));
        Assert.Contains("Test name cannot exceed 120 characters", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInvalidPriority_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = "CBC",
            Priority = (LabOrderPriority)99
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(20, request));
        Assert.Contains("Invalid lab order priority", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateOrderAsync_WithInvalidPatientId_ThrowsArgumentException(int invalidPatientId)
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = invalidPatientId,
            TestName = "CBC"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(20, request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateOrderAsync_WithInvalidDoctorId_ThrowsArgumentException(int invalidDoctorId)
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = "CBC"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(invalidDoctorId, request));
    }

    [Fact]
    public async Task CreateOrderAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        using var context = CreateInMemoryDbContext();
        var service = new LabOrderService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateOrderAsync(20, null!));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Status Transition Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_OrderedToSampleCollected_Succeeds()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 1,
            OrderNumber = "LAB-1",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Glucose Fasting",
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var updated = await service.UpdateStatusAsync(1, LabOrderStatus.SampleCollected);

        Assert.NotNull(updated);
        Assert.Equal("SampleCollected", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_OrderedToInProgress_Succeeds()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 2,
            OrderNumber = "LAB-2",
            PatientId = 10,
            DoctorId = 20,
            TestName = "X-Ray Chest",
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var updated = await service.UpdateStatusAsync(2, LabOrderStatus.InProgress);

        Assert.NotNull(updated);
        Assert.Equal("InProgress", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_OrderedToCancelled_Succeeds()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 3,
            OrderNumber = "LAB-3",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var updated = await service.UpdateStatusAsync(3, LabOrderStatus.Cancelled);

        Assert.NotNull(updated);
        Assert.Equal("Cancelled", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_SampleCollectedToInProgress_Succeeds()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 4,
            OrderNumber = "LAB-4",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Blood Culture",
            Status = LabOrderStatus.SampleCollected,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var updated = await service.UpdateStatusAsync(4, LabOrderStatus.InProgress);

        Assert.NotNull(updated);
        Assert.Equal("InProgress", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_SampleCollectedToCancelled_Succeeds()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 5,
            OrderNumber = "LAB-5",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Blood Culture",
            Status = LabOrderStatus.SampleCollected,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var updated = await service.UpdateStatusAsync(5, LabOrderStatus.Cancelled);

        Assert.NotNull(updated);
        Assert.Equal("Cancelled", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_InProgressToCancelled_Succeeds()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 6,
            OrderNumber = "LAB-6",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Electrolytes",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var updated = await service.UpdateStatusAsync(6, LabOrderStatus.Cancelled);

        Assert.NotNull(updated);
        Assert.Equal("Cancelled", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToCompleted_WithoutReport_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 7,
            OrderNumber = "LAB-7",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(7, LabOrderStatus.Completed));

        Assert.Contains("Cannot mark lab order as completed without a lab report", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_InProgressToCompleted_WithExistingReport_Succeeds()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 8,
            OrderNumber = "LAB-8",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);

        var report = new LabReport
        {
            Id = 88,
            LabOrderId = 8,
            ConductedByUserId = 30,
            ReportDate = DateTime.UtcNow,
            ResultSummary = "Normal hemoglobin",
            Findings = "Hb: 14.2 g/dL, WBC: 6.5 x10^9/L",
            CreatedAt = DateTime.UtcNow
        };
        context.LabReports.Add(report);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var updated = await service.UpdateStatusAsync(8, LabOrderStatus.Completed);

        Assert.NotNull(updated);
        Assert.Equal("Completed", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_SameStatus_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 9,
            OrderNumber = "LAB-9",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.SampleCollected,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(9, LabOrderStatus.SampleCollected));

        Assert.Contains("already in 'SampleCollected' status", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_BackwardsToOrdered_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 10,
            OrderNumber = "LAB-10",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.SampleCollected,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(10, LabOrderStatus.Ordered));

        Assert.Contains("Cannot transition lab order from 'SampleCollected' back to 'Ordered'", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_InProgressBackToSampleCollected_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 11,
            OrderNumber = "LAB-11",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(11, LabOrderStatus.SampleCollected));

        Assert.Contains("Cannot transition lab order from 'InProgress' back to 'SampleCollected'", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromCompletedToAnyStatus_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 12,
            OrderNumber = "LAB-12",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.Completed,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(12, LabOrderStatus.InProgress));

        Assert.Contains("Cannot change status of a completed lab order", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromCancelledToAnyStatus_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 13,
            OrderNumber = "LAB-13",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.Cancelled,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(13, LabOrderStatus.Ordered));

        Assert.Contains("Cannot change status of a cancelled lab order", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatusEnum_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 14,
            OrderNumber = "LAB-14",
            PatientId = 10,
            DoctorId = 20,
            TestName = "CBC",
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateStatusAsync(14, (LabOrderStatus)99));
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentOrder_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new LabOrderService(context);

        var result = await service.UpdateStatusAsync(999, LabOrderStatus.InProgress);
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Lab Report Recording & Duplicate Prevention Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordReportAsync_ValidRequest_CreatesReportAndMarksOrderCompleted()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 20,
            OrderNumber = "LAB-20",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Lipid Profile",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow.AddHours(-2)
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var reportRequest = new RecordLabReportRequest
        {
            ResultSummary = "Elevated cholesterol",
            Findings = "Total cholesterol: 240 mg/dL, HDL: 45 mg/dL, LDL: 160 mg/dL",
            ReferenceRange = "Total cholesterol < 200 mg/dL",
            DoctorRemarks = "Diet modification and statin therapy recommended",
            AttachmentUrl = "https://example.com/reports/lipid-profile.pdf",
            ReportDate = DateTime.UtcNow.AddMinutes(-30)
        };

        var result = await service.RecordReportAsync(20, 30, reportRequest);

        Assert.NotNull(result);
        Assert.Equal(20, result.LabOrderId);
        Assert.Equal(30, result.ConductedByUserId);
        Assert.Equal("Sarah Connor", result.ConductedByUserName);
        Assert.Equal("Elevated cholesterol", result.ResultSummary);
        Assert.Equal("Total cholesterol: 240 mg/dL, HDL: 45 mg/dL, LDL: 160 mg/dL", result.Findings);
        Assert.Equal("Total cholesterol < 200 mg/dL", result.ReferenceRange);
        Assert.Equal("Diet modification and statin therapy recommended", result.DoctorRemarks);
        Assert.Equal("https://example.com/reports/lipid-profile.pdf", result.AttachmentUrl);

        // Verify order is now completed
        var updatedOrder = await service.GetByIdAsync(20);
        Assert.NotNull(updatedOrder);
        Assert.Equal("Completed", updatedOrder.Status);
        Assert.NotNull(updatedOrder.Report);
        Assert.Equal("Elevated cholesterol", updatedOrder.Report.ResultSummary);
    }

    [Fact]
    public async Task RecordReportAsync_WhenReportAlreadyExists_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 21,
            OrderNumber = "LAB-21",
            PatientId = 10,
            DoctorId = 20,
            TestName = "HbA1c",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.LabOrders.Add(order);

        var existingReport = new LabReport
        {
            Id = 210,
            LabOrderId = 21,
            ConductedByUserId = 30,
            ReportDate = DateTime.UtcNow,
            ResultSummary = "HbA1c: 6.8%",
            Findings = "Fair diabetic control",
            CreatedAt = DateTime.UtcNow
        };
        context.LabReports.Add(existingReport);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var duplicateRequest = new RecordLabReportRequest
        {
            ResultSummary = "Duplicate HbA1c",
            Findings = "Duplicate findings"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordReportAsync(21, 30, duplicateRequest));

        Assert.Contains("A report has already been recorded for this lab order", ex.Message);
    }

    [Fact]
    public async Task RecordReportAsync_OnCancelledOrder_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 22,
            OrderNumber = "LAB-22",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Thyroid Panel",
            Status = LabOrderStatus.Cancelled,
            OrderedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var reportRequest = new RecordLabReportRequest
        {
            ResultSummary = "Normal TSH",
            Findings = "TSH: 2.1 mIU/L"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordReportAsync(22, 30, reportRequest));

        Assert.Contains("Cannot record report for a cancelled lab order", ex.Message);
    }

    [Fact]
    public async Task RecordReportAsync_OnAlreadyCompletedOrder_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 23,
            OrderNumber = "LAB-23",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Thyroid Panel",
            Status = LabOrderStatus.Completed,
            OrderedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var reportRequest = new RecordLabReportRequest
        {
            ResultSummary = "Normal TSH",
            Findings = "TSH: 2.1 mIU/L"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordReportAsync(23, 30, reportRequest));

        Assert.Contains("Cannot record report", ex.Message);
    }

    [Fact]
    public async Task RecordReportAsync_NonExistentOrder_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var reportRequest = new RecordLabReportRequest
        {
            ResultSummary = "Summary",
            Findings = "Findings"
        };

        var result = await service.RecordReportAsync(999, 30, reportRequest);
        Assert.Null(result);
    }

    [Fact]
    public async Task RecordReportAsync_NonExistentConductedByUser_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 24,
            OrderNumber = "LAB-24",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Electrolytes",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var reportRequest = new RecordLabReportRequest
        {
            ResultSummary = "Normal electrolytes",
            Findings = "Na: 140, K: 4.2"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordReportAsync(24, 999, reportRequest));

        Assert.Contains("Active user recording the report not found", ex.Message);
    }

    [Fact]
    public async Task RecordReportAsync_ConductedByUserWithPatientRole_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context); // Patient is ID 10
        var order = new LabOrder
        {
            Id = 25,
            OrderNumber = "LAB-25",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Electrolytes",
            Status = LabOrderStatus.InProgress,
            OrderedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var reportRequest = new RecordLabReportRequest
        {
            ResultSummary = "Normal electrolytes",
            Findings = "Na: 140, K: 4.2"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordReportAsync(25, 10, reportRequest)); // Patient cannot record lab report

        Assert.Contains("must have Staff, Doctor, or Admin role", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public async Task RecordReportAsync_EmptyResultSummary_ThrowsArgumentException(string? summary)
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = summary!,
            Findings = "Findings"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(1, 30, request));
    }

    [Fact]
    public async Task RecordReportAsync_ResultSummaryExceeding500Chars_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = new string('A', 501),
            Findings = "Findings"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(1, 30, request));
        Assert.Contains("Result summary cannot exceed 500 characters", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public async Task RecordReportAsync_EmptyFindings_ThrowsArgumentException(string? findings)
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Summary",
            Findings = findings!
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(1, 30, request));
    }

    [Fact]
    public async Task RecordReportAsync_FindingsExceeding2000Chars_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Summary",
            Findings = new string('B', 2001)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(1, 30, request));
        Assert.Contains("Findings cannot exceed 2000 characters", ex.Message);
    }

    [Fact]
    public async Task RecordReportAsync_ReportDateInFuture_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 26,
            OrderNumber = "LAB-26",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Blood Group",
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = "O Positive",
            Findings = "Rh Positive",
            ReportDate = DateTime.UtcNow.AddDays(1) // Future date!
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(26, 30, request));
        Assert.Contains("Report date cannot be in the future", ex.Message);
    }

    [Fact]
    public async Task RecordReportAsync_ReportDateEarlierThanOrderedAt_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var order = new LabOrder
        {
            Id = 27,
            OrderNumber = "LAB-27",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Blood Group",
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow.AddHours(-2)
        };
        context.LabOrders.Add(order);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = "O Positive",
            Findings = "Rh Positive",
            ReportDate = DateTime.UtcNow.AddHours(-5) // 3 hours before order was made!
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(27, 30, request));
        Assert.Contains("Report date cannot be earlier than the order date", ex.Message);
    }

    [Theory]
    [InlineData("ftp://invalid-scheme.com/report.pdf")]
    [InlineData("not-a-valid-url")]
    [InlineData("javascript:alert(1)")]
    public async Task RecordReportAsync_InvalidAttachmentUrlScheme_ThrowsArgumentException(string invalidUrl)
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Summary",
            Findings = "Findings",
            AttachmentUrl = invalidUrl
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(1, 30, request));
        Assert.Contains("Attachment URL must be a valid HTTP or HTTPS URL", ex.Message);
    }

    [Fact]
    public async Task RecordReportAsync_AttachmentUrlExceeding500Chars_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var request = new RecordLabReportRequest
        {
            ResultSummary = "Summary",
            Findings = "Findings",
            AttachmentUrl = "https://example.com/" + new string('x', 500)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.RecordReportAsync(1, 30, request));
        Assert.Contains("Attachment URL cannot exceed 500 characters", ex.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Retrieval Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPatientIdAsync_ValidPatientWithOrders_ReturnsOrderedList()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);

        var order1 = new LabOrder
        {
            Id = 31,
            OrderNumber = "LAB-31",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Test 1",
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow.AddDays(-2)
        };

        var order2 = new LabOrder
        {
            Id = 32,
            OrderNumber = "LAB-32",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Test 2",
            Status = LabOrderStatus.Completed,
            OrderedAt = DateTime.UtcNow.AddDays(-1)
        };

        context.LabOrders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var results = await service.GetByPatientIdAsync(10);

        Assert.Equal(2, results.Count);
        Assert.Equal("LAB-32", results[0].OrderNumber); // Descending by OrderedAt
        Assert.Equal("LAB-31", results[1].OrderNumber);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ValidPatientNoOrders_ReturnsEmptyList()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var results = await service.GetByPatientIdAsync(10);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByPatientIdAsync_NonExistentPatient_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetByPatientIdAsync(999));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task GetByPatientIdAsync_InvalidPatientId_ThrowsArgumentException(int invalidPatientId)
    {
        using var context = CreateInMemoryDbContext();
        var service = new LabOrderService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByPatientIdAsync(invalidPatientId));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsOrderWithReportAndUsers()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);

        var order = new LabOrder
        {
            Id = 40,
            OrderNumber = "LAB-40",
            PatientId = 10,
            DoctorId = 20,
            TestName = "Cardiac Troponin",
            Category = "Cardiology",
            Priority = LabOrderPriority.Stat,
            Status = LabOrderStatus.Completed,
            OrderedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.LabOrders.Add(order);

        var report = new LabReport
        {
            Id = 400,
            LabOrderId = 40,
            ConductedByUserId = 30,
            ReportDate = DateTime.UtcNow,
            ResultSummary = "Troponin within normal limits",
            Findings = "hs-cTnI < 0.01 ng/mL",
            CreatedAt = DateTime.UtcNow
        };
        context.LabReports.Add(report);
        await context.SaveChangesAsync();

        var service = new LabOrderService(context);
        var result = await service.GetByIdAsync(40);

        Assert.NotNull(result);
        Assert.Equal("LAB-40", result.OrderNumber);
        Assert.Equal("Alice Johnson", result.PatientName);
        Assert.Equal("Dr. Robert Chen", result.DoctorName);
        Assert.Equal("Stat", result.Priority);
        Assert.NotNull(result.Report);
        Assert.Equal("Troponin within normal limits", result.Report.ResultSummary);
        Assert.Equal("Sarah Connor", result.Report.ConductedByUserName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentOrder_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new LabOrderService(context);

        var result = await service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        using var context = CreateInMemoryDbContext();
        var service = new LabOrderService(context);

        var result = await service.GetByIdAsync(invalidId);
        Assert.Null(result);
    }
}
