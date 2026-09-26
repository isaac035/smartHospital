using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class MedicalRecordsControllerTests
{
    private class StubMedicalRecordService : IMedicalRecordService
    {
        public Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 999)
            {
                throw new InvalidOperationException("Patient with ID 999 was not found.");
            }

            return Task.FromResult(new List<MedicalRecordSummaryResponse>
            {
                new()
                {
                    Id = 1,
                    RecordNumber = "REC-001",
                    PatientId = patientId,
                    PatientName = "Test Patient",
                    DoctorId = 2,
                    DoctorName = "Dr. Smith",
                    VisitDate = DateTime.UtcNow,
                    ChiefComplaint = "Fever",
                    Diagnosis = "Common Cold"
                }
            });
        }

        public Task<MedicalRecordResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<MedicalRecordResponse?>(null);

            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = id,
                RecordNumber = "REC-001",
                PatientId = 1,
                DoctorId = 2,
                VisitDate = DateTime.UtcNow,
                ChiefComplaint = "Fever",
                Diagnosis = "Common Cold"
            });
        }

        public Task<MedicalRecordResponse?> GetByAppointmentIdAsync(int appointmentId)
        {
            if (appointmentId == 999) return Task.FromResult<MedicalRecordResponse?>(null);

            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = 1,
                RecordNumber = "REC-001",
                PatientId = 1,
                DoctorId = 2,
                AppointmentId = appointmentId,
                VisitDate = DateTime.UtcNow,
                ChiefComplaint = "Fever",
                Diagnosis = "Common Cold"
            });
        }

        public Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request)
        {
            if (request.PatientId == 999)
            {
                throw new InvalidOperationException("Active patient not found.");
            }

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
            if (id == 888)
            {
                throw new InvalidOperationException("Appointment does not belong to the specified patient.");
            }

            if (id == 999)
            {
                return Task.FromResult<MedicalRecordResponse?>(null);
            }

            return Task.FromResult<MedicalRecordResponse?>(new MedicalRecordResponse
            {
                Id = id,
                DoctorId = doctorId,
                ChiefComplaint = request.ChiefComplaint,
                Diagnosis = request.Diagnosis
            });
        }

        public Task<DiagnosisResponse> AddDiagnosisAsync(int medicalRecordId, int doctorId, AddDiagnosisRequest request)
        {
            if (medicalRecordId == 999) throw new InvalidOperationException("Medical record not found.");
            if (doctorId == 777) throw new UnauthorizedAccessException("Unauthorized doctor.");

            return Task.FromResult(new DiagnosisResponse
            {
                Id = 10,
                MedicalRecordId = medicalRecordId,
                PatientId = 1,
                DoctorId = doctorId,
                Description = request.Description,
                Type = request.Type.ToString(),
                Status = request.Status.ToString(),
                Severity = request.Severity.ToString(),
                DiagnosedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<DiagnosisResponse?> UpdateDiagnosisAsync(int medicalRecordId, int diagnosisId, int doctorId, UpdateDiagnosisRequest request)
        {
            if (medicalRecordId == 999 || diagnosisId == 999) return Task.FromResult<DiagnosisResponse?>(null);
            if (doctorId == 777) throw new UnauthorizedAccessException("Unauthorized doctor.");

            return Task.FromResult<DiagnosisResponse?>(new DiagnosisResponse
            {
                Id = diagnosisId,
                MedicalRecordId = medicalRecordId,
                PatientId = 1,
                DoctorId = doctorId,
                Description = request.Description,
                Type = request.Type.ToString(),
                Status = request.Status.ToString(),
                Severity = request.Severity.ToString(),
                DiagnosedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<DiagnosisResponse?> UpdatePrimaryDiagnosisAsync(int medicalRecordId, int doctorId, UpdateDiagnosisRequest request)
        {
            if (medicalRecordId == 999) return Task.FromResult<DiagnosisResponse?>(null);
            if (doctorId == 777) throw new UnauthorizedAccessException("Unauthorized doctor.");

            return Task.FromResult<DiagnosisResponse?>(new DiagnosisResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                PatientId = 1,
                DoctorId = doctorId,
                Description = request.Description,
                Type = DiagnosisType.Primary.ToString(),
                Status = request.Status.ToString(),
                Severity = request.Severity.ToString(),
                DiagnosedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<List<DiagnosisResponse>> GetDiagnosesAsync(int medicalRecordId)
        {
            if (medicalRecordId == 999) throw new InvalidOperationException("Medical record not found.");

            return Task.FromResult(new List<DiagnosisResponse>
            {
                new()
                {
                    Id = 1,
                    MedicalRecordId = medicalRecordId,
                    PatientId = 1,
                    DoctorId = 2,
                    Description = "Primary Diagnosis",
                    Type = "Primary",
                    Status = "Active",
                    Severity = "Moderate",
                    DiagnosedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            });
        }

        public Task<TreatmentPlanResponse> RecordTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request)
        {
            if (medicalRecordId == 999) throw new InvalidOperationException("Medical record not found.");
            if (doctorId == 777) throw new UnauthorizedAccessException("Unauthorized doctor.");

            return Task.FromResult(new TreatmentPlanResponse
            {
                Id = 20,
                MedicalRecordId = medicalRecordId,
                PatientId = 1,
                DoctorId = doctorId,
                Title = request.Title,
                Category = request.Category.ToString(),
                Description = request.Description,
                Status = request.Status.ToString(),
                StartDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<TreatmentPlanResponse?> UpdateTreatmentPlanAsync(int medicalRecordId, int treatmentPlanId, int doctorId, UpdateTreatmentPlanRequest request)
        {
            if (medicalRecordId == 999 || treatmentPlanId == 999) return Task.FromResult<TreatmentPlanResponse?>(null);
            if (doctorId == 777) throw new UnauthorizedAccessException("Unauthorized doctor.");

            return Task.FromResult<TreatmentPlanResponse?>(new TreatmentPlanResponse
            {
                Id = treatmentPlanId,
                MedicalRecordId = medicalRecordId,
                PatientId = 1,
                DoctorId = doctorId,
                Title = request.Title,
                Category = request.Category.ToString(),
                Description = request.Description,
                Status = request.Status.ToString(),
                StartDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<TreatmentPlanResponse?> UpdatePrimaryTreatmentPlanAsync(int medicalRecordId, int doctorId, UpdateTreatmentPlanRequest request)
        {
            if (medicalRecordId == 999) return Task.FromResult<TreatmentPlanResponse?>(null);
            if (doctorId == 777) throw new UnauthorizedAccessException("Unauthorized doctor.");

            return Task.FromResult<TreatmentPlanResponse?>(new TreatmentPlanResponse
            {
                Id = 1,
                MedicalRecordId = medicalRecordId,
                PatientId = 1,
                DoctorId = doctorId,
                Title = request.Title,
                Category = request.Category.ToString(),
                Description = request.Description,
                Status = request.Status.ToString(),
                StartDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<List<TreatmentPlanResponse>> GetTreatmentPlansAsync(int medicalRecordId)
        {
            if (medicalRecordId == 999) throw new InvalidOperationException("Medical record not found.");

            return Task.FromResult(new List<TreatmentPlanResponse>
            {
                new()
                {
                    Id = 1,
                    MedicalRecordId = medicalRecordId,
                    PatientId = 1,
                    DoctorId = 2,
                    Title = "Primary Plan",
                    Category = "General",
                    Description = "Standard care",
                    Status = "Active",
                    StartDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            });
        }

        public Task<List<MedicalRecordVersionResponse>> GetVersionHistoryAsync(int medicalRecordId)
        {
            if (medicalRecordId == 999) throw new InvalidOperationException("Medical record not found.");

            return Task.FromResult(new List<MedicalRecordVersionResponse>
            {
                new()
                {
                    Id = 2,
                    MedicalRecordId = medicalRecordId,
                    VersionNumber = 2,
                    ChangedByUserId = 2,
                    ChangedByUserName = "Dr. Smith",
                    ChangedAt = DateTime.UtcNow,
                    ChangeType = "Update",
                    ChangeSummary = "Diagnosis updated",
                    ChiefComplaint = "Fever and persistent cough",
                    Symptoms = "Cough, fatigue",
                    ExaminationNotes = "Throat red",
                    Diagnosis = "Acute Bronchitis",
                    TreatmentPlan = "Rest and antibiotics",
                    PreviousChiefComplaint = "Fever",
                    PreviousDiagnosis = "Common Cold"
                },
                new()
                {
                    Id = 1,
                    MedicalRecordId = medicalRecordId,
                    VersionNumber = 1,
                    ChangedByUserId = 2,
                    ChangedByUserName = "Dr. Smith",
                    ChangedAt = DateTime.UtcNow.AddDays(-2),
                    ChangeType = "Initial",
                    ChangeSummary = "Initial medical record created",
                    ChiefComplaint = "Fever",
                    Symptoms = "Mild fever",
                    ExaminationNotes = "Clear lungs",
                    Diagnosis = "Common Cold",
                    TreatmentPlan = "Hydration"
                }
            });
        }

        public Task<MedicalRecordVersionResponse?> GetVersionAsync(int medicalRecordId, int versionNumber)
        {
            if (medicalRecordId == 999) throw new InvalidOperationException("Medical record not found.");
            if (versionNumber == 999) return Task.FromResult<MedicalRecordVersionResponse?>(null);

            return Task.FromResult<MedicalRecordVersionResponse?>(new MedicalRecordVersionResponse
            {
                Id = versionNumber,
                MedicalRecordId = medicalRecordId,
                VersionNumber = versionNumber,
                ChangedByUserId = 2,
                ChangedByUserName = "Dr. Smith",
                ChangedAt = DateTime.UtcNow,
                ChangeType = versionNumber == 1 ? "Initial" : "Update",
                ChangeSummary = versionNumber == 1 ? "Initial medical record created" : "Diagnosis updated",
                ChiefComplaint = "Fever",
                Diagnosis = "Common Cold"
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
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        // Act
        var result = await controller.GetByPatient(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var records = Assert.IsAssignableFrom<List<MedicalRecordSummaryResponse>>(okResult.Value);
        Assert.Single(records);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByPatient_WithInvalidId_ReturnsBadRequest(int invalidId)
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        // Act
        var result = await controller.GetByPatient(invalidId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByPatient_WhenPatientNotFound_ReturnsNotFound()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        // Act
        var result = await controller.GetByPatient(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

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
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        // Act
        var result = await controller.GetById(invalidId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithDoctorClaims_ReturnsCreated()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Chest pain",
            Diagnosis = "Angina"
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var record = Assert.IsType<MedicalRecordResponse>(createdResult.Value);
        Assert.Equal(5, record.DoctorId);
    }

    [Fact]
    public async Task Create_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
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
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };
        controller.ModelState.AddModelError("ChiefComplaint", "Chief complaint is required.");

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "",
            Diagnosis = "Angina"
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
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No claims
            }
        };

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Chest pain",
            Diagnosis = "Angina"
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 999, // Stub throws InvalidOperationException for 999
            ChiefComplaint = "Chest pain",
            Diagnosis = "Angina"
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Resolved pain",
            Diagnosis = "Angina stable"
        };

        // Act
        var result = await controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var record = Assert.IsType<MedicalRecordResponse>(okResult.Value);
        Assert.Equal("Resolved pain", record.ChiefComplaint);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Update_WithInvalidId_ReturnsBadRequest(int invalidId)
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Resolved pain",
            Diagnosis = "Angina stable"
        };

        // Act
        var result = await controller.Update(invalidId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenRecordNotFoundOrNotEditable_ReturnsNotFound()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Resolved pain",
            Diagnosis = "Angina stable"
        };

        // Act - ID 999 returns null from stub
        var result = await controller.Update(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new UpdateMedicalRecordRequest
        {
            ChiefComplaint = "Resolved pain",
            Diagnosis = "Angina stable"
        };

        // Act - ID 888 throws InvalidOperationException from stub
        var result = await controller.Update(888, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static ControllerContext CreatePatientContext(string patientId = "1")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, patientId),
            new Claim(ClaimTypes.Role, "Patient")
        }, "TestAuth"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetByAppointment_WithExistingAppointment_ReturnsOk()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetByAppointment(100);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var record = Assert.IsType<MedicalRecordResponse>(okResult.Value);
        Assert.Equal(100, record.AppointmentId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByAppointment_WithInvalidId_ReturnsBadRequest(int invalidId)
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        var result = await controller.GetByAppointment(invalidId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByAppointment_WhenNotFound_ReturnsNotFound()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        var result = await controller.GetByAppointment(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetByAppointment_PatientAccessingOtherPatientRecord_ReturnsForbid()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("99") // Patient 99 accessing record belonging to Patient 1
        };

        var result = await controller.GetByAppointment(100);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetByAppointment_PatientAccessingOwnRecord_ReturnsOk()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("1") // Patient 1 accessing record belonging to Patient 1
        };

        var result = await controller.GetByAppointment(100);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var record = Assert.IsType<MedicalRecordResponse>(okResult.Value);
        Assert.Equal(1, record.PatientId);
    }

    [Fact]
    public async Task GetByPatient_PatientAccessingOtherPatientRecords_ReturnsForbid()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("2") // Patient 2 accessing Patient 1's records
        };

        var result = await controller.GetByPatient(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetById_PatientAccessingOtherPatientRecord_ReturnsForbid()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("2") // Patient 2 accessing Record 1 (which belongs to Patient 1)
        };

        var result = await controller.GetById(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_UnauthenticatedUser_ReturnsUnauthorized()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // No claims
                }
            }
        };

        var request = new CreateMedicalRecordRequest
        {
            PatientId = 1,
            ChiefComplaint = "Fever",
            Diagnosis = "Flu"
        };

        var result = await controller.Create(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetDiagnoses_WithValidRecord_ReturnsOk()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetDiagnoses(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var diagnoses = Assert.IsType<List<DiagnosisResponse>>(okResult.Value);
        Assert.Single(diagnoses);
    }

    [Fact]
    public async Task GetDiagnoses_WhenRecordNotFound_ReturnsNotFound()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetDiagnoses(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDiagnoses_PatientAccessingOtherPatientRecord_ReturnsForbid()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("99") // Patient 99 accessing Record 1 (which belongs to Patient 1)
        };

        var result = await controller.GetDiagnoses(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AddDiagnosis_WithValidRequest_ReturnsCreated()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new AddDiagnosisRequest
        {
            Code = "J06.9",
            Description = "Acute upper respiratory infection",
            Type = DiagnosisType.Primary,
            Status = DiagnosisStatus.Active,
            Severity = DiagnosisSeverity.Moderate
        };

        var result = await controller.AddDiagnosis(1, request);

        var createdResult = Assert.IsType<CreatedResult>(result);
        var diagnosis = Assert.IsType<DiagnosisResponse>(createdResult.Value);
        Assert.Equal("Acute upper respiratory infection", diagnosis.Description);
    }

    [Fact]
    public async Task AddDiagnosis_UnauthorizedDoctor_ReturnsForbid()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("777")
        };

        var request = new AddDiagnosisRequest
        {
            Description = "Test Diagnosis"
        };

        var result = await controller.AddDiagnosis(1, request);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateDiagnosis_WithValidRequest_ReturnsOk()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new UpdateDiagnosisRequest
        {
            Description = "Resolved infection",
            Status = DiagnosisStatus.Resolved
        };

        var result = await controller.UpdateDiagnosis(1, 10, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var diagnosis = Assert.IsType<DiagnosisResponse>(okResult.Value);
        Assert.Equal("Resolved infection", diagnosis.Description);
    }

    [Fact]
    public async Task GetTreatmentPlans_WithValidRecord_ReturnsOk()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetTreatmentPlans(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var plans = Assert.IsType<List<TreatmentPlanResponse>>(okResult.Value);
        Assert.Single(plans);
    }

    [Fact]
    public async Task RecordTreatmentPlan_WithValidRequest_ReturnsCreated()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new RecordTreatmentPlanRequest
        {
            Title = "Hypertension Protocol",
            Category = TreatmentPlanCategory.Pharmacological,
            Description = "Prescribe ACE inhibitors and monitor BP weekly"
        };

        var result = await controller.RecordTreatmentPlan(1, request);

        var createdResult = Assert.IsType<CreatedResult>(result);
        var plan = Assert.IsType<TreatmentPlanResponse>(createdResult.Value);
        Assert.Equal("Hypertension Protocol", plan.Title);
    }

    [Fact]
    public async Task UpdateTreatmentPlan_WithValidRequest_ReturnsOk()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var request = new UpdateTreatmentPlanRequest
        {
            Title = "Updated Protocol",
            Category = TreatmentPlanCategory.Lifestyle,
            Description = "Dietary DASH protocol and aerobic exercise",
            Status = TreatmentPlanStatus.InProgress
        };

        var result = await controller.UpdateTreatmentPlan(1, 20, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<TreatmentPlanResponse>(okResult.Value);
        Assert.Equal("Updated Protocol", plan.Title);
    }

    [Fact]
    public async Task GetVersionHistory_ReturnsOk_WithOrderedVersions_ForDoctor()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetVersionHistory(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var versions = Assert.IsType<List<MedicalRecordVersionResponse>>(okResult.Value);
        Assert.Equal(2, versions.Count);
        Assert.Equal(2, versions[0].VersionNumber);
        Assert.Equal(1, versions[1].VersionNumber);
    }

    [Fact]
    public async Task GetVersionHistory_ReturnsOk_ForAuthorizedPatient_OwnRecord()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("1") // Record 1 has PatientId 1
        };

        var result = await controller.GetVersionHistory(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var versions = Assert.IsType<List<MedicalRecordVersionResponse>>(okResult.Value);
        Assert.Equal(2, versions.Count);
    }

    [Fact]
    public async Task GetVersionHistory_ReturnsForbid_ForPatient_AccessingOtherPatientRecord()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("99") // Patient 99 accessing record of patient 1
        };

        var result = await controller.GetVersionHistory(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetVersionHistory_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetVersionHistory(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetVersionHistory_ReturnsBadRequest_WhenIdIsInvalid(int invalidId)
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        var result = await controller.GetVersionHistory(invalidId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetVersion_ReturnsOk_WhenVersionExists()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetVersion(1, 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var version = Assert.IsType<MedicalRecordVersionResponse>(okResult.Value);
        Assert.Equal(1, version.VersionNumber);
    }

    [Fact]
    public async Task GetVersion_ReturnsForbid_ForUnauthorizedPatient()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreatePatientContext("99")
        };

        var result = await controller.GetVersion(1, 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetVersion_ReturnsNotFound_WhenVersionDoesNotExist()
    {
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService)
        {
            ControllerContext = CreateDoctorContext("5")
        };

        var result = await controller.GetVersion(1, 999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
