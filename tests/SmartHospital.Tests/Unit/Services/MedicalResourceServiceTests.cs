using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.MedicalResources;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class MedicalResourceServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static (Ward ward, Room room, Bed bed) SeedHierarchy(AppDbContext context)
    {
        var ward = new Ward
        {
            Name = "ICU Ward",
            Code = "ICU-" + Guid.NewGuid().ToString()[..4].ToUpper(),
            Floor = "2nd Floor",
            Capacity = 10,
            Type = WardType.ICU,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward);
        context.SaveChanges();

        var room = new Room
        {
            WardId = ward.Id,
            RoomNumber = "ICU-R1",
            Capacity = 2,
            Type = RoomType.ICU,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Rooms.Add(room);
        context.SaveChanges();

        var bed = new Bed
        {
            RoomId = room.Id,
            BedNumber = "ICU-B1",
            Status = BedStatus.Available,
            Type = BedType.ICU,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Beds.Add(bed);
        context.SaveChanges();

        return (ward, room, bed);
    }

    [Fact]
    public async Task CreateResourceAsync_ValidRequest_CreatesResourceWithAvailableStatus()
    {
        await using var context = CreateContext();
        var (ward, room, bed) = SeedHierarchy(context);
        var service = new MedicalResourceService(context);

        var response = await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "  res-vent-001  ",
            Name = " Hamilton Ventilator G5 ",
            Category = ResourceCategory.Ventilator,
            LocationDescription = "Bedside ICU-B1",
            SerialNumber = "SN-987654",
            Manufacturer = "Hamilton Medical",
            ModelNumber = "G5",
            WardId = ward.Id,
            RoomId = room.Id,
            BedId = bed.Id
        });

        Assert.True(response.Id > 0);
        Assert.Equal("RES-VENT-001", response.ResourceCode);
        Assert.Equal("Hamilton Ventilator G5", response.Name);
        Assert.Equal("Ventilator", response.Category);
        Assert.Equal("Available", response.Status);
        Assert.Equal(ward.Id, response.WardId);
        Assert.Equal(room.Id, response.RoomId);
        Assert.Equal(bed.Id, response.BedId);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task CreateResourceAsync_DuplicateResourceCode_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "RES-001",
            Name = "Monitor 1",
            Category = ResourceCategory.Monitor,
            LocationDescription = "Storage"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateResourceAsync(new CreateResourceRequest
            {
                ResourceCode = "res-001",
                Name = "Monitor 2",
                Category = ResourceCategory.Monitor,
                LocationDescription = "Storage"
            }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateResourceAsync_InvalidBed_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateResourceAsync(new CreateResourceRequest
            {
                ResourceCode = "RES-001",
                Name = "Monitor",
                Category = ResourceCategory.Monitor,
                LocationDescription = "Location",
                BedId = 999
            }));

        Assert.Contains("Specified bed does not exist", ex.Message);
    }

    [Fact]
    public async Task CreateResourceAsync_BedDoesNotBelongToRoom_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var (ward, room1, bed) = SeedHierarchy(context);

        var room2 = new Room
        {
            WardId = ward.Id,
            RoomNumber = "ICU-R2",
            Capacity = 2,
            Type = RoomType.ICU,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Rooms.Add(room2);
        await context.SaveChangesAsync();

        var service = new MedicalResourceService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateResourceAsync(new CreateResourceRequest
            {
                ResourceCode = "RES-001",
                Name = "Monitor",
                Category = ResourceCategory.Monitor,
                LocationDescription = "Location",
                WardId = ward.Id,
                RoomId = room2.Id, // Mismatched room
                BedId = bed.Id     // Belongs to room1
            }));

        Assert.Contains("Specified bed does not belong to the selected room", ex.Message);
    }

    [Fact]
    public async Task CreateResourceAsync_RoomDoesNotBelongToWard_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var (ward1, room, _) = SeedHierarchy(context);

        var ward2 = new Ward
        {
            Name = "General Ward 2",
            Code = "GW02",
            Floor = "1",
            Capacity = 10,
            Type = WardType.General,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward2);
        await context.SaveChangesAsync();

        var service = new MedicalResourceService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateResourceAsync(new CreateResourceRequest
            {
                ResourceCode = "RES-001",
                Name = "Monitor",
                Category = ResourceCategory.Monitor,
                LocationDescription = "Location",
                WardId = ward2.Id, // Mismatched ward
                RoomId = room.Id   // Belongs to ward1
            }));

        Assert.Contains("Specified room does not belong to the selected ward", ex.Message);
    }

    [Fact]
    public async Task GetResourcesAsync_FiltersByCategory()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        await service.CreateResourceAsync(new CreateResourceRequest { ResourceCode = "R1", Name = "Ventilator 1", Category = ResourceCategory.Ventilator, LocationDescription = "ICU" });
        await service.CreateResourceAsync(new CreateResourceRequest { ResourceCode = "R2", Name = "Monitor 1", Category = ResourceCategory.Monitor, LocationDescription = "ICU" });

        var results = await service.GetResourcesAsync(new ResourceQueryFilter { Category = ResourceCategory.Ventilator });

        Assert.Single(results);
        Assert.Equal("R1", results[0].ResourceCode);
        Assert.Equal("Ventilator", results[0].Category);
    }

    [Fact]
    public async Task GetResourcesAsync_FiltersByStatus()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var r1 = await service.CreateResourceAsync(new CreateResourceRequest { ResourceCode = "R1", Name = "Ventilator 1", Category = ResourceCategory.Ventilator, LocationDescription = "ICU" });
        var r2 = await service.CreateResourceAsync(new CreateResourceRequest { ResourceCode = "R2", Name = "Ventilator 2", Category = ResourceCategory.Ventilator, LocationDescription = "ICU" });

        await service.UpdateResourceAsync(r2.Id, new UpdateResourceRequest
        {
            Name = "Ventilator 2",
            Category = ResourceCategory.Ventilator,
            Status = ResourceStatus.InUse,
            LocationDescription = "ICU",
            IsActive = true
        });

        var inUseResults = await service.GetResourcesAsync(new ResourceQueryFilter { Status = ResourceStatus.InUse });

        Assert.Single(inUseResults);
        Assert.Equal("R2", inUseResults[0].ResourceCode);
    }

    [Fact]
    public async Task GetResourcesAsync_FiltersBySearch()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        await service.CreateResourceAsync(new CreateResourceRequest { ResourceCode = "RES-DEFIB-1", Name = "Zoll Defibrillator", Category = ResourceCategory.Defibrillator, LocationDescription = "Crash Cart" });
        await service.CreateResourceAsync(new CreateResourceRequest { ResourceCode = "RES-VENT-2", Name = "Evita Ventilator", Category = ResourceCategory.Ventilator, LocationDescription = "Storage" });

        var results = await service.GetResourcesAsync(new ResourceQueryFilter { Search = "Defibrillator" });

        Assert.Single(results);
        Assert.Equal("RES-DEFIB-1", results[0].ResourceCode);
    }

    [Fact]
    public async Task GetResourceByIdAsync_ExistingId_ReturnsResource()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var created = await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "RES-001",
            Name = "Dialysis Machine",
            Category = ResourceCategory.Dialysis,
            LocationDescription = "Renal Unit"
        });

        var fetched = await service.GetResourceByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("RES-001", fetched.ResourceCode);
        Assert.Equal("Dialysis", fetched.Category);
    }

    [Fact]
    public async Task GetResourceByIdAsync_NonExistentId_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var result = await service.GetResourceByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateResourceAsync_ValidUpdate_UpdatesProperties()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var created = await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "RES-001",
            Name = "Standard Monitor",
            Category = ResourceCategory.Monitor,
            LocationDescription = "Floor 1"
        });

        var updated = await service.UpdateResourceAsync(created.Id, new UpdateResourceRequest
        {
            Name = "Upgraded Multi-parameter Monitor",
            Category = ResourceCategory.Monitor,
            Status = ResourceStatus.Available,
            LocationDescription = "Floor 2 Room 101",
            SerialNumber = "SN-NEW-123",
            IsActive = true
        });

        Assert.NotNull(updated);
        Assert.Equal("Upgraded Multi-parameter Monitor", updated.Name);
        Assert.Equal("Floor 2 Room 101", updated.LocationDescription);
        Assert.Equal("SN-NEW-123", updated.SerialNumber);
    }

    [Fact]
    public async Task AssignResourceAsync_ValidAssignment_UpdatesWardRoomBed()
    {
        await using var context = CreateContext();
        var (ward, room, bed) = SeedHierarchy(context);
        var service = new MedicalResourceService(context);

        var created = await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "RES-001",
            Name = "Syringe Pump",
            Category = ResourceCategory.InfusionPump,
            LocationDescription = "Storage"
        });

        var assigned = await service.AssignResourceAsync(created.Id, new AssignResourceRequest
        {
            WardId = ward.Id,
            RoomId = room.Id,
            BedId = bed.Id,
            LocationDescription = "Assigned to bed ICU-B1"
        });

        Assert.NotNull(assigned);
        Assert.Equal(ward.Id, assigned.WardId);
        Assert.Equal(room.Id, assigned.RoomId);
        Assert.Equal(bed.Id, assigned.BedId);
        Assert.Equal("Assigned to bed ICU-B1", assigned.LocationDescription);
    }

    [Fact]
    public async Task DeactivateResourceAsync_AvailableResource_SetsIsActiveFalse()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var created = await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "RES-001",
            Name = "Wheelchair",
            Category = ResourceCategory.Wheelchair,
            LocationDescription = "Lobby"
        });

        var success = await service.DeactivateResourceAsync(created.Id);

        Assert.True(success);
        var entity = await context.MedicalResources.FindAsync(created.Id);
        Assert.False(entity!.IsActive);
    }

    [Fact]
    public async Task DeactivateResourceAsync_InUseResource_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var created = await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "RES-001",
            Name = "Ventilator",
            Category = ResourceCategory.Ventilator,
            LocationDescription = "ICU"
        });

        var entity = await context.MedicalResources.FindAsync(created.Id);
        entity!.Status = ResourceStatus.InUse;
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeactivateResourceAsync(created.Id));

        Assert.Contains("currently in use", ex.Message);
    }

    [Fact]
    public async Task ActivateResourceAsync_SetsIsActiveTrue()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        var created = await service.CreateResourceAsync(new CreateResourceRequest
        {
            ResourceCode = "RES-001",
            Name = "Wheelchair",
            Category = ResourceCategory.Wheelchair,
            LocationDescription = "Lobby"
        });
        await service.DeactivateResourceAsync(created.Id);

        var success = await service.ActivateResourceAsync(created.Id);

        Assert.True(success);
        var entity = await context.MedicalResources.FindAsync(created.Id);
        Assert.True(entity!.IsActive);
    }

    [Fact]
    public async Task ActivateResourceAsync_NonExistentId_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new MedicalResourceService(context);

        Assert.False(await service.ActivateResourceAsync(999));
    }
}
