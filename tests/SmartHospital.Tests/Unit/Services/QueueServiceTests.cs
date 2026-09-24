using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartHospital.Tests.Unit.Services;

/// <summary>
/// Unit tests for QueueService covering:
///   - Priority ordering (Emergency > Urgent > Normal, then FIFO within same priority)
///   - Check-in flow
///   - No-show handling
///   - Completion handling
///   - Wait-time estimation
/// </summary>
public class QueueServiceTests
{
    // ── Test infrastructure ───────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static QueueService BuildService(AppDbContext ctx)
    {
        var notifSvc = new NotificationService(ctx, NullLogger<NotificationService>.Instance);
        return new QueueService(ctx, notifSvc, NullLogger<QueueService>.Instance);
    }

    private static async Task<User> SeedDoctorAsync(AppDbContext ctx, int id = 10)
    {
        var doctor = new User
        {
            Id = id, FirstName = "Dr", LastName = "Smith",
            Email = $"doctor{id}@test.com", PasswordHash = "hash",
            Role = UserRole.Doctor, Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(doctor);
        await ctx.SaveChangesAsync();
        return doctor;
    }

    private static async Task<User> SeedPatientAsync(AppDbContext ctx, int id, string name = "Patient")
    {
        var patient = new User
        {
            Id = id, FirstName = name, LastName = "Test",
            Email = $"p{id}@test.com", PasswordHash = "hash",
            Role = UserRole.Patient, Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(patient);
        await ctx.SaveChangesAsync();
        return patient;
    }

    private static async Task<(Appointment apt, QueueEntry entry)> SeedEntryAsync(
        AppDbContext ctx,
        int doctorId,
        int patientId,
        AppointmentPriority priority,
        int queueNumber,
        int durationMinutes = 30)
    {
        var apt = new Appointment
        {
            PatientId = patientId, DoctorId = doctorId,
            ReferenceNumber = $"APT-{queueNumber:D4}",
            ScheduledStart  = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = durationMinutes,
            Status    = AppointmentStatus.Scheduled,
            Priority  = priority,
            QueueNumber = queueNumber,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Appointments.Add(apt);
        await ctx.SaveChangesAsync();

        var entry = new QueueEntry
        {
            AppointmentId = apt.Id,
            DoctorId      = doctorId,
            QueueNumber   = queueNumber,
            Status        = QueueEntryStatus.Waiting,
            Priority      = priority
        };
        ctx.QueueEntries.Add(entry);
        await ctx.SaveChangesAsync();
        return (apt, entry);
    }

    // ── Priority ordering ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetQueueForDoctor_OrderedByPriorityThenFifo()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        var doctor = await SeedDoctorAsync(ctx, 10);

        // Seed 3 patients
        await SeedPatientAsync(ctx, 1, "Normal");
        await SeedPatientAsync(ctx, 2, "Urgent");
        await SeedPatientAsync(ctx, 3, "Emergency");

        // Book in FIFO order: Normal(Q1), Urgent(Q2), Emergency(Q3)
        await SeedEntryAsync(ctx, 10, 1, AppointmentPriority.Normal,    queueNumber: 1);
        await SeedEntryAsync(ctx, 10, 2, AppointmentPriority.Urgent,    queueNumber: 2);
        await SeedEntryAsync(ctx, 10, 3, AppointmentPriority.Emergency, queueNumber: 3);

        // Act
        var queue = await svc.GetQueueForDoctorAsync(10);

        // Assert: Emergency first, then Urgent, then Normal
        Assert.Equal(3, queue.Count);
        Assert.Equal("Emergency", queue[0].Priority);
        Assert.Equal("Urgent",    queue[1].Priority);
        Assert.Equal("Normal",    queue[2].Priority);
    }

    [Fact]
    public async Task GetQueueForDoctor_SamePriority_OrderedByQueueNumberFifo()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedDoctorAsync(ctx, 10);

        await SeedPatientAsync(ctx, 1, "PatA");
        await SeedPatientAsync(ctx, 2, "PatB");

        // Both Normal — booked in order Q1, Q2
        await SeedEntryAsync(ctx, 10, 1, AppointmentPriority.Normal, queueNumber: 1);
        await SeedEntryAsync(ctx, 10, 2, AppointmentPriority.Normal, queueNumber: 2);

        var queue = await svc.GetQueueForDoctorAsync(10);

        Assert.Equal(2, queue.Count);
        Assert.Equal(1, queue[0].QueueNumber); // Q1 served first (FIFO)
        Assert.Equal(2, queue[1].QueueNumber);
    }

    // ── Queue position ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQueueForDoctor_AssignsCorrectPositions()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedDoctorAsync(ctx, 10);

        await SeedPatientAsync(ctx, 1);
        await SeedPatientAsync(ctx, 2);

        await SeedEntryAsync(ctx, 10, 1, AppointmentPriority.Normal, queueNumber: 1);
        await SeedEntryAsync(ctx, 10, 2, AppointmentPriority.Normal, queueNumber: 2);

        var queue = await svc.GetQueueForDoctorAsync(10);

        Assert.Equal(1, queue[0].QueuePosition);
        Assert.Equal(2, queue[1].QueuePosition);
    }

    // ── No-show handling ──────────────────────────────────────────────────────

    [Fact]
    public async Task MarkNoShow_UpdatesQueueAndAppointmentStatus()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedDoctorAsync(ctx, 10);
        await SeedPatientAsync(ctx, 1);

        var (apt, entry) = await SeedEntryAsync(ctx, 10, 1, AppointmentPriority.Normal, queueNumber: 1);

        var result = await svc.MarkNoShowAsync(entry.Id, staffUserId: 99);

        Assert.Equal("NoShow", result.Status);

        var updatedApt = await ctx.Appointments.FindAsync(apt.Id);
        Assert.Equal(AppointmentStatus.NoShow, updatedApt!.Status);
    }

    // ── Cannot mark a completed entry as no-show ──────────────────────────────

    [Fact]
    public async Task MarkNoShow_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedDoctorAsync(ctx, 10);
        await SeedPatientAsync(ctx, 1);

        var (_, entry) = await SeedEntryAsync(ctx, 10, 1, AppointmentPriority.Normal, queueNumber: 1);

        // Manually complete first
        entry.Status      = QueueEntryStatus.Completed;
        entry.CompletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.MarkNoShowAsync(entry.Id, staffUserId: 99));
    }

    // ── Wait-time estimation ──────────────────────────────────────────────────

    [Fact]
    public async Task GetQueueStatus_EstimatedWaitIncreasesForLaterEntries()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedDoctorAsync(ctx, 10);

        await SeedPatientAsync(ctx, 1);
        await SeedPatientAsync(ctx, 2);
        await SeedPatientAsync(ctx, 3);

        // All Normal, 30-min slots
        await SeedEntryAsync(ctx, 10, 1, AppointmentPriority.Normal, queueNumber: 1, durationMinutes: 30);
        await SeedEntryAsync(ctx, 10, 2, AppointmentPriority.Normal, queueNumber: 2, durationMinutes: 30);
        await SeedEntryAsync(ctx, 10, 3, AppointmentPriority.Normal, queueNumber: 3, durationMinutes: 30);

        // Call the first entry — triggers wait-time recalculation
        var entries = await svc.GetQueueForDoctorAsync(10);
        await svc.CallQueueEntryAsync(entries[0].Id, staffUserId: 99);

        // Re-fetch
        var status = await svc.GetQueueStatusForDoctorAsync(10);
        var waiting = status.Queue.Where(e => e.Status == "Waiting").OrderBy(e => e.QueuePosition).ToList();

        // First waiting entry has 0 wait (it's next), second has 30 min (one ahead of it)
        Assert.True(waiting[0].EstimatedWaitMinutes == 0);
        Assert.True(waiting[1].EstimatedWaitMinutes == 30);
    }

    // ── Completion ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkCompleted_UpdatesBothQueueAndAppointmentToCompleted()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx);
        await SeedDoctorAsync(ctx, 10);
        await SeedPatientAsync(ctx, 1);

        var (apt, entry) = await SeedEntryAsync(ctx, 10, 1, AppointmentPriority.Normal, queueNumber: 1);

        var result = await svc.MarkCompletedAsync(entry.Id, staffUserId: 99);

        Assert.Equal("Completed", result.Status);
        var updatedApt = await ctx.Appointments.FindAsync(apt.Id);
        Assert.Equal(AppointmentStatus.Completed, updatedApt!.Status);
    }
}
