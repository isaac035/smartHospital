using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class MedicalHistoryControllerTests
{
    private class StubMedicalHistoryService : IMedicalHistoryService
    {
        public Task<PatientMedicalTimelineResponse> GetPatientTimelineAsync(int patientId)
        {
            if (patientId == 999)
            {
                throw new InvalidOperationException($"Patient with ID {patientId} was not found.");
            }

            if (patientId <= 0)
            {
                throw new ArgumentException("Patient ID must be a positive integer.");
            }

            return Task.FromResult(new PatientMedicalTimelineResponse
            {
                PatientId = patientId,
                PatientName = "Jane Doe",
                TotalEvents = 2,
                Events = new List<MedicalTimelineEventResponse>
                {
                    new()
                    {
                        EventType = "Diagnosis",
                        EventDate = DateTime.UtcNow,
                        PatientId = patientId,
                        PatientName = "Jane Doe",
                        DoctorId = 5,
                        DoctorName = "Dr. Strange",
                        Summary = "Primary Diagnosis: Hypertension",
                        SourceRecordId = 1,
                        Category = "Primary",
                        Status = "Active"
                    },
                    new()
                    {
                        EventType = "MedicalRecord",
                        EventDate = DateTime.UtcNow.AddDays(-1),
                        PatientId = patientId,
                        PatientName = "Jane Doe",
                        DoctorId = 5,
                        DoctorName = "Dr. Strange",
                        Summary = "Encounter REC-1: Routine checkup",
                        SourceRecordId = 1,
                        Category = "ClinicalEncounter",
                        Status = "Recorded"
                    }
                }
            });
        }
    }

    private ControllerContext CreateContext(string role, string userId)
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

    [Fact]
    public async Task GetPatientTimeline_AsDoctor_ReturnsOkWithTimeline()
    {
        var service = new StubMedicalHistoryService();
        var controller = new MedicalHistoryController(service)
        {
            ControllerContext = CreateContext("Doctor", "5")
        };

        var result = await controller.GetPatientTimeline(10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var timeline = Assert.IsType<PatientMedicalTimelineResponse>(okResult.Value);
        Assert.Equal(10, timeline.PatientId);
        Assert.Equal(2, timeline.TotalEvents);
    }

    [Fact]
    public async Task GetPatientTimeline_AsPatientAccessingOwnTimeline_ReturnsOk()
    {
        var service = new StubMedicalHistoryService();
        var controller = new MedicalHistoryController(service)
        {
            ControllerContext = CreateContext("Patient", "10")
        };

        var result = await controller.GetPatientTimeline(10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var timeline = Assert.IsType<PatientMedicalTimelineResponse>(okResult.Value);
        Assert.Equal(10, timeline.PatientId);
    }

    [Fact]
    public async Task GetPatientTimeline_AsPatientAccessingOtherPatient_ReturnsForbid()
    {
        var service = new StubMedicalHistoryService();
        var controller = new MedicalHistoryController(service)
        {
            ControllerContext = CreateContext("Patient", "10") // Patient 10 accessing Patient 20
        };

        var result = await controller.GetPatientTimeline(20);

        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPatientTimeline_WithInvalidId_ReturnsBadRequest(int invalidId)
    {
        var service = new StubMedicalHistoryService();
        var controller = new MedicalHistoryController(service)
        {
            ControllerContext = CreateContext("Doctor", "5")
        };

        var result = await controller.GetPatientTimeline(invalidId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetPatientTimeline_WhenPatientNotFound_ReturnsNotFound()
    {
        var service = new StubMedicalHistoryService();
        var controller = new MedicalHistoryController(service)
        {
            ControllerContext = CreateContext("Doctor", "5")
        };

        var result = await controller.GetPatientTimeline(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetPatientTimelineAlias_ReturnsSameResultAsMainEndpoint()
    {
        var service = new StubMedicalHistoryService();
        var controller = new MedicalHistoryController(service)
        {
            ControllerContext = CreateContext("Doctor", "5")
        };

        var result = await controller.GetPatientTimelineAlias(10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var timeline = Assert.IsType<PatientMedicalTimelineResponse>(okResult.Value);
        Assert.Equal(10, timeline.PatientId);
    }
}
