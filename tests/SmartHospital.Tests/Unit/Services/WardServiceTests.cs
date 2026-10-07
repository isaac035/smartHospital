using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Wards;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class WardServiceTests
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
    public async Task CreateWardAsync_ValidRequest_CreatesActiveWardWithUppercasedCode()
    {
        await using var context = CreateContext();
        var service = new WardService(context);

        var response = await service.CreateWardAsync(new CreateWardRequest
        {
            Name = "  Cardiology Ward ",
            Code = "  card-01 ",
            Floor = "3rd Floor",
            BuildingBlock = "Building B",
            Capacity = 20,
            Type = WardType.General
        });

        Assert.True(response.Id > 0);
        Assert.Equal("Cardiology Ward", response.Name);
        Assert.Equal("CARD-01", response.Code);
        Assert.Equal("3rd Floor", response.Floor);
        Assert.Equal("Building B", response.BuildingBlock);
        Assert.Equal(20, response.Capacity);
        Assert.Equal("General", response.Type);
        Assert.True(response.IsActive);
        Assert.Equal(0, response.TotalRooms);
        Assert.Equal(0, response.TotalBeds);
    }

    [Fact]
    public async Task CreateWardAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = new WardService(context);

        await service.CreateWardAsync(new CreateWardRequest
        {
            Name = "Ward A",
            Code = "WA01",
            Floor = "1",
            Capacity = 10,
            Type = WardType.General
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateWardAsync(new CreateWardRequest
            {
                Name = "Ward B",
                Code = "wa01",
                Floor = "2",
                Capacity = 10,
                Type = WardType.General
            }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task GetAllWardsAsync_ComputesTotalRoomsTotalBedsAvailableAndOccupied()
    {
        await using var context = CreateContext();
        var ward = new Ward
        {
            Name = "Pediatrics",
            Code = "PED-01",
            Floor = "2",
            Capacity = 10,
            Type = WardType.Pediatric,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room
        {
            WardId = ward.Id,
            RoomNumber = "PED-R1",
            Capacity = 4,
            Type = RoomType.Standard,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.AddRange(
            new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B2", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B3", Status = BedStatus.Maintenance, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new WardService(context);
        var wards = await service.GetAllWardsAsync();

        Assert.Single(wards);
        var item = wards[0];
        Assert.Equal(1, item.TotalRooms);
        Assert.Equal(2, item.TotalBeds); // Only active beds
        Assert.Equal(1, item.AvailableBeds);
        Assert.Equal(1, item.OccupiedBeds);
        Assert.Equal(50.0, item.OccupancyRate);
    }

    [Fact]
    public async Task GetAllWardsAsync_FiltersByIsActive()
    {
        await using var context = CreateContext();
        context.Wards.AddRange(
            new Ward { Name = "Active Ward", Code = "AW", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ward { Name = "Inactive Ward", Code = "IW", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new WardService(context);

        var activeOnly = await service.GetAllWardsAsync(isActive: true);
        var inactiveOnly = await service.GetAllWardsAsync(isActive: false);

        Assert.Single(activeOnly);
        Assert.Equal("Active Ward", activeOnly[0].Name);
        Assert.Single(inactiveOnly);
        Assert.Equal("Inactive Ward", inactiveOnly[0].Name);
    }

    [Fact]
    public async Task GetWardByIdAsync_ExistingId_ReturnsWardWithChildRoomsAndBedCounts()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "ICU Ward", Code = "ICU-01", Floor = "3", Capacity = 6, Type = WardType.ICU, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "ICU-R1", Capacity = 2, Type = RoomType.ICU, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.Add(new Bed { RoomId = room.Id, BedNumber = "ICU-B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new WardService(context);
        var result = await service.GetWardByIdAsync(ward.Id);

        Assert.NotNull(result);
        Assert.Equal("ICU Ward", result.Ward.Name);
        Assert.Single(result.Rooms);
        Assert.Equal("ICU-R1", result.Rooms[0].RoomNumber);
        Assert.Equal(1, result.Rooms[0].TotalBeds);
        Assert.Equal(1, result.Rooms[0].AvailableBeds);
    }

    [Fact]
    public async Task GetWardByIdAsync_UnknownId_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = new WardService(context);

        var result = await service.GetWardByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateWardAsync_ValidUpdate_UpdatesProperties()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Old Name", Code = "W01", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var service = new WardService(context);
        var updated = await service.UpdateWardAsync(ward.Id, new UpdateWardRequest
        {
            Name = "New Name",
            Floor = "2nd Floor",
            BuildingBlock = "Block C",
            Capacity = 15,
            Type = WardType.Surgical,
            IsActive = true
        });

        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("2nd Floor", updated.Floor);
        Assert.Equal(15, updated.Capacity);
        Assert.Equal("Surgical", updated.Type);
    }

    [Fact]
    public async Task UpdateWardAsync_CapacityLessThanActiveBeds_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W01", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 5, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.AddRange(
            new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B2", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B3", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new WardService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateWardAsync(ward.Id, new UpdateWardRequest
            {
                Name = ward.Name,
                Floor = ward.Floor,
                Capacity = 2, // Less than 3 active beds
                Type = ward.Type,
                IsActive = true
            }));

        Assert.Contains("capacity cannot be less than current active beds", ex.Message);
    }

    [Fact]
    public async Task UpdateWardAsync_CannotDeactivateWardWithOccupiedBeds_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W01", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.Add(new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new WardService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateWardAsync(ward.Id, new UpdateWardRequest
            {
                Name = ward.Name,
                Floor = ward.Floor,
                Capacity = ward.Capacity,
                Type = ward.Type,
                IsActive = false
            }));

        Assert.Contains("occupied beds", ex.Message);
    }

    [Fact]
    public async Task DeactivateWardAsync_CascadesDeactivationToRoomsAndBeds()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W01", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var bed = new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        var service = new WardService(context);
        var success = await service.DeactivateWardAsync(ward.Id);

        Assert.True(success);

        var wardEntity = await context.Wards.FindAsync(ward.Id);
        var roomEntity = await context.Rooms.FindAsync(room.Id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);

        Assert.False(wardEntity!.IsActive);
        Assert.False(roomEntity!.IsActive);
        Assert.False(bedEntity!.IsActive);
    }

    [Fact]
    public async Task DeactivateWardAsync_WithOccupiedBeds_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W01", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.Add(new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new WardService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeactivateWardAsync(ward.Id));

        Assert.Contains("occupied beds", ex.Message);
    }

    [Fact]
    public async Task DeactivateWardAsync_NonExistentId_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new WardService(context);

        Assert.False(await service.DeactivateWardAsync(999));
    }

    [Fact]
    public async Task ActivateWardAsync_CascadesActivationToRoomsAndBeds()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W01", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var bed = new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        var service = new WardService(context);
        var success = await service.ActivateWardAsync(ward.Id);

        Assert.True(success);

        var wardEntity = await context.Wards.FindAsync(ward.Id);
        var roomEntity = await context.Rooms.FindAsync(room.Id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);

        Assert.True(wardEntity!.IsActive);
        Assert.True(roomEntity!.IsActive);
        Assert.True(bedEntity!.IsActive);
    }

    [Fact]
    public async Task ActivateWardAsync_NonExistentId_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new WardService(context);

        Assert.False(await service.ActivateWardAsync(999));
    }

    [Fact]
    public async Task GetWardOccupancyAsync_CalculatesOccupancyRateAndLowAvailabilityFlag()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Maternity", Code = "MAT-01", Floor = "4", Capacity = 5, Type = WardType.Maternity, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "MAT-R1", Capacity = 5, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        // 5 beds: 4 occupied, 1 available (20% available -> IsLowAvailability = true)
        context.Beds.AddRange(
            new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B2", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B3", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B4", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B5", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new WardService(context);
        var occupancy = await service.GetWardOccupancyAsync(ward.Id);

        Assert.NotNull(occupancy);
        Assert.Equal(5, occupancy.TotalBeds);
        Assert.Equal(4, occupancy.OccupiedBeds);
        Assert.Equal(1, occupancy.AvailableBeds);
        Assert.Equal(80.0, occupancy.OccupancyRate);
        Assert.True(occupancy.IsLowAvailability);
    }

    [Fact]
    public async Task GetWardOccupancyAsync_NonExistentId_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = new WardService(context);

        var result = await service.GetWardOccupancyAsync(999);

        Assert.Null(result);
    }
}
