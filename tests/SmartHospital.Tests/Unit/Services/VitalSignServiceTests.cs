using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class VitalSignServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static (User patient, User recorder) SeedUsers(AppDbContext context, int patientId = 1, int recorderId = 2)
    {
        var patient = context.Users.Find(patientId);
        if (patient == null)
        {
            patient = new User
            {
                Id = patientId,
                FirstName = "Alice",
                LastName = "Smith",
                Email = $"patient{patientId}@hospital.com",
                PasswordHash = "hash",
                Role = UserRole.Patient,
                Status = UserStatus.Active
            };
            context.Users.Add(patient);
        }

        var recorder = context.Users.Find(recorderId);
        if (recorder == null)
        {
            recorder = new User
            {
                Id = recorderId,
                FirstName = "Nurse",
                LastName = "Joy",
                Email = $"nurse{recorderId}@hospital.com",
                PasswordHash = "hash",
                Role = UserRole.Staff,
                Status = UserStatus.Active
            };
            context.Users.Add(recorder);
        }

        context.SaveChanges();
        return (patient, recorder);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WhenNoVitalsRecorded_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        var service = new VitalSignService(context);

        // Act
        var result = await service.GetByPatientIdAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsRecordsOrderedByRecordedAtDescending()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (patient, recorder) = SeedUsers(context, 10, 20);

        var earlier = new VitalSign
        {
            PatientId = 10,
            RecordedByUserId = 20,
            RecordedAt = DateTime.UtcNow.AddHours(-5),
            SystolicBloodPressure = 120,
            DiastolicBloodPressure = 80,
            HeartRateBpm = 70,
            Notes = "Morning check",
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        };

        var later = new VitalSign
        {
            PatientId = 10,
            RecordedByUserId = 20,
            RecordedAt = DateTime.UtcNow.AddHours(-1),
            SystolicBloodPressure = 130,
            DiastolicBloodPressure = 85,
            HeartRateBpm = 78,
            Notes = "Afternoon check",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        context.VitalSigns.AddRange(earlier, later);
        await context.SaveChangesAsync();

        var service = new VitalSignService(context);

        // Act
        var result = await service.GetByPatientIdAsync(10);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Afternoon check", result[0].Notes); // Latest first
        Assert.Equal("Morning check", result[1].Notes);
        Assert.Equal(130, result[0].SystolicBloodPressure);
        Assert.Equal(120, result[1].SystolicBloodPressure);
        Assert.Equal("Alice Smith", result[0].PatientName);
        Assert.Equal("Nurse Joy", result[0].RecordedByUserName);
    }

    [Fact]
    public async Task GetByPatientIdAsync_DoesNotReturnOtherPatientsVitals()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 1, 20);
        SeedUsers(context, 2, 20);

        context.VitalSigns.AddRange(
            new VitalSign
            {
                PatientId = 1,
                RecordedByUserId = 20,
                RecordedAt = DateTime.UtcNow,
                HeartRateBpm = 65,
                Notes = "Patient 1",
                CreatedAt = DateTime.UtcNow
            },
            new VitalSign
            {
                PatientId = 2,
                RecordedByUserId = 20,
                RecordedAt = DateTime.UtcNow,
                HeartRateBpm = 85,
                Notes = "Patient 2",
                CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var service = new VitalSignService(context);

        // Act
        var result = await service.GetByPatientIdAsync(1);

        // Assert
        Assert.Single(result);
        Assert.Equal("Patient 1", result[0].Notes);
    }

    [Fact]
    public async Task RecordAsync_WithAllFields_CreatesAndReturnsVitalSign()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (patient, recorder) = SeedUsers(context, 10, 20);
        var service = new VitalSignService(context);

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            TemperatureCelsius = 36.8m,
            SystolicBloodPressure = 118,
            DiastolicBloodPressure = 78,
            HeartRateBpm = 72,
            RespiratoryRateBpm = 16,
            OxygenSaturationSpO2 = 99.0m,
            WeightKg = 70.0m,
            HeightCm = 175.0m,
            Notes = "  Routine baseline vitals  "
        };

        // Act
        var result = await service.RecordAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(10, result.PatientId);
        Assert.Equal("Alice Smith", result.PatientName);
        Assert.Equal(20, result.RecordedByUserId);
        Assert.Equal("Nurse Joy", result.RecordedByUserName);
        Assert.Equal(36.8m, result.TemperatureCelsius);
        Assert.Equal(118, result.SystolicBloodPressure);
        Assert.Equal(78, result.DiastolicBloodPressure);
        Assert.Equal(72, result.HeartRateBpm);
        Assert.Equal(16, result.RespiratoryRateBpm);
        Assert.Equal(99.0m, result.OxygenSaturationSpO2);
        Assert.Equal(70.0m, result.WeightKg);
        Assert.Equal(175.0m, result.HeightCm);
        Assert.Equal("Routine baseline vitals", result.Notes); // Trimmed

        // Verify BMI calculation: 70 / (1.75 ^ 2) = 22.86
        Assert.NotNull(result.Bmi);
        Assert.Equal(22.86m, result.Bmi.Value);

        // Verify persisted in DB
        var persisted = await context.VitalSigns.FirstOrDefaultAsync(v => v.Id == result.Id);
        Assert.NotNull(persisted);
        Assert.Equal(22.86m, persisted.Bmi);
    }

    [Theory]
    [InlineData(70.0, 175.0, 22.86)]
    [InlineData(85.0, 180.0, 26.23)]
    [InlineData(50.0, 160.0, 19.53)]
    [InlineData(100.0, 200.0, 25.00)]
    public async Task RecordAsync_CalculatesCorrectBmi(double weight, double height, decimal expectedBmi)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        var service = new VitalSignService(context);

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            WeightKg = (decimal)weight,
            HeightCm = (decimal)height
        };

        // Act
        var result = await service.RecordAsync(20, request);

        // Assert
        Assert.NotNull(result.Bmi);
        Assert.Equal(expectedBmi, result.Bmi.Value);
    }

    [Fact]
    public async Task RecordAsync_WhenHeightOrWeightNull_BmiIsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        var service = new VitalSignService(context);

        // Act 1: Weight without height
        var r1 = await service.RecordAsync(20, new RecordVitalSignRequest { PatientId = 10, WeightKg = 70m, HeightCm = null });
        // Act 2: Height without weight
        var r2 = await service.RecordAsync(20, new RecordVitalSignRequest { PatientId = 10, WeightKg = null, HeightCm = 175m });
        // Act 3: Both null
        var r3 = await service.RecordAsync(20, new RecordVitalSignRequest { PatientId = 10, WeightKg = null, HeightCm = null });

        // Assert
        Assert.Null(r1.Bmi);
        Assert.Null(r2.Bmi);
        Assert.Null(r3.Bmi);
    }

    [Fact]
    public async Task RecordAsync_WhenHeightIsZeroOrNegative_BmiIsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedUsers(context, 10, 20);
        var service = new VitalSignService(context);

        // Act 1: Zero height
        var r1 = await service.RecordAsync(20, new RecordVitalSignRequest { PatientId = 10, WeightKg = 70m, HeightCm = 0m });
        // Act 2: Negative height
        var r2 = await service.RecordAsync(20, new RecordVitalSignRequest { PatientId = 10, WeightKg = 70m, HeightCm = -170m });

        // Assert
        Assert.Null(r1.Bmi);
        Assert.Null(r2.Bmi);
    }

    [Fact]
    public async Task RecordAsync_WithMedicalRecordId_LinksToMedicalRecord()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (patient, recorder) = SeedUsers(context, 10, 20);

        var record = new MedicalRecord
        {
            Id = 55,
            RecordNumber = "REC-055",
            PatientId = 10,
            DoctorId = 20,
            ChiefComplaint = "Fever",
            Diagnosis = "Viral",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new VitalSignService(context);

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            MedicalRecordId = 55,
            TemperatureCelsius = 38.5m,
            HeartRateBpm = 95
        };

        // Act
        var result = await service.RecordAsync(20, request);

        // Assert
        Assert.Equal(55, result.MedicalRecordId);
    }
}
