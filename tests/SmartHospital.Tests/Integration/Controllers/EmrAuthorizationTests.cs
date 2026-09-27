using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class EmrAuthorizationTests
{
    #region Stub Services

    private class StubProfileService : IPatientMedicalProfileService
    {
        public Task<PatientMedicalProfileResponse?> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 999) return Task.FromResult<PatientMedicalProfileResponse?>(null);

            return Task.FromResult<PatientMedicalProfileResponse?>(new PatientMedicalProfileResponse
            {
                Id = 1,
                PatientId = patientId,
                BloodGroup = "O_Positive",
                Allergies = "Penicillin"
            });
        }

        public Task<PatientMedicalProfileResponse> UpsertAsync(int patientId, UpsertPatientMedicalProfileRequest request)
        {
            return Task.FromResult(new PatientMedicalProfileResponse
            {
                Id = 1,
                PatientId = patientId,
                BloodGroup = request.BloodGroup.ToString(),
                Allergies = request.Allergies
            });
        }
    }

    private class StubVitalsService : IVitalSignService
    {
        public Task<List<VitalSignResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<VitalSignResponse>
            {
                new()
                {
                    Id = 1,
                    PatientId = patientId,
                    HeartRateBpm = 75,
                    SystolicBloodPressure = 120,
                    DiastolicBloodPressure = 80
                }
            });
        }

        public Task<VitalSignResponse> RecordAsync(int recordedByUserId, RecordVitalSignRequest request)
        {
            return Task.FromResult(new VitalSignResponse
            {
                Id = 1,
                PatientId = request.PatientId,
                HeartRateBpm = request.HeartRateBpm,
                SystolicBloodPressure = request.SystolicBloodPressure,
                DiastolicBloodPressure = request.DiastolicBloodPressure
            });
        }
    }

    private class StubPrescriptionService : IPrescriptionService
    {
        public Task<List<PrescriptionResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<PrescriptionResponse>
            {
                new() { Id = 1, PrescriptionNumber = "RX-01", PatientId = patientId, DoctorId = 2 }
            });
        }

        public Task<PrescriptionResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<PrescriptionResponse?>(null);
            // Record 1 belongs to Patient 10, Record 2 belongs to Patient 20
            return Task.FromResult<PrescriptionResponse?>(new PrescriptionResponse
            {
                Id = id,
                PrescriptionNumber = $"RX-{id:D3}",
                PatientId = id == 2 ? 20 : 10,
                DoctorId = 5
            });
        }

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
        public Task<List<LabOrderResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<LabOrderResponse>
            {
                new() { Id = 1, OrderNumber = "LAB-01", PatientId = patientId, DoctorId = 5, TestName = "CBC" }
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
                DoctorId = 5,
                TestName = "CBC"
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
            return Task.FromResult<LabOrderResponse?>(new LabOrderResponse { Id = id, Status = request.Status.ToString() });
        }

        public Task<LabOrderResponse?> UpdateStatusAsync(int id, LabOrderStatus newStatus)
        {
            return Task.FromResult<LabOrderResponse?>(new LabOrderResponse { Id = id, Status = newStatus.ToString() });
        }

        public Task<LabReportResponse?> RecordReportAsync(int labOrderId, int conductedByUserId, RecordLabReportRequest request)
        {
            return Task.FromResult<LabReportResponse?>(new LabReportResponse
            {
                Id = 1,
                LabOrderId = labOrderId,
                ConductedByUserId = conductedByUserId,
                ResultSummary = request.ResultSummary
            });
        }
    }

    private class StubMedicalRecordService : IMedicalRecordService
    {
        public Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId)
        {
            return Task.FromResult(new List<MedicalRecordSummaryResponse>
            {
                new() { Id = 1, RecordNumber = "REC-01", PatientId = patientId, DoctorId = 5 }
            });
        }

        public Task<MedicalRecordResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<MedicalRecordResponse?>(null);
            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = id,
                RecordNumber = "REC-01",
                PatientId = id == 2 ? 20 : 10,
                DoctorId = 5,
                ChiefComplaint = "Headache",
                Diagnosis = "Migraine"
            });
        }

        public Task<MedicalRecordResponse?> GetByAppointmentIdAsync(int appointmentId)
        {
            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = 1,
                AppointmentId = appointmentId,
                PatientId = 10,
                DoctorId = 5
            });
        }

        public Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request)
        {
            return Task.FromResult(new MedicalRecordResponse
            {
                Id = 1,
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
                PatientId = 10,
                DoctorId = doctorId,
                ChiefComplaint = request.ChiefComplaint,
                Diagnosis = request.Diagnosis
            });
        }

        public Task<DiagnosisResponse> AddDiagnosisAsync(int medicalRecordId, int doctorId, AddDiagnosisRequest request)
        {
            return Task.FromResult(new DiagnosisResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                PatientId = 10,
                DoctorId = doctorId,
                Description = request.Description,
                Type = request.Type.ToString(),
                Status = request.Status.ToString(),
                Severity = request.Severity.ToString()
            });
        }

        public Task<DiagnosisResponse?> UpdateDiagnosisAsync(int medicalRecordId, int diagnosisId, int doctorId, UpdateDiagnosisRequest request)
        {
            return Task.FromResult<DiagnosisResponse?>(new DiagnosisResponse
            {
                Id = diagnosisId,
                MedicalRecordId = medicalRecordId,
                PatientId = 10,
                DoctorId = doctorId,
                Description = request.Description
            });
        }

        public Task<DiagnosisResponse?> UpdatePrimaryDiagnosisAsync(int medicalRecordId, int doctorId, UpdateDiagnosisRequest request)
        {
            return Task.FromResult<DiagnosisResponse?>(new DiagnosisResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                PatientId = 10,
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
            return Task.FromResult<TreatmentPlanResponse?>(new TreatmentPlanResponse
            {
                Id = treatmentPlanId,
                MedicalRecordId = medicalRecordId,
                PatientId = 10,
                DoctorId = doctorId,
                Title = request.Title,
                Description = request.Description
            });
        }

        public Task<TreatmentPlanResponse?> UpdatePrimaryTreatmentPlanAsync(int medicalRecordId, int doctorId, UpdateTreatmentPlanRequest request)
        {
            return Task.FromResult<TreatmentPlanResponse?>(new TreatmentPlanResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                PatientId = 10,
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
            return Task.FromResult(new List<MedicalRecordVersionResponse>
            {
                new() { Id = 1, MedicalRecordId = medicalRecordId, VersionNumber = 1, ChangeType = "Initial" }
            });
        }

        public Task<MedicalRecordVersionResponse?> GetVersionAsync(int medicalRecordId, int versionNumber)
        {
            return Task.FromResult<MedicalRecordVersionResponse?>(new MedicalRecordVersionResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                VersionNumber = versionNumber,
                ChangeType = "Initial"
            });
        }

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

    private class StubMedicalHistoryService : IMedicalHistoryService
    {
        public Task<PatientMedicalTimelineResponse> GetPatientTimelineAsync(int patientId)
        {
            return Task.FromResult(new PatientMedicalTimelineResponse
            {
                PatientId = patientId,
                PatientName = "Test Patient",
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

    #endregion

    #region Patient Profile Authorization Tests

    [Fact]
    public async Task PatientProfile_PatientCannotAccessOtherPatientProfile_ReturnsForbid()
    {
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        // Attempting to access Patient 20's profile
        var result = await controller.GetByPatientId(20);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PatientProfile_PatientCanAccessOwnProfile_ReturnsOk()
    {
        var controller = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatientId(10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<PatientMedicalProfileResponse>(okResult.Value);
        Assert.Equal(10, profile.PatientId);
    }

    [Fact]
    public async Task PatientProfile_StaffAndDoctorCanAccessAnyPatientProfile_ReturnsOk()
    {
        var staffController = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Staff", "50")
        };
        var doctorController = new PatientProfilesController(new StubProfileService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };

        var staffResult = await staffController.GetByPatientId(10);
        var doctorResult = await doctorController.GetByPatientId(10);

        Assert.IsType<OkObjectResult>(staffResult);
        Assert.IsType<OkObjectResult>(doctorResult);
    }

    #endregion

    #region Vitals Authorization Tests

    [Fact]
    public async Task Vitals_PatientCannotAccessOtherPatientVitals_ReturnsForbid()
    {
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatient(20);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Vitals_PatientCanAccessOwnVitals_ReturnsOk()
    {
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatient(10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var vitals = Assert.IsType<List<VitalSignResponse>>(okResult.Value);
        Assert.Single(vitals);
    }

    [Fact]
    public async Task Vitals_StaffCanAccessPatientVitals_ReturnsOk()
    {
        var controller = new VitalsController(new StubVitalsService())
        {
            ControllerContext = CreateUserContext("Staff", "50")
        };

        var result = await controller.GetByPatient(10);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Prescriptions Authorization Tests

    [Fact]
    public async Task Prescriptions_PatientCannotAccessOtherPatientPrescriptions_ReturnsForbid()
    {
        var controller = new PrescriptionsController(new StubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatient(20);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Prescriptions_PatientCannotAccessOtherPatientPrescriptionById_ReturnsForbid()
    {
        var controller = new PrescriptionsController(new StubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        // Prescription ID 2 belongs to Patient 20
        var result = await controller.GetById(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Prescriptions_PatientCanAccessOwnPrescription_ReturnsOk()
    {
        var controller = new PrescriptionsController(new StubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        // Prescription ID 1 belongs to Patient 10
        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rx = Assert.IsType<PrescriptionResponse>(okResult.Value);
        Assert.Equal(10, rx.PatientId);
    }

    [Fact]
    public async Task Prescriptions_StaffAndDoctorCanAccessPrescriptions_ReturnsOk()
    {
        var staffController = new PrescriptionsController(new StubPrescriptionService())
        {
            ControllerContext = CreateUserContext("Staff", "50")
        };

        var result = await staffController.GetById(2);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Lab Orders Authorization Tests

    [Fact]
    public async Task LabOrders_PatientCannotAccessOtherPatientOrders_ReturnsForbid()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatient(20);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task LabOrders_PatientCannotAccessOtherPatientOrderById_ReturnsForbid()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        // Order 2 belongs to Patient 20
        var result = await controller.GetById(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task LabOrders_PatientCanAccessOwnOrder_ReturnsOk()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        // Order 1 belongs to Patient 10
        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var order = Assert.IsType<LabOrderResponse>(okResult.Value);
        Assert.Equal(10, order.PatientId);
    }

    [Fact]
    public async Task LabOrders_StaffCanUpdateLabOrderStatus_ReturnsOk()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("Staff", "50")
        };

        var result = await controller.UpdateStatus(1, new UpdateLabOrderStatusRequest
        {
            Status = LabOrderStatus.InProgress
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task LabOrders_StaffCanRecordLabReport_ReturnsOk()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("Staff", "50")
        };

        var result = await controller.RecordReport(1, new RecordLabReportRequest
        {
            ResultSummary = "Normal hemoglobin levels"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Medical Records & History Authorization Tests

    [Fact]
    public async Task MedicalRecords_PatientCannotAccessOtherPatientRecords_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new StubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetByPatient(20);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MedicalRecords_PatientCannotAccessOtherPatientRecordById_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new StubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10") // Patient 10
        };

        // Record 2 belongs to Patient 20
        var result = await controller.GetById(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MedicalRecords_PatientCannotAccessOtherPatientDiagnoses_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new StubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Record 2 belongs to Patient 20
        var result = await controller.GetDiagnoses(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MedicalRecords_PatientCannotAccessOtherPatientTreatmentPlans_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new StubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Record 2 belongs to Patient 20
        var result = await controller.GetTreatmentPlans(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MedicalRecords_PatientCannotAccessOtherPatientVersionHistory_ReturnsForbid()
    {
        var controller = new MedicalRecordsController(new StubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        // Record 2 belongs to Patient 20
        var result = await controller.GetVersionHistory(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MedicalHistory_PatientCannotAccessOtherPatientTimeline_ReturnsForbid()
    {
        var controller = new MedicalHistoryController(new StubMedicalHistoryService())
        {
            ControllerContext = CreateUserContext("Patient", "10")
        };

        var result = await controller.GetPatientTimeline(20);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MedicalHistory_DoctorAndStaffCanAccessPatientTimeline_ReturnsOk()
    {
        var docController = new MedicalHistoryController(new StubMedicalHistoryService())
        {
            ControllerContext = CreateUserContext("Doctor", "5")
        };
        var staffController = new MedicalHistoryController(new StubMedicalHistoryService())
        {
            ControllerContext = CreateUserContext("Staff", "50")
        };

        var docResult = await docController.GetPatientTimeline(10);
        var staffResult = await staffController.GetPatientTimeline(10);

        Assert.IsType<OkObjectResult>(docResult);
        Assert.IsType<OkObjectResult>(staffResult);
    }

    [Fact]
    public async Task Admin_RetainsFullAccess_CanAccessAnyPatientMedicalRecord()
    {
        var controller = new MedicalRecordsController(new StubMedicalRecordService())
        {
            ControllerContext = CreateUserContext("Admin", "99")
        };

        var result = await controller.GetById(2);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var record = Assert.IsType<MedicalRecordResponse>(okResult.Value);
        Assert.Equal(2, record.Id);
    }

    #endregion
}
