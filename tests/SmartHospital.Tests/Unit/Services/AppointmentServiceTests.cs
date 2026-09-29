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
    public async Task BookAppointment_ValidRequest_CreatesAppointmentWithoutQueueUntilCheckIn()
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
            EstimatedDurationMinutes = 30
        };

        // Act
        var result = await svc.BookAppointmentAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("APT-", result.ReferenceNumber);
        Assert.Null(result.QueueNumber);
        Assert.Empty(ctx.QueueEntries);
        Assert.Equal("Scheduled", result.Status);
        Assert.Equal("Normal", result.Priority);
        Assert.False(result.PriorityNeedsReview);
    }

    [Fact]
    public async Task BookAppointment_InvalidPriority_IsRejected()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.BookAppointmentAsync(
            requestingUserId: 1,
            new CreateAppointmentRequest { Priority = (AppointmentPriority)99 }));
    }

    [Theory]
    [InlineData(AppointmentPriority.Urgent)]
    [InlineData(AppointmentPriority.Emergency)]
    public async Task BookAppointment_PatientPriorityIsRecordedAsRequestUntilStaffApproves(AppointmentPriority requestedPriority)
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);

        var result = await svc.BookAppointmentAsync(1, new CreateAppointmentRequest
        {
            PatientId = 1,
            DoctorId = 2,
            AppointmentType = AppointmentType.General,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Priority = requestedPriority
        });
        var appointment = await ctx.Appointments.SingleAsync(a => a.Id == result.Id);

        Assert.Equal(requestedPriority.ToString(), result.Priority);
        Assert.True(result.PriorityNeedsReview);
        Assert.Equal(AppointmentPriority.Normal, appointment.Priority);
        Assert.Equal(requestedPriority, appointment.RequestedPriority);

        var filtered = await svc.GetAppointmentsAsync(2, "Admin", new AppointmentQueryFilter
        {
            Priority = requestedPriority
        });
        Assert.Contains(filtered, item => item.Id == result.Id);
    }

    [Fact]
    public async Task GetAppointmentsPage_CombinesDoctorPriorityDatesAndCountsAcrossAllPages()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);
        ctx.Users.Add(new User
        {
            Id = 3, FirstName = "Other", LastName = "Doctor",
            Email = "other-doctor@test.com", PasswordHash = "hash",
            Role = UserRole.Doctor, Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        var firstDate = DateTime.UtcNow.Date.AddDays(2);
        var matching = new[]
        {
            new Appointment { PatientId = 1, DoctorId = 2, ReferenceNumber = "APT-PAGE-1", ScheduledStart = firstDate.AddHours(9), EstimatedDurationMinutes = 30, Status = AppointmentStatus.Scheduled, Priority = AppointmentPriority.Normal, RequestedPriority = AppointmentPriority.Urgent, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Appointment { PatientId = 1, DoctorId = 2, ReferenceNumber = "APT-PAGE-2", ScheduledStart = firstDate.AddHours(10), EstimatedDurationMinutes = 30, Status = AppointmentStatus.InProgress, Priority = AppointmentPriority.Urgent, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Appointment { PatientId = 1, DoctorId = 2, ReferenceNumber = "APT-PAGE-3", ScheduledStart = firstDate.AddHours(11), EstimatedDurationMinutes = 30, Status = AppointmentStatus.Scheduled, Priority = AppointmentPriority.Emergency, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Appointment { PatientId = 1, DoctorId = 3, ReferenceNumber = "APT-PAGE-4", ScheduledStart = firstDate.AddHours(12), EstimatedDurationMinutes = 30, Status = AppointmentStatus.Scheduled, Priority = AppointmentPriority.Urgent, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };
        ctx.Appointments.AddRange(matching);
        await ctx.SaveChangesAsync();

        var result = await svc.GetAppointmentsPageAsync(1, "Admin", new AppointmentQueryFilter
        {
            DoctorId = 2,
            Priority = AppointmentPriority.Urgent,
            FromDate = firstDate,
            ToDate = firstDate.AddDays(1),
            Page = 1,
            PageSize = 1
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Appointments);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.StatusCounts[nameof(AppointmentStatus.Scheduled)]);
        Assert.Equal(1, result.StatusCounts[nameof(AppointmentStatus.InProgress)]);
        Assert.Equal(2, result.PriorityCounts[nameof(AppointmentPriority.Urgent)]);
    }

    [Fact]
    public async Task UpdateAppointmentPriority_UpdatesAppointmentAndQueueEntry()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);
        var appointment = new Appointment
        {
            PatientId = 1, DoctorId = 2,
            ReferenceNumber = "APT-PRIORITY-0001",
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Status = AppointmentStatus.CheckedIn,
            Priority = AppointmentPriority.Emergency,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();
        var queueEntry = new QueueEntry
        {
            AppointmentId = appointment.Id,
            DoctorId = 2,
            QueueNumber = 1,
            QueueCode = "Q-TEST-001",
            QueueDate = DateOnly.FromDateTime(appointment.ScheduledStart),
            Status = QueueEntryStatus.Waiting,
            Priority = AppointmentPriority.Emergency
        };
        ctx.QueueEntries.Add(queueEntry);
        await ctx.SaveChangesAsync();

        var result = await svc.UpdateAppointmentPriorityAsync(
            appointment.Id, AppointmentPriority.Normal, staffUserId: 2);

        Assert.Equal("Normal", result.Priority);
        Assert.False(result.PriorityNeedsReview);
        Assert.Equal(AppointmentPriority.Normal, appointment.Priority);
        Assert.Null(appointment.RequestedPriority);
        Assert.Equal(AppointmentPriority.Normal, queueEntry.Priority);
        Assert.True(appointment.UpdatedAt > appointment.CreatedAt);
    }

    [Fact]
    public async Task RescheduleAppointment_PreservesPriority()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);
        var original = new Appointment
        {
            PatientId = 1, DoctorId = 2,
            ReferenceNumber = "APT-RESCHEDULE-PRIORITY",
            ScheduledStart = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 30,
            Status = AppointmentStatus.Scheduled,
            Priority = AppointmentPriority.Normal,
            RequestedPriority = AppointmentPriority.Urgent,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(original);
        await ctx.SaveChangesAsync();

        var result = await svc.RescheduleAppointmentAsync(
            original.Id,
            new RescheduleAppointmentRequest { NewScheduledStart = DateTime.UtcNow.AddDays(3) },
            requestingUserId: 1,
            requestingUserRole: "Patient");

        Assert.Equal("Urgent", result.Priority);
        Assert.True(result.PriorityNeedsReview);
        Assert.Equal(AppointmentStatus.Rescheduled, original.Status);
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
        Assert.Null(r1.QueueNumber);
        Assert.Null(r2.QueueNumber);
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

    [Fact]
    public async Task ConfirmAppointment_Scheduled_ConfirmsAndAuditsStaffAndTime()
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);
        var appointment = new Appointment
        {
            PatientId = 1, DoctorId = 2,
            ReferenceNumber = "APT-CONFIRM-0001",
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Status = AppointmentStatus.Scheduled,
            Priority = AppointmentPriority.Normal,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();

        var result = await svc.ConfirmAppointmentAsync(appointment.Id, staffUserId: 1);

        Assert.Equal("Confirmed", result.Status);
        var history = Assert.Single(ctx.AppointmentStatusHistories);
        Assert.Equal(AppointmentStatus.Scheduled, history.OldStatus);
        Assert.Equal(AppointmentStatus.Confirmed, history.NewStatus);
        Assert.Equal(1, history.ChangedBy);
        Assert.NotEqual(default, history.ChangedAt);
    }

    [Theory]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.InProgress)]
    [InlineData(AppointmentStatus.Completed)]
    public async Task ConfirmAppointment_NonScheduledStatus_IsRejected(AppointmentStatus status)
    {
        using var ctx = CreateContext();
        var (svc, _) = BuildService(ctx);
        await SeedUsersAsync(ctx);
        var appointment = new Appointment
        {
            PatientId = 1, DoctorId = 2,
            ReferenceNumber = $"APT-CONFIRM-{status}",
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 30,
            Status = status,
            Priority = AppointmentPriority.Normal,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ConfirmAppointmentAsync(appointment.Id, staffUserId: 1));
        Assert.Empty(ctx.AppointmentStatusHistories);
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
