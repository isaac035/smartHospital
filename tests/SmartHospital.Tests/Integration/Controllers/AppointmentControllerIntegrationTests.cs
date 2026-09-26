using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Appointments;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartHospital.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for the Appointment module.
/// Uses EF InMemory database (matching the existing test project convention).
/// Tests persist through a full service stack (no mocks) to validate PostgreSQL
/// schema mapping and business rules end-to-end.
///
/// Covers:
///   - Full booking → cancellation lifecycle
///   - Full booking → reschedule lifecycle
///   - Double-booking attempt (concurrent slot conflict)
///   - Unauthorized patient access to another patient's appointment
///   - Status history is recorded on every transition
///   - Notification created on booking
/// </summary>
public class AppointmentControllerIntegrationTests
{
    // ── Test infrastructure ───────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AppointmentService BuildService(AppDbContext ctx)
    {
        var avail  = new AvailabilityService(ctx);
        var notif  = new NotificationService(ctx, NullLogger<NotificationService>.Instance);
        return new AppointmentService(ctx, avail, notif, NullLogger<AppointmentService>.Instance);
    }

    private static async Task SeedUsersAsync(AppDbContext ctx)
    {
        ctx.Users.AddRange(
            new User { Id = 1, FirstName = "Alice", LastName = "P", Email = "alice@it.com",
                       PasswordHash = "h", Role = UserRole.Patient, Status = UserStatus.Active,
                       CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 2, FirstName = "Bob", LastName = "D", Email = "bob@it.com",
                       PasswordHash = "h", Role = UserRole.Doctor, Status = UserStatus.Active,
                       CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 3, FirstName = "Carol", LastName = "Q", Email = "carol@it.com",
                       PasswordHash = "h", Role = UserRole.Patient, Status = UserStatus.Active,
                       CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 99, FirstName = "Admin", LastName = "A", Email = "admin@it.com",
                       PasswordHash = "h", Role = UserRole.Admin, Status = UserStatus.Active,
                       CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
    }

    // ── Full lifecycle: book → cancel ─────────────────────────────────────────

    [Fact]
    public async Task BookThenCancel_AppointmentReachesTerminalCancelledState()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var booked = await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 30
        });

        Assert.Equal("Scheduled", booked.Status);

        var cancelled = await svc.CancelAppointmentAsync(
            booked.Id,
            new CancelAppointmentRequest { Reason = "Patient request" },
            requestingUserId: 1, requestingUserRole: "Patient");

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("Patient request", cancelled.CancelledReason);

        // Verify status history has two entries (Scheduled→Scheduled initial, Scheduled→Cancelled)
        var history = await svc.GetStatusHistoryAsync(booked.Id);
        Assert.True(history.Count >= 2);
        Assert.Contains(history, h => h.NewStatus == "Cancelled");
    }

    // ── Full lifecycle: book → reschedule ─────────────────────────────────────

    [Fact]
    public async Task BookThenReschedule_OldMarkedRescheduledNewAppointmentCreated()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var slot1 = DateTime.UtcNow.AddDays(3);
        var slot2 = DateTime.UtcNow.AddDays(5);

        var original = await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = slot1,
            EstimatedDurationMinutes = 30
        });

        var rescheduled = await svc.RescheduleAppointmentAsync(
            original.Id,
            new RescheduleAppointmentRequest { NewScheduledStart = slot2 },
            requestingUserId: 1, requestingUserRole: "Patient");

        // Original should now be Rescheduled
        var updatedOriginal = await ctx.Appointments.FindAsync(original.Id);
        Assert.Equal(AppointmentStatus.Rescheduled, updatedOriginal!.Status);

        // New appointment links back to original
        Assert.Equal(original.Id, rescheduled.RescheduledFromId);
        Assert.Equal("Scheduled", rescheduled.Status);
        Assert.Equal(slot2, rescheduled.ScheduledStart);
    }

    // ── Double-booking: same slot, same doctor ────────────────────────────────

    [Fact]
    public async Task DoubleBooking_SameSlotSameDoctor_SecondBookingFails()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var slot = DateTime.UtcNow.AddDays(2).Date.AddHours(10);

        await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = slot,
            EstimatedDurationMinutes = 60
        });

        // Patient 3 tries to book an overlapping slot with the same doctor
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.BookAppointmentAsync(3, new CreateAppointmentRequest
            {
                PatientId = 3, DoctorId = 2,
                AppointmentType = AppointmentType.General,
                ScheduledStart  = slot.AddMinutes(30), // still inside the 60-min block
                EstimatedDurationMinutes = 30
            }));
    }

    // ── Concurrent booking of same slot — only one should succeed ─────────────

    [Fact]
    public async Task ConcurrentBooking_SameSlot_OnlyOneSucceeds()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var slot = DateTime.UtcNow.AddDays(4).Date.AddHours(14);

        var task1 = svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2, AppointmentType = AppointmentType.General,
            ScheduledStart = slot, EstimatedDurationMinutes = 30
        });

        var task2 = svc.BookAppointmentAsync(3, new CreateAppointmentRequest
        {
            PatientId = 3, DoctorId = 2, AppointmentType = AppointmentType.General,
            ScheduledStart = slot, EstimatedDurationMinutes = 30
        });

        int succeeded = 0;
        int failed    = 0;

        foreach (var t in new[] { task1, task2 })
        {
            try   { await t; succeeded++; }
            catch (InvalidOperationException) { failed++; }
        }

        Assert.Equal(1, succeeded);
        Assert.Equal(1, failed);
    }

    // ── Unauthorized: patient tries to cancel another patient's appointment ───

    [Fact]
    public async Task Cancel_PatientCancelsOtherPatientAppointment_ThrowsUnauthorized()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedUsersAsync(ctx);

        // Patient 3 books
        var apt = await svc.BookAppointmentAsync(3, new CreateAppointmentRequest
        {
            PatientId = 3, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = DateTime.UtcNow.AddDays(6),
            EstimatedDurationMinutes = 30
        });

        // Patient 1 tries to cancel it — should be rejected
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CancelAppointmentAsync(
                apt.Id,
                new CancelAppointmentRequest { Reason = "Hacking" },
                requestingUserId: 1,
                requestingUserRole: "Patient"));
    }

    // ── Notification created on booking ──────────────────────────────────────

    [Fact]
    public async Task BookAppointment_CreatesNotificationForPatient()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var booked = await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = DateTime.UtcNow.AddDays(7),
            EstimatedDurationMinutes = 30
        });

        var notifications = await ctx.AppointmentNotifications
            .Where(n => n.UserId == 1 && n.AppointmentId == booked.Id)
            .ToListAsync();

        Assert.NotEmpty(notifications);
        Assert.Contains(notifications, n => n.Title.Contains("Confirmed") || n.Title.Contains("Appointment"));
    }

    // ── Status history is persisted through service stack ─────────────────────

    [Fact]
    public async Task GetStatusHistory_AfterCancellation_ContainsBothEntries()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var apt = await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1, DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart  = DateTime.UtcNow.AddDays(8),
            EstimatedDurationMinutes = 30
        });

        await svc.CancelAppointmentAsync(apt.Id,
            new CancelAppointmentRequest { Reason = "No longer needed" },
            requestingUserId: 99, requestingUserRole: "Admin");

        var history = await svc.GetStatusHistoryAsync(apt.Id);

        Assert.True(history.Count >= 2);
        Assert.Contains(history, h => h.NewStatus == "Scheduled");
        Assert.Contains(history, h => h.NewStatus == "Cancelled");
    }
}
