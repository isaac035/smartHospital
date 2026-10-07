using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class OccupancyServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetOccupancyOverviewAsync_WithZeroBeds_ReturnsZeroOccupancyRate()
    {
        await using var context = CreateContext();
        var service = new OccupancyService(context);

        var overview = await service.GetOccupancyOverviewAsync();

        Assert.NotNull(overview);
        Assert.Equal(0, overview.TotalBeds);
        Assert.Equal(0, overview.AvailableBeds);
        Assert.Equal(0, overview.OccupiedBeds);
        Assert.Equal(0.0, overview.HospitalOccupancyRate);
        Assert.Equal(0, overview.TotalWards);
    }

    [Fact]
    public async Task GetOccupancyOverviewAsync_CalculatesHospitalOccupancyRateCorrectly()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "General Ward", Code = "GW", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        // 4 active beds: 3 occupied, 1 available (75.0% occupancy rate)
        context.Beds.AddRange(
            new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B2", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B3", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B4", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new OccupancyService(context);
        var overview = await service.GetOccupancyOverviewAsync();

        Assert.Equal(4, overview.TotalBeds);
        Assert.Equal(3, overview.OccupiedBeds);
        Assert.Equal(1, overview.AvailableBeds);
        Assert.Equal(75.0, overview.HospitalOccupancyRate);
    }

    [Fact]
    public async Task GetOccupancyOverviewAsync_ExcludesInactiveBedsFromCounts()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.AddRange(
            new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B2", Status = BedStatus.Available, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow } // Inactive bed
        );
        await context.SaveChangesAsync();

        var service = new OccupancyService(context);
        var overview = await service.GetOccupancyOverviewAsync();

        Assert.Equal(1, overview.TotalBeds);
        Assert.Equal(1, overview.AvailableBeds);
    }

    [Fact]
    public async Task GetOccupancyOverviewAsync_CountsLowAvailabilityWardsCorrectly()
    {
        await using var context = CreateContext();
        // Ward 1: 5 beds, 4 occupied, 1 available (20% available -> low availability)
        var ward1 = new Ward { Name = "Ward 1", Code = "W1", Floor = "1", Capacity = 5, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        // Ward 2: 5 beds, 1 occupied, 4 available (80% available -> normal availability)
        var ward2 = new Ward { Name = "Ward 2", Code = "W2", Floor = "2", Capacity = 5, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.AddRange(ward1, ward2);
        await context.SaveChangesAsync();

        var room1 = new Room { WardId = ward1.Id, RoomNumber = "R1", Capacity = 5, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var room2 = new Room { WardId = ward2.Id, RoomNumber = "R2", Capacity = 5, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.AddRange(room1, room2);
        await context.SaveChangesAsync();

        context.Beds.AddRange(
            new Bed { RoomId = room1.Id, BedNumber = "W1-B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room1.Id, BedNumber = "W1-B2", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room1.Id, BedNumber = "W1-B3", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room1.Id, BedNumber = "W1-B4", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room1.Id, BedNumber = "W1-B5", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room2.Id, BedNumber = "W2-B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room2.Id, BedNumber = "W2-B2", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room2.Id, BedNumber = "W2-B3", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room2.Id, BedNumber = "W2-B4", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room2.Id, BedNumber = "W2-B5", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new OccupancyService(context);
        var overview = await service.GetOccupancyOverviewAsync();

        Assert.Equal(2, overview.TotalWards);
        Assert.Equal(2, overview.ActiveWards);
        Assert.Equal(1, overview.LowAvailabilityWardCount); // Only Ward 1 is <= 20% available
    }

    [Fact]
    public async Task GetOccupancyOverviewAsync_AggregatesMedicalResourceCounts()
    {
        await using var context = CreateContext();
        context.MedicalResources.AddRange(
            new MedicalResource { ResourceCode = "R1", Name = "Vent 1", Category = ResourceCategory.Ventilator, Status = ResourceStatus.Available, LocationDescription = "ICU", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MedicalResource { ResourceCode = "R2", Name = "Vent 2", Category = ResourceCategory.Ventilator, Status = ResourceStatus.InUse, LocationDescription = "ICU", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MedicalResource { ResourceCode = "R3", Name = "Monitor 1", Category = ResourceCategory.Monitor, Status = ResourceStatus.Maintenance, LocationDescription = "ICU", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MedicalResource { ResourceCode = "R4", Name = "Pump 1", Category = ResourceCategory.InfusionPump, Status = ResourceStatus.Available, LocationDescription = "Storage", IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow } // Inactive
        );
        await context.SaveChangesAsync();

        var service = new OccupancyService(context);
        var overview = await service.GetOccupancyOverviewAsync();

        Assert.Equal(3, overview.TotalMedicalResources); // Only active
        Assert.Equal(1, overview.AvailableMedicalResources);
        Assert.Equal(1, overview.InUseMedicalResources);
        Assert.Equal(1, overview.MaintenanceMedicalResources);
    }

    [Fact]
    public async Task GetWardOccupanciesAsync_ReturnsAllWardsOrderedByName()
    {
        await using var context = CreateContext();
        context.Wards.AddRange(
            new Ward { Name = "Surgical Ward", Code = "SW", Floor = "3", Capacity = 10, Type = WardType.Surgical, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ward { Name = "Cardiology Ward", Code = "CW", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new OccupancyService(context);
        var wards = await service.GetWardOccupanciesAsync();

        Assert.Equal(2, wards.Count);
        Assert.Equal("Cardiology Ward", wards[0].WardName);
        Assert.Equal("Surgical Ward", wards[1].WardName);
    }

    [Fact]
    public async Task GetWardOccupancyByIdAsync_ExistingWard_ReturnsCorrectMetrics()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "ICU", Code = "ICU-01", Floor = "2", Capacity = 4, Type = WardType.ICU, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.ICU, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.AddRange(
            new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B2", Status = BedStatus.Reserved, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B3", Status = BedStatus.Maintenance, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B4", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new OccupancyService(context);
        var occ = await service.GetWardOccupancyByIdAsync(ward.Id);

        Assert.NotNull(occ);
        Assert.Equal(ward.Id, occ.WardId);
        Assert.Equal("ICU", occ.WardName);
        Assert.Equal(4, occ.TotalBeds);
        Assert.Equal(1, occ.OccupiedBeds);
        Assert.Equal(1, occ.ReservedBeds);
        Assert.Equal(1, occ.MaintenanceBeds);
        Assert.Equal(1, occ.AvailableBeds);
        Assert.Equal(25.0, occ.OccupancyRate);
        Assert.False(occ.IsLowAvailability);
    }

    [Fact]
    public async Task GetWardOccupancyByIdAsync_NonExistentWard_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = new OccupancyService(context);

        var occ = await service.GetWardOccupancyByIdAsync(999);

        Assert.Null(occ);
    }
}
