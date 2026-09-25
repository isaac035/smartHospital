using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class PrescriptionsControllerTests
{
    private class StubPrescriptionService : IPrescriptionService
    {
        public Task<List<PrescriptionResponse>> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 999)
            {
                throw new InvalidOperationException("Patient with ID 999 was not found.");
            }

            return Task.FromResult(new List<PrescriptionResponse>
            {
                new()
                {
                    Id = 1,
                    PrescriptionNumber = "RX-001",
                    PatientId = patientId,
                    DoctorId = 2,
                    IssueDate = DateTime.UtcNow,
                    Status = "Active"
                }
            });
        }

        public Task<PrescriptionResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<PrescriptionResponse?>(null);

            return Task.FromResult<PrescriptionResponse?>(new PrescriptionResponse
            {
                Id = id,
                PrescriptionNumber = "RX-001",
                PatientId = 1,
                DoctorId = 2,
                IssueDate = DateTime.UtcNow,
                Status = "Active"
            });
        }

        public Task<PrescriptionResponse> CreateAsync(int doctorId, CreatePrescriptionRequest request)
        {
            if (request.PatientId == 999)
            {
                throw new InvalidOperationException("Active patient not found.");
            }

            if (request.MedicalRecordId == 999)
            {
                throw new InvalidOperationException("Medical record does not belong to the specified patient.");
            }

            return Task.FromResult(new PrescriptionResponse
            {
                Id = 1,
                PrescriptionNumber = "RX-001",
                PatientId = request.PatientId,
                DoctorId = doctorId,
                MedicalRecordId = request.MedicalRecordId,
                Status = "Active"
            });
        }
    }

    private ControllerContext CreateDoctorContext(string doctorId = "5")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, doctorId),
            new Claim(ClaimTypes.Role, "Doctor")
        }, "TestAuth"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetByPatient_ReturnsOkWithList()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService);

        // Act
        var result = await controller.GetByPatient(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var records = Assert.IsAssignableFrom<List<PrescriptionResponse>>(okResult.Value);
        Assert.Single(records);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByPatient_WithInvalidId_ReturnsBadRequest(int invalidId)
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService);

        // Act
        var result = await controller.GetByPatient(invalidId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByPatient_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService);

        // Act
        var result = await controller.GetByPatient(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService);

        // Act
        var result = await controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetById_WithInvalidId_ReturnsBadRequest(int invalidId)
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService);

        // Act
        var result = await controller.GetById(invalidId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithDoctorClaims_ReturnsCreated()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Amoxicillin",
                    Dosage = "500mg",
                    Frequency = "TDS",
                    DurationDays = 7
                }
            }
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var record = Assert.IsType<PrescriptionResponse>(createdResult.Value);
        Assert.Equal(5, record.DoctorId);
    }

    [Fact]
    public async Task Create_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        // Act
        var result = await controller.Create(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };
        controller.ModelState.AddModelError("Items", "At least one prescription item is required.");

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            Items = new List<CreatePrescriptionItemRequest>()
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithoutDoctorClaims_ReturnsUnauthorized()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Amoxicillin",
                    Dosage = "500mg",
                    Frequency = "TDS",
                    DurationDays = 7
                }
            }
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 999, // Non-existent patient
            Items = new List<CreatePrescriptionItemRequest>
            {
                new()
                {
                    MedicineName = "Amoxicillin",
                    Dosage = "500mg",
                    Frequency = "TDS",
                    DurationDays = 7
                }
            }
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }
}
