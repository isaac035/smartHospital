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

    [Fact]
    public async Task CreateAsync_CreatesMedicalRecordWithRecordNumber()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var patient = new User
        {
            Id = 10,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor = new User
        {
            Id = 20,
            FirstName = "Sarah",
            LastName = "Smith",
            Email = "sarah@example.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor);
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
}
