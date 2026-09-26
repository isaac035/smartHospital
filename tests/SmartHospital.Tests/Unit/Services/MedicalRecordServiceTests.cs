using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class MedicalRecordServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private (User patient, User doctor) SeedDefaultUsers(AppDbContext context, int patientId = 10, int doctorId = 20)
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
            FirstName = "Sarah",
            LastName = "Smith",
            Email = $"doctor{doctorId}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor);
        return (patient, doctor);
    }

    [Fact]
    public async Task CreateAsync_CreatesMedicalRecordWithRecordNumber()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Headache and fever",
            Diagnosis = "Viral infection",
            TreatmentPlan = "Rest and fluids"
        };

        // Act
        var result = await service.CreateAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("REC-", result.RecordNumber);
        Assert.Equal(10, result.PatientId);
        Assert.Equal(20, result.DoctorId);
        Assert.Equal("Headache and fever", result.ChiefComplaint);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 999, // Does not exist
            ChiefComplaint = "Chest tightness",
            Diagnosis = "Asthma exacerbation"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Active patient not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNonPatientUserRole_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var staffUser = new User
        {
            Id = 30,
            FirstName = "Staff",
            LastName = "Member",
            Email = "staff@example.com",
            PasswordHash = "hash",
            Role = UserRole.Staff,
            Status = UserStatus.Active
        };
        context.Users.Add(staffUser);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 30, // User is Staff, not Patient
            ChiefComplaint = "Fatigue",
            Diagnosis = "Burnout"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Active patient not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithInactivePatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (_, doctor) = SeedDefaultUsers(context, 10, 20);

        var inactivePatient = new User
        {
            Id = 40,
            FirstName = "Inactive",
            LastName = "Patient",
            Email = "inactive@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Inactive
        };
        context.Users.Add(inactivePatient);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 40,
            ChiefComplaint = "Knee pain",
            Diagnosis = "Ligament sprain"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Active patient not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentDoctor_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Dizziness",
            Diagnosis = "Vertigo"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(999, request));
        Assert.Equal("Active doctor not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithAppointmentNotBelongingToPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var otherPatient = new User
        {
            Id = 50,
            FirstName = "Other",
            LastName = "Patient",
            Email = "other@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };
        context.Users.Add(otherPatient);

        var appointment = new Appointment
        {
            Id = 101,
            PatientId = 50, // Belongs to patient 50, not patient 10
            DoctorId = 20,
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 101,
            ChiefComplaint = "Follow-up consultation",
            Diagnosis = "Hypertension"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Appointment does not belong to the specified patient.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 9999,
            ChiefComplaint = "Consultation",
            Diagnosis = "Routine checkup"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Appointment not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithAdmissionNotBelongingToPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var otherPatient = new User
        {
            Id = 60,
            FirstName = "Another",
            LastName = "Person",
            Email = "another@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };
        context.Users.Add(otherPatient);

        var admission = new Admission
        {
            Id = 201,
            AdmissionNumber = "ADM-201",
            PatientId = 60, // Belongs to patient 60, not 10
            AdmissionDate = DateTime.UtcNow
        };
        context.Admissions.Add(admission);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AdmissionId = 201,
            ChiefComplaint = "Inpatient round",
            Diagnosis = "Post-surgical monitoring"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Admission does not belong to the specified patient.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithValidAppointmentAndAdmission_Succeeds()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var appointment = new Appointment
        {
            Id = 301,
            PatientId = 10,
            DoctorId = 20,
            ScheduledStart = DateTime.UtcNow.AddHours(2)
        };

        var admission = new Admission
        {
            Id = 401,
            AdmissionNumber = "ADM-401",
            PatientId = 10,
            AdmissionDate = DateTime.UtcNow.AddDays(-1)
        };

        context.Appointments.Add(appointment);
        context.Admissions.Add(admission);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            AppointmentId = 301,
            AdmissionId = 401,
            ChiefComplaint = "Inpatient consultation during stay",
            Symptoms = "Fever and mild cough",
            ExaminationNotes = "Chest clear, temp 37.8C",
            Diagnosis = "Mild post-op atelectasis",
            TreatmentPlan = "Deep breathing exercises and ambulation",
            FollowUpDate = DateTime.UtcNow.AddDays(5)
        };

        // Act
        var result = await service.CreateAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(301, result.AppointmentId);
        Assert.Equal(401, result.AdmissionId);
        Assert.Equal(10, result.PatientId);
        Assert.Equal(20, result.DoctorId);
    }

    [Fact]
    public async Task CreateAsync_WithPastFollowUpDate_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Sore throat",
            Diagnosis = "Pharyngitis",
            FollowUpDate = DateTime.UtcNow.AddDays(-3)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
        Assert.Contains("Follow-up date must be in the future.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyChiefComplaint_ThrowsArgumentException(string chiefComplaint)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = chiefComplaint,
            Diagnosis = "Gastritis"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyDiagnosis_ThrowsArgumentException(string diagnosis)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Abdominal cramps",
            Diagnosis = diagnosis
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task UpdateAsync_WhenRecordDoesNotExist_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Updated complaint",
            Diagnosis = "Updated diagnosis"
        };

        // Act
        var result = await service.UpdateAsync(999, 20, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenDoctorDoesNotOwnRecord_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var record = new MedicalRecord
        {
            Id = 1,
            RecordNumber = "REC-001",
            PatientId = 10,
            DoctorId = 20,
            ChiefComplaint = "Initial complaint",
            Diagnosis = "Initial diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Updated complaint",
            Diagnosis = "Updated diagnosis"
        };

        // Act - Doctor 99 is attempting to update Doctor 20's record
        var result = await service.UpdateAsync(1, 99, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesRecordSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var record = new MedicalRecord
        {
            Id = 2,
            RecordNumber = "REC-002",
            PatientId = 10,
            DoctorId = 20,
            ChiefComplaint = "Original complaint",
            Diagnosis = "Original diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Revised complaint: back pain radiating to left leg",
            Symptoms = "Numbness in toes",
            ExaminationNotes = "Positive straight leg raise test",
            Diagnosis = "Sciatica / Lumbar disc herniation",
            TreatmentPlan = "Physiotherapy referral and NSAIDs",
            FollowUpDate = DateTime.UtcNow.AddDays(14)
        };

        // Act
        var result = await service.UpdateAsync(2, 20, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Revised complaint: back pain radiating to left leg", result.ChiefComplaint);
        Assert.Equal("Sciatica / Lumbar disc herniation", result.Diagnosis);
        Assert.Equal("Physiotherapy referral and NSAIDs", result.TreatmentPlan);
    }

    [Fact]
    public async Task UpdateAsync_WithPastFollowUpDate_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var record = new MedicalRecord
        {
            Id = 3,
            RecordNumber = "REC-003",
            PatientId = 10,
            DoctorId = 20,
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
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis",
            FollowUpDate = DateTime.UtcNow.AddDays(-5)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(3, 20, request));
    }

    [Fact]
    public async Task UpdateAsync_WithCrossPatientAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var record = new MedicalRecord
        {
            Id = 4,
            RecordNumber = "REC-004",
            PatientId = 10,
            DoctorId = 20,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);

        var otherPatient = new User
        {
            Id = 70,
            FirstName = "Other",
            LastName = "Patient",
            Email = "other70@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };
        context.Users.Add(otherPatient);

        var crossAppointment = new Appointment
        {
            Id = 505,
            PatientId = 70, // Belongs to patient 70, record is patient 10
            DoctorId = 20,
            ScheduledStart = DateTime.UtcNow.AddDays(2)
        };
        context.Appointments.Add(crossAppointment);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis",
            AppointmentId = 505
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(4, 20, request));
        Assert.Equal("Appointment does not belong to the specified patient.", ex.Message);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithInvalidPatientId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MedicalRecordService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByPatientIdAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByPatientIdAsync(-1));
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithNonExistentPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MedicalRecordService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetByPatientIdAsync(999));
        Assert.Equal("Patient with ID 999 was not found.", ex.Message);
    }
}
