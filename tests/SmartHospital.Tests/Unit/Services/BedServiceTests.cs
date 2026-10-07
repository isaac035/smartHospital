using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Beds;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class BedServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static Ward SeedWard(AppDbContext context, string name = "General Ward", string code = "GW01", int capacity = 10, bool isActive = true)
    {
        var ward = new Ward
        {
            Name = name,
            Code = code,
            Floor = "1st Floor",
            BuildingBlock = "Block A",
            Capacity = capacity,
            Type = WardType.General,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward);
        context.SaveChanges();
        return ward;
    }

    private static Room SeedRoom(AppDbContext context, int wardId, string roomNumber = "R-101", int capacity = 4, bool isActive = true)
    {
        var room = new Room
        {
            WardId = wardId,
            RoomNumber = roomNumber,
            Type = RoomType.Standard,
            Capacity = capacity,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Rooms.Add(room);
        context.SaveChanges();
        return room;
    }

    [Fact]
    public async Task CreateBedAsync_ValidRequest_CreatesBedWithAvailableStatusAndUppercaseBedNumber()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var response = await service.CreateBedAsync(new CreateBedRequest
        {
            RoomId = room.Id,
            BedNumber = "  b-101a  ",
            Type = BedType.Standard
        });

        Assert.True(response.Id > 0);
        Assert.Equal("B-101A", response.BedNumber);
        Assert.Equal(room.Id, response.RoomId);
        Assert.Equal(room.RoomNumber, response.RoomNumber);
        Assert.Equal(ward.Id, response.WardId);
        Assert.Equal(ward.Name, response.WardName);
        Assert.Equal("Available", response.Status);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task CreateBedAsync_TargetRoomNotFound_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = new BedService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBedAsync(new CreateBedRequest
            {
                RoomId = 999,
                BedNumber = "B-01",
                Type = BedType.Standard
            }));

        Assert.Contains("does not exist or is inactive", ex.Message);
    }

    [Fact]
    public async Task CreateBedAsync_TargetRoomInactive_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id, isActive: false);
        var service = new BedService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBedAsync(new CreateBedRequest
            {
                RoomId = room.Id,
                BedNumber = "B-01",
                Type = BedType.Standard
            }));

        Assert.Contains("does not exist or is inactive", ex.Message);
    }

    [Fact]
    public async Task CreateBedAsync_ExceedsRoomCapacity_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id, capacity: 1);
        var service = new BedService(context);

        await service.CreateBedAsync(new CreateBedRequest
        {
            RoomId = room.Id,
            BedNumber = "B-01",
            Type = BedType.Standard
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBedAsync(new CreateBedRequest
            {
                RoomId = room.Id,
                BedNumber = "B-02",
                Type = BedType.Standard
            }));

        Assert.Contains("capacity", ex.Message);
    }

    [Fact]
    public async Task CreateBedAsync_DuplicateBedNumberInSameRoom_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id, capacity: 4);
        var service = new BedService(context);

        await service.CreateBedAsync(new CreateBedRequest
        {
            RoomId = room.Id,
            BedNumber = "B-01",
            Type = BedType.Standard
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBedAsync(new CreateBedRequest
            {
                RoomId = room.Id,
                BedNumber = " b-01 ",
                Type = BedType.Standard
            }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateBedAsync_SameBedNumberInDifferentRoom_Succeeds()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room1 = SeedRoom(context, ward.Id, "R-101");
        var room2 = SeedRoom(context, ward.Id, "R-102");
        var service = new BedService(context);

        var bed1 = await service.CreateBedAsync(new CreateBedRequest
        {
            RoomId = room1.Id,
            BedNumber = "B-01",
            Type = BedType.Standard
        });

        var bed2 = await service.CreateBedAsync(new CreateBedRequest
        {
            RoomId = room2.Id,
            BedNumber = "B-01",
            Type = BedType.Standard
        });

        Assert.Equal("B-01", bed1.BedNumber);
        Assert.Equal("B-01", bed2.BedNumber);
        Assert.NotEqual(bed1.RoomId, bed2.RoomId);
    }

    [Fact]
    public async Task GetBedsAsync_WithoutFilters_ReturnsPagedBeds()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02" });

        var beds = await service.GetBedsAsync(new BedQueryFilter());

        Assert.Equal(2, beds.Count);
    }

    [Fact]
    public async Task GetBedsAsync_FilterByWardId_ReturnsOnlyMatchingBeds()
    {
        await using var context = CreateContext();
        var ward1 = SeedWard(context, "Ward 1", "W01");
        var ward2 = SeedWard(context, "Ward 2", "W02");
        var room1 = SeedRoom(context, ward1.Id, "R-101");
        var room2 = SeedRoom(context, ward2.Id, "R-201");
        var service = new BedService(context);

        await service.CreateBedAsync(new CreateBedRequest { RoomId = room1.Id, BedNumber = "B-01" });
        await service.CreateBedAsync(new CreateBedRequest { RoomId = room2.Id, BedNumber = "B-02" });

        var result = await service.GetBedsAsync(new BedQueryFilter { WardId = ward1.Id });

        Assert.Single(result);
        Assert.Equal("B-01", result[0].BedNumber);
        Assert.Equal(ward1.Id, result[0].WardId);
    }

    [Fact]
    public async Task GetBedsAsync_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed1 = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var bed2 = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02" });

        await service.UpdateBedStatusAsync(bed2.Id, new UpdateBedStatusRequest { Status = BedStatus.Maintenance });

        var maintenanceBeds = await service.GetBedsAsync(new BedQueryFilter { Status = BedStatus.Maintenance });

        Assert.Single(maintenanceBeds);
        Assert.Equal("B-02", maintenanceBeds[0].BedNumber);
    }

    [Fact]
    public async Task GetAvailableBedsAsync_ExcludesOccupiedAndMaintenanceBeds()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed1 = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var bed2 = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02" });

        await service.UpdateBedStatusAsync(bed2.Id, new UpdateBedStatusRequest { Status = BedStatus.Maintenance });

        var available = await service.GetAvailableBedsAsync(ward.Id);

        Assert.Single(available);
        Assert.Equal("B-01", available[0].BedNumber);
    }

    [Fact]
    public async Task GetAvailableBedsAsync_ExcludesBedsWithActiveAllocations()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });

        // Seed an admission and active allocation directly
        var patient = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@hospital.test",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(patient);
        await context.SaveChangesAsync();

        var admission = new Admission
        {
            AdmissionNumber = "ADM-20261006-1111",
            PatientId = patient.Id,
            Status = AdmissionStatus.Admitted,
            Priority = AdmissionPriority.Normal,
            ReasonForAdmission = "Observation",
            AdmissionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Admissions.Add(admission);
        await context.SaveChangesAsync();

        context.BedAllocations.Add(new BedAllocation
        {
            AdmissionId = admission.Id,
            BedId = bed.Id,
            AllocatedAt = DateTime.UtcNow,
            Status = BedAllocationStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var available = await service.GetAvailableBedsAsync(ward.Id);

        Assert.Empty(available);
    }

    [Fact]
    public async Task GetAvailableBedsAsync_FilterByBedType_ReturnsOnlyMatchingType()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01", Type = BedType.Standard });
        await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02", Type = BedType.ICU });

        var icuBeds = await service.GetAvailableBedsAsync(ward.Id, type: BedType.ICU);

        Assert.Single(icuBeds);
        Assert.Equal("B-02", icuBeds[0].BedNumber);
        Assert.Equal("ICU", icuBeds[0].Type);
    }

    [Fact]
    public async Task GetBedByIdAsync_ExistingId_ReturnsBedWithRoomAndWardDetails()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var created = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });

        var fetched = await service.GetBedByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("B-01", fetched.BedNumber);
        Assert.Equal(room.RoomNumber, fetched.RoomNumber);
        Assert.Equal(ward.Name, fetched.WardName);
    }

    [Fact]
    public async Task GetBedByIdAsync_NonExistentId_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = new BedService(context);

        var bed = await service.GetBedByIdAsync(999);

        Assert.Null(bed);
    }

    [Fact]
    public async Task UpdateBedAsync_ValidUpdate_UpdatesBedNumberAndType()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var created = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01", Type = BedType.Standard });

        var updated = await service.UpdateBedAsync(created.Id, new UpdateBedRequest
        {
            BedNumber = "B-01-RENAMED",
            Type = BedType.ICU,
            IsActive = true
        });

        Assert.NotNull(updated);
        Assert.Equal("B-01-RENAMED", updated.BedNumber);
        Assert.Equal("ICU", updated.Type);
    }

    [Fact]
    public async Task UpdateBedAsync_DuplicateBedNumberInSameRoom_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var bed2 = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateBedAsync(bed2.Id, new UpdateBedRequest
            {
                BedNumber = "B-01",
                Type = BedType.Standard,
                IsActive = true
            }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateBedAsync_CannotDeactivateOccupiedBed_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });

        // Manually mark entity as occupied
        var bedEntity = await context.Beds.FindAsync(bed.Id);
        bedEntity!.Status = BedStatus.Occupied;
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateBedAsync(bed.Id, new UpdateBedRequest
            {
                BedNumber = "B-01",
                Type = BedType.Standard,
                IsActive = false
            }));

        Assert.Contains("occupied", ex.Message);
    }

    [Fact]
    public async Task UpdateBedStatusAsync_CanChangeAvailableToMaintenance()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });

        var result = await service.UpdateBedStatusAsync(bed.Id, new UpdateBedStatusRequest { Status = BedStatus.Maintenance });

        Assert.NotNull(result);
        Assert.Equal("Maintenance", result.Status);
    }

    [Fact]
    public async Task UpdateBedStatusAsync_CannotManuallyMarkAsOccupied_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateBedStatusAsync(bed.Id, new UpdateBedStatusRequest { Status = BedStatus.Occupied }));

        Assert.Contains("cannot be manually marked as Occupied", ex.Message);
    }

    [Fact]
    public async Task DeactivateBedAsync_UnoccupiedBed_SetsIsActiveFalse()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });

        var success = await service.DeactivateBedAsync(bed.Id);

        Assert.True(success);
        var entity = await context.Beds.FindAsync(bed.Id);
        Assert.False(entity!.IsActive);
    }

    [Fact]
    public async Task DeactivateBedAsync_OccupiedBed_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });

        var entity = await context.Beds.FindAsync(bed.Id);
        entity!.Status = BedStatus.Occupied;
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeactivateBedAsync(bed.Id));

        Assert.Contains("occupied", ex.Message);
    }

    [Fact]
    public async Task DeactivateBedAsync_NonExistentId_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new BedService(context);

        var success = await service.DeactivateBedAsync(999);

        Assert.False(success);
    }

    [Fact]
    public async Task ActivateBedAsync_DeactivatedBed_SetsIsActiveTrue()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var service = new BedService(context);

        var bed = await service.CreateBedAsync(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        await service.DeactivateBedAsync(bed.Id);

        var success = await service.ActivateBedAsync(bed.Id);

        Assert.True(success);
        var entity = await context.Beds.FindAsync(bed.Id);
        Assert.True(entity!.IsActive);
    }

    [Fact]
    public async Task ActivateBedAsync_NonExistentId_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new BedService(context);

        var success = await service.ActivateBedAsync(999);

        Assert.False(success);
    }
}
