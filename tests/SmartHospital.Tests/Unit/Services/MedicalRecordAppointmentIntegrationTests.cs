using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class MedicalRecordAppointmentIntegrationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static (User patient, User doctor, User otherPatient, User otherDoctor) SeedUsers(
        AppDbContext context,
        int patientId = 10,
        int doctorId = 20,
        int otherPatientId = 30,
        int otherDoctorId = 40)
    {
        var patient = new User
        {
            Id = patientId,
            FirstName = "John",
            LastName = "Doe",
            Email = $"patient{patientId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor = new User
        {
            Id = doctorId,
            FirstName = "Gregory",
            LastName = "House",
            Email = $"doctor{doctorId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        var otherPatient = new User
        {
            Id = otherPatientId,
            FirstName = "Jane",
            LastName = "Smith",
            Email = $"otherpatient{otherPatientId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var otherDoctor = new User
        {
            Id = otherDoctorId,
            FirstName = "James",
            LastName = "Wilson",
            Email = $"otherdoctor{otherDoctorId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor, otherPatient, otherDoctor);
        return (patient, doctor, otherPatient, otherDoctor);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Valid appointment → medical record
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AppointmentStatus.Scheduled)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.CheckedIn)]
    [InlineData(AppointmentStatus.InProgress)]
    public async Task CreateAsync_WithValidAppointment_AssociatesAppointmentAndCompletesIt(AppointmentStatus initialStatus)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 100,
            ReferenceNumber = "APT-20260926-100",
            PatientId = 10,
            DoctorId = 20,
            Status = initialStatus,
            ScheduledStart = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 100,
            ChiefComplaint = "Headache and fever",
            Diagnosis = "Viral flu",
            Symptoms = "Body aches, chills",
            TreatmentPlan = "Rest and hydration"
        };

        // Act
        var result = await service.CreateAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.AppointmentId);
        Assert.Equal(10, result.PatientId);
        Assert.Equal(20, result.DoctorId);

        // Verify the appointment status has been updated to Completed
        var updatedAppointment = await context.Appointments.FindAsync(100);
        Assert.NotNull(updatedAppointment);
        Assert.Equal(AppointmentStatus.Completed, updatedAppointment.Status);
    }

    [Fact]
    public async Task CreateAsync_WithAlreadyCompletedAppointment_SuccessfullyAssociates()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 101,
            ReferenceNumber = "APT-20260926-101",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.Completed,
            ScheduledStart = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 101,
            ChiefComplaint = "Post-consultation record",
            Diagnosis = "Migraine"
        };

        // Act
        var result = await service.CreateAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.AppointmentId);
        var storedAppointment = await context.Appointments.FindAsync(101);
        Assert.NotNull(storedAppointment);
        Assert.Equal(AppointmentStatus.Completed, storedAppointment.Status);
    }

    [Fact]
    public async Task GetByAppointmentIdAsync_WithExistingRecord_ReturnsMedicalRecord()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 102,
            ReferenceNumber = "APT-20260926-102",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.InProgress,
            ScheduledStart = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var createRequest = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 102,
            ChiefComplaint = "Chest congestion",
            Diagnosis = "Acute bronchitis"
        };
        var created = await service.CreateAsync(20, createRequest);

        // Act
        var fetched = await service.GetByAppointmentIdAsync(102);

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(102, fetched.AppointmentId);
        Assert.Equal(10, fetched.PatientId);
        Assert.Equal("John Doe", fetched.PatientName);
        Assert.Equal(20, fetched.DoctorId);
        Assert.Equal("Dr. Gregory House", fetched.DoctorName);
        Assert.Equal("Acute bronchitis", fetched.Diagnosis);
    }

    [Fact]
    public async Task UpdateAsync_WithValidAppointment_SuccessfullyLinksAndCompletes()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 103,
            ReferenceNumber = "APT-20260926-103",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.CheckedIn,
            ScheduledStart = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Appointments.Add(appointment);

        // Medical record initially created without appointment
        var record = new MedicalRecord
        {
            Id = 50,
            RecordNumber = "REC-050",
            PatientId = 10,
            DoctorId = 20,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Initial complaint",
            Diagnosis = "Initial diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var updateRequest = new UpdateMedicalRecordRequest
        {
            AppointmentId = 103,
            ChiefComplaint = "Updated complaint with appointment",
            Diagnosis = "Confirmed diagnosis"
        };

        // Act
        var updated = await service.UpdateAsync(50, 20, updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(103, updated.AppointmentId);

        var updatedAppointment = await context.Appointments.FindAsync(103);
        Assert.NotNull(updatedAppointment);
        Assert.Equal(AppointmentStatus.Completed, updatedAppointment.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Invalid patient / doctor relationship
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AppointmentBelongsToDifferentPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context); // Patient 10 and OtherPatient 30

        var appointment = new Appointment
        {
            Id = 201,
            ReferenceNumber = "APT-201",
            PatientId = 30, // Belongs to OtherPatient 30
            DoctorId = 20,
            Status = AppointmentStatus.Confirmed,
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10, // Requesting for Patient 10
            AppointmentId = 201,
            ChiefComplaint = "Routine checkup",
            Diagnosis = "Healthy"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(20, request));
        Assert.Equal("Appointment does not belong to the specified patient.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_AppointmentBelongsToDifferentDoctor_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context); // Doctor 20 and OtherDoctor 40

        var appointment = new Appointment
        {
            Id = 202,
            ReferenceNumber = "APT-202",
            PatientId = 10,
            DoctorId = 40, // Booked with OtherDoctor 40
            Status = AppointmentStatus.Confirmed,
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 202,
            ChiefComplaint = "Checkup",
            Diagnosis = "Checkup"
        };

        // Act & Assert: Doctor 20 tries to record for appointment booked with Doctor 40
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(20, request));
        Assert.Equal("Appointment does not belong to the specified doctor.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_AppointmentBelongsToDifferentDoctor_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 203,
            ReferenceNumber = "APT-203",
            PatientId = 10,
            DoctorId = 40, // Different doctor
            Status = AppointmentStatus.Confirmed,
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        };
        context.Appointments.Add(appointment);

        var record = new MedicalRecord
        {
            Id = 51,
            RecordNumber = "REC-051",
            PatientId = 10,
            DoctorId = 20,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "C",
            Diagnosis = "D",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new UpdateMedicalRecordRequest
        {
            AppointmentId = 203,
            ChiefComplaint = "C",
            Diagnosis = "D"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(51, 20, request));
        Assert.Equal("Appointment does not belong to the specified doctor.", ex.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Invalid appointment
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NonExistentAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 99999, // Does not exist
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(20, request));
        Assert.Equal("Appointment not found.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task CreateAsync_NegativeAppointmentId_ThrowsArgumentException(int invalidId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = invalidId,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task CreateAsync_CancelledAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 301,
            ReferenceNumber = "APT-301",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.Cancelled,
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 301,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(20, request));
        Assert.Contains("Cancelled", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_NoShowAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 302,
            ReferenceNumber = "APT-302",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.NoShow,
            ScheduledStart = DateTime.UtcNow.AddHours(-3)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 302,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(20, request));
        Assert.Contains("NoShow", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_RescheduledAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 303,
            ReferenceNumber = "APT-303",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.Rescheduled,
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 303,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(20, request));
        Assert.Contains("Rescheduled", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateRecordForSameAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 304,
            ReferenceNumber = "APT-304",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.InProgress,
            ScheduledStart = DateTime.UtcNow.AddHours(-1)
        };
        context.Appointments.Add(appointment);

        // Pre-existing medical record for appointment 304
        var existingRecord = new MedicalRecord
        {
            Id = 60,
            RecordNumber = "REC-060",
            PatientId = 10,
            DoctorId = 20,
            AppointmentId = 304,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "First visit",
            Diagnosis = "First diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(existingRecord);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var duplicateRequest = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 304,
            ChiefComplaint = "Duplicate attempt",
            Diagnosis = "Duplicate diagnosis"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(20, duplicateRequest));
        Assert.Contains("already been created for this appointment", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_CancelledAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var appointment = new Appointment
        {
            Id = 305,
            ReferenceNumber = "APT-305",
            PatientId = 10,
            DoctorId = 20,
            Status = AppointmentStatus.Cancelled,
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        };
        context.Appointments.Add(appointment);

        var record = new MedicalRecord
        {
            Id = 61,
            RecordNumber = "REC-061",
            PatientId = 10,
            DoctorId = 20,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Initial",
            Diagnosis = "Initial",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new UpdateMedicalRecordRequest
        {
            AppointmentId = 305,
            ChiefComplaint = "Initial",
            Diagnosis = "Initial"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(61, 20, request));
        Assert.Contains("Cancelled", ex.Message);
    }

    [Fact]
    public async Task GetByAppointmentIdAsync_NonExistentAppointment_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MedicalRecordService(context);

        // Act
        var result = await service.GetByAppointmentIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByAppointmentIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MedicalRecordService(context);

        // Act
        var result = await service.GetByAppointmentIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Unauthorized access & role checks (Service level)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_InactiveDoctor_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context);

        var inactiveDoctor = new User
        {
            Id = 25,
            FirstName = "Inactive",
            LastName = "Doc",
            Email = "inactive@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Inactive
        };
        context.Users.Add(inactiveDoctor);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(25, request));
        Assert.Contains("Active doctor not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_PatientRoleAsDoctor_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context); // Patient 10 has Role = UserRole.Patient
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis"
        };

        // Act & Assert: Patient attempts to act as doctor
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(10, request));
        Assert.Contains("Active doctor not found", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_DifferentDoctor_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context); // Doctor 20 and OtherDoctor 40

        var record = new MedicalRecord
        {
            Id = 70,
            RecordNumber = "REC-070",
            PatientId = 10,
            DoctorId = 20, // Owned by Doctor 20
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Unauthorized edit",
            Diagnosis = "Unauthorized edit"
        };

        // Act: Doctor 40 tries to edit Doctor 20's record
        var result = await service.UpdateAsync(70, 40, request);

        // Assert
        Assert.Null(result);
    }
}
