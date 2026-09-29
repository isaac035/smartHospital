using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class PrescriptionServiceTests
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
            FirstName = "Alice",
            LastName = "Johnson",
            Email = $"alice{patientId}@example.com",
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

        context.Users.AddRange(patient, doctor);
        return (patient, doctor);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPrescriptionSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            GeneralInstructions = "Take with food",
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Amoxicillin",
                    Dosage = "500mg",
                    Route = "Oral",
                    Frequency = "TDS",
                    DurationDays = 7,
                    SpecialInstructions = "Complete full course"
                }
            }
        };

        // Act
        var result = await service.CreateAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("RX-", result.PrescriptionNumber);
        Assert.Equal(10, result.PatientId);
        Assert.Equal(20, result.DoctorId);
        Assert.Single(result.Items);
        Assert.Equal("Amoxicillin", result.Items[0].MedicineName);
        Assert.Equal("500mg", result.Items[0].Dosage);
        Assert.Equal(7, result.Items[0].DurationDays);
        Assert.True(result.ExpiryDate > DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 999, // Non-existent patient
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Paracetamol",
                    Dosage = "500mg",
                    Frequency = "BD",
                    DurationDays = 3
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Active patient not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNonPatientRole_ThrowsInvalidOperationException()
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

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 30, // Staff, not Patient
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Paracetamol",
                    Dosage = "500mg",
                    Frequency = "BD",
                    DurationDays = 3
                }
            }
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

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 40,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Ibuprofen",
                    Dosage = "400mg",
                    Frequency = "PRN",
                    DurationDays = 5
                }
            }
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

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Paracetamol",
                    Dosage = "500mg",
                    Frequency = "BD",
                    DurationDays = 3
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(999, request));
        Assert.Equal("Active doctor not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentMedicalRecord_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            MedicalRecordId = 9999, // Record does not exist
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Cetirizine",
                    Dosage = "10mg",
                    Frequency = "Once daily",
                    DurationDays = 10
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Medical record not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithMedicalRecordBelongingToDifferentPatient_ThrowsInvalidOperationException()
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

        var record = new MedicalRecord
        {
            Id = 101,
            RecordNumber = "REC-101",
            PatientId = 50, // Belongs to other patient (50), not requested patient (10)
            DoctorId = 20,
            ChiefComplaint = "Allergy symptoms",
            Diagnosis = "Allergic rhinitis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            MedicalRecordId = 101,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Cetirizine",
                    Dosage = "10mg",
                    Frequency = "Once daily",
                    DurationDays = 14
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Medical record does not belong to the specified patient.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithMatchingMedicalRecord_CreatesPrescriptionWithRecordLinkage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);

        var record = new MedicalRecord
        {
            Id = 202,
            RecordNumber = "REC-202",
            PatientId = 10,
            DoctorId = 20,
            ChiefComplaint = "Bacterial pharyngitis",
            Diagnosis = "Streptococcal pharyngitis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            MedicalRecordId = 202,
            GeneralInstructions = "Finish all antibiotic capsules",
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Penicillin V",
                    Dosage = "500mg",
                    Route = "Oral",
                    Frequency = "QID",
                    DurationDays = 10,
                    SpecialInstructions = "Take on an empty stomach"
                }
            }
        };

        // Act
        var result = await service.CreateAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(202, result.MedicalRecordId);
        Assert.Equal(10, result.PatientId);
        Assert.Equal(20, result.DoctorId);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyItems_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task CreateAsync_WithPastExpiryDate_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            ExpiryDate = DateTime.UtcNow.AddDays(-2),
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Paracetamol",
                    Dosage = "500mg",
                    Frequency = "BD",
                    DurationDays = 5
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
        Assert.Contains("Expiry date must be in the future.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithWhitespaceMedicineName_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "   ",
                    Dosage = "500mg",
                    Frequency = "Daily",
                    DurationDays = 5
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDuration_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Aspirin",
                    Dosage = "100mg",
                    Frequency = "Daily",
                    DurationDays = 400 // Exceeds 365
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithInvalidPatientId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByPatientIdAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByPatientIdAsync(-1));
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithNonExistentPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetByPatientIdAsync(999));
        Assert.Equal("Patient with ID 999 was not found.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull(int id)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);

        // Act
        var result = await service.GetByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingPrescription_ReturnsFullResponse()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (patient, doctor) = SeedDefaultUsers(context, 10, 20);

        var prescription = new Prescription
        {
            Id = 50,
            PrescriptionNumber = "RX-20260901",
            PatientId = 10,
            DoctorId = 20,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            Status = PrescriptionStatus.Active,
            GeneralInstructions = "Take after breakfast",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<PrescriptionItem>
            {
                new()
                {
                    MedicineName = "Amoxicillin",
                    Dosage = "500mg",
                    Route = "Oral",
                    Frequency = "TDS",
                    DurationDays = 7,
                    SpecialInstructions = "Complete the course"
                }
            }
        };
        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);

        // Act
        var result = await service.GetByIdAsync(50);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Id);
        Assert.Equal("RX-20260901", result.PrescriptionNumber);
        Assert.Equal(10, result.PatientId);
        Assert.Equal("Alice Johnson", result.PatientName);
        Assert.Equal(20, result.DoctorId);
        Assert.Equal("Dr. Robert Chen", result.DoctorName);
        Assert.Equal("Active", result.Status);
        Assert.Single(result.Items);
        Assert.Equal("Amoxicillin", result.Items[0].MedicineName);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(20, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WhenDoctorIdIsInvalid_ThrowsArgumentException(int doctorId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(doctorId, request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WhenPatientIdIsInvalid_ThrowsArgumentException(int patientId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = patientId,
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task CreateAsync_WhenItemsIsNull_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = null!
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task CreateAsync_WhenItemsIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task CreateAsync_WhenDoctorNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert: Doctor 999 does not exist
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(999, request));
        Assert.Equal("Active doctor not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPatientNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 999, // Does not exist
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Active patient not found.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WhenMedicalRecordIdIsZeroOrNegative_ThrowsArgumentException(int medRecordId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            MedicalRecordId = medRecordId,
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
    }

    [Fact]
    public async Task CreateAsync_WhenMedicalRecordDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            MedicalRecordId = 999, // Does not exist
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Medical record not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenMedicalRecordBelongsToDifferentPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (patient, doctor) = SeedDefaultUsers(context, 10, 20);

        var otherPatient = new User
        {
            Id = 30,
            FirstName = "Other",
            LastName = "Patient",
            Email = "other@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };
        context.Users.Add(otherPatient);

        // Medical record belongs to patient 30
        var record = new MedicalRecord
        {
            Id = 88,
            RecordNumber = "REC-088",
            PatientId = 30,
            DoctorId = 20,
            ChiefComplaint = "Complaint",
            Diagnosis = "Diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10, // Requesting for patient 10, but record belongs to 30
            MedicalRecordId = 88,
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(20, request));
        Assert.Equal("Medical record does not belong to the specified patient.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenExpiryDateMoreThanOneYearInFuture_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            ExpiryDate = DateTime.UtcNow.AddDays(400), // Exceeds 1 year
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
        Assert.Contains("Expiry date cannot be more than 1 year in the future.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenGeneralInstructionsExceeds1000Chars_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            GeneralInstructions = new string('G', 1001),
            Items = new List<CreatePrescriptionItemRequest> { new() { MedicineName = "Meds", Dosage = "1", Frequency = "1", DurationDays = 1 } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(20, request));
        Assert.Contains("General instructions cannot exceed 1000 characters.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenItemRouteIsEmpty_DefaultsToOral()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context, 10, 20);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Paracetamol",
                    Dosage = "500mg",
                    Route = "", // Empty route should fallback to "Oral"
                    Frequency = "TDS",
                    DurationDays = 5
                }
            }
        };

        // Act
        var result = await service.CreateAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Oral", result.Items[0].Route);
    }
}
