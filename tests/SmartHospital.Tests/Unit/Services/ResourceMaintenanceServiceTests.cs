using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Maintenance;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class ResourceMaintenanceServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static ResourceMaintenanceService CreateService(AppDbContext context)
    {
        return new ResourceMaintenanceService(context, NullLogger<ResourceMaintenanceService>.Instance);
    }

    private static Bed SeedBed(AppDbContext context, string bedNumber = "B-101", BedStatus status = BedStatus.Available, bool isActive = true)
    {
        var ward = new Ward
        {
            Name = "Ward A",
            Code = "WA-" + Guid.NewGuid().ToString()[..4].ToUpper(),
            Floor = "1",
            Capacity = 10,
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
            Capacity = 4,
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
            BedNumber = bedNumber,
            Status = status,
            Type = BedType.Standard,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Beds.Add(bed);
        context.SaveChanges();

        return bed;
    }

    private static MedicalResource SeedResource(AppDbContext context, string code = "RES-01", ResourceStatus status = ResourceStatus.Available, bool isActive = true)
    {
        var resource = new MedicalResource
        {
            ResourceCode = code,
            Name = "Infusion Pump",
            Category = ResourceCategory.InfusionPump,
            Status = status,
            LocationDescription = "Room 101",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalResources.Add(resource);
        context.SaveChanges();
        return resource;
    }

    [Fact]
    public async Task ScheduleMaintenanceAsync_WithBedOnly_SchedulesSuccessfully()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var start = DateTime.UtcNow.AddDays(1);
        var response = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.CleaningAndSanitization,
            Description = "Routine sanitization",
            ScheduledStart = start
        });

        Assert.True(response.Id > 0);
        Assert.StartsWith("MNT-", response.MaintenanceCode);
        Assert.Equal(bed.Id, response.BedId);
        Assert.Null(response.MedicalResourceId);
        Assert.Equal("Scheduled", response.Status);
        Assert.Equal("CleaningAndSanitization", response.Type);
    }

    [Fact]
    public async Task ScheduleMaintenanceAsync_WithMedicalResourceOnly_SchedulesSuccessfully()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context);
        var service = CreateService(context);

        var start = DateTime.UtcNow.AddDays(1);
        var response = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            MedicalResourceId = resource.Id,
            Type = MaintenanceType.Calibration,
            Description = "Annual calibration",
            ScheduledStart = start
        });

        Assert.True(response.Id > 0);
        Assert.StartsWith("MNT-", response.MaintenanceCode);
        Assert.Equal(resource.Id, response.MedicalResourceId);
        Assert.Null(response.BedId);
        Assert.Equal("Scheduled", response.Status);
        Assert.Equal("Calibration", response.Type);
    }

    [Fact]
    public async Task ScheduleMaintenanceAsync_WithBothBedAndResource_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var resource = SeedResource(context);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
            {
                BedId = bed.Id,
                MedicalResourceId = resource.Id,
                Type = MaintenanceType.RoutineInspection,
                Description = "Invalid dual target",
                ScheduledStart = DateTime.UtcNow.AddDays(1)
            }));

        Assert.Contains("cannot target both a bed and a medical resource", ex.Message);
    }

    [Fact]
    public async Task ScheduleMaintenanceAsync_WithNeitherBedNorResource_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
            {
                Type = MaintenanceType.RoutineInspection,
                Description = "No target",
                ScheduledStart = DateTime.UtcNow.AddDays(1)
            }));

        Assert.Contains("must target either a bed or a medical resource", ex.Message);
    }

    [Fact]
    public async Task ScheduleMaintenanceAsync_BedNotFound_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
            {
                BedId = 999,
                Type = MaintenanceType.Repair,
                Description = "Non-existent bed",
                ScheduledStart = DateTime.UtcNow.AddDays(1)
            }));

        Assert.Contains("Specified bed does not exist", ex.Message);
    }

    [Fact]
    public async Task StartMaintenanceAsync_BedTarget_SetsBedStatusMaintenanceAndMaintenanceInProgress()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.Repair,
            Description = "Fix bed motor",
            ScheduledStart = DateTime.UtcNow
        });

        var started = await service.StartMaintenanceAsync(m.Id);

        Assert.NotNull(started);
        Assert.Equal("InProgress", started.Status);

        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.Equal(BedStatus.Maintenance, bedEntity!.Status);
    }

    [Fact]
    public async Task StartMaintenanceAsync_BedCurrentlyOccupied_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context, status: BedStatus.Available);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.Repair,
            Description = "Repair",
            ScheduledStart = DateTime.UtcNow
        });

        bed.Status = BedStatus.Occupied;
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartMaintenanceAsync(m.Id));

        Assert.Contains("active patient allocation", ex.Message);
    }

    [Fact]
    public async Task StartMaintenanceAsync_ResourceInUse_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, status: ResourceStatus.Available);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            MedicalResourceId = resource.Id,
            Type = MaintenanceType.RoutineInspection,
            Description = "Safety inspection",
            ScheduledStart = DateTime.UtcNow
        });

        resource.Status = ResourceStatus.InUse;
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartMaintenanceAsync(m.Id));

        Assert.Contains("currently in use", ex.Message);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_BedTarget_RestoresBedStatusToAvailable()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.CleaningAndSanitization,
            Description = "Deep clean",
            ScheduledStart = DateTime.UtcNow
        });

        await service.StartMaintenanceAsync(m.Id);

        var completed = await service.CompleteMaintenanceAsync(m.Id, new CompleteMaintenanceRequest
        {
            ResolutionNotes = "Sanitization completed successfully"
        });

        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);
        Assert.Equal("Sanitization completed successfully", completed.ResolutionNotes);
        Assert.NotNull(completed.ActualCompletedAt);

        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.Equal(BedStatus.Available, bedEntity!.Status);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_DeactivatedBed_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.Repair,
            Description = "Maintenance",
            ScheduledStart = DateTime.UtcNow
        });

        await service.StartMaintenanceAsync(m.Id);

        bed.IsActive = false;
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteMaintenanceAsync(m.Id, new CompleteMaintenanceRequest
            {
                ResolutionNotes = "Done"
            }));

        Assert.Contains("cannot restore a deactivated bed to Available", ex.Message);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_MedicalResource_RestoresResourceToAvailable()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            MedicalResourceId = resource.Id,
            Type = MaintenanceType.Calibration,
            Description = "Recalibrate sensors",
            ScheduledStart = DateTime.UtcNow
        });

        await service.StartMaintenanceAsync(m.Id);

        var completed = await service.CompleteMaintenanceAsync(m.Id, new CompleteMaintenanceRequest
        {
            ResolutionNotes = "Sensors calibrated within tolerance"
        });

        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);

        var resEntity = await context.MedicalResources.FindAsync(resource.Id);
        Assert.Equal(ResourceStatus.Available, resEntity!.Status);
    }

    [Fact]
    public async Task UpdateScheduledMaintenanceAsync_ValidUpdate_UpdatesDetails()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.RoutineInspection,
            Description = "Old description",
            ScheduledStart = DateTime.UtcNow.AddDays(2)
        });

        var updated = await service.UpdateScheduledMaintenanceAsync(m.Id, new UpdateMaintenanceRequest
        {
            Type = MaintenanceType.CleaningAndSanitization,
            Description = "Updated description",
            ScheduledStart = DateTime.UtcNow.AddDays(3),
            ScheduledEnd = DateTime.UtcNow.AddDays(3).AddHours(2)
        });

        Assert.NotNull(updated);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal("CleaningAndSanitization", updated.Type);
    }

    [Fact]
    public async Task UpdateScheduledMaintenanceAsync_WhenStatusIsNotScheduled_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.RoutineInspection,
            Description = "Checkup",
            ScheduledStart = DateTime.UtcNow
        });

        await service.StartMaintenanceAsync(m.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateScheduledMaintenanceAsync(m.Id, new UpdateMaintenanceRequest
            {
                Type = MaintenanceType.Repair,
                Description = "New desc",
                ScheduledStart = DateTime.UtcNow
            }));

        Assert.Contains("Only Scheduled maintenance can be edited", ex.Message);
    }

    [Fact]
    public async Task CancelScheduledMaintenanceAsync_SetsStatusCancelledAndStoresReason()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.CleaningAndSanitization,
            Description = "Sanitization",
            ScheduledStart = DateTime.UtcNow.AddDays(1)
        });

        var cancelled = await service.CancelScheduledMaintenanceAsync(m.Id, new CancelMaintenanceRequest
        {
            Reason = "Rescheduled due to emergency admission surge"
        });

        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("Rescheduled due to emergency admission surge", cancelled.ResolutionNotes);
    }

    [Fact]
    public async Task CancelScheduledMaintenanceAsync_WhenNotScheduled_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var bed = SeedBed(context);
        var service = CreateService(context);

        var m = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed.Id,
            Type = MaintenanceType.CleaningAndSanitization,
            Description = "Sanitization",
            ScheduledStart = DateTime.UtcNow
        });

        await service.StartMaintenanceAsync(m.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelScheduledMaintenanceAsync(m.Id));

        Assert.Contains("Only Scheduled maintenance can be cancelled", ex.Message);
    }

    [Fact]
    public async Task GetMaintenanceRecordsAsync_FiltersByStatus()
    {
        await using var context = CreateContext();
        var bed1 = SeedBed(context, "B-01");
        var bed2 = SeedBed(context, "B-02");
        var service = CreateService(context);

        var m1 = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed1.Id,
            Type = MaintenanceType.CleaningAndSanitization,
            Description = "M1",
            ScheduledStart = DateTime.UtcNow
        });

        var m2 = await service.ScheduleMaintenanceAsync(new CreateMaintenanceRequest
        {
            BedId = bed2.Id,
            Type = MaintenanceType.CleaningAndSanitization,
            Description = "M2",
            ScheduledStart = DateTime.UtcNow
        });

        await service.StartMaintenanceAsync(m2.Id);

        var scheduledOnly = await service.GetMaintenanceRecordsAsync(status: MaintenanceStatus.Scheduled);
        var inProgressOnly = await service.GetMaintenanceRecordsAsync(status: MaintenanceStatus.InProgress);

        Assert.Single(scheduledOnly);
        Assert.Equal(m1.Id, scheduledOnly[0].Id);
        Assert.Single(inProgressOnly);
        Assert.Equal(m2.Id, inProgressOnly[0].Id);
    }

    [Fact]
    public async Task GetMaintenanceByIdAsync_NonExistentId_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetMaintenanceByIdAsync(999);

        Assert.Null(result);
    }
}
