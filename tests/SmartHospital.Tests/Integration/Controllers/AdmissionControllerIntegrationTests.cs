using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Admissions;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class AdmissionControllerIntegrationTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static AdmissionController CreateController(AppDbContext context, string role = "Admin", int? userId = 1)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        var identity = userId.HasValue
            ? new ClaimsIdentity(claims, "test")
            : new ClaimsIdentity(claims);

        return new AdmissionController(new AdmissionService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static User SeedUser(AppDbContext context, string email, UserRole role = UserRole.Patient)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = role.ToString(),
            Email = email,
            PasswordHash = "hash",
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private static (Ward ward, Room room, Bed bed) SeedHierarchy(AppDbContext context, int wardCapacity = 10, int roomCapacity = 4)
    {
        var ward = new Ward
        {
            Name = "General Ward",
            Code = "GW-" + Guid.NewGuid().ToString()[..4].ToUpper(),
            Floor = "1",
            Capacity = wardCapacity,
            Type = WardType.General,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward);
        context.SaveChanges();

        var room = new Room
        {
            WardId = ward.Id,
            RoomNumber = "R-" + Guid.NewGuid().ToString()[..4].ToUpper(),
            Capacity = roomCapacity,
            Type = RoomType.Standard,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Rooms.Add(room);
        context.SaveChanges();

        var bed = new Bed
        {
            RoomId = room.Id,
            BedNumber = "B-01",
            Status = BedStatus.Available,
            Type = BedType.Standard,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Beds.Add(bed);
        context.SaveChanges();

        return (ward, room, bed);
    }

    private static Admission SeedAdmission(AppDbContext context, int patientId, int? doctorId = null, AdmissionStatus status = AdmissionStatus.Admitted)
    {
        var admission = new Admission
        {
            AdmissionNumber = $"ADM-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            PatientId = patientId,
            AdmittingDoctorId = doctorId,
            AdmissionDate = DateTime.UtcNow.AddDays(-1),
            Status = status,
            Priority = AdmissionPriority.Normal,
            ReasonForAdmission = "Observation",
            Diagnosis = "Preliminary diagnosis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Admissions.Add(admission);
        context.SaveChanges();
        return admission;
    }

    [Fact]
    public async Task CreateAdmission_ValidRequest_Returns201CreatedWithLocationHeader()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient1@test.com", UserRole.Patient);
        var doctor = SeedUser(context, "doc1@test.com", UserRole.Doctor);
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateAdmission(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            AdmittingDoctorId = doctor.Id,
            Priority = AdmissionPriority.Urgent,
            ReasonForAdmission = "Severe abdominal pain",
            Diagnosis = "Acute appendicitis"
        });

        var created = Assert.IsType<CreatedResult>(result);
        var admission = Assert.IsType<AdmissionResponse>(created.Value);
        Assert.Equal($"/api/admissions/{admission.Id}", created.Location);
        Assert.Equal(patient.Id, admission.PatientId);
        Assert.Equal("Urgent", admission.Priority);
        Assert.Equal("Admitted", admission.Status);
    }

    [Fact]
    public async Task CreateAdmission_PatientNotFound_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateAdmission(new CreateAdmissionRequest
        {
            PatientId = 999,
            Priority = AdmissionPriority.Normal,
            ReasonForAdmission = "Observation"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task CreateAdmission_PatientAlreadyHasActiveAdmission_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient2@test.com", UserRole.Patient);
        SeedAdmission(context, patient.Id, status: AdmissionStatus.Admitted);
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateAdmission(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            Priority = AdmissionPriority.Normal,
            ReasonForAdmission = "Duplicate admission attempt"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetAdmissions_WithFilter_Returns200OkWithList()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient3@test.com", UserRole.Patient);
        SeedAdmission(context, patient.Id, status: AdmissionStatus.Admitted);
        var controller = CreateController(context, "Staff");

        var result = await controller.GetAdmissions(new AdmissionQueryFilter
        {
            Status = AdmissionStatus.Admitted
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<AdmissionResponse>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetAdmissionById_ExistingId_Returns200Ok()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient4@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id);
        var controller = CreateController(context, "Doctor");

        var result = await controller.GetAdmissionById(admission.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdmissionResponse>(ok.Value);
        Assert.Equal(admission.Id, response.Id);
    }

    [Fact]
    public async Task GetAdmissionById_NonExistingId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetAdmissionById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMyActiveAdmission_AuthenticatedUserWithAdmission_Returns200Ok()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient5@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id, status: AdmissionStatus.Admitted);
        var controller = CreateController(context, "Patient", userId: patient.Id);

        var result = await controller.GetMyActiveAdmission();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PatientAdmissionSummaryResponse>(ok.Value);
        Assert.Equal(admission.Id, response.AdmissionId);
    }

    [Fact]
    public async Task GetMyActiveAdmission_Unauthenticated_Returns401Unauthorized()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Patient", userId: null);

        var result = await controller.GetMyActiveAdmission();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyActiveAdmission_NoActiveAdmission_Returns404NotFound()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient6@test.com", UserRole.Patient);
        var controller = CreateController(context, "Patient", userId: patient.Id);

        var result = await controller.GetMyActiveAdmission();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMyAdmissionHistory_AuthenticatedUser_Returns200Ok()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient7@test.com", UserRole.Patient);
        SeedAdmission(context, patient.Id, status: AdmissionStatus.Discharged);
        var controller = CreateController(context, "Patient", userId: patient.Id);

        var result = await controller.GetMyAdmissionHistory();

        var ok = Assert.IsType<OkObjectResult>(result);
        var history = Assert.IsType<List<PatientAdmissionSummaryResponse>>(ok.Value);
        Assert.Single(history);
    }

    [Fact]
    public async Task GetMyAdmissionHistory_Unauthenticated_Returns401Unauthorized()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Patient", userId: null);

        var result = await controller.GetMyAdmissionHistory();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetPatientActiveAdmission_ExistingPatient_Returns200Ok()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient8@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id, status: AdmissionStatus.Admitted);
        var controller = CreateController(context, "Doctor");

        var result = await controller.GetPatientActiveAdmission(patient.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PatientAdmissionSummaryResponse>(ok.Value);
        Assert.Equal(admission.Id, response.AdmissionId);
    }

    [Fact]
    public async Task GetPatientActiveAdmission_NonExistingAdmission_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Staff");

        var result = await controller.GetPatientActiveAdmission(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetPatientAdmissionHistory_ExistingPatient_Returns200Ok()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient9@test.com", UserRole.Patient);
        SeedAdmission(context, patient.Id, status: AdmissionStatus.Discharged);
        var controller = CreateController(context, "Doctor");

        var result = await controller.GetPatientAdmissionHistory(patient.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var history = Assert.IsType<List<PatientAdmissionSummaryResponse>>(ok.Value);
        Assert.Single(history);
    }

    [Fact]
    public async Task UpdateAdmission_ExistingAdmission_Returns200Ok()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient10@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id);
        var controller = CreateController(context, "Doctor");

        var result = await controller.UpdateAdmission(admission.Id, new UpdateAdmissionRequest
        {
            ReasonForAdmission = "Updated reason",
            Diagnosis = "Confirmed diagnosis",
            Priority = AdmissionPriority.Urgent
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<AdmissionResponse>(ok.Value);
        Assert.Equal("Updated reason", updated.ReasonForAdmission);
        Assert.Equal("Confirmed diagnosis", updated.Diagnosis);
        Assert.Equal("Urgent", updated.Priority);
    }

    [Fact]
    public async Task UpdateAdmission_NonExistingAdmission_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Doctor");

        var result = await controller.UpdateAdmission(999, new UpdateAdmissionRequest
        {
            ReasonForAdmission = "Non-existing"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AllocateBed_ValidRequest_Returns200OkAndOccupiesBed()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient11@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id);
        var (_, _, bed) = SeedHierarchy(context);
        var controller = CreateController(context, "ResourceAdmin");

        var result = await controller.AllocateBed(admission.Id, new AllocateBedRequest
        {
            BedId = bed.Id,
            Notes = "Standard allocation"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdmissionResponse>(ok.Value);
        Assert.NotNull(response.ActiveBedId);
        Assert.Equal(bed.Id, response.ActiveBedId);

        var reloadedBed = await context.Beds.FindAsync(bed.Id);
        Assert.Equal(BedStatus.Occupied, reloadedBed!.Status);
    }

    [Fact]
    public async Task AllocateBed_BedAlreadyOccupied_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient12@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id);
        var (_, _, bed) = SeedHierarchy(context);
        bed.Status = BedStatus.Occupied;
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Staff");

        var result = await controller.AllocateBed(admission.Id, new AllocateBedRequest
        {
            BedId = bed.Id
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task TransferPatient_ValidRequest_Returns200OkAndUpdatesBeds()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient13@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id);
        var (_, room, bed1) = SeedHierarchy(context);

        var bed2 = new Bed
        {
            RoomId = room.Id,
            BedNumber = "B-02",
            Status = BedStatus.Available,
            Type = BedType.Standard,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Beds.Add(bed2);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "Staff");
        await controller.AllocateBed(admission.Id, new AllocateBedRequest { BedId = bed1.Id });

        var transferResult = await controller.TransferPatient(admission.Id, new TransferPatientRequest
        {
            NewBedId = bed2.Id,
            TransferReason = "Patient requested window view"
        });

        var ok = Assert.IsType<OkObjectResult>(transferResult);
        var response = Assert.IsType<AdmissionResponse>(ok.Value);
        Assert.Equal(bed2.Id, response.ActiveBedId);

        var reloadedBed1 = await context.Beds.FindAsync(bed1.Id);
        var reloadedBed2 = await context.Beds.FindAsync(bed2.Id);
        Assert.Equal(BedStatus.Available, reloadedBed1!.Status);
        Assert.Equal(BedStatus.Occupied, reloadedBed2!.Status);
    }

    [Fact]
    public async Task TransferPatient_SameBed_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient14@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id);
        var (_, _, bed) = SeedHierarchy(context);
        var controller = CreateController(context, "Staff");
        await controller.AllocateBed(admission.Id, new AllocateBedRequest { BedId = bed.Id });

        var transferResult = await controller.TransferPatient(admission.Id, new TransferPatientRequest
        {
            NewBedId = bed.Id,
            TransferReason = "Same bed"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(transferResult);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task DischargePatient_ActiveAdmission_Returns200OkAndFreesBed()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient15@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id);
        var (_, _, bed) = SeedHierarchy(context);
        var controller = CreateController(context, "Doctor");
        await controller.AllocateBed(admission.Id, new AllocateBedRequest { BedId = bed.Id });

        var result = await controller.DischargePatient(admission.Id, new DischargePatientRequest
        {
            DischargeSummary = "Patient recovered fully"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdmissionResponse>(ok.Value);
        Assert.Equal("Discharged", response.Status);
        Assert.NotNull(response.DischargeDate);

        var reloadedBed = await context.Beds.FindAsync(bed.Id);
        Assert.Equal(BedStatus.Available, reloadedBed!.Status);
    }

    [Fact]
    public async Task DischargePatient_AlreadyDischarged_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient16@test.com", UserRole.Patient);
        var admission = SeedAdmission(context, patient.Id, status: AdmissionStatus.Discharged);
        var controller = CreateController(context, "Doctor");

        var result = await controller.DischargePatient(admission.Id, new DischargePatientRequest
        {
            DischargeSummary = "Discharging again"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public void Controller_HasExpectedRouteAndAuthorizeAttribute()
    {
        var type = typeof(AdmissionController);
        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        var authAttr = type.GetCustomAttribute<AuthorizeAttribute>();
        var apiControllerAttr = type.GetCustomAttribute<ApiControllerAttribute>();

        Assert.NotNull(routeAttr);
        Assert.Equal("api/admissions", routeAttr.Template);
        Assert.NotNull(authAttr);
        Assert.NotNull(apiControllerAttr);
    }

    [Theory]
    [InlineData(nameof(AdmissionController.CreateAdmission), "Admin,Staff,Doctor,ResourceAdmin")]
    [InlineData(nameof(AdmissionController.GetAdmissions), "Admin,Staff,Doctor,ResourceAdmin")]
    [InlineData(nameof(AdmissionController.GetPatientActiveAdmission), "Admin,Staff,Doctor")]
    [InlineData(nameof(AdmissionController.GetPatientAdmissionHistory), "Admin,Staff,Doctor")]
    [InlineData(nameof(AdmissionController.UpdateAdmission), "Admin,Staff,Doctor,ResourceAdmin")]
    [InlineData(nameof(AdmissionController.AllocateBed), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(AdmissionController.TransferPatient), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(AdmissionController.DischargePatient), "Admin,Staff,Doctor,ResourceAdmin")]
    public void Actions_HaveExpectedRoleAuthorizeAttributes(string actionName, string expectedRoles)
    {
        var method = typeof(AdmissionController).GetMethod(actionName);
        Assert.NotNull(method);

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal(expectedRoles, authAttr.Roles);
    }
}
