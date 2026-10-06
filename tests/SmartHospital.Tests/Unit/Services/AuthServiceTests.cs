using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Auth;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class AuthServiceTests
{
    private const string Password = "Password123!";

    // ── Register ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_CreatesActivePatientAndReturnsToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RegisterAsync(NewRegisterRequest("nimal@example.test"));

        Assert.True(result.UserId > 0);
        Assert.Equal("Patient", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        var saved = await context.Users.SingleAsync();
        Assert.Equal(UserRole.Patient, saved.Role);
        Assert.Equal(UserStatus.Active, saved.Status);
    }

    [Fact]
    public async Task RegisterAsync_NormalisesEmailToLowercaseAndTrims()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RegisterAsync(NewRegisterRequest("  Nimal.Perera@Example.TEST  "));

        Assert.Equal("nimal.perera@example.test", result.Email);
        Assert.Equal("nimal.perera@example.test", (await context.Users.SingleAsync()).Email);
    }

    [Fact]
    public async Task RegisterAsync_TrimsNameAndPhoneFields()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var request = NewRegisterRequest("trim@example.test");
        request.FirstName = "  Nimal ";
        request.LastName = " Perera  ";
        request.PhoneNumber = " 0771234567 ";

        await service.RegisterAsync(request);

        var saved = await context.Users.SingleAsync();
        Assert.Equal("Nimal", saved.FirstName);
        Assert.Equal("Perera", saved.LastName);
        Assert.Equal("0771234567", saved.PhoneNumber);
    }

    [Fact]
    public async Task RegisterAsync_StoresBCryptHashNotPlainPassword()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.RegisterAsync(NewRegisterRequest("hash@example.test"));

        var saved = await context.Users.SingleAsync();
        Assert.NotEqual(Password, saved.PasswordHash);
        Assert.StartsWith("$2", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, saved.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(NewRegisterRequest("dup@example.test"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(NewRegisterRequest("dup@example.test")));

        Assert.Equal("A user with this email already exists.", ex.Message);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmailWithDifferentCase()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(NewRegisterRequest("case@example.test"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(NewRegisterRequest("CASE@Example.Test")));
    }

    [Fact]
    public async Task RegisterAsync_AlwaysAssignsPatientRole()
    {
        // Self-registration must never grant staff or admin privileges.
        await using var context = CreateContext();
        var service = CreateService(context);

        var first = await service.RegisterAsync(NewRegisterRequest("a@example.test"));
        var second = await service.RegisterAsync(NewRegisterRequest("b@example.test"));

        Assert.Equal("Patient", first.Role);
        Assert.Equal("Patient", second.Role);
        Assert.All(await context.Users.ToListAsync(), u => Assert.Equal(UserRole.Patient, u.Role));
    }

    [Fact]
    public async Task RegisterAsync_TokenContainsUserIdEmailAndRoleClaims()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RegisterAsync(NewRegisterRequest("claims@example.test"));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(result.UserId.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("claims@example.test", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Patient", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    // ── Login ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SucceedsWithCorrectCredentials()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "login@example.test", UserRole.Doctor);
        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest { Email = "login@example.test", Password = Password });

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.UserId);
        Assert.Equal("Doctor", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task LoginAsync_IsCaseInsensitiveAndTrimsEmail()
    {
        await using var context = CreateContext();
        SeedUser(context, "login@example.test", UserRole.Patient);
        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest { Email = "  LOGIN@Example.Test ", Password = Password });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForWrongPassword()
    {
        await using var context = CreateContext();
        SeedUser(context, "login@example.test", UserRole.Patient);
        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest { Email = "login@example.test", Password = "WrongPass1!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_PasswordIsCaseSensitive()
    {
        await using var context = CreateContext();
        SeedUser(context, "login@example.test", UserRole.Patient);
        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest { Email = "login@example.test", Password = Password.ToUpperInvariant() });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForUnknownEmail()
    {
        await using var context = CreateContext();
        SeedUser(context, "login@example.test", UserRole.Patient);
        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest { Email = "nobody@example.test", Password = Password });

        Assert.Null(result);
    }

    [Theory]
    [InlineData(UserStatus.Inactive)]
    [InlineData(UserStatus.Suspended)]
    public async Task LoginAsync_ReturnsNullForNonActiveAccount(UserStatus status)
    {
        await using var context = CreateContext();
        SeedUser(context, "blocked@example.test", UserRole.Patient, status);
        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest { Email = "blocked@example.test", Password = Password });

        Assert.Null(result);
    }

    [Theory]
    [InlineData(UserRole.Patient)]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.Staff)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.AppointmentManager)]
    [InlineData(UserRole.ResourceAdmin)]
    [InlineData(UserRole.DoctorManager)]
    public async Task LoginAsync_ReturnsTheUsersRoleInResponseAndToken(UserRole role)
    {
        await using var context = CreateContext();
        SeedUser(context, "role@example.test", role);
        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest { Email = "role@example.test", Password = Password });

        Assert.NotNull(result);
        Assert.Equal(role.ToString(), result!.Role);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(role.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task RegisterThenLogin_WorksEndToEnd()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var registered = await service.RegisterAsync(NewRegisterRequest("flow@example.test"));

        var loggedIn = await service.LoginAsync(new LoginRequest { Email = "flow@example.test", Password = Password });

        Assert.NotNull(loggedIn);
        Assert.Equal(registered.UserId, loggedIn!.UserId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthService CreateService(AppDbContext context) =>
        new(context, new JwtService(Options.Create(new JwtSettings
        {
            Key = "unit-test-signing-key-that-is-at-least-32-characters",
            Issuer = "SmartHospital.Tests",
            Audience = "SmartHospital.Tests",
            ExpiryMinutes = 60
        })));

    private static RegisterRequest NewRegisterRequest(string email) => new()
    {
        FirstName = "Nimal",
        LastName = "Perera",
        Email = email,
        Password = Password,
        PhoneNumber = "0771234567"
    };

    private static User SeedUser(AppDbContext context, string email, UserRole role, UserStatus status = UserStatus.Active)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            PhoneNumber = "000",
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }
}
