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
using SmartHospital.Api.DTOs.Users;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for AuthController and UsersController wired to the real
/// AuthService, JwtService and UserService over an in-memory database.
/// </summary>
public class AuthAndUsersControllerTests
{
    // ── AuthController ───────────────────────────────────────────────────

    [Fact]
    public async Task Register_Returns201WithLocationAndToken()
    {
        await using var context = CreateContext();
        var controller = CreateAuthController(context);

        var result = await controller.Register(NewRegisterRequest("new@example.test"));

        var created = Assert.IsType<CreatedResult>(result);
        var body = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal($"/api/users/{body.UserId}", created.Location);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
    }

    [Fact]
    public async Task Register_DuplicateEmailReturns409()
    {
        await using var context = CreateContext();
        var controller = CreateAuthController(context);
        await controller.Register(NewRegisterRequest("dup@example.test"));

        var result = await controller.Register(NewRegisterRequest("dup@example.test"));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentialsReturns200()
    {
        await using var context = CreateContext();
        var controller = CreateAuthController(context);
        await controller.Register(NewRegisterRequest("ok@example.test"));

        var result = await controller.Login(new LoginRequest { Email = "ok@example.test", Password = "Password123!" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ok@example.test", Assert.IsType<AuthResponse>(ok.Value).Email);
    }

    [Fact]
    public async Task Login_WrongPasswordReturns401()
    {
        await using var context = CreateContext();
        var controller = CreateAuthController(context);
        await controller.Register(NewRegisterRequest("ok@example.test"));

        var result = await controller.Login(new LoginRequest { Email = "ok@example.test", Password = "nope-nope" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPasswordGiveTheSameResponse()
    {
        // Prevents user enumeration: both failures must look identical to the caller.
        await using var context = CreateContext();
        var controller = CreateAuthController(context);
        await controller.Register(NewRegisterRequest("ok@example.test"));

        var unknown = Assert.IsType<UnauthorizedObjectResult>(
            await controller.Login(new LoginRequest { Email = "ghost@example.test", Password = "Password123!" }));
        var wrong = Assert.IsType<UnauthorizedObjectResult>(
            await controller.Login(new LoginRequest { Email = "ok@example.test", Password = "bad-password" }));

        Assert.Equal(MessageOf(unknown.Value), MessageOf(wrong.Value));
        Assert.Equal("Invalid email or password.", MessageOf(unknown.Value));
    }

    [Fact]
    public async Task Login_DeactivatedUserReturns401()
    {
        await using var context = CreateContext();
        var auth = CreateAuthController(context);
        var created = Assert.IsType<AuthResponse>(
            Assert.IsType<CreatedResult>(await auth.Register(NewRegisterRequest("gone@example.test"))).Value);
        await CreateUsersController(context, "Admin").Deactivate(created.UserId);

        var result = await auth.Login(new LoginRequest { Email = "gone@example.test", Password = "Password123!" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void AuthController_EndpointsAllowAnonymousAccess()
    {
        Assert.Null(typeof(AuthController).GetCustomAttribute<AuthorizeAttribute>());
    }

    // ── Request validation (DataAnnotations) ─────────────────────────────

    [Theory]
    [InlineData("", "Perera", "a@example.test", "Password123!", "077", "FirstName")]
    [InlineData("Nimal", "", "a@example.test", "Password123!", "077", "LastName")]
    [InlineData("Nimal", "Perera", "not-an-email", "Password123!", "077", "Email")]
    [InlineData("Nimal", "Perera", "a@example.test", "short", "077", "Password")]
    [InlineData("Nimal", "Perera", "a@example.test", "Password123!", "", "PhoneNumber")]
    public void RegisterRequest_InvalidFieldFailsValidation(
        string first, string last, string email, string password, string phone, string expectedMember)
    {
        var errors = Validate(new RegisterRequest
        {
            FirstName = first, LastName = last, Email = email, Password = password, PhoneNumber = phone
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(expectedMember));
    }

    [Fact]
    public void RegisterRequest_ValidRequestPassesValidation()
    {
        Assert.Empty(Validate(NewRegisterRequest("valid@example.test")));
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("bad-email", "Password123!")]
    [InlineData("a@example.test", "")]
    public void LoginRequest_MissingOrInvalidFieldsFailValidation(string email, string password)
    {
        Assert.NotEmpty(Validate(new LoginRequest { Email = email, Password = password }));
    }

    [Fact]
    public void CreateDoctorManagerRequest_PasswordShorterThanSixFails()
    {
        var errors = Validate(new CreateDoctorManagerRequest
        {
            FirstName = "A", LastName = "B", Email = "dm@example.test", Password = "12345"
        });

        Assert.Contains(errors, e => e.MemberNames.Contains("Password"));
    }

    // ── UsersController ──────────────────────────────────────────────────

    [Fact]
    public async Task Users_GetAllReturnsOkWithUsers()
    {
        await using var context = CreateContext();
        SeedUser(context, "one@example.test", UserRole.Patient);
        SeedUser(context, "two@example.test", UserRole.Doctor);

        var result = await CreateUsersController(context, "Admin").GetAll();

        var users = Assert.IsType<List<UserResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task Users_ResourceAdminOnlySeesPatientsEvenWhenAskingForDoctors()
    {
        await using var context = CreateContext();
        SeedUser(context, "p@example.test", UserRole.Patient);
        SeedUser(context, "d@example.test", UserRole.Doctor);
        SeedUser(context, "a@example.test", UserRole.Admin);

        var result = await CreateUsersController(context, "ResourceAdmin").GetAll(UserRole.Doctor);

        var users = Assert.IsType<List<UserResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(users);
        Assert.Equal("Patient", users[0].Role);
    }

    [Fact]
    public async Task Users_GetByIdUnknownReturns404()
    {
        await using var context = CreateContext();

        Assert.IsType<NotFoundObjectResult>(await CreateUsersController(context, "Admin").GetById(123));
    }

    [Fact]
    public async Task Users_GetByIdKnownReturns200()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "known@example.test", UserRole.Patient);

        var ok = Assert.IsType<OkObjectResult>(await CreateUsersController(context, "Admin").GetById(user.Id));

        Assert.Equal(user.Id, Assert.IsType<UserResponse>(ok.Value).Id);
    }

    // Regression: BUG-002
    [Fact]
    public async Task Users_PatientCannotViewAnotherUsersDetails()
    {
        await using var context = CreateContext();
        var me = SeedUser(context, "me@example.test", UserRole.Patient);
        var other = SeedUser(context, "other@example.test", UserRole.Patient);

        var result = await CreateUsersController(context, "Patient", me.Id).GetById(other.Id);

        Assert.IsType<ForbidResult>(result);
    }

    // Regression: BUG-002
    [Fact]
    public async Task Users_PatientCannotEditAnotherUsersProfile()
    {
        await using var context = CreateContext();
        var me = SeedUser(context, "me@example.test", UserRole.Patient);
        var other = SeedUser(context, "other@example.test", UserRole.Patient);

        var result = await CreateUsersController(context, "Patient", me.Id).Update(
            other.Id, new UpdateUserRequest { FirstName = "Hacked", LastName = "Name", PhoneNumber = "0" });

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("Test", (await context.Users.AsNoTracking().SingleAsync(u => u.Id == other.Id)).FirstName);
    }

    [Fact]
    public async Task Users_UserCanViewAndEditOwnProfile()
    {
        await using var context = CreateContext();
        var me = SeedUser(context, "me@example.test", UserRole.Patient);
        var controller = CreateUsersController(context, "Patient", me.Id);

        Assert.IsType<OkObjectResult>(await controller.GetById(me.Id));
        Assert.IsType<OkObjectResult>(await controller.Update(
            me.Id, new UpdateUserRequest { FirstName = "New", LastName = "Name", PhoneNumber = "0" }));
    }

    [Fact]
    public async Task Users_UpdateUnknownReturns404()
    {
        await using var context = CreateContext();

        var result = await CreateUsersController(context, "Admin").Update(
            123, new UpdateUserRequest { FirstName = "A", LastName = "B", PhoneNumber = "1" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Users_DeactivateThenActivateReturns200()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "toggle@example.test", UserRole.Staff);
        var controller = CreateUsersController(context, "Admin");

        Assert.IsType<OkObjectResult>(await controller.Deactivate(user.Id));
        Assert.Equal(UserStatus.Inactive, (await context.Users.AsNoTracking().SingleAsync()).Status);

        Assert.IsType<OkObjectResult>(await controller.Activate(user.Id));
        Assert.Equal(UserStatus.Active, (await context.Users.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Users_DeactivateAndActivateUnknownReturn404()
    {
        await using var context = CreateContext();
        var controller = CreateUsersController(context, "Admin");

        Assert.IsType<NotFoundObjectResult>(await controller.Deactivate(999));
        Assert.IsType<NotFoundObjectResult>(await controller.Activate(999));
    }

    [Fact]
    public async Task Users_CreateDoctorManagerReturns201ThenDuplicateReturns409()
    {
        await using var context = CreateContext();
        var controller = CreateUsersController(context, "Admin");
        var request = new CreateDoctorManagerRequest
        {
            FirstName = "Dilani", LastName = "Fernando", Email = "dm@example.test", Password = "Secret123", PhoneNumber = "071"
        };

        Assert.IsType<CreatedResult>(await controller.CreateDoctorManager(request));
        Assert.IsType<ConflictObjectResult>(await controller.CreateDoctorManager(request));
    }

    [Fact]
    public async Task Users_CreateAppointmentManagerReturns201()
    {
        await using var context = CreateContext();

        var result = await CreateUsersController(context, "Admin").CreateAppointmentManager(new CreateAppointmentManagerRequest
        {
            FirstName = "Ruwan", LastName = "J", Email = "am@example.test", Password = "Secret123", PhoneNumber = "072"
        });

        var body = Assert.IsType<UserResponse>(Assert.IsType<CreatedResult>(result).Value);
        Assert.Equal("AppointmentManager", body.Role);
    }

    [Fact]
    public async Task Users_NewDoctorManagerCanLogInWithAssignedRole()
    {
        await using var context = CreateContext();
        await CreateUsersController(context, "Admin").CreateDoctorManager(new CreateDoctorManagerRequest
        {
            FirstName = "D", LastName = "M", Email = "dm@example.test", Password = "Secret123", PhoneNumber = ""
        });

        var ok = Assert.IsType<OkObjectResult>(
            await CreateAuthController(context).Login(new LoginRequest { Email = "dm@example.test", Password = "Secret123" }));

        Assert.Equal("DoctorManager", Assert.IsType<AuthResponse>(ok.Value).Role);
    }

    [Fact]
    public async Task Users_CreateWalkInPatientCreatesActivePatientWithoutUsableLogin()
    {
        await using var context = CreateContext();

        var result = await CreateUsersController(context, "Staff").CreateWalkInPatient(new CreateWalkInPatientRequest { FullName = "  Kamala  Devi Silva " });

        var body = Assert.IsType<PatientSearchResult>(Assert.IsType<CreatedResult>(result).Value);
        Assert.Equal("Kamala Devi Silva", body.DisplayName);
        var stored = await context.Users.SingleAsync(u => u.Id == body.Id);
        Assert.Equal(UserRole.Patient, stored.Role);
        Assert.Equal(UserStatus.Active, stored.Status);
        Assert.Equal(("Kamala", "Devi Silva"), (stored.FirstName, stored.LastName));
        Assert.EndsWith("@walkin.invalid", stored.Email);
        Assert.False(BCrypt.Net.BCrypt.Verify("", stored.PasswordHash));
    }

    [Fact]
    public async Task Users_CreateWalkInPatientAcceptsSingleWordNameAndIsSearchable()
    {
        await using var context = CreateContext();
        var controller = CreateUsersController(context, "Admin");

        var body = Assert.IsType<PatientSearchResult>(Assert.IsType<CreatedResult>(
            await controller.CreateWalkInPatient(new CreateWalkInPatientRequest { FullName = "Kamal" })).Value);
        Assert.Equal("Kamal", body.DisplayName);

        var second = Assert.IsType<PatientSearchResult>(Assert.IsType<CreatedResult>(
            await controller.CreateWalkInPatient(new CreateWalkInPatientRequest { FullName = "Kamal" })).Value);
        Assert.NotEqual(body.Id, second.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("K")]
    [InlineData("Kamal123")]
    [InlineData("<b>Kamal</b>")]
    public void CreateWalkInPatientRequest_InvalidNamesFail(string name)
    {
        Assert.NotEmpty(Validate(new CreateWalkInPatientRequest { FullName = name }));
    }

    // ── Role restrictions declared on UsersController ────────────────────

    [Fact]
    public void UsersController_RequiresAuthenticationAtClassLevel()
    {
        Assert.NotNull(typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(UsersController.CreateAppointmentManager), "Admin")]
    [InlineData(nameof(UsersController.CreateDoctorManager), "Admin")]
    [InlineData(nameof(UsersController.Deactivate), "Admin")]
    [InlineData(nameof(UsersController.Activate), "Admin")]
    [InlineData(nameof(UsersController.GetAll), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(UsersController.SearchPatients), "Admin,Staff,ClinicalCareManager")]
    [InlineData(nameof(UsersController.CreateWalkInPatient), "Admin,Staff")]
    public void UsersController_ActionsAreRestrictedToExpectedRoles(string action, string expectedRoles)
    {
        var attribute = typeof(UsersController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRoles, attribute!.Roles);
    }

    [Theory]
    [InlineData(nameof(UsersController.CreateDoctorManager))]
    [InlineData(nameof(UsersController.CreateAppointmentManager))]
    [InlineData(nameof(UsersController.Deactivate))]
    public void UsersController_PatientsAndDoctorsCannotManageAccounts(string action)
    {
        var roles = typeof(UsersController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>()!.Roles!.Split(',');

        Assert.DoesNotContain("Patient", roles);
        Assert.DoesNotContain("Doctor", roles);
        Assert.DoesNotContain("Staff", roles);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthController CreateAuthController(AppDbContext context) =>
        new(new AuthService(context, new JwtService(Options.Create(new JwtSettings
        {
            Key = "integration-test-signing-key-at-least-32-characters",
            Issuer = "SmartHospital.Tests",
            Audience = "SmartHospital.Tests",
            ExpiryMinutes = 60
        }))));

    private static UsersController CreateUsersController(AppDbContext context, string role, int userId = 1) =>
        new(new UserService(context))
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

    private static RegisterRequest NewRegisterRequest(string email) => new()
    {
        FirstName = "Nimal",
        LastName = "Perera",
        Email = email,
        Password = "Password123!",
        PhoneNumber = "0771234567"
    };

    private static User SeedUser(AppDbContext context, string email, UserRole role)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PhoneNumber = "000",
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static string? MessageOf(object? value) =>
        value?.GetType().GetProperty("message")?.GetValue(value) as string;
}
