using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Auth;
using SmartHospital.Api.DTOs.Users;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class UserServiceTests
{
    // ── Queries ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsersOrderedById()
    {
        await using var context = CreateContext();
        SeedUser(context, "c@example.test", UserRole.Admin);
        SeedUser(context, "a@example.test", UserRole.Patient);
        SeedUser(context, "b@example.test", UserRole.Doctor);

        var users = await new UserService(context).GetAllAsync();

        Assert.Equal(3, users.Count);
        Assert.Equal(users.Select(u => u.Id).OrderBy(id => id), users.Select(u => u.Id));
    }

    [Fact]
    public async Task GetAllAsync_FiltersByRole()
    {
        await using var context = CreateContext();
        SeedUser(context, "p1@example.test", UserRole.Patient);
        SeedUser(context, "p2@example.test", UserRole.Patient);
        SeedUser(context, "d1@example.test", UserRole.Doctor);

        var patients = await new UserService(context).GetAllAsync(UserRole.Patient);

        Assert.Equal(2, patients.Count);
        Assert.All(patients, p => Assert.Equal("Patient", p.Role));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyListWhenNoUsers()
    {
        await using var context = CreateContext();

        Assert.Empty(await new UserService(context).GetAllAsync());
    }

    [Fact]
    public void UserResponse_DoesNotExposePasswordHash()
    {
        // UserResponse must not carry credential material back to clients.
        Assert.Null(typeof(UserResponse).GetProperty("PasswordHash"));
        Assert.Null(typeof(UserResponse).GetProperty("Password"));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedUser()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "find@example.test", UserRole.Staff);

        var result = await new UserService(context).GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("find@example.test", result!.Email);
        Assert.Equal("Staff", result.Role);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForUnknownId()
    {
        await using var context = CreateContext();

        Assert.Null(await new UserService(context).GetByIdAsync(999));
    }

    // ── Manager account creation ─────────────────────────────────────────

    [Fact]
    public async Task CreateDoctorManagerAsync_CreatesActiveDoctorManager()
    {
        await using var context = CreateContext();

        var result = await new UserService(context).CreateDoctorManagerAsync(new CreateDoctorManagerRequest
        {
            FirstName = " Dilani ",
            LastName = " Fernando ",
            Email = " DM@Example.Test ",
            Password = "Secret123",
            PhoneNumber = " 0711111111 "
        });

        Assert.Equal("DoctorManager", result.Role);
        Assert.Equal("Active", result.Status);
        Assert.Equal("dm@example.test", result.Email);
        Assert.Equal("Dilani", result.FirstName);
        Assert.Equal("0711111111", result.PhoneNumber);
    }

    [Fact]
    public async Task CreateAppointmentManagerAsync_CreatesActiveAppointmentManager()
    {
        await using var context = CreateContext();

        var result = await new UserService(context).CreateAppointmentManagerAsync(new CreateAppointmentManagerRequest
        {
            FirstName = "Ruwan",
            LastName = "Jayasinghe",
            Email = "am@example.test",
            Password = "Secret123",
            PhoneNumber = "0722222222"
        });

        Assert.Equal("AppointmentManager", result.Role);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task CreateDoctorManagerAsync_HashesPassword()
    {
        await using var context = CreateContext();

        var result = await new UserService(context).CreateDoctorManagerAsync(new CreateDoctorManagerRequest
        {
            FirstName = "A", LastName = "B", Email = "hash@example.test", Password = "Secret123", PhoneNumber = ""
        });

        var saved = await context.Users.SingleAsync(u => u.Id == result.Id);
        Assert.NotEqual("Secret123", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Secret123", saved.PasswordHash));
    }

    [Fact]
    public async Task CreateDoctorManagerAsync_RejectsEmailAlreadyUsed()
    {
        await using var context = CreateContext();
        SeedUser(context, "taken@example.test", UserRole.Patient);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UserService(context).CreateDoctorManagerAsync(new CreateDoctorManagerRequest
            {
                FirstName = "A", LastName = "B", Email = "TAKEN@example.test", Password = "Secret123", PhoneNumber = ""
            }));

        Assert.Equal("A user with this email already exists.", ex.Message);
    }

    [Fact]
    public async Task CreateAppointmentManagerAsync_RejectsEmailAlreadyUsed()
    {
        await using var context = CreateContext();
        SeedUser(context, "taken@example.test", UserRole.Doctor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UserService(context).CreateAppointmentManagerAsync(new CreateAppointmentManagerRequest
            {
                FirstName = "A", LastName = "B", Email = "taken@example.test", Password = "Secret123", PhoneNumber = ""
            }));
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesNameAndPhoneTrimmed()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "upd@example.test", UserRole.Patient);
        var before = user.UpdatedAt;

        var result = await new UserService(context).UpdateAsync(user.Id, new UpdateUserRequest
        {
            FirstName = "  Sunil ",
            LastName = " Bandara ",
            PhoneNumber = " 0779999999 "
        });

        Assert.NotNull(result);
        Assert.Equal("Sunil", result!.FirstName);
        Assert.Equal("Bandara", result.LastName);
        Assert.Equal("0779999999", result.PhoneNumber);
        Assert.True(result.UpdatedAt >= before);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotChangeEmailRoleOrStatus()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "keep@example.test", UserRole.Patient);

        var result = await new UserService(context).UpdateAsync(user.Id, new UpdateUserRequest
        {
            FirstName = "X", LastName = "Y", PhoneNumber = "1"
        });

        Assert.Equal("keep@example.test", result!.Email);
        Assert.Equal("Patient", result.Role);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullForUnknownId()
    {
        await using var context = CreateContext();

        var result = await new UserService(context).UpdateAsync(404, new UpdateUserRequest
        {
            FirstName = "X", LastName = "Y", PhoneNumber = "1"
        });

        Assert.Null(result);
    }

    // ── Activate / deactivate ────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_SetsStatusInactive()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "deact@example.test", UserRole.Patient);

        var ok = await new UserService(context).DeactivateAsync(user.Id);

        Assert.True(ok);
        Assert.Equal(UserStatus.Inactive, (await context.Users.SingleAsync()).Status);
    }

    [Fact]
    public async Task DeactivateAsync_DoesNotDeleteTheUserRow()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "keeprow@example.test", UserRole.Patient);

        await new UserService(context).DeactivateAsync(user.Id);

        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task DeactivateAsync_ReturnsFalseForUnknownId()
    {
        await using var context = CreateContext();

        Assert.False(await new UserService(context).DeactivateAsync(404));
    }

    [Fact]
    public async Task ActivateAsync_SetsStatusActive()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "act@example.test", UserRole.Patient, UserStatus.Inactive);

        var ok = await new UserService(context).ActivateAsync(user.Id);

        Assert.True(ok);
        Assert.Equal(UserStatus.Active, (await context.Users.SingleAsync()).Status);
    }

    [Fact]
    public async Task ActivateAsync_ReturnsFalseForUnknownId()
    {
        await using var context = CreateContext();

        Assert.False(await new UserService(context).ActivateAsync(404));
    }

    [Fact]
    public async Task DeactivatedUser_CannotLogInUntilReactivated()
    {
        await using var context = CreateContext();
        var user = SeedUser(context, "cycle@example.test", UserRole.Staff);
        var users = new UserService(context);
        var auth = new AuthService(context, new JwtService(Options.Create(new JwtSettings
        {
            Key = "unit-test-signing-key-that-is-at-least-32-characters",
            Issuer = "t",
            Audience = "t",
            ExpiryMinutes = 5
        })));
        var login = new LoginRequest { Email = "cycle@example.test", Password = "Password123!" };

        await users.DeactivateAsync(user.Id);
        Assert.Null(await auth.LoginAsync(login));

        await users.ActivateAsync(user.Id);
        Assert.NotNull(await auth.LoginAsync(login));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static User SeedUser(AppDbContext context, string email, UserRole role, UserStatus status = UserStatus.Active)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PhoneNumber = "000",
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }
}
