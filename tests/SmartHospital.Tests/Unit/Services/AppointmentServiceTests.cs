using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Appointments;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartHospital.Tests.Unit.Services;

/// <summary>
/// Unit tests for AppointmentService covering:
///   - Slot validation (future-only, conflict detection)
///   - Status-transition rules (including negative/invalid paths)
///   - Double-booking prevention
///   - Role-based access (patients cannot see other patients' appointments)
/// </summary>
public class AppointmentServiceTests
{
    // ── Test infrastructure ───────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static (AppointmentService svc, AppDbContext ctx) BuildService(AppDbContext ctx)
    {
        var availSvc = new AvailabilityService(ctx);
        var notifSvc = new NotificationService(ctx, NullLogger<NotificationService>.Instance);
        var svc = new AppointmentService(
            ctx, availSvc, notifSvc,
            NullLogger<AppointmentService>.Instance);
        return (svc, ctx);
    }

    private static async Task<(User patient, User doctor)> SeedUsersAsync(AppDbContext ctx)
    {
        var patient = new User
        {
            Id = 1, FirstName = "Alice", LastName = "Patient",
            Email = "alice@test.com", PasswordHash = "hash",
            Role = UserRole.Patient, Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var doctor = new User
        {
            Id = 2, FirstName = "Bob", LastName = "Doctor",
            Email = "bob@test.com", PasswordHash = "hash",
            Role = UserRole.Doctor, Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Users.AddRange(patient, doctor);
        await ctx.SaveChangesAsync();
        return (patient, doctor);
    }

    // ── Happy path: booking ───────────────────────────────────────────────────

    [Fact]
    public async Task BookAppointment_ValidSlot_CreatesAppointmentWithReferenceAndQueueNumber()
    {
        // Arrange
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var request = new CreateAppointmentRequest
        {
            PatientId = 1,
            DoctorId  = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Priority = AppointmentPriority.Normal
        };

        // Act
        var result = await svc.BookAppointmentAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("APT-", result.ReferenceNumber);
        Assert.Equal(1, result.QueueNumber);
        Assert.Equal("Scheduled", result.Status);
    }

    // ── Slot must be in the future ────────────────────────────────────────────

    [Fact]
    public async Task BookAppointment_PastSlot_ThrowsInvalidOperationException()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var request = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = DateTime.UtcNow.AddHours(-1),
            EstimatedDurationMinutes = 30
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.BookAppointmentAsync(1, request));
    }

    // ── Double-booking prevention ─────────────────────────────────────────────

    [Fact]
    public async Task BookAppointment_DoubleBooking_ThrowsInvalidOperationException()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var slot  = DateTime.UtcNow.AddDays(1);
        var first = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = slot,
            EstimatedDurationMinutes = 30
        };

        // Book the slot once — should succeed
        await svc.BookAppointmentAsync(1, first);

        // Attempt to book the same slot with an overlapping time — should fail
        var second = new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = slot.AddMinutes(10), // still overlaps the first 30-min block
            EstimatedDurationMinutes = 30
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.BookAppointmentAsync(1, second));
    }

    // ── Non-overlapping consecutive slots SHOULD succeed ─────────────────────

    [Fact]
    public async Task BookAppointment_ConsecutiveNonOverlappingSlots_BothSucceed()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var slot1 = DateTime.UtcNow.AddDays(1);
        var slot2 = slot1.AddMinutes(30); // starts exactly when first ends

        var r1 = await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = slot1,
            EstimatedDurationMinutes = 30
        });

        var r2 = await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = slot2,
            EstimatedDurationMinutes = 30
        });

        Assert.NotNull(r1);
        Assert.NotNull(r2);
        Assert.Equal(1, r1.QueueNumber);
        Assert.Equal(2, r2.QueueNumber);
    }

    // ── Invalid patient ───────────────────────────────────────────────────────

    [Fact]
    public async Task BookAppointment_InactivePatient_ThrowsInvalidOperationException()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);

        // Only seed doctor — no patient
        ctx.Users.Add(new User
        {
            Id = 2, FirstName = "Bob", LastName = "Doc",
            Email = "doc@test.com", PasswordHash = "hash",
            Role = UserRole.Doctor, Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.BookAppointmentAsync(99, new CreateAppointmentRequest
            {
                PatientId = 99, DoctorId = 2,
                AppointmentType = AppointmentType.General,
                ScheduledStart  = DateTime.UtcNow.AddDays(1)
            }));
    }

    // ── Invalid status transition: cancel a completed appointment ─────────────

    [Fact]
    public async Task CancelAppointment_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        // Manually insert a Completed appointment
        var apt = new Appointment
        {
            PatientId = 1, DoctorId = 2,
            ReferenceNumber = "APT-TEST-0001",
            ScheduledStart  = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Status    = AppointmentStatus.Completed,
            Priority  = AppointmentPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(apt);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelAppointmentAsync(apt.Id,
                new CancelAppointmentRequest { Reason = "Test" },
                requestingUserId: 1, requestingUserRole: "Staff"));
    }

    // ── Invalid status transition: cancel a cancelled appointment ─────────────

    [Fact]
    public async Task CancelAppointment_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var apt = new Appointment
        {
            PatientId = 1, DoctorId = 2,
            ReferenceNumber = "APT-TEST-0002",
            ScheduledStart  = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Status    = AppointmentStatus.Cancelled,
            Priority  = AppointmentPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(apt);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelAppointmentAsync(apt.Id,
                new CancelAppointmentRequest { Reason = "Test" },
                requestingUserId: 1, requestingUserRole: "Staff"));
    }

    // ── Rescheduling to a past slot must fail ─────────────────────────────────

    [Fact]
    public async Task RescheduleAppointment_PastSlot_ThrowsInvalidOperationException()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var apt = new Appointment
        {
            PatientId = 1, DoctorId = 2,
            ReferenceNumber = "APT-TEST-0003",
            ScheduledStart  = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Status    = AppointmentStatus.Scheduled,
            Priority  = AppointmentPriority.Normal,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(apt);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RescheduleAppointmentAsync(apt.Id,
                new RescheduleAppointmentRequest
                {
                    NewScheduledStart = DateTime.UtcNow.AddHours(-2)
                },
                requestingUserId: 1, requestingUserRole: "Patient"));
    }

    // ── Patient cannot view another patient's appointment ────────────────────

    [Fact]
    public async Task GetAppointmentById_PatientViewsOthers_ThrowsUnauthorized()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        // Seed a third user (another patient)
        ctx.Users.Add(new User
        {
            Id = 3, FirstName = "Carol", LastName = "Other",
            Email = "carol@test.com", PasswordHash = "hash",
            Role = UserRole.Patient, Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        var apt = new Appointment
        {
            PatientId = 3, DoctorId = 2,
            ReferenceNumber = "APT-TEST-0004",
            ScheduledStart  = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Status    = AppointmentStatus.Scheduled,
            Priority  = AppointmentPriority.Normal,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(apt);
        await ctx.SaveChangesAsync();

        // User 1 (Patient) tries to read Patient 3's appointment
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.GetAppointmentByIdAsync(apt.Id,
                requestingUserId: 1, requestingUserRole: "Patient"));
    }
}
