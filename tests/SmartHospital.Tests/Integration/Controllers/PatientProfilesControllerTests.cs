using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class PatientProfilesControllerTests
{
    #region Stub Services

    private class StubProfileService : IPatientMedicalProfileService
    {
        public Task<PatientMedicalProfileResponse?> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 999) return Task.FromResult<PatientMedicalProfileResponse?>(null);

            if (patientId == 888)
            {
                throw new InvalidOperationException("Patient record is corrupted or inactive.");
            }

            if (patientId == 777)
            {
                throw new ArgumentException("Patient ID is outside acceptable range.");
            }

            return Task.FromResult<PatientMedicalProfileResponse?>(new PatientMedicalProfileResponse
            {
                Id = 1,
                PatientId = patientId,
                DateOfBirth = new DateTime(1990, 1, 15),
                Gender = "Male",
                BloodGroup = "APositive",
                Allergies = "Penicillin, Peanuts",
                ChronicDiseases = "Asthma",
                EmergencyContactName = "Jane Doe",
                EmergencyContactPhone = "+1234567890"
            });
        }

        public Task<PatientMedicalProfileResponse> UpsertAsync(int patientId, UpsertPatientMedicalProfileRequest request)
        {
            if (patientId == 999)
            {
                throw new InvalidOperationException("Patient not found.");
            }

            if (patientId == 777)
            {
                throw new ArgumentException("Invalid medical profile parameters.");
            }

            return Task.FromResult(new PatientMedicalProfileResponse
            {
                Id = 10,
                PatientId = patientId,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                BloodGroup = request.BloodGroup.ToString(),
                Allergies = request.Allergies,
                ChronicDiseases = request.ChronicDiseases,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone
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

    #region 1. GetByPatientId Tests

    [Fact]
    public async Task GetByPatientId_AsDoctor_ReturnsOkWithProfile()
    {
        // Arrange
        var profileService = new StubProfileService();
        var auditService = new StubAuditService();
        var controller = new PatientProfilesController(profileService, auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act
        var result = await controller.GetByPatientId(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<PatientMedicalProfileResponse>(okResult.Value);
        Assert.Equal(10, profile.PatientId);
        Assert.Equal("APositive", profile.BloodGroup);
        Assert.Equal("Male", profile.Gender);
    }

    [Fact]
    public async Task GetByPatientId_AsStaff_ReturnsOkWithProfile()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Staff", "20")
        };

        // Act
        var result = await controller.GetByPatientId(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<PatientMedicalProfileResponse>(okResult.Value);
        Assert.Equal(10, profile.PatientId);
    }

    [Fact]
    public async Task GetByPatientId_AsAdmin_ReturnsOkWithProfile()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Admin", "1")
        };

        // Act
        var result = await controller.GetByPatientId(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<PatientMedicalProfileResponse>(okResult.Value);
        Assert.Equal(10, profile.PatientId);
    }

    [Fact]
    public async Task GetByPatientId_AsPatientAccessingOwnProfile_ReturnsOk()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Act
        var result = await controller.GetByPatientId(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<PatientMedicalProfileResponse>(okResult.Value);
        Assert.Equal(10, profile.PatientId);
    }

    [Fact]
    public async Task GetByPatientId_AsPatientAccessingOtherPatientProfile_ReturnsForbidAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new PatientProfilesController(new StubProfileService(), auditService)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Act: Patient 10 attempting to view Patient 25's medical profile
        var result = await controller.GetByPatientId(25);

        // Assert
        Assert.IsType<ForbidResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(10, log.UserId);
        Assert.Equal(25, log.PatientId);
        Assert.Equal("PatientMedicalProfile", log.EntityType);
        Assert.Equal("View", log.Action);
        Assert.False(log.IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task GetByPatientId_WithInvalidPatientId_ReturnsBadRequest(int invalidPatientId)
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act
        var result = await controller.GetByPatientId(invalidPatientId);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetByPatientId_WhenProfileDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act: Patient 999 profile does not exist
        var result = await controller.GetByPatientId(999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task GetByPatientId_WhenServiceThrowsInvalidOperationException_ReturnsNotFound()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act: Patient 888 throws InvalidOperationException
        var result = await controller.GetByPatientId(888);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task GetByPatientId_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act: Patient 777 throws ArgumentException
        var result = await controller.GetByPatientId(777);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    #endregion

    #region 2. Upsert Tests

    [Fact]
    public async Task Upsert_AsDoctor_ReturnsOkAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new PatientProfilesController(new StubProfileService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var request = new UpsertPatientMedicalProfileRequest
        {
            DateOfBirth = new DateTime(1985, 5, 20),
            Gender = "Female",
            BloodGroup = BloodGroup.OPositive,
            Allergies = "Sulfa drugs",
            ChronicDiseases = "Hypertension",
            EmergencyContactName = "Bob Smith",
            EmergencyContactPhone = "+987654321"
        };

        // Act
        var result = await controller.Upsert(10, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<PatientMedicalProfileResponse>(okResult.Value);
        Assert.Equal(10, profile.PatientId);
        Assert.Equal("OPositive", profile.BloodGroup);
        Assert.Equal("Female", profile.Gender);

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal("UpdateProfile", log.Action);
        Assert.True(log.IsSuccess);
    }

    [Fact]
    public async Task Upsert_AsStaff_ReturnsOk()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Staff", "22")
        };

        var request = new UpsertPatientMedicalProfileRequest
        {
            Gender = "Male",
            BloodGroup = BloodGroup.BNegative
        };

        // Act
        var result = await controller.Upsert(12, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<PatientMedicalProfileResponse>(okResult.Value);
        Assert.Equal(12, profile.PatientId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Upsert_WithInvalidPatientId_ReturnsBadRequest(int invalidPatientId)
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var request = new UpsertPatientMedicalProfileRequest
        {
            Gender = "Male",
            BloodGroup = BloodGroup.APositive
        };

        // Act
        var result = await controller.Upsert(invalidPatientId, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Upsert_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        // Act
        var result = await controller.Upsert(10, null!);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Upsert_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };
        controller.ModelState.AddModelError("Gender", "Gender exceeds 20 characters.");

        var request = new UpsertPatientMedicalProfileRequest
        {
            Gender = new string('A', 30),
            BloodGroup = BloodGroup.APositive
        };

        // Act
        var result = await controller.Upsert(10, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upsert_WhenServiceThrowsArgumentException_ReturnsBadRequestAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new PatientProfilesController(new StubProfileService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var request = new UpsertPatientMedicalProfileRequest
        {
            Gender = "Male",
            BloodGroup = BloodGroup.APositive
        };

        // Act: Patient 777 throws ArgumentException
        var result = await controller.Upsert(777, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);

        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal(5, log.UserId);
        Assert.Equal(777, log.PatientId);
        Assert.False(log.IsSuccess);
    }

    [Fact]
    public async Task Upsert_WhenPatientNotFound_ReturnsNotFoundAndLogsAudit()
    {
        // Arrange
        var auditService = new StubAuditService();
        var controller = new PatientProfilesController(new StubProfileService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var request = new UpsertPatientMedicalProfileRequest
        {
            Gender = "Male",
            BloodGroup = BloodGroup.APositive
        };

        // Act: Patient 999 throws InvalidOperationException
        var result = await controller.Upsert(999, request);

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
        var authAttr = typeof(PatientProfilesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Fact]
    public void UpsertAction_HasAuthorizeRolesAttribute_RestrictedToDoctorStaffAdmin()
    {
        var method = typeof(PatientProfilesController).GetMethod(nameof(PatientProfilesController.Upsert));
        Assert.NotNull(method);

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Doctor,Staff,Admin", authAttr.Roles);
    }

    #endregion
}
