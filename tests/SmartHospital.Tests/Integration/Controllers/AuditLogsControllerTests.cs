using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class AuditLogsControllerTests
{
    #region Stub Audit Service

    public class StubAuditService : IEmrAuditService
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
                UserName = $"User {userId}",
                UserRole = "Doctor",
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

        public Task<List<EmrAuditLogResponse>> GetAuditLogsAsync(
            int? patientId = null,
            string? entityType = null,
            int? userId = null,
            int limit = 100)
        {
            var query = Logs.AsEnumerable();
            if (patientId.HasValue) query = query.Where(l => l.PatientId == patientId.Value);
            if (!string.IsNullOrEmpty(entityType)) query = query.Where(l => l.EntityType == entityType);
            if (userId.HasValue) query = query.Where(l => l.UserId == userId.Value);
            return Task.FromResult(query.Take(limit).ToList());
        }

        public Task<EmrAuditLogResponse?> GetByIdAsync(int id)
        {
            var log = Logs.FirstOrDefault(l => l.Id == id);
            return Task.FromResult<EmrAuditLogResponse?>(log);
        }

        public Task<List<EmrAuditLogResponse>> GetByPatientIdAsync(int patientId, int limit = 100)
        {
            return GetAuditLogsAsync(patientId: patientId, limit: limit);
        }
    }

    #endregion

    #region Other Stubs for Controller Integration

    private class StubMedicalRecordService : IMedicalRecordService
    {
        public Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<MedicalRecordSummaryResponse>
            {
                new() { Id = 1, PatientId = patientId, RecordNumber = "REC-1" }
            });
        }

        public Task<MedicalRecordResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<MedicalRecordResponse?>(null);
            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = id,
                RecordNumber = $"REC-{id}",
                PatientId = 10,
                DoctorId = 20,
                ChiefComplaint = "Headache",
                Diagnosis = "Migraine"
            });
        }

        public Task<MedicalRecordResponse?> GetByAppointmentIdAsync(int appointmentId) => Task.FromResult<MedicalRecordResponse?>(null);

        public Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request)
        {
            return Task.FromResult(new MedicalRecordResponse
            {
                Id = 1,
                RecordNumber = "REC-001",
                PatientId = request.PatientId,
                DoctorId = doctorId,
                ChiefComplaint = request.ChiefComplaint,
                Diagnosis = request.Diagnosis
            });
        }

        public Task<MedicalRecordResponse?> UpdateAsync(int id, int doctorId, UpdateMedicalRecordRequest request)
        {
            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = id,
                RecordNumber = "REC-001",
                PatientId = 10,
                DoctorId = doctorId,
                ChiefComplaint = request.ChiefComplaint,
                Diagnosis = request.Diagnosis
            });
        }

        public Task<List<MedicalRecordVersionResponse>> GetVersionHistoryAsync(int medicalRecordId) => throw new NotImplementedException();
        public Task<MedicalRecordVersionResponse?> GetVersionAsync(int medicalRecordId, int versionNumber) => throw new NotImplementedException();
        public Task<List<DiagnosisResponse>> GetDiagnosesAsync(int medicalRecordId) => throw new NotImplementedException();
        public Task<DiagnosisResponse> AddDiagnosisAsync(int medicalRecordId, int doctorId, AddDiagnosisRequest request) => throw new NotImplementedException();
        public Task<DiagnosisResponse?> UpdateDiagnosisAsync(int medicalRecordId, int diagnosisId, int doctorId, UpdateDiagnosisRequest request) => throw new NotImplementedException();
        public Task<DiagnosisResponse?> UpdatePrimaryDiagnosisAsync(int medicalRecordId, int doctorId, UpdateDiagnosisRequest request) => throw new NotImplementedException();
        public Task<List<TreatmentPlanResponse>> GetTreatmentPlansAsync(int medicalRecordId) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse> AddTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse> RecordTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse?> UpdateTreatmentPlanAsync(int medicalRecordId, int treatmentPlanId, int doctorId, UpdateTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<TreatmentPlanResponse?> UpdatePrimaryTreatmentPlanAsync(int medicalRecordId, int doctorId, UpdateTreatmentPlanRequest request) => throw new NotImplementedException();
        public Task<PagedMedicalRecordResult> SearchAsync(MedicalRecordQueryFilter filter)
        {
            return Task.FromResult(new PagedMedicalRecordResult
            {
                Items = new List<MedicalRecordSummaryResponse>(),
                TotalCount = 0,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }
    }

    private class StubPrescriptionService : IPrescriptionService
    {
        public Task<List<PrescriptionResponse>> GetByPatientIdAsync(int patientId) => throw new NotImplementedException();
        public Task<PrescriptionResponse?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<PrescriptionResponse> CreateAsync(int doctorId, CreatePrescriptionRequest request)
        {
            return Task.FromResult(new PrescriptionResponse
            {
                Id = 1,
                PrescriptionNumber = "RX-001",
                PatientId = request.PatientId,
                DoctorId = doctorId
            });
        }
    }

    private class StubLabOrderService : ILabOrderService
    {
        public Task<List<LabOrderResponse>> GetByPatientIdAsync(int patientId) => throw new NotImplementedException();
        public Task<LabOrderResponse?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<LabOrderResponse> CreateOrderAsync(int doctorId, CreateLabOrderRequest request)
        {
            return Task.FromResult(new LabOrderResponse
            {
                Id = 1,
                OrderNumber = "LAB-001",
                TestName = request.TestName,
                PatientId = request.PatientId,
                DoctorId = doctorId
            });
        }
        public Task<LabOrderResponse?> UpdateStatusAsync(int id, UpdateLabOrderStatusRequest request) => throw new NotImplementedException();
        public Task<LabOrderResponse?> UpdateStatusAsync(int id, LabOrderStatus newStatus) => throw new NotImplementedException();
        public Task<LabReportResponse?> RecordReportAsync(int id, int conductedByUserId, RecordLabReportRequest request)
        {
            return Task.FromResult<LabReportResponse?>(new LabReportResponse
            {
                Id = 1,
                LabOrderId = id,
                ConductedByUserId = conductedByUserId,
                ResultSummary = request.ResultSummary
            });
        }
    }

    private class StubProfileService : IPatientMedicalProfileService
    {
        public Task<PatientMedicalProfileResponse?> GetByPatientIdAsync(int patientId) => throw new NotImplementedException();
        public Task<PatientMedicalProfileResponse> UpsertAsync(int patientId, UpsertPatientMedicalProfileRequest request)
        {
            return Task.FromResult(new PatientMedicalProfileResponse
            {
                Id = 1,
                PatientId = patientId,
                BloodGroup = request.BloodGroup.ToString()
            });
        }
    }

    private class StubVitalsService : IVitalSignService
    {
        public Task<List<VitalSignResponse>> GetByPatientIdAsync(int patientId) => throw new NotImplementedException();
        public Task<VitalSignResponse> RecordAsync(int recordedByUserId, RecordVitalSignRequest request)
        {
            return Task.FromResult(new VitalSignResponse
            {
                Id = 1,
                PatientId = request.PatientId,
                RecordedByUserId = recordedByUserId,
                TemperatureCelsius = request.TemperatureCelsius,
                SystolicBloodPressure = request.SystolicBloodPressure,
                DiastolicBloodPressure = request.DiastolicBloodPressure
            });
        }
    }

    #endregion

    #region Helper

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

    #endregion

    #region AuditLogsController Authorization & Endpoint Tests

    [Fact]
    public async Task AuditLogs_PatientRole_AccessingAuditLogs_ReturnsForbid()
    {
        var auditService = new StubAuditService();
        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetAuditLogs(null, null, null, 100);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AuditLogs_PatientRole_AccessingPatientAuditLogs_ReturnsForbid()
    {
        var auditService = new StubAuditService();
        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatient(10);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AuditLogs_PatientRole_AccessingAuditLogById_ReturnsForbid()
    {
        var auditService = new StubAuditService();
        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetById(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AuditLogs_AdminRole_CanViewAuditLogs_ReturnsOk()
    {
        var auditService = new StubAuditService();
        await auditService.LogAsync(20, "Create", "MedicalRecord", 1, 10, "RecordNumber: REC-001");

        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Admin", "1")
        };

        var result = await controller.GetAuditLogs(null, null, null, 100);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var logs = Assert.IsAssignableFrom<List<EmrAuditLogResponse>>(okResult.Value);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AuditLogs_DoctorRole_CanViewAuditLogs_ReturnsOk()
    {
        var auditService = new StubAuditService();
        await auditService.LogAsync(20, "Create", "MedicalRecord", 1, 10);

        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var result = await controller.GetAuditLogs(null, null, null, 100);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var logs = Assert.IsAssignableFrom<List<EmrAuditLogResponse>>(okResult.Value);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AuditLogs_StaffRole_CanViewAuditLogs_ReturnsOk()
    {
        var auditService = new StubAuditService();
        await auditService.LogAsync(30, "RecordVitals", "VitalSign", 1, 10);

        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Staff", "30")
        };

        var result = await controller.GetAuditLogs(null, null, null, 100);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var logs = Assert.IsAssignableFrom<List<EmrAuditLogResponse>>(okResult.Value);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AuditLogs_GetById_ReturnsOk_WhenExists()
    {
        var auditService = new StubAuditService();
        var logged = await auditService.LogAsync(20, "Create", "MedicalRecord", 1, 10);

        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Admin", "1")
        };

        var result = await controller.GetById(logged.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var log = Assert.IsType<EmrAuditLogResponse>(okResult.Value);
        Assert.Equal(logged.Id, log.Id);
    }

    [Fact]
    public async Task AuditLogs_GetById_ReturnsNotFound_WhenDoesNotExist()
    {
        var auditService = new StubAuditService();
        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Admin", "1")
        };

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AuditLogs_GetByPatient_ReturnsOk_WithPatientLogs()
    {
        var auditService = new StubAuditService();
        await auditService.LogAsync(20, "Create", "MedicalRecord", 1, 10);
        await auditService.LogAsync(20, "Create", "MedicalRecord", 2, 20);

        var controller = new AuditLogsController(auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var result = await controller.GetByPatient(10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var logs = Assert.IsAssignableFrom<List<EmrAuditLogResponse>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal(10, logs[0].PatientId);
    }

    #endregion

    #region EMR Operations Audit Integration Tests

    [Fact]
    public async Task MedicalRecordsController_GetById_AuditsViewOperation()
    {
        var auditService = new StubAuditService();
        var controller = new MedicalRecordsController(new StubMedicalRecordService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var result = await controller.GetById(1);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("View", log.Action);
        Assert.Equal("MedicalRecord", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal(20, log.UserId);
        Assert.True(log.IsSuccess);
    }

    [Fact]
    public async Task MedicalRecordsController_GetById_ForbiddenPatient_AuditsFailedAccessAttempt()
    {
        var auditService = new StubAuditService();
        // Record 1 belongs to Patient 10. Patient 99 attempts to access.
        var controller = new MedicalRecordsController(new StubMedicalRecordService(), auditService)
        {
            ControllerContext = CreateUserContext("Patient", "99")
        };

        var result = await controller.GetById(1);

        Assert.IsType<ForbidResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("View", log.Action);
        Assert.Equal("MedicalRecord", log.EntityType);
        Assert.Equal(99, log.UserId);
        Assert.False(log.IsSuccess);
    }

    [Fact]
    public async Task MedicalRecordsController_Create_AuditsCreateOperation()
    {
        var auditService = new StubAuditService();
        var controller = new MedicalRecordsController(new StubMedicalRecordService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Migraine",
            Diagnosis = "Acute Migraine"
        };

        var result = await controller.Create(request);

        Assert.IsType<CreatedResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("Create", log.Action);
        Assert.Equal("MedicalRecord", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal(20, log.UserId);
        Assert.True(log.IsSuccess);
        Assert.Contains("REC-001", log.Metadata);
    }

    [Fact]
    public async Task MedicalRecordsController_Update_AuditsUpdateOperation()
    {
        var auditService = new StubAuditService();
        var controller = new MedicalRecordsController(new StubMedicalRecordService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Updated headache",
            Diagnosis = "Tension headache"
        };

        var result = await controller.Update(1, request);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("Update", log.Action);
        Assert.Equal("MedicalRecord", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(20, log.UserId);
        Assert.True(log.IsSuccess);
    }

    [Fact]
    public async Task PrescriptionsController_Create_AuditsPrescriptionCreation()
    {
        var auditService = new StubAuditService();
        var controller = new PrescriptionsController(new StubPrescriptionService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            Items = new List<CreatePrescriptionItemRequest>
            {
                new() { MedicineName = "Ibuprofen", Dosage = "400mg", Frequency = "Twice daily", DurationDays = 7 }
            }
        };

        var result = await controller.Create(request);

        Assert.IsType<CreatedResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("Create", log.Action);
        Assert.Equal("Prescription", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal(20, log.UserId);
        Assert.True(log.IsSuccess);
        Assert.Contains("RX-001", log.Metadata);
    }

    [Fact]
    public async Task LabOrdersController_CreateOrder_AuditsLabOrderCreation()
    {
        var auditService = new StubAuditService();
        var controller = new LabOrdersController(new StubLabOrderService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var request = new CreateLabOrderRequest
        {
            PatientId = 10,
            TestName = "Full Blood Count",
            Priority = LabOrderPriority.Routine
        };

        var result = await controller.CreateOrder(request);

        Assert.IsType<CreatedResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("Create", log.Action);
        Assert.Equal("LabOrder", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal(20, log.UserId);
        Assert.True(log.IsSuccess);
        Assert.Contains("LAB-001", log.Metadata);
    }

    [Fact]
    public async Task LabOrdersController_RecordReport_AuditsLabReportRecording()
    {
        var auditService = new StubAuditService();
        var controller = new LabOrdersController(new StubLabOrderService(), auditService)
        {
            ControllerContext = CreateUserContext("Staff", "30")
        };

        var request = new RecordLabReportRequest
        {
            ReportDate = DateTime.UtcNow,
            ResultSummary = "Normal hemoglobin",
            Findings = "Hb 14.5 g/dL"
        };

        var result = await controller.RecordReport(1, request);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("RecordReport", log.Action);
        Assert.Equal("LabReport", log.EntityType);
        Assert.Equal(30, log.UserId);
        Assert.True(log.IsSuccess);
    }

    [Fact]
    public async Task PatientProfilesController_Upsert_AuditsProfileUpdate()
    {
        var auditService = new StubAuditService();
        var controller = new PatientProfilesController(new StubProfileService(), auditService)
        {
            ControllerContext = CreateUserContext("Doctor", "20")
        };

        var request = new UpsertPatientMedicalProfileRequest
        {
            BloodGroup = BloodGroup.OPositive,
            Allergies = "None"
        };

        var result = await controller.Upsert(10, request);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("UpdateProfile", log.Action);
        Assert.Equal("PatientMedicalProfile", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal(20, log.UserId);
        Assert.True(log.IsSuccess);
        Assert.Contains("OPositive", log.Metadata);
    }

    [Fact]
    public async Task VitalsController_Record_AuditsVitalsRecording()
    {
        var auditService = new StubAuditService();
        var controller = new VitalsController(new StubVitalsService(), auditService)
        {
            ControllerContext = CreateUserContext("Staff", "30")
        };

        var request = new RecordVitalSignRequest
        {
            PatientId = 10,
            TemperatureCelsius = 37.0m,
            SystolicBloodPressure = 120,
            DiastolicBloodPressure = 80
        };

        var result = await controller.Record(request);

        Assert.IsType<CreatedResult>(result);
        Assert.Single(auditService.Logs);
        var log = auditService.Logs[0];
        Assert.Equal("RecordVitals", log.Action);
        Assert.Equal("VitalSign", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(10, log.PatientId);
        Assert.Equal(30, log.UserId);
        Assert.True(log.IsSuccess);
        Assert.Contains("120/80", log.Metadata);
    }

    #endregion
}
