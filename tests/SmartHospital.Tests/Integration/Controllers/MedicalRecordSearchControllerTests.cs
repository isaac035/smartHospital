using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class MedicalRecordSearchControllerTests
{
    private class FakeMedicalRecordService : IMedicalRecordService
    {
        public MedicalRecordQueryFilter? LastFilter { get; private set; }

        public Task<PagedMedicalRecordResult> SearchAsync(MedicalRecordQueryFilter filter)
        {
            LastFilter = filter;

            if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
            {
                throw new ArgumentException("Start date cannot be after end date.");
            }

            var items = new List<MedicalRecordSummaryResponse>
            {
                new()
                {
                    Id = 1,
                    RecordNumber = filter.RecordNumber ?? "REC-2026-001",
                    PatientId = filter.PatientId ?? 10,
                    DoctorId = filter.DoctorId ?? 30,
                    VisitDate = DateTime.UtcNow,
                    ChiefComplaint = "Headache check",
                    Diagnosis = filter.Diagnosis ?? "Migraine"
                }
            };

            return Task.FromResult(new PagedMedicalRecordResult
            {
                Items = items,
                TotalCount = 1,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId) => throw new NotImplementedException();
        public Task<MedicalRecordResponse?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<MedicalRecordResponse?> GetByAppointmentIdAsync(int appointmentId) => throw new NotImplementedException();
        public Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request) => throw new NotImplementedException();
        public Task<MedicalRecordResponse?> UpdateAsync(int id, int doctorId, UpdateMedicalRecordRequest request) => throw new NotImplementedException();
        public Task<DiagnosisResponse> AddDiagnosisAsync(int medicalRecordId, int doctorId, AddDiagnosisRequest request) => throw new NotImplementedException();
        public Task<DiagnosisResponse?> UpdateDiagnosisAsync(int medicalRecordId, int diagnosisId, int doctorId, UpdateDiagnosisRequest request) => throw new NotImplementedException();
        public Task<DiagnosisResponse?> UpdatePrimaryDiagnosisAsync(int medicalRecordId, int doctorId, UpdateDiagnosisRequest request) => throw new NotImplementedException();
        public Task<List<DiagnosisResponse>> GetDiagnosesAsync(int medicalRecordId) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse> AddTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse> RecordTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse?> UpdateTreatmentPlanAsync(int medicalRecordId, int treatmentPlanId, int doctorId, UpdateTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse?> UpdatePrimaryTreatmentPlanAsync(int medicalRecordId, int doctorId, UpdateTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<List<TreatmentPlanResponse>> GetTreatmentPlansAsync(int medicalRecordId) => throw new NotImplementedException();
        public Task<List<MedicalRecordVersionResponse>> GetVersionHistoryAsync(int medicalRecordId) => throw new NotImplementedException();
        public Task<MedicalRecordVersionResponse?> GetVersionAsync(int medicalRecordId, int versionNumber) => throw new NotImplementedException();
    }

    private class FakeAuditService : IEmrAuditService
    {
        public List<EmrAuditLogResponse> Logs { get; } = new();

        public Task<EmrAuditLogResponse> LogAsync(int userId, string action, string entityType, int? entityId, int? patientId = null, string? metadata = null, bool isSuccess = true)
        {
            var log = new EmrAuditLogResponse
            {
                Id = Logs.Count + 1,
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                PatientId = patientId,
                Metadata = metadata,
                IsSuccess = isSuccess,
                Timestamp = DateTime.UtcNow
            };
            Logs.Add(log);
            return Task.FromResult(log);
        }

        public Task<List<EmrAuditLogResponse>> GetAuditLogsAsync(int? patientId = null, string? entityType = null, int? userId = null, int limit = 100) => Task.FromResult(Logs);
        public Task<EmrAuditLogResponse?> GetByIdAsync(int id) => Task.FromResult(Logs.FirstOrDefault(l => l.Id == id));
        public Task<List<EmrAuditLogResponse>> GetByPatientIdAsync(int patientId, int limit = 100) => Task.FromResult(Logs);
    }

    private static ControllerContext CreateUserContext(string role, string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Search_PatientCanQueryOwnRecords_ReturnsOk()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();
        var controller = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var filter = new MedicalRecordQueryFilter { PatientId = 10 };
        var result = await controller.Search(filter);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedMedicalRecordResult>(okResult.Value);
        Assert.Single(paged.Items);
        Assert.Equal(10, service.LastFilter!.PatientId);
    }

    [Fact]
    public async Task Search_PatientAttemptingToQueryOtherPatient_ReturnsForbidAndAuditsFailure()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();
        var controller = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        // Patient 10 attempts to query records for Patient 25
        var filter = new MedicalRecordQueryFilter { PatientId = 25 };
        var result = await controller.Search(filter);

        Assert.IsType<ForbidResult>(result);
        Assert.Single(audit.Logs);
        Assert.False(audit.Logs[0].IsSuccess);
        Assert.Equal(10, audit.Logs[0].UserId);
        Assert.Equal(25, audit.Logs[0].PatientId);
    }

    [Fact]
    public async Task Search_PatientWithoutSpecifyingPatientId_AutomaticallyScopedToOwnId()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();
        var controller = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Patient", "15") // Patient 15
        };

        // Patient does not supply PatientId
        var filter = new MedicalRecordQueryFilter { Diagnosis = "Cold" };
        var result = await controller.Search(filter);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        // Controller must force PatientId to 15
        Assert.Equal(15, service.LastFilter!.PatientId);
    }

    [Fact]
    public async Task Search_DoctorCanQueryAnyPatientAndFilterCriteria_ReturnsOk()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();
        var controller = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Doctor", "30")
        };

        var filter = new MedicalRecordQueryFilter
        {
            PatientId = 99,
            DoctorId = 30,
            Diagnosis = "Hypertension",
            Page = 2,
            PageSize = 20
        };
        var result = await controller.Search(filter);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedMedicalRecordResult>(okResult.Value);
        Assert.Equal(99, service.LastFilter!.PatientId);
        Assert.Equal(30, service.LastFilter!.DoctorId);
        Assert.Equal("Hypertension", service.LastFilter!.Diagnosis);
        Assert.Equal(2, service.LastFilter!.Page);
        Assert.Equal(20, service.LastFilter!.PageSize);
    }

    [Fact]
    public async Task Search_StaffAndAdminRoles_CanSearchRecords_ReturnsOk()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();

        var staffController = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Staff", "50")
        };
        var adminController = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Admin", "1")
        };

        var staffResult = await staffController.Search(new MedicalRecordQueryFilter { PatientId = 10 });
        var adminResult = await adminController.Search(new MedicalRecordQueryFilter { RecordNumber = "REC-001" });

        Assert.IsType<OkObjectResult>(staffResult);
        Assert.IsType<OkObjectResult>(adminResult);
    }

    [Fact]
    public async Task Search_InvalidDateRange_ReturnsBadRequest()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();
        var controller = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Doctor", "30")
        };

        var filter = new MedicalRecordQueryFilter
        {
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 1, 1)
        };

        var result = await controller.Search(filter);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Search_InvalidModelState_ReturnsBadRequest()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();
        var controller = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Doctor", "30")
        };
        controller.ModelState.AddModelError("Page", "Page must be at least 1.");

        var filter = new MedicalRecordQueryFilter { Page = 0 };
        var result = await controller.Search(filter);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Search_SuccessfulSearch_AuditsOperation()
    {
        var service = new FakeMedicalRecordService();
        var audit = new FakeAuditService();
        var controller = new MedicalRecordsController(service, audit)
        {
            ControllerContext = CreateUserContext("Doctor", "30")
        };

        var filter = new MedicalRecordQueryFilter { Diagnosis = "Flu" };
        var result = await controller.Search(filter);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(audit.Logs);
        Assert.Equal("Search", audit.Logs[0].Action);
        Assert.Equal("MedicalRecord", audit.Logs[0].EntityType);
        Assert.True(audit.Logs[0].IsSuccess);
    }
}
