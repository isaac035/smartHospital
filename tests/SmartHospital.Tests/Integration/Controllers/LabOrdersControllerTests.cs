using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class LabOrdersControllerTests
{
    private class StubLabOrderService : ILabOrderService
    {
        public Task<List<LabOrderResponse>> GetByPatientIdAsync(int patientId)
        {
            if (patientId == 999)
            {
                throw new InvalidOperationException("Patient with ID 999 was not found.");
            }

            return Task.FromResult(new List<LabOrderResponse>
            {
                new()
                {
                    Id = 1,
                    OrderNumber = "LAB-001",
                    PatientId = patientId,
                    DoctorId = 2,
                    TestName = "CBC",
                    Status = "Ordered"
                }
            });
        }

        public Task<LabOrderResponse?> GetByIdAsync(int id)
        {
            if (id == 999) return Task.FromResult<LabOrderResponse?>(null);

            return Task.FromResult<LabOrderResponse?>(new LabOrderResponse
            {
                Id = id,
                OrderNumber = "LAB-001",
                PatientId = 1,
                DoctorId = 2,
                TestName = "CBC",
                Status = "Ordered"
            });
        }

        public Task<LabOrderResponse> CreateOrderAsync(int doctorId, CreateLabOrderRequest request)
        {
            if (request.PatientId == 999)
            {
                throw new InvalidOperationException("Active patient not found.");
            }

            if (request.MedicalRecordId == 999)
            {
                throw new InvalidOperationException("Medical record does not belong to the specified patient.");
            }

            return Task.FromResult(new LabOrderResponse
            {
                Id = 10,
                OrderNumber = "LAB-010",
                PatientId = request.PatientId,
                DoctorId = doctorId,
                MedicalRecordId = request.MedicalRecordId,
                TestName = request.TestName,
                Status = "Ordered"
            });
        }

        public Task<LabOrderResponse?> UpdateStatusAsync(int id, UpdateLabOrderStatusRequest request)
        {
            return UpdateStatusAsync(id, request.Status);
        }

        public Task<LabOrderResponse?> UpdateStatusAsync(int id, LabOrderStatus newStatus)
        {
            if (id == 999) return Task.FromResult<LabOrderResponse?>(null);

            if (newStatus == LabOrderStatus.Completed)
            {
                throw new InvalidOperationException("Cannot mark lab order as completed without a lab report.");
            }

            return Task.FromResult<LabOrderResponse?>(new LabOrderResponse
            {
                Id = id,
                OrderNumber = "LAB-001",
                Status = newStatus.ToString()
            });
        }

        public Task<LabReportResponse?> RecordReportAsync(int labOrderId, int conductedByUserId, RecordLabReportRequest request)
        {
            if (labOrderId == 999) return Task.FromResult<LabReportResponse?>(null);

            if (labOrderId == 777)
            {
                throw new InvalidOperationException("A report has already been recorded for this lab order.");
            }

            if (labOrderId == 666)
            {
                throw new InvalidOperationException("Cannot record report for a cancelled lab order.");
            }

            return Task.FromResult<LabReportResponse?>(new LabReportResponse
            {
                Id = 100,
                LabOrderId = labOrderId,
                ConductedByUserId = conductedByUserId,
                ResultSummary = request.ResultSummary,
                Findings = request.Findings
            });
        }
    }

    private static ControllerContext CreateUserContext(string userId = "5", string role = "Doctor")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "mock"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByPatient
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPatient_WithValidId_ReturnsOkWithList()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext()
        };

        var result = await controller.GetByPatient(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var orders = Assert.IsAssignableFrom<List<LabOrderResponse>>(okResult.Value);
        Assert.Single(orders);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByPatient_WithInvalidId_ReturnsBadRequest(int patientId)
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext()
        };

        var result = await controller.GetByPatient(patientId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByPatient_WhenNotFound_ReturnsNotFound()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext()
        };

        var result = await controller.GetByPatient(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetById
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOkWithOrder()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext()
        };

        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var order = Assert.IsType<LabOrderResponse>(okResult.Value);
        Assert.Equal(1, order.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetById_WithInvalidId_ReturnsBadRequest(int id)
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext()
        };

        var result = await controller.GetById(id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext()
        };

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateOrder
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreated()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("5", "Doctor")
        };

        var request = new CreateLabOrderRequest
        {
            PatientId = 1,
            TestName = "Full Blood Count"
        };

        var result = await controller.CreateOrder(request);

        var createdResult = Assert.IsType<CreatedResult>(result);
        var order = Assert.IsType<LabOrderResponse>(createdResult.Value);
        Assert.Equal(10, order.Id);
        Assert.Equal($"/api/laborders/{order.Id}", createdResult.Location);
    }

    [Fact]
    public async Task CreateOrder_WithNullRequest_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("5", "Doctor")
        };

        var result = await controller.CreateOrder(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidModelState_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("5", "Doctor")
        };
        controller.ModelState.AddModelError("TestName", "Test name is required.");

        var request = new CreateLabOrderRequest();
        var result = await controller.CreateOrder(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidDoctorClaim_ReturnsUnauthorized()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // No claims
                }
            }
        };

        var request = new CreateLabOrderRequest { PatientId = 1, TestName = "CBC" };
        var result = await controller.CreateOrder(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrder_WhenPatientNotFound_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("5", "Doctor")
        };

        var request = new CreateLabOrderRequest
        {
            PatientId = 999, // Throws InvalidOperationException in stub
            TestName = "CBC"
        };

        var result = await controller.CreateOrder(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateStatus
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_WithValidTransition_ReturnsOk()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var request = new UpdateLabOrderStatusRequest
        {
            Status = LabOrderStatus.SampleCollected
        };

        var result = await controller.UpdateStatus(1, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var order = Assert.IsType<LabOrderResponse>(okResult.Value);
        Assert.Equal("SampleCollected", order.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateStatus_WithInvalidId_ReturnsBadRequest(int id)
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var result = await controller.UpdateStatus(id, new UpdateLabOrderStatusRequest { Status = LabOrderStatus.SampleCollected });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentOrder_ReturnsNotFound()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var result = await controller.UpdateStatus(999, new UpdateLabOrderStatusRequest { Status = LabOrderStatus.SampleCollected });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_InvalidCompletedTransition_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var result = await controller.UpdateStatus(1, new UpdateLabOrderStatusRequest { Status = LabOrderStatus.Completed });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RecordReport
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordReport_WithValidRequest_ReturnsOkWithReport()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var request = new RecordLabReportRequest
        {
            ResultSummary = "Normal hemoglobin",
            Findings = "Hb: 14.5 g/dL"
        };

        var result = await controller.RecordReport(1, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var report = Assert.IsType<LabReportResponse>(okResult.Value);
        Assert.Equal(1, report.LabOrderId);
        Assert.Equal(6, report.ConductedByUserId);
        Assert.Equal("Normal hemoglobin", report.ResultSummary);
    }

    [Fact]
    public async Task RecordReport_NonExistentOrder_ReturnsNotFound()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var request = new RecordLabReportRequest
        {
            ResultSummary = "Summary",
            Findings = "Findings"
        };

        var result = await controller.RecordReport(999, request);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RecordReport_DuplicateReport_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var request = new RecordLabReportRequest
        {
            ResultSummary = "Duplicate",
            Findings = "Duplicate"
        };

        var result = await controller.RecordReport(777, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task RecordReport_CancelledOrder_ReturnsBadRequest()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = CreateUserContext("6", "Staff")
        };

        var request = new RecordLabReportRequest
        {
            ResultSummary = "Summary",
            Findings = "Findings"
        };

        var result = await controller.RecordReport(666, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task RecordReport_InvalidUserClaims_ReturnsUnauthorized()
    {
        var controller = new LabOrdersController(new StubLabOrderService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var request = new RecordLabReportRequest { ResultSummary = "S", Findings = "F" };
        var result = await controller.RecordReport(1, request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
