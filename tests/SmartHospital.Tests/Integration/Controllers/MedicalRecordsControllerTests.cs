using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class MedicalRecordsControllerTests
{
    private class StubMedicalRecordService : IMedicalRecordService
    {
        public Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId)
        {
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
                DoctorId = doctorId,
                ChiefComplaint = request.ChiefComplaint,
                Diagnosis = request.Diagnosis
            });
        }
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

    [Fact]
    public async Task Create_WithDoctorClaims_ReturnsCreated()
    {
        // Arrange
        var stubService = new StubMedicalRecordService();
        var controller = new MedicalRecordsController(stubService);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim(ClaimTypes.Role, "Doctor")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
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
}
