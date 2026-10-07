using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Rooms;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class RoomServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static Ward SeedWard(AppDbContext context, string name = "General Ward", string code = "GW01", bool isActive = true)
    {
        var ward = new Ward
        {
            Name = name,
            Code = code,
            Floor = "1st Floor",
            Capacity = 20,
            Type = WardType.General,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward);
        context.SaveChanges();
        return ward;
    }

    [Fact]
    public async Task CreateRoomAsync_ValidRequest_CreatesRoomWithStandardCapacity4()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var response = await service.CreateRoomAsync(new CreateRoomRequest
        {
            WardId = ward.Id,
            RoomNumber = "  r-101  ",
            Type = RoomType.Standard,
            Capacity = 10 // Code forces StandardRoomCapacity = 4
        });

        Assert.True(response.Id > 0);
        Assert.Equal("R-101", response.RoomNumber);
        Assert.Equal(ward.Id, response.WardId);
        Assert.Equal(ward.Name, response.WardName);
        Assert.Equal(4, response.Capacity); // Hardcoded standard capacity
        Assert.True(response.IsActive);
        Assert.Equal(0, response.TotalBeds);
    }

    [Fact]
    public async Task CreateRoomAsync_TargetWardNotFound_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = new RoomService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRoomAsync(new CreateRoomRequest
            {
                WardId = 999,
                RoomNumber = "R-101",
                Type = RoomType.Standard
            }));

        Assert.Contains("does not exist or is inactive", ex.Message);
    }

    [Fact]
    public async Task CreateRoomAsync_TargetWardInactive_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context, isActive: false);
        var service = new RoomService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRoomAsync(new CreateRoomRequest
            {
                WardId = ward.Id,
                RoomNumber = "R-101",
                Type = RoomType.Standard
            }));

        Assert.Contains("does not exist or is inactive", ex.Message);
    }

    [Fact]
    public async Task CreateRoomAsync_DuplicateRoomNumberAcrossHospital_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward1 = SeedWard(context, "Ward 1", "W01");
        var ward2 = SeedWard(context, "Ward 2", "W02");
        var service = new RoomService(context);

        await service.CreateRoomAsync(new CreateRoomRequest
        {
            WardId = ward1.Id,
            RoomNumber = "R-100",
            Type = RoomType.Standard
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRoomAsync(new CreateRoomRequest
            {
                WardId = ward2.Id,
                RoomNumber = "r-100",
                Type = RoomType.Standard
            }));

        Assert.Contains("already exists in the hospital", ex.Message);
    }

    [Fact]
    public async Task GetRoomsByWardAsync_ReturnsRoomsOrderedByRoomNumber()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-200" });
        await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-100" });

        var rooms = await service.GetRoomsByWardAsync(ward.Id);

        Assert.Equal(2, rooms.Count);
        Assert.Equal("R-100", rooms[0].RoomNumber);
        Assert.Equal("R-200", rooms[1].RoomNumber);
    }

    [Fact]
    public async Task GetRoomsByWardAsync_FiltersByIsActive()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var r1 = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var r2 = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-102" });
        await service.DeactivateRoomAsync(r2.Id);

        var activeOnly = await service.GetRoomsByWardAsync(ward.Id, isActive: true);
        var inactiveOnly = await service.GetRoomsByWardAsync(ward.Id, isActive: false);

        Assert.Single(activeOnly);
        Assert.Equal("R-101", activeOnly[0].RoomNumber);
        Assert.Single(inactiveOnly);
        Assert.Equal("R-102", inactiveOnly[0].RoomNumber);
    }

    [Fact]
    public async Task GetRoomByIdAsync_ExistingId_ReturnsRoomWithBedCounts()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var created = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101", Type = RoomType.Standard });

        context.Beds.AddRange(
            new Bed { RoomId = created.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = created.Id, BedNumber = "B2", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var fetched = await service.GetRoomByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(2, fetched.TotalBeds);
        Assert.Equal(1, fetched.AvailableBeds);
        Assert.Equal(1, fetched.OccupiedBeds);
    }

    [Fact]
    public async Task GetRoomByIdAsync_NonExistentId_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = new RoomService(context);

        var room = await service.GetRoomByIdAsync(999);

        Assert.Null(room);
    }

    [Fact]
    public async Task UpdateRoomAsync_ValidUpdate_UpdatesRoomNumberAndType()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var created = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101", Type = RoomType.Standard });

        var updated = await service.UpdateRoomAsync(created.Id, new UpdateRoomRequest
        {
            RoomNumber = "R-101-NEW",
            Type = RoomType.Private,
            Capacity = 4,
            IsActive = true
        });

        Assert.NotNull(updated);
        Assert.Equal("R-101-NEW", updated.RoomNumber);
        Assert.Equal("Private", updated.Type);
    }

    [Fact]
    public async Task UpdateRoomAsync_DuplicateRoomNumber_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var r2 = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-102" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoomAsync(r2.Id, new UpdateRoomRequest
            {
                RoomNumber = "R-101",
                Type = RoomType.Standard,
                Capacity = 4,
                IsActive = true
            }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateRoomAsync_SameRoomNumberUnchanged_Succeeds()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var created = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101", Type = RoomType.Standard });

        var updated = await service.UpdateRoomAsync(created.Id, new UpdateRoomRequest
        {
            RoomNumber = "R-101",
            Type = RoomType.ICU,
            Capacity = 4,
            IsActive = true
        });

        Assert.NotNull(updated);
        Assert.Equal("ICU", updated.Type);
    }

    [Fact]
    public async Task UpdateRoomAsync_CannotDeactivateRoomWithOccupiedBeds_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var room = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });

        context.Beds.Add(new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoomAsync(room.Id, new UpdateRoomRequest
            {
                RoomNumber = "R-101",
                Type = RoomType.Standard,
                Capacity = 4,
                IsActive = false
            }));

        Assert.Contains("occupied beds", ex.Message);
    }

    [Fact]
    public async Task DeactivateRoomAsync_CascadesDeactivationToBeds()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var room = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var bed = new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        var success = await service.DeactivateRoomAsync(room.Id);

        Assert.True(success);
        var roomEntity = await context.Rooms.FindAsync(room.Id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.False(roomEntity!.IsActive);
        Assert.False(bedEntity!.IsActive);
    }

    [Fact]
    public async Task DeactivateRoomAsync_WithOccupiedBeds_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var room = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        context.Beds.Add(new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeactivateRoomAsync(room.Id));

        Assert.Contains("occupied beds", ex.Message);
    }

    [Fact]
    public async Task DeactivateRoomAsync_NonExistentId_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new RoomService(context);

        Assert.False(await service.DeactivateRoomAsync(999));
    }

    [Fact]
    public async Task ActivateRoomAsync_CascadesActivationToBeds()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var service = new RoomService(context);

        var room = await service.CreateRoomAsync(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var bed = new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        await service.DeactivateRoomAsync(room.Id);
        var success = await service.ActivateRoomAsync(room.Id);

        Assert.True(success);
        var roomEntity = await context.Rooms.FindAsync(room.Id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.True(roomEntity!.IsActive);
        Assert.True(bedEntity!.IsActive);
    }

    [Fact]
    public async Task ActivateRoomAsync_NonExistentId_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new RoomService(context);

        Assert.False(await service.ActivateRoomAsync(999));
    }
}
