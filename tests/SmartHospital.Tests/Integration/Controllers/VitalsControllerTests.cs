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

public class VitalsControllerTests
{
    #region Stub Services

    private class StubVitalsService : IVitalSignService
    {
        public Task<List<VitalSignResponse>> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 888)
            {
                throw new InvalidOperationException("Patient record is corrupted or inactive.");
            }

            if (patientId == 777)
            {
                throw new ArgumentException("Patient identifier is invalid.");
            }

            return Task.FromResult(new List<VitalSignResponse>
            {
                new()
                {
                    Id = 1,
                    PatientId = patientId,
                    PatientName = "Jane Doe",
                    RecordedByUserId = 20,
                    RecordedByUserName = "Nurse Joy",
                    RecordedAt = DateTime.UtcNow.AddHours(-2),
                    TemperatureCelsius = 37.1m,
                    SystolicBloodPressure = 120,
                    DiastolicBloodPressure = 80,
                    HeartRateBpm = 72,
                    RespiratoryRateBpm = 16,
                    OxygenSaturationSpO2 = 98.5m,
                    WeightKg = 68.5m,
                    HeightCm = 172.0m,
                    Bmi = 23.15m,
                    Notes = "Normal vitals"
                }
            });
        }

        public Task<VitalSignResponse> RecordAsync(int recordedByUserId, RecordVitalSignRequest request)
        {
            if (request.PatientId == 999)
            {
                throw new InvalidOperationException("Active patient not found.");
            }

            if (request.HeartRateBpm < 30)
            {
                throw new ArgumentException("Heart rate cannot be below 30 bpm.");
            }

            decimal? bmi = null;
            if (request.WeightKg.HasValue && request.HeightCm.HasValue && request.HeightCm.Value > 0)
            {
                var heightMeters = request.HeightCm.Value / 100m;
                bmi = Math.Round(request.WeightKg.Value / (heightMeters * heightMeters), 2);
            }

            return Task.FromResult(new VitalSignResponse
            {
                Id = 42,
                PatientId = request.PatientId,
                PatientName = "Test Patient",
                MedicalRecordId = request.MedicalRecordId,
                RecordedByUserId = recordedByUserId,
                RecordedByUserName = "Staff User",
                RecordedAt = DateTime.UtcNow,
                TemperatureCelsius = request.TemperatureCelsius,
                SystolicBloodPressure = request.SystolicBloodPressure,
                DiastolicBloodPressure = request.DiastolicBloodPressure,
                HeartRateBpm = request.HeartRateBpm,
                RespiratoryRateBpm = request.RespiratoryRateBpm,
                OxygenSaturationSpO2 = request.OxygenSaturationSpO2,
                WeightKg = request.WeightKg,
                HeightCm = request.HeightCm,
                Bmi = bmi,
                Notes = request.Notes
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

    #endregion

    #region Context Helpers

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

    private static ControllerContext CreateUnauthenticatedContext()
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }

    #endregion

    #region 1. GetByPatient Tests

    [Fact]
    public async Task GetByPatient_AsDoctor_ReturnsOkWithVitals()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act
        var result = await controller.GetByPatient(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var vitals = Assert.IsType<List<VitalSignResponse>>(okResult.Value);
        Assert.Single(vitals);
        Assert.Equal(10, vitals[0].PatientId);
        Assert.Equal(120, vitals[0].SystolicBloodPressure);
    }

    [Fact]
    public async Task GetByPatient_AsStaff_ReturnsOkWithVitals()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Staff", "20")
        };

        // Act
        var result = await controller.GetByPatient(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var vitals = Assert.IsType<List<VitalSignResponse>>(okResult.Value);
        Assert.Single(vitals);
    }

    [Fact]
    public async Task GetByPatient_AsAdmin_ReturnsOkWithVitals()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Admin", "1")
        };

        // Act
        var result = await controller.GetByPatient(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var vitals = Assert.IsType<List<VitalSignResponse>>(okResult.Value);
        Assert.Single(vitals);
    }

    [Fact]
    public async Task GetByPatient_AsPatientAccessingOwnVitals_ReturnsOk()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Act
        var result = await controller.GetByPatient(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var vitals = Assert.IsType<List<VitalSignResponse>>(okResult.Value);
        Assert.Single(vitals);
        Assert.Equal(10, vitals[0].PatientId);
    }

    [Fact]
    public async Task GetByPatient_AsPatientAccessingOtherPatientVitals_ReturnsForbidAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new VitalsController(new StubVitalsService(), auditService)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Act: Patient 10 accessing Patient 30's vitals
        var result = await controller.GetByPatient(30);

        // Assert
        Assert.IsType<ForbidResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(10, log.UserId);
        Assert.Equal(30, log.PatientId);
        Assert.Equal("VitalSign", log.EntityType);
        Assert.Equal("ViewList", log.Action);
        Assert.False(log.IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByPatient_WithInvalidPatientId_ReturnsBadRequest(int invalidId)
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act
        var result = await controller.GetByPatient(invalidId);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetByPatient_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act: Patient 777 throws ArgumentException
        var result = await controller.GetByPatient(777);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetByPatient_WhenServiceThrowsInvalidOperationException_ReturnsNotFound()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act: Patient 888 throws InvalidOperationException
        var result = await controller.GetByPatient(888);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
    }

    #endregion

    #region 2. Record Tests

    [Fact]
    public async Task Record_AsNurseStaff_ReturnsCreatedWithLocationAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new VitalsController(new StubVitalsService(), auditService)
        {
            ControllerContext = CreateUserContext("Staff", "20")
        };

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            TemperatureCelsius = 36.8m,
            SystolicBloodPressure = 118,
            DiastolicBloodPressure = 78,
            HeartRateBpm = 70,
            RespiratoryRateBpm = 14,
            OxygenSaturationSpO2 = 99.0m,
            WeightKg = 70.0m,
            HeightCm = 175.0m,
            Notes = "Routine pre-op check"
        };

        // Act
        var result = await controller.Record(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/vitals/42", createdResult.Location);
        var created = Assert.IsType<VitalSignResponse>(createdResult.Value);
        Assert.Equal(42, created.Id);
        Assert.Equal(10, created.PatientId);
        Assert.Equal(20, created.RecordedByUserId);
        Assert.Equal(22.86m, created.Bmi);

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(20, log.UserId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal("RecordVitals", log.Action);
        Assert.True(log.IsSuccess);
    }

    [Fact]
    public async Task Record_AsDoctor_ReturnsCreated()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            HeartRateBpm = 80
        };

        // Act
        var result = await controller.Record(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var created = Assert.IsType<VitalSignResponse>(createdResult.Value);
        Assert.Equal(42, created.Id);
    }

    [Fact]
    public async Task Record_WithoutValidUserIdentification_ReturnsUnauthorized()
    {
        // Arrange: unauthenticated or invalid user ID
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            HeartRateBpm = 80
        };

        // Act
        var result = await controller.Record(request);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorized.Value);
    }

    [Fact]
    public async Task Record_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act
        var result = await controller.Record(null!);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Record_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };
        controller.ModelState.AddModelError("TemperatureCelsius", "Temperature must be between 30 and 45 °C.");

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            TemperatureCelsius = 50.0m
        };

        // Act
        var result = await controller.Record(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Record_WhenServiceThrowsArgumentException_ReturnsBadRequestAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new VitalsController(new StubVitalsService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            HeartRateBpm = 15 // Under 30 triggers ArgumentException in stub
        };

        // Act
        var result = await controller.Record(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(10, log.PatientId);
        Assert.False(log.IsSuccess);
    }

    [Fact]
    public async Task Record_WhenPatientNotFound_ReturnsNotFoundAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new VitalsController(new StubVitalsService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var request = new RecordVitalSignRequest
        {
            PatientId = 999, // Triggers InvalidOperationException in stub
            HeartRateBpm = 75
        };

        // Act
        var result = await controller.Record(request);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(999, log.PatientId);
        Assert.False(log.IsSuccess);
    }

    #endregion

    #region 3. Role Restriction & Authorization Attribute Verification

    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var authAttr = typeof(VitalsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Fact]
    public void RecordAction_HasAuthorizeRolesAttribute_RestrictedToDoctorStaffAdmin()
    {
        var method = typeof(VitalsController).GetMethod(nameof(VitalsController.Record));
        Assert.NotNull(method);

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Doctor,Staff,Admin", authAttr.Roles);
    }

    #endregion
}
