using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Occupancy;
using SmartHospital.Api.DTOs.Wards;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class WardControllerIntegrationTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static WardController CreateController(AppDbContext context, string role = "Admin", int userId = 1) =>
        new(new WardService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, role)
                    }, "test"))
                }
            }
        };

    [Fact]
    public async Task CreateWard_ValidRequest_Returns201CreatedWithLocationHeader()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateWard(new CreateWardRequest
        {
            Name = "Cardiology",
            Code = "CARD-01",
            Floor = "3rd Floor",
            Capacity = 20,
            Type = WardType.General
        });

        var created = Assert.IsType<CreatedResult>(result);
        var ward = Assert.IsType<WardResponse>(created.Value);
        Assert.Equal($"/api/wards/{ward.Id}", created.Location);
        Assert.Equal("Cardiology", ward.Name);
        Assert.Equal("CARD-01", ward.Code);
    }

    [Fact]
    public async Task CreateWard_DuplicateCode_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        await controller.CreateWard(new CreateWardRequest { Name = "Ward 1", Code = "W1", Floor = "1", Capacity = 10, Type = WardType.General });
        var result = await controller.CreateWard(new CreateWardRequest { Name = "Ward 2", Code = "w1", Floor = "2", Capacity = 10, Type = WardType.General });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAllWards_Returns200WithWardList()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        await controller.CreateWard(new CreateWardRequest { Name = "Ward A", Code = "WA", Floor = "1", Capacity = 10 });
        await controller.CreateWard(new CreateWardRequest { Name = "Ward B", Code = "WB", Floor = "2", Capacity = 10 });

        var result = await controller.GetAllWards();

        var ok = Assert.IsType<OkObjectResult>(result);
        var wards = Assert.IsType<List<WardResponse>>(ok.Value);
        Assert.Equal(2, wards.Count);
    }

    [Fact]
    public async Task GetWardById_ExistingId_Returns200WithChildRooms()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "ICU", Code = "ICU01", Floor = "2", Capacity = 5, Type = WardType.ICU, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R-01", Capacity = 2, Type = RoomType.ICU, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "Admin");
        var result = await controller.GetWardById(ward.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<WardDetailResponse>(ok.Value);
        Assert.Equal("ICU", detail.Ward.Name);
        Assert.Single(detail.Rooms);
        Assert.Equal("R-01", detail.Rooms[0].RoomNumber);
    }

    [Fact]
    public async Task GetWardById_NonExistentId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.GetWardById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateWard_ValidRequest_Returns200()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateWard(new CreateWardRequest { Name = "Old Name", Code = "W1", Floor = "1", Capacity = 10 });
        var id = ((WardResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.UpdateWard(id, new UpdateWardRequest
        {
            Name = "New Name",
            Floor = "2",
            Capacity = 15,
            Type = WardType.Surgical,
            IsActive = true
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var ward = Assert.IsType<WardResponse>(ok.Value);
        Assert.Equal("New Name", ward.Name);
        Assert.Equal(15, ward.Capacity);
    }

    [Fact]
    public async Task UpdateWard_CapacityLessThanActiveBeds_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W1", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.AddRange(
            new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Bed { RoomId = room.Id, BedNumber = "B2", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = CreateController(context, "Admin");
        var result = await controller.UpdateWard(ward.Id, new UpdateWardRequest
        {
            Name = "Ward",
            Floor = "1",
            Capacity = 1, // Less than 2 active beds
            Type = WardType.General,
            IsActive = true
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateWard_Cascade_Returns200()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W1", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var bed = new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "Admin");
        var result = await controller.DeactivateWard(ward.Id);

        Assert.IsType<OkObjectResult>(result);
        var wardEntity = await context.Wards.FindAsync(ward.Id);
        var roomEntity = await context.Rooms.FindAsync(room.Id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);

        Assert.False(wardEntity!.IsActive);
        Assert.False(roomEntity!.IsActive);
        Assert.False(bedEntity!.IsActive);
    }

    [Fact]
    public async Task DeactivateWard_WithOccupiedBeds_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W1", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Beds.Add(new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context, "Admin");
        var result = await controller.DeactivateWard(ward.Id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ActivateWard_Cascade_Returns200()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W1", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var room = new Room { WardId = ward.Id, RoomNumber = "R1", Capacity = 4, Type = RoomType.Standard, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var bed = new Bed { RoomId = room.Id, BedNumber = "B1", Status = BedStatus.Available, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "Admin");
        var result = await controller.ActivateWard(ward.Id);

        Assert.IsType<OkObjectResult>(result);
        var wardEntity = await context.Wards.FindAsync(ward.Id);
        var roomEntity = await context.Rooms.FindAsync(room.Id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);

        Assert.True(wardEntity!.IsActive);
        Assert.True(roomEntity!.IsActive);
        Assert.True(bedEntity!.IsActive);
    }

    [Fact]
    public async Task GetWardOccupancy_ExistingId_Returns200()
    {
        await using var context = CreateContext();
        var ward = new Ward { Name = "Ward", Code = "W1", Floor = "1", Capacity = 10, Type = WardType.General, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "Admin");
        var result = await controller.GetWardOccupancy(ward.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var occ = Assert.IsType<WardOccupancyResponse>(ok.Value);
        Assert.Equal(ward.Id, occ.WardId);
    }

    [Fact]
    public async Task GetWardOccupancy_NonExistentId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.GetWardOccupancy(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void WardController_HasAuthorizeAttribute()
    {
        var attribute = typeof(WardController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
    }

    [Theory]
    [InlineData(nameof(WardController.CreateWard), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(WardController.UpdateWard), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(WardController.DeactivateWard), "Admin,ResourceAdmin")]
    [InlineData(nameof(WardController.ActivateWard), "Admin,ResourceAdmin")]
    public void WardController_ActionsEnforceRoleRestrictions(string action, string expectedRoles)
    {
        var method = typeof(WardController).GetMethod(action);
        Assert.NotNull(method);
        var attribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(expectedRoles, attribute!.Roles);
    }
}
