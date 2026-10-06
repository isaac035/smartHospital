using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Auth;
using SmartHospital.Api.DTOs.Doctors;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for DoctorsController wired to the real DoctorService over an
/// in-memory database, covering the full doctor-management lifecycle.
/// </summary>
public class DoctorsControllerTests
{
    [Fact]
    public async Task Create_Returns201WithDoctorAndTemporaryPassword()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);

        var result = await CreateController(context, "Admin").Create(NewCreateRequest(dept.Id, "new@example.test"));

        var created = Assert.IsType<CreatedResult>(result);
        var body = Assert.IsType<CreateDoctorResponse>(created.Value);
        Assert.Equal($"/api/doctors/{body.Doctor.Id}", created.Location);
        Assert.False(string.IsNullOrWhiteSpace(body.TemporaryPassword));
    }

    [Fact]
    public async Task Create_DuplicateEmailReturns409()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var controller = CreateController(context, "Admin");
        await controller.Create(NewCreateRequest(dept.Id, "dup@example.test"));

        Assert.IsType<ConflictObjectResult>(await controller.Create(NewCreateRequest(dept.Id, "dup@example.test")));
    }

    [Fact]
    public async Task Create_UnknownDepartmentReturns409()
    {
        await using var context = CreateContext();

        var result = await CreateController(context, "Admin").Create(NewCreateRequest(999, "x@example.test"));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreatedDoctor_CanLogInWithTemporaryPassword()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var body = Assert.IsType<CreateDoctorResponse>(Assert.IsType<CreatedResult>(
            await CreateController(context, "Admin").Create(NewCreateRequest(dept.Id, "login@example.test"))).Value);

        var auth = new AuthService(context, new JwtService(Options.Create(new JwtSettings
        {
            Key = "integration-test-signing-key-at-least-32-characters",
            Issuer = "t",
            Audience = "t",
            ExpiryMinutes = 60
        })));
        var login = await auth.LoginAsync(new LoginRequest { Email = "login@example.test", Password = body.TemporaryPassword });

        Assert.NotNull(login);
        Assert.Equal("Doctor", login!.Role);
    }

    [Fact]
    public async Task GetById_Returns200Or404()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "d@example.test");
        var controller = CreateController(context, "Patient");

        Assert.IsType<OkObjectResult>(await controller.GetById(doctor.Id));
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(9999));
    }

    [Fact]
    public async Task GetAll_ReturnsActiveDoctors()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "a@example.test");
        SeedDoctor(context, dept, "b@example.test", status: DoctorStatus.Inactive);

        var ok = Assert.IsType<OkObjectResult>(await CreateController(context, "Patient").GetAll(new DoctorFilterRequest()));

        Assert.Single(Assert.IsType<List<DoctorResponse>>(ok.Value));
    }

    [Fact]
    public async Task Update_Returns200_404_And409()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "first@example.test");
        var second = SeedDoctor(context, dept, "second@example.test");
        var controller = CreateController(context, "DoctorManager");

        Assert.IsType<OkObjectResult>(await controller.Update(second.Id, NewUpdateRequest(dept.Id, "second@example.test")));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(9999, NewUpdateRequest(dept.Id, "z@example.test")));
        Assert.IsType<ConflictObjectResult>(await controller.Update(second.Id, NewUpdateRequest(dept.Id, "first@example.test")));
    }

    [Fact]
    public async Task DeactivateThenActivate_FullLifecycle()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "cycle@example.test");
        var controller = CreateController(context, "Admin");

        Assert.IsType<OkObjectResult>(await controller.Deactivate(doctor.Id));
        Assert.IsType<OkObjectResult>(await controller.Activate(doctor.Id));
        // Activating an already-active doctor is a conflict.
        Assert.IsType<ConflictObjectResult>(await controller.Activate(doctor.Id));
    }

    [Fact]
    public async Task Deactivate_Activate_AndDelete_UnknownReturn404()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        Assert.IsType<NotFoundObjectResult>(await controller.Deactivate(9999));
        Assert.IsType<NotFoundObjectResult>(await controller.Activate(9999));
        Assert.IsType<NotFoundObjectResult>(await controller.DeletePermanently(9999));
    }

    [Fact]
    public async Task DeletePermanently_Returns200AndRemovesDoctor()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "del@example.test");

        Assert.IsType<OkObjectResult>(await CreateController(context, "Admin").DeletePermanently(doctor.Id));
        Assert.Empty(context.Doctors);
    }

    [Fact]
    public async Task CreateLogin_Returns200ThenConflictOnSecondAttempt()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "legacy@example.test");
        var controller = CreateController(context, "Admin");

        Assert.IsType<OkObjectResult>(await controller.CreateLoginForExistingDoctor(doctor.Id));
        Assert.IsType<ConflictObjectResult>(await controller.CreateLoginForExistingDoctor(doctor.Id));
        Assert.IsType<NotFoundObjectResult>(await controller.CreateLoginForExistingDoctor(9999));
    }

    [Fact]
    public async Task GetMyProfile_ReturnsProfileLinkedToSignedInDoctor()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "me@example.test", userId: 25);

        var ok = Assert.IsType<OkObjectResult>(await CreateController(context, "Doctor", userId: 25).GetMyProfile());

        Assert.Equal(doctor.Id, Assert.IsType<DoctorResponse>(ok.Value).Id);
    }

    [Fact]
    public async Task GetMyProfile_Returns404WhenNoProfileLinked()
    {
        await using var context = CreateContext();

        Assert.IsType<NotFoundObjectResult>(await CreateController(context, "Doctor", userId: 25).GetMyProfile());
    }

    [Fact]
    public async Task UpdateMyProfile_OnlyAffectsTheSignedInDoctor()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "me@example.test", userId: 25);
        var other = SeedDoctor(context, dept, "other@example.test", userId: 26);

        var ok = Assert.IsType<OkObjectResult>(await CreateController(context, "Doctor", userId: 25)
            .UpdateMyProfile(new UpdateMyDoctorProfileRequest { PhoneNumber = "0711111111", Bio = "Mine" }));

        Assert.Equal("Mine", Assert.IsType<DoctorResponse>(ok.Value).Bio);
        Assert.Equal("", (await context.Doctors.AsNoTracking().SingleAsync(d => d.Id == other.Id)).Bio);
    }

    // ── Role restrictions declared on DoctorsController ──────────────────

    [Fact]
    public void DoctorsController_RequiresAuthenticationAtClassLevel()
    {
        Assert.NotNull(typeof(DoctorsController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(DoctorsController.Create), "Admin,Staff,DoctorManager")]
    [InlineData(nameof(DoctorsController.Update), "Admin,Staff,DoctorManager")]
    [InlineData(nameof(DoctorsController.Deactivate), "Admin,DoctorManager")]
    [InlineData(nameof(DoctorsController.Activate), "Admin,DoctorManager")]
    [InlineData(nameof(DoctorsController.DeletePermanently), "Admin,DoctorManager")]
    [InlineData(nameof(DoctorsController.CreateLoginForExistingDoctor), "Admin,DoctorManager")]
    [InlineData(nameof(DoctorsController.GetMyProfile), "Doctor")]
    [InlineData(nameof(DoctorsController.UpdateMyProfile), "Doctor")]
    public void DoctorsController_ActionsAreRestrictedToExpectedRoles(string action, string expectedRoles)
    {
        var attribute = typeof(DoctorsController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRoles, attribute!.Roles);
    }

    [Theory]
    [InlineData(nameof(DoctorsController.Create))]
    [InlineData(nameof(DoctorsController.Update))]
    [InlineData(nameof(DoctorsController.Deactivate))]
    [InlineData(nameof(DoctorsController.DeletePermanently))]
    public void DoctorsController_PatientsCannotManageDoctors(string action)
    {
        var roles = typeof(DoctorsController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>()!.Roles!.Split(',');

        Assert.DoesNotContain("Patient", roles);
    }

    // ── Request validation (DataAnnotations) ─────────────────────────────

    [Theory]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("Email")]
    [InlineData("PhoneNumber")]
    [InlineData("Specialization")]
    [InlineData("LicenseNumber")]
    public void CreateDoctorRequest_MissingRequiredFieldFailsValidation(string field)
    {
        var request = NewCreateRequest(1, "valid@example.test");
        typeof(CreateDoctorRequest).GetProperty(field)!.SetValue(request, "");

        Assert.Contains(Validate(request), e => e.MemberNames.Contains(field));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(81)]
    public void CreateDoctorRequest_ExperienceOutOfRangeFailsValidation(int years)
    {
        var request = NewCreateRequest(1, "valid@example.test");
        request.YearsOfExperience = years;

        Assert.Contains(Validate(request), e => e.MemberNames.Contains(nameof(CreateDoctorRequest.YearsOfExperience)));
    }

    [Fact]
    public void CreateDoctorRequest_InvalidEmailFailsValidation()
    {
        Assert.Contains(Validate(NewCreateRequest(1, "not-an-email")), e => e.MemberNames.Contains("Email"));
    }

    [Fact]
    public void CreateDoctorRequest_ValidRequestPassesValidation()
    {
        Assert.Empty(Validate(NewCreateRequest(1, "valid@example.test")));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static DoctorsController CreateController(AppDbContext context, string role, int userId = 1) =>
        new(new DoctorService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, role)
                    }, "test"))
                }
            }
        };

    private static Department SeedDepartment(AppDbContext context)
    {
        var department = new Department
        {
            Name = "General Medicine",
            Description = "Test",
            Status = DepartmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Departments.Add(department);
        context.SaveChanges();
        return department;
    }

    private static Doctor SeedDoctor(
        AppDbContext context, Department department, string email, int? userId = null, DoctorStatus status = DoctorStatus.Active)
    {
        var doctor = new Doctor
        {
            UserId = userId,
            DepartmentId = department.Id,
            FirstName = "Kamal",
            LastName = "Silva",
            Email = email,
            PhoneNumber = "000",
            Specialization = "General Medicine",
            LicenseNumber = "SLMC-" + email,
            YearsOfExperience = 5,
            Bio = "",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Doctors.Add(doctor);
        context.SaveChanges();
        return doctor;
    }

    private static CreateDoctorRequest NewCreateRequest(int departmentId, string email) => new()
    {
        FirstName = "Kamal",
        LastName = "Silva",
        Email = email,
        PhoneNumber = "0771234567",
        DepartmentId = departmentId,
        Specialization = "General Medicine",
        LicenseNumber = "SLMC-12345",
        YearsOfExperience = 8,
        Bio = "Experienced physician"
    };

    private static UpdateDoctorRequest NewUpdateRequest(int departmentId, string email) => new()
    {
        FirstName = "Kamal",
        LastName = "Silva",
        Email = email,
        PhoneNumber = "0771234567",
        DepartmentId = departmentId,
        Specialization = "General Medicine",
        LicenseNumber = "SLMC-12345",
        YearsOfExperience = 8,
        Bio = ""
    };

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
