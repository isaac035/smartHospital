using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class PatientMedicalProfileServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static User SeedPatient(AppDbContext context, int patientId = 1, string firstName = "Jane", string lastName = "Doe")
    {
        var patient = new User
        {
            Id = patientId,
            FirstName = firstName,
            LastName = lastName,
            Email = $"patient{patientId}@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        context.Users.Add(patient);
        context.SaveChanges();
        return patient;
    }

    [Fact]
    public async Task GetByPatientIdAsync_WhenProfileDoesNotExist_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedPatient(context, 10);
        var service = new PatientMedicalProfileService(context);

        // Act
        var result = await service.GetByPatientIdAsync(10);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WhenNonExistentPatientId_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new PatientMedicalProfileService(context);

        // Act
        var result = await service.GetByPatientIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WhenProfileExists_ReturnsMappedResponse()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var patient = SeedPatient(context, 15, "Michael", "Scott");

        var profile = new PatientMedicalProfile
        {
            PatientId = 15,
            DateOfBirth = new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodGroup = BloodGroup.OPositive,
            Allergies = "Penicillin, Peanuts",
            ChronicDiseases = "Hypertension",
            EmergencyContactName = "Holly Flax",
            EmergencyContactPhone = "+1-555-0199",
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.PatientMedicalProfiles.Add(profile);
        await context.SaveChangesAsync();

        var service = new PatientMedicalProfileService(context);

        // Act
        var result = await service.GetByPatientIdAsync(15);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result.PatientId);
        Assert.Equal("Michael Scott", result.PatientName);
        Assert.Equal(new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc), result.DateOfBirth);
        Assert.Equal("Male", result.Gender);
        Assert.Equal("OPositive", result.BloodGroup);
        Assert.Equal("Penicillin, Peanuts", result.Allergies);
        Assert.Equal("Hypertension", result.ChronicDiseases);
        Assert.Equal("Holly Flax", result.EmergencyContactName);
        Assert.Equal("+1-555-0199", result.EmergencyContactPhone);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithMultipleProfilesInDb_ReturnsCorrectPatientProfile()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedPatient(context, 1, "Patient", "One");
        SeedPatient(context, 2, "Patient", "Two");

        context.PatientMedicalProfiles.AddRange(
            new PatientMedicalProfile
            {
                PatientId = 1,
                Gender = "Female",
                BloodGroup = BloodGroup.APositive,
                Allergies = "None",
                ChronicDiseases = "None",
                EmergencyContactName = "Contact One",
                EmergencyContactPhone = "111",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PatientMedicalProfile
            {
                PatientId = 2,
                Gender = "Male",
                BloodGroup = BloodGroup.BNegative,
                Allergies = "Sulfa",
                ChronicDiseases = "Asthma",
                EmergencyContactName = "Contact Two",
                EmergencyContactPhone = "222",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var service = new PatientMedicalProfileService(context);

        // Act
        var result = await service.GetByPatientIdAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PatientId);
        Assert.Equal("BNegative", result.BloodGroup);
        Assert.Equal("Sulfa", result.Allergies);
        Assert.Equal("Asthma", result.ChronicDiseases);
    }

    [Fact]
    public async Task UpsertAsync_WhenProfileDoesNotExist_CreatesNewProfile()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedPatient(context, 20, "Jim", "Halpert");
        var service = new PatientMedicalProfileService(context);

        var request = new UpsertPatientMedicalProfileRequest
        {
            DateOfBirth = new DateTime(1988, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "  Male  ",
            BloodGroup = BloodGroup.ABPositive,
            Allergies = "  Dust mites  ",
            ChronicDiseases = "  None  ",
            EmergencyContactName = "  Pam Beesly  ",
            EmergencyContactPhone = "  +1-555-0144  "
        };

        // Act
        var result = await service.UpsertAsync(20, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.PatientId);
        Assert.Equal("Male", result.Gender); // Trimmed
        Assert.Equal("ABPositive", result.BloodGroup);
        Assert.Equal("Dust mites", result.Allergies); // Trimmed
        Assert.Equal("None", result.ChronicDiseases); // Trimmed
        Assert.Equal("Pam Beesly", result.EmergencyContactName); // Trimmed
        Assert.Equal("+1-555-0144", result.EmergencyContactPhone); // Trimmed

        // Verify persisted in DB
        var savedInDb = await context.PatientMedicalProfiles.FirstOrDefaultAsync(p => p.PatientId == 20);
        Assert.NotNull(savedInDb);
        Assert.Equal("Male", savedInDb.Gender);
        Assert.Equal(BloodGroup.ABPositive, savedInDb.BloodGroup);
    }

    [Fact]
    public async Task UpsertAsync_WhenProfileAlreadyExists_UpdatesExistingProfileAndPreservesCreatedAt()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedPatient(context, 30, "Dwight", "Schrute");

        var initialCreatedAt = DateTime.UtcNow.AddDays(-30);
        var existing = new PatientMedicalProfile
        {
            PatientId = 30,
            DateOfBirth = new DateTime(1975, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodGroup = BloodGroup.ONegative,
            Allergies = "None",
            ChronicDiseases = "None",
            EmergencyContactName = "Mose Schrute",
            EmergencyContactPhone = "555-BEETS",
            CreatedAt = initialCreatedAt,
            UpdatedAt = initialCreatedAt
        };
        context.PatientMedicalProfiles.Add(existing);
        await context.SaveChangesAsync();

        var service = new PatientMedicalProfileService(context);

        var updateRequest = new UpsertPatientMedicalProfileRequest
        {
            DateOfBirth = new DateTime(1975, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodGroup = BloodGroup.ONegative,
            Allergies = "Poison Ivy",
            ChronicDiseases = "Concussion history",
            EmergencyContactName = "Angela Martin",
            EmergencyContactPhone = "555-CATS"
        };

        // Act
        var result = await service.UpsertAsync(30, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(30, result.PatientId);
        Assert.Equal("Poison Ivy", result.Allergies);
        Assert.Equal("Concussion history", result.ChronicDiseases);
        Assert.Equal("Angela Martin", result.EmergencyContactName);
        Assert.Equal("555-CATS", result.EmergencyContactPhone);

        // Verify only 1 profile exists and CreatedAt was preserved
        var count = await context.PatientMedicalProfiles.CountAsync(p => p.PatientId == 30);
        Assert.Equal(1, count);

        var persisted = await context.PatientMedicalProfiles.FirstAsync(p => p.PatientId == 30);
        Assert.Equal(initialCreatedAt, persisted.CreatedAt);
        Assert.True(persisted.UpdatedAt > initialCreatedAt);
    }

    [Theory]
    [InlineData(BloodGroup.APositive, "APositive")]
    [InlineData(BloodGroup.ANegative, "ANegative")]
    [InlineData(BloodGroup.BPositive, "BPositive")]
    [InlineData(BloodGroup.BNegative, "BNegative")]
    [InlineData(BloodGroup.ABPositive, "ABPositive")]
    [InlineData(BloodGroup.ABNegative, "ABNegative")]
    [InlineData(BloodGroup.OPositive, "OPositive")]
    [InlineData(BloodGroup.ONegative, "ONegative")]
    [InlineData(BloodGroup.Unknown, "Unknown")]
    public async Task UpsertAsync_SupportsAllBloodGroupVariants(BloodGroup bloodGroup, string expectedString)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedPatient(context, 40);
        var service = new PatientMedicalProfileService(context);

        var request = new UpsertPatientMedicalProfileRequest
        {
            Gender = "Other",
            BloodGroup = bloodGroup,
            Allergies = "None",
            ChronicDiseases = "None",
            EmergencyContactName = "Guardian",
            EmergencyContactPhone = "999"
        };

        // Act
        var result = await service.UpsertAsync(40, request);

        // Assert
        Assert.Equal(expectedString, result.BloodGroup);
    }

    [Fact]
    public async Task UpsertAsync_WithNullDateOfBirth_Succeeds()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        SeedPatient(context, 50);
        var service = new PatientMedicalProfileService(context);

        var request = new UpsertPatientMedicalProfileRequest
        {
            DateOfBirth = null,
            Gender = "Female",
            BloodGroup = BloodGroup.APositive,
            Allergies = "",
            ChronicDiseases = "",
            EmergencyContactName = "Contact",
            EmergencyContactPhone = "123"
        };

        // Act
        var result = await service.UpsertAsync(50, request);

        // Assert
        Assert.Null(result.DateOfBirth);
        Assert.Equal(50, result.PatientId);
    }
}
