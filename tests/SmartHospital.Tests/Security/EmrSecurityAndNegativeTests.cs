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

namespace SmartHospital.Tests.Security;

/// <summary>
/// Focused security, authorization, and negative-test suite verifying:
/// 1. Unauthenticated users cannot access protected EMR APIs
/// 2. Patients cannot access another patient's records (ownership isolation)
/// 3. Doctors cannot modify records they are not authorized to modify
/// 4. Staff cannot perform doctor-only clinical operations
/// 5. Invalid patient IDs are rejected safely
/// 6. Invalid record IDs are handled correctly
/// 7. Invalid prescription requests are rejected
/// 8. Invalid lab workflows and illegal state transitions are rejected
/// 9. Audit and version history cannot be modified or tampered with
/// </summary>
public class EmrSecurityAndNegativeTests
{
    #region Stub Services for Security Testing

    private class SecurityStubMedicalRecordService : IMedicalRecordService
    {
        public Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 999) throw new InvalidOperationException("Patient not found.");
            return Task.FromResult(new List<MedicalRecordSummaryResponse>
            {
                new() { Id = 1, PatientId = patientId, RecordNumber = "REC-01", DoctorId = 10 }
            });
        }

        public Task<MedicalRecordResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<MedicalRecordResponse?>(null);
            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = id,
                RecordNumber = $"REC-{id:D3}",
                PatientId = id == 2 ? 20 : 10, // Record 1 -> Patient 10, Record 2 -> Patient 20
                DoctorId = 10,
                ChiefComplaint = "Headache",
                Diagnosis = "Tension headache"
            });
        }

        public Task<MedicalRecordResponse?> GetByAppointmentIdAsync(int appointmentId)
        {
            if (appointmentId == 999) return Task.FromResult<MedicalRecordResponse?>(null);
            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = 1,
                RecordNumber = "REC-001",
                PatientId = appointmentId == 200 ? 20 : 10,
                DoctorId = 10,
                AppointmentId = appointmentId
            });
        }

        public Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request)
        {
            return Task.FromResult(new MedicalRecordResponse
            {
                Id = 1,
                RecordNumber = "REC-NEW",
                PatientId = request.PatientId,
                DoctorId = doctorId
            });
        }

        public Task<MedicalRecordResponse?> UpdateAsync(int id, int doctorId, UpdateMedicalRecordRequest request)
        {
            // Record 1 authored by Doctor 10
            if (doctorId != 10) return Task.FromResult<MedicalRecordResponse?>(null);

            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = id,
                PatientId = 10,
                DoctorId = doctorId,
                ChiefComplaint = request.ChiefComplaint,
                Diagnosis = request.Diagnosis
            });
        }

        public Task<DiagnosisResponse> AddDiagnosisAsync(int medicalRecordId, int doctorId, AddDiagnosisRequest request)
        {
            if (doctorId != 10) throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
            return Task.FromResult(new DiagnosisResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                PatientId = 10,
                DoctorId = doctorId,
                Description = request.Description
            });
        }

        public Task<DiagnosisResponse?> UpdateDiagnosisAsync(int medicalRecordId, int diagnosisId, int doctorId, UpdateDiagnosisRequest request)
        {
            if (doctorId != 10) throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
            return Task.FromResult<DiagnosisResponse?>(new DiagnosisResponse
            {
                Id = diagnosisId,
                MedicalRecordId = medicalRecordId,
                DoctorId = doctorId,
                Description = request.Description
            });
        }

        public Task<DiagnosisResponse?> UpdatePrimaryDiagnosisAsync(int medicalRecordId, int doctorId, UpdateDiagnosisRequest request)
        {
            if (doctorId != 10) throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
            return Task.FromResult<DiagnosisResponse?>(new DiagnosisResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                DoctorId = doctorId,
                Description = request.Description
            });
        }

        public Task<List<DiagnosisResponse>> GetDiagnosesAsync(int medicalRecordId)
        {
            return Task.FromResult(new List<DiagnosisResponse>
            {
                new() { Id = 1, MedicalRecordId = medicalRecordId, PatientId = 10, Description = "Diagnosis" }
            });
        }

        public Task<TreatmentPlanResponse> RecordTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request)
        {
            if (doctorId != 10) throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
            return Task.FromResult(new TreatmentPlanResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                PatientId = 10,
                DoctorId = doctorId,
                Title = request.Title,
                Description = request.Description
            });
        }

        public Task<TreatmentPlanResponse?> UpdateTreatmentPlanAsync(int medicalRecordId, int treatmentPlanId, int doctorId, UpdateTreatmentPlanRequest request)
        {
            if (doctorId != 10) throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
            return Task.FromResult<TreatmentPlanResponse?>(new TreatmentPlanResponse
            {
                Id = treatmentPlanId,
                MedicalRecordId = medicalRecordId,
                DoctorId = doctorId,
                Title = request.Title,
                Description = request.Description
            });
        }

        public Task<TreatmentPlanResponse?> UpdatePrimaryTreatmentPlanAsync(int medicalRecordId, int doctorId, UpdateTreatmentPlanRequest request)
        {
            if (doctorId != 10) throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
            return Task.FromResult<TreatmentPlanResponse?>(new TreatmentPlanResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                DoctorId = doctorId,
                Title = request.Title,
                Description = request.Description
            });
        }

        public Task<List<TreatmentPlanResponse>> GetTreatmentPlansAsync(int medicalRecordId)
        {
            return Task.FromResult(new List<TreatmentPlanResponse>
            {
                new() { Id = 1, MedicalRecordId = medicalRecordId, PatientId = 10, Title = "Plan" }
            });
        }

        public Task<List<MedicalRecordVersionResponse>> GetVersionHistoryAsync(int medicalRecordId)
        {
            if (medicalRecordId == 999) throw new InvalidOperationException("Medical record not found.");
            return Task.FromResult(new List<MedicalRecordVersionResponse>
            {
                new() { Id = 1, MedicalRecordId = medicalRecordId, VersionNumber = 1, ChangeSummary = "Initial" }
            });
        }

        public Task<MedicalRecordVersionResponse?> GetVersionAsync(int medicalRecordId, int versionNumber)
        {
            if (versionNumber == 999) return Task.FromResult<MedicalRecordVersionResponse?>(null);
            return Task.FromResult<MedicalRecordVersionResponse?>(new MedicalRecordVersionResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                VersionNumber = versionNumber
            });
        }

        public Task<PagedMedicalRecordResult> SearchAsync(MedicalRecordQueryFilter filter)
        {
            return Task.FromResult(new PagedMedicalRecordResult
            {
                Items = new List<MedicalRecordSummaryResponse>(),
                TotalCount = 0,
                Page = filter?.Page ?? 1,
                PageSize = filter?.PageSize ?? 20
            });
        }
    }

    private class SecurityStubPrescriptionService : IPrescriptionService
    {
        public Task<List<PrescriptionResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<PrescriptionResponse>
            {
                new() { Id = 1, PrescriptionNumber = "RX-01", PatientId = patientId, DoctorId = 10 }
            });
        }

        public Task<PrescriptionResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<PrescriptionResponse?>(null);
            return Task.FromResult<PrescriptionResponse?>(new PrescriptionResponse
            {
                Id = id,
                PrescriptionNumber = $"RX-{id:D3}",
                PatientId = id == 2 ? 20 : 10, // Prescription 1 -> Patient 10, Prescription 2 -> Patient 20
                DoctorId = 10
            });
        }

        public Task<PrescriptionResponse> CreateAsync(int doctorId, CreatePrescriptionRequest request)
        {
            if (request.MedicalRecordId == 100) // Medical record belongs to another patient
            {
                throw new InvalidOperationException("Medical record does not belong to the specified patient.");
            }

            return Task.FromResult(new PrescriptionResponse
            {
                Id = 1,
                PrescriptionNumber = "RX-001",
                PatientId = request.PatientId,
                DoctorId = doctorId,
                MedicalRecordId = request.MedicalRecordId
            });
        }
    }

    private class SecurityStubLabOrderService : ILabOrderService
    {
        public Task<List<LabOrderResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<LabOrderResponse>
            {
                new() { Id = 1, OrderNumber = "LAB-01", PatientId = patientId, DoctorId = 10, TestName = "CBC" }
            });
        }

        public Task<LabOrderResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<LabOrderResponse?>(null);
            return Task.FromResult<LabOrderResponse?>(new LabOrderResponse
            {
                Id = id,
                OrderNumber = $"LAB-{id:D3}",
                PatientId = id == 2 ? 20 : 10,
                DoctorId = 10,
                TestName = "CBC",
                Status = "Ordered"
            });
        }

        public Task<LabOrderResponse> CreateOrderAsync(int doctorId, CreateLabOrderRequest request)
        {
            return Task.FromResult(new LabOrderResponse
            {
                Id = 1,
                OrderNumber = "LAB-001",
                PatientId = request.PatientId,
                DoctorId = doctorId,
                TestName = request.TestName
            });
        }

        public Task<LabOrderResponse?> UpdateStatusAsync(int id, UpdateLabOrderStatusRequest request)
        {
            return UpdateStatusAsync(id, request.Status);
        }

        public Task<LabOrderResponse?> UpdateStatusAsync(int id, LabOrderStatus newStatus)
        {
            if (id == 999) return Task.FromResult<LabOrderResponse?>(null);

            // Illegal transitions
            if (id == 888 && newStatus == LabOrderStatus.Completed)
            {
                throw new InvalidOperationException("Cannot mark lab order as completed without a lab report.");
            }

            if (id == 777) // Already completed order
            {
                throw new InvalidOperationException("Cannot modify status of a completed lab order.");
            }

            return Task.FromResult<LabOrderResponse?>(new LabOrderResponse { Id = id, Status = newStatus.ToString() });
        }

        public Task<LabReportResponse?> RecordReportAsync(int labOrderId, int conductedByUserId, RecordLabReportRequest request)
        {
            if (labOrderId == 999) return Task.FromResult<LabReportResponse?>(null);

            if (labOrderId == 777) // Already completed order
            {
                throw new InvalidOperationException("Cannot add a report to an order that is already completed.");
            }

            if (labOrderId == 666) // Cancelled order
            {
                throw new InvalidOperationException("Cannot add a report to a cancelled lab order.");
            }

            return Task.FromResult<LabReportResponse?>(new LabReportResponse
            {
                Id = 1,
                LabOrderId = labOrderId,
                ConductedByUserId = conductedByUserId,
                ResultSummary = request.ResultSummary
            });
        }
    }

    private class SecurityStubAuditService : IEmrAuditService
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

    private class SecurityStubProfileService : IPatientMedicalProfileService
    {
        public Task<PatientMedicalProfileResponse?> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 999) return Task.FromResult<PatientMedicalProfileResponse?>(null);
            return Task.FromResult<PatientMedicalProfileResponse?>(new PatientMedicalProfileResponse
            {
                Id = 1,
                PatientId = patientId,
                BloodGroup = "OPositive"
            });
        }

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

    private class SecurityStubVitalsService : IVitalSignService
    {
        public Task<List<VitalSignResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<VitalSignResponse>
            {
                new() { Id = 1, PatientId = patientId, HeartRateBpm = 75 }
            });
        }

        public Task<VitalSignResponse> RecordAsync(int recordedByUserId, RecordVitalSignRequest request)
        {
            return Task.FromResult(new VitalSignResponse
            {
                Id = 1,
                PatientId = request.PatientId,
                RecordedByUserId = recordedByUserId
            });
        }
    }

    private class SecurityStubMedicalHistoryService : IMedicalHistoryService
    {
        public Task<PatientMedicalTimelineResponse> GetPatientTimelineAsync(int patientId)
        {
            return Task.FromResult(new PatientMedicalTimelineResponse
            {
                PatientId = patientId,
                TotalEvents = 1,
                Events = new List<MedicalTimelineEventResponse>()
            });
        }
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

    #region 1. Unauthenticated Users Cannot Access Protected EMR APIs

    [Theory]
    [InlineData(typeof(MedicalRecordsController))]
    [InlineData(typeof(PatientProfilesController))]
    [InlineData(typeof(VitalsController))]
    [InlineData(typeof(PrescriptionsController))]
    [InlineData(typeof(LabOrdersController))]
    [InlineData(typeof(MedicalHistoryController))]
    [InlineData(typeof(AuditLogsController))]
    public void AllEmrControllers_MustHaveAuthorizeAttribute(Type controllerType)
    {
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.True(authAttr != null, $"{controllerType.Name} is missing the [Authorize] attribute!");
    }

    [Fact]
    public async Task MedicalRecords_Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 10,
            ChiefComplaint = "Fever",
            Diagnosis = "Viral infection"
        };

        var result = await controller.Create(request);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Prescriptions_Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        var controller = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>
            {
                new() { MedicineName = "Paracetamol", Dosage = "500mg", Frequency = "TDS" }
            }
        };

        var result = await controller.Create(request);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task LabOrders_CreateOrder_WithoutAuthentication_ReturnsUnauthorized()
    {
        var controller = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var request = new CreateLabOrderRequest { PatientId = 10, TestName = "CBC" };
        var result = await controller.CreateOrder(request);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Vitals_Record_WithoutAuthentication_ReturnsUnauthorized()
    {
        var controller = new VitalsController(new SecurityStubVitalsService())
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var request = new RecordVitalSignRequest { PatientId = 10, HeartRateBpm = 75 };
        var result = await controller.Record(request);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task LabOrders_RecordReport_WithoutAuthentication_ReturnsUnauthorized()
    {
        var controller = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var request = new RecordLabReportRequest { ResultSummary = "Normal" };
        var result = await controller.RecordReport(1, request);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region 2. Patients Cannot Access Another Patient's Records

    [Fact]
    public async Task Patient_CannotAccessOtherPatientMedicalRecordList_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        var result = await controller.GetByPatient(20); // Patient 20
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientMedicalRecordById_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetById(2); // Record 2 belongs to Patient 20
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientAppointmentRecord_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByAppointment(200); // Appointment 200 belongs to Patient 20
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientDiagnoses_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetDiagnoses(2); // Record 2 belongs to Patient 20
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientTreatmentPlans_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetTreatmentPlans(2);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientVersionHistory_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetVersionHistory(2);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientMedicalProfile_ReturnsForbid()
    {
        var controller = new PatientProfilesController(new SecurityStubProfileService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatientId(20);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientVitals_ReturnsForbid()
    {
        var controller = new VitalsController(new SecurityStubVitalsService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatient(20);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientPrescriptions_ReturnsForbid()
    {
        var controller = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetById(2); // Prescription 2 belongs to Patient 20
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientLabOrders_ReturnsForbid()
    {
        var controller = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetById(2); // Order 2 belongs to Patient 20
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessOtherPatientMedicalHistoryTimeline_ReturnsForbid()
    {
        var controller = new MedicalHistoryController(new SecurityStubMedicalHistoryService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetPatientTimeline(20);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patient_CannotAccessAuditLogs_ReturnsForbid()
    {
        var controller = new AuditLogsController(new SecurityStubAuditService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetAuditLogs(patientId: 10, entityType: null, userId: null);
        Assert.IsType<ForbidResult>(result);
    }

    #endregion

    #region 3. Doctors Cannot Modify Records They Are Not Authorized to Modify

    [Fact]
    public async Task Doctor_CannotModifyRecordAuthoredByAnotherDoctor_ReturnsNotFound()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Doctor", "99") // Doctor 99 (Record 1 was authored by Doctor 10)
        };

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Altered complaint",
            Diagnosis = "Altered diagnosis"
        };

        var result = await controller.Update(1, request);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Doctor_CannotAddDiagnosisToRecordAuthoredByAnotherDoctor_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Doctor", "99") // Doctor 99
        };

        var request = new AddDiagnosisRequest
        {
            Code = "I10",
            Description = "Hypertension",
            Type = DiagnosisType.Secondary
        };

        var result = await controller.AddDiagnosis(1, request);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Doctor_CannotRecordTreatmentPlanForRecordAuthoredByAnotherDoctor_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Doctor", "99") // Doctor 99
        };

        var request = new RecordTreatmentPlanRequest
        {
            Title = "Unauthorized Protocol",
            Description = "Unauthorized Description"
        };

        var result = await controller.RecordTreatmentPlan(1, request);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Doctor_CannotPrescribeAgainstRecordBelongingToDifferentPatient_ReturnsBadRequest()
    {
        var controller = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Doctor", "99")
        };

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            MedicalRecordId = 100, // Record 100 authored by another doctor
            Items = new List<CreatePrescriptionItemRequest>
            {
                new() { MedicineName = "Amoxicillin", Dosage = "500mg", Frequency = "TDS" }
            }
        };

        var result = await controller.Create(request);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    #endregion

    #region 4. Staff Cannot Perform Doctor-Only Operations

    [Theory]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.Create))]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.Update))]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.AddDiagnosis))]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.UpdateDiagnosis))]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.UpdatePrimaryDiagnosis))]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.RecordTreatmentPlan))]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.UpdateTreatmentPlan))]
    [InlineData(typeof(MedicalRecordsController), nameof(MedicalRecordsController.UpdatePrimaryTreatmentPlan))]
    [InlineData(typeof(PrescriptionsController), nameof(PrescriptionsController.Create))]
    [InlineData(typeof(LabOrdersController), nameof(LabOrdersController.CreateOrder))]
    public void DoctorOnlyActions_MustRestrictRolesExcludingStaff(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.False(string.IsNullOrWhiteSpace(authAttr.Roles));

        var roles = authAttr.Roles.Split(',').Select(r => r.Trim()).ToList();
        Assert.Contains("Doctor", roles);
        Assert.DoesNotContain("Staff", roles);
        Assert.DoesNotContain("Patient", roles);
    }

    #endregion

    #region 5. Invalid Patient IDs Are Rejected Safely

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task InvalidPatientId_IsRejectedWithBadRequest_AcrossAllPatientEndpoints(int invalidPatientId)
    {
        var medRecordController = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var profileController = new PatientProfilesController(new SecurityStubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var vitalsController = new VitalsController(new SecurityStubVitalsService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var rxController = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var labController = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var historyController = new MedicalHistoryController(new SecurityStubMedicalHistoryService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var auditController = new AuditLogsController(new SecurityStubAuditService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };

        Assert.IsType<BadRequestObjectResult>(await medRecordController.GetByPatient(invalidPatientId));
        Assert.IsType<BadRequestObjectResult>(await profileController.GetByPatientId(invalidPatientId));
        Assert.IsType<BadRequestObjectResult>(await profileController.Upsert(invalidPatientId, new UpsertPatientMedicalProfileRequest()));
        Assert.IsType<BadRequestObjectResult>(await vitalsController.GetByPatient(invalidPatientId));
        Assert.IsType<BadRequestObjectResult>(await rxController.GetByPatient(invalidPatientId));
        Assert.IsType<BadRequestObjectResult>(await labController.GetByPatient(invalidPatientId));
        Assert.IsType<BadRequestObjectResult>(await historyController.GetPatientTimeline(invalidPatientId));
        Assert.IsType<BadRequestObjectResult>(await auditController.GetByPatient(invalidPatientId));
    }

    #endregion

    #region 6. Invalid Record IDs Are Handled Correctly

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task InvalidRecordId_ReturnsBadRequest(int invalidId)
    {
        var medRecordController = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var rxController = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var labController = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var auditController = new AuditLogsController(new SecurityStubAuditService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };

        Assert.IsType<BadRequestObjectResult>(await medRecordController.GetById(invalidId));
        Assert.IsType<BadRequestObjectResult>(await medRecordController.GetByAppointment(invalidId));
        Assert.IsType<BadRequestObjectResult>(await medRecordController.GetVersionHistory(invalidId));
        Assert.IsType<BadRequestObjectResult>(await medRecordController.GetVersion(invalidId, 1));
        Assert.IsType<BadRequestObjectResult>(await medRecordController.GetDiagnoses(invalidId));
        Assert.IsType<BadRequestObjectResult>(await medRecordController.GetTreatmentPlans(invalidId));
        Assert.IsType<BadRequestObjectResult>(await rxController.GetById(invalidId));
        Assert.IsType<BadRequestObjectResult>(await labController.GetById(invalidId));
        Assert.IsType<BadRequestObjectResult>(await auditController.GetById(invalidId));
    }

    [Fact]
    public async Task NonExistentRecordId_ReturnsNotFound()
    {
        var medRecordController = new MedicalRecordsController(new SecurityStubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var rxController = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var labController = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        var auditController = new AuditLogsController(new SecurityStubAuditService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };

        Assert.IsType<NotFoundObjectResult>(await medRecordController.GetById(999));
        Assert.IsType<NotFoundObjectResult>(await medRecordController.GetByAppointment(999));
        Assert.IsType<NotFoundObjectResult>(await medRecordController.GetVersionHistory(999));
        Assert.IsType<NotFoundObjectResult>(await medRecordController.GetVersion(1, 999));
        Assert.IsType<NotFoundObjectResult>(await rxController.GetById(999));
        Assert.IsType<NotFoundObjectResult>(await labController.GetById(999));
        Assert.IsType<NotFoundObjectResult>(await auditController.GetById(999));
    }

    #endregion

    #region 7. Invalid Prescription Requests Are Rejected

    [Fact]
    public async Task Prescription_WithEmptyItems_ReturnsBadRequest()
    {
        var controller = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };
        controller.ModelState.AddModelError("Items", "At least one prescription item is required.");

        var request = new CreatePrescriptionRequest
        {
            PatientId = 10,
            Items = new List<CreatePrescriptionItemRequest>()
        };

        var result = await controller.Create(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Prescription_WithNullRequest_ReturnsBadRequest()
    {
        var controller = new PrescriptionsController(new SecurityStubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Doctor", "10")
        };

        var result = await controller.Create(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region 8. Invalid Lab Workflows Are Rejected

    [Fact]
    public async Task LabWorkflow_CompletingOrderWithoutReport_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Staff", "20")
        };

        // Order 888 throws when completed without report
        var request = new UpdateLabOrderStatusRequest { Status = LabOrderStatus.Completed };
        var result = await controller.UpdateStatus(888, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task LabWorkflow_ModifyingCompletedOrder_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Staff", "20")
        };

        // Order 777 is already completed
        var request = new UpdateLabOrderStatusRequest { Status = LabOrderStatus.Cancelled };
        var result = await controller.UpdateStatus(777, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task LabWorkflow_RecordingReportOnCompletedOrder_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Staff", "20")
        };

        // Order 777 is already completed
        var request = new RecordLabReportRequest { ResultSummary = "Duplicate report" };
        var result = await controller.RecordReport(777, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task LabWorkflow_RecordingReportOnCancelledOrder_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new SecurityStubLabOrderService())
        {
            ControllerContext = CreateUserContext("Staff", "20")
        };

        // Order 666 is cancelled
        var request = new RecordLabReportRequest { ResultSummary = "Report for cancelled" };
        var result = await controller.RecordReport(666, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    #endregion

    #region 9. Audit and Version History Cannot Be Modified by Unauthorized Users

    [Fact]
    public void AuditLogsController_ExposesNoModifyingEndpoints()
    {
        // Assert that AuditLogsController only contains GET endpoints (no POST, PUT, PATCH, DELETE)
        var methods = typeof(AuditLogsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            Assert.Empty(method.GetCustomAttributes<HttpPostAttribute>());
            Assert.Empty(method.GetCustomAttributes<HttpPutAttribute>());
            Assert.Empty(method.GetCustomAttributes<HttpPatchAttribute>());
            Assert.Empty(method.GetCustomAttributes<HttpDeleteAttribute>());
            Assert.NotEmpty(method.GetCustomAttributes<HttpGetAttribute>());
        }
    }

    [Fact]
    public void MedicalRecordVersionHistory_ExposesNoDirectModifyingEndpoints()
    {
        // Assert that MedicalRecordsController only has GET endpoints for versions/history
        var methods = typeof(MedicalRecordsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var versionMethods = methods.Where(m => m.Name.Contains("Version") || m.Name.Contains("History")).ToList();
        Assert.NotEmpty(versionMethods);

        foreach (var method in versionMethods)
        {
            Assert.Empty(method.GetCustomAttributes<HttpPostAttribute>());
            Assert.Empty(method.GetCustomAttributes<HttpPutAttribute>());
            Assert.Empty(method.GetCustomAttributes<HttpPatchAttribute>());
            Assert.Empty(method.GetCustomAttributes<HttpDeleteAttribute>());
            Assert.NotEmpty(method.GetCustomAttributes<HttpGetAttribute>());
        }
    }

    [Fact]
    public void AuditLogsController_IsRestrictedToAdminDoctorStaff()
    {
        var authAttr = typeof(AuditLogsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Admin,Doctor,Staff", authAttr.Roles);
    }

    #endregion
}
