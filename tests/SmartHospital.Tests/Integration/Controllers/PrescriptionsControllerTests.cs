using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

            if (patientId == 777)
            {
                throw new ArgumentException("Patient ID is invalid.");
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
                PrescriptionNumber = $"RX-{id:D3}",
                PatientId = id == 2 ? 20 : 1, // Prescription 1 -> Patient 1, Prescription 2 -> Patient 20
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

            if (request.GeneralInstructions != null && request.GeneralInstructions.Length > 1000)
            {
                throw new ArgumentException("Instructions cannot exceed 1000 characters.");
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

    private class StubAuditService : IEmrAuditService
    {
        public List<EmrAuditLogResponse> Logs { get; } = new();

        public Task<EmrAuditLogResponse> LogAsync(
            int userId,
            string action,
            string entityType,
            int? entityId,
            int? patientId = null,
            string? metadata = null,
            bool isSuccess = true)
        {
            var log = new EmrAuditLogResponse
            {
                Id = Logs.Count + 1,
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                PatientId = patientId,
                Timestamp = DateTime.UtcNow,
                Metadata = metadata,
                IsSuccess = isSuccess
            };
            Logs.Add(log);
            return Task.FromResult(log);
        }

        public Task<List<EmrAuditLogResponse>> GetAuditLogsAsync(int? patientId = null, string? entityType = null, int? userId = null, int limit = 100)
            => Task.FromResult(Logs);

        public Task<EmrAuditLogResponse?> GetByIdAsync(int id)
            => Task.FromResult(Logs.FirstOrDefault(l => l.Id == id));

        public Task<List<EmrAuditLogResponse>> GetByPatientIdAsync(int patientId, int limit = 100)
            => Task.FromResult(Logs.Where(l => l.PatientId == patientId).ToList());
    }

    private static ControllerContext CreateUserContext(string role, string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private static ControllerContext CreateDoctorContext(string doctorId = "5")
        => CreateUserContext("Doctor", doctorId);

    #region 1. GetByPatient Tests

    [Fact]
    public async Task GetByPatient_ReturnsOkWithList()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

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
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

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
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        // Act
        var result = await controller.GetByPatient(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetByPatient_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        // Act
        var result = await controller.GetByPatient(777);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByPatient_AsPatientAccessingOwnPrescriptions_ReturnsOk()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Act
        var result = await controller.GetByPatient(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var records = Assert.IsAssignableFrom<List<PrescriptionResponse>>(okResult.Value);
        Assert.Single(records);
    }

    [Fact]
    public async Task GetByPatient_AsPatientAccessingOtherPatientPrescriptions_ReturnsForbid()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Act: Patient 10 attempting to view Patient 20's prescriptions
        var result = await controller.GetByPatient(20);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    #endregion

    #region 2. GetById Tests

    [Fact]
    public async Task GetById_ReturnsOk_AndWritesAuditLog()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var auditService = new StubAuditService();
        var controller = new PrescriptionsController(stubService, auditService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        // Act
        var result = await controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var rx = Assert.IsType<PrescriptionResponse>(okResult.Value);
        Assert.Equal(1, rx.Id);

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(1, log.PatientId);
        Assert.Equal("Prescription", log.EntityType);
        Assert.True(log.IsSuccess);
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
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        // Act
        var result = await controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_AsPatientAccessingOwnPrescription_ReturnsOk()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var controller = new PrescriptionsController(stubService)
        {
            ControllerContext = CreateUserContext("Patient", "1") // Prescription 1 has PatientId 1
        };

        // Act
        var result = await controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var rx = Assert.IsType<PrescriptionResponse>(okResult.Value);
        Assert.Equal(1, rx.PatientId);
    }

    [Fact]
    public async Task GetById_AsPatientAccessingOtherPatientPrescription_ReturnsForbidAndLogsAudit()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var auditService = new StubAuditService();
        var controller = new PrescriptionsController(stubService, auditService)
        {
            ControllerContext = CreateUserContext("Patient", "1") // Patient 1
        };

        // Act: Prescription 2 belongs to Patient 20
        var result = await controller.GetById(2);

        // Assert
        Assert.IsType<ForbidResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(1, log.UserId);
        Assert.Equal(20, log.PatientId);
        Assert.False(log.IsSuccess);
    }

    #endregion

    #region 3. Create Tests

    [Fact]
    public async Task Create_ReturnsCreated_AndWritesAuditLog()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var auditService = new StubAuditService();
        var controller = new PrescriptionsController(stubService, auditService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            MedicalRecordId = 10,
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
        Assert.Equal("/api/prescriptions/1", createdResult.Location);
        var prescription = Assert.IsType<PrescriptionResponse>(createdResult.Value);
        Assert.Equal("RX-001", prescription.PrescriptionNumber);

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(1, log.PatientId);
        Assert.Equal("Prescription", log.EntityType);
        Assert.True(log.IsSuccess);
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
    public async Task Create_WhenServiceThrowsInvalidOperation_ReturnsBadRequestAndLogsAudit()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var auditService = new StubAuditService();
        var controller = new PrescriptionsController(stubService, auditService)
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

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(999, log.PatientId);
        Assert.False(log.IsSuccess);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsArgumentException_ReturnsBadRequestAndLogsAudit()
    {
        // Arrange
        var stubService = new StubPrescriptionService();
        var auditService = new StubAuditService();
        var controller = new PrescriptionsController(stubService, auditService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 1,
            GeneralInstructions = new string('A', 1001),
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

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(1, log.PatientId);
        Assert.False(log.IsSuccess);
    }

    #endregion

    #region 4. Role Restriction & Authorization Attribute Verification

    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var authAttr = typeof(PrescriptionsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Fact]
    public void CreateAction_HasAuthorizeRolesAttribute_RestrictedToDoctorAdmin()
    {
        var method = typeof(PrescriptionsController).GetMethod(nameof(PrescriptionsController.Create));
        Assert.NotNull(method);

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Doctor,Admin", authAttr.Roles);
    }

    #endregion
}
