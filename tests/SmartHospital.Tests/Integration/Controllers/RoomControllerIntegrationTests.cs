using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Rooms;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class RoomControllerIntegrationTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static RoomController CreateController(AppDbContext context, string role = "Admin", int userId = 1) =>
        new(new RoomService(context))
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

    private static Ward SeedWard(AppDbContext context, string code = "GW01", bool isActive = true)
    {
        var ward = new Ward
        {
            Name = "General Ward",
            Code = code,
            Floor = "1st Floor",
            Capacity = 10,
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
    public async Task CreateRoom_ValidRequest_Returns201CreatedWithLocationHeader()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateRoom(new CreateRoomRequest
        {
            WardId = ward.Id,
            RoomNumber = "R-101",
            Type = RoomType.Standard,
            Capacity = 4
        });

        var created = Assert.IsType<CreatedResult>(result);
        var room = Assert.IsType<RoomResponse>(created.Value);
        Assert.Equal($"/api/rooms/{room.Id}", created.Location);
        Assert.Equal("R-101", room.RoomNumber);
        Assert.Equal(ward.Id, room.WardId);
    }

    [Fact]
    public async Task CreateRoom_DuplicateRoomNumber_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var result = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "r-101" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRoom_InactiveWard_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context, isActive: false);
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetRoomsByWard_Returns200WithRoomList()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-102" });

        var result = await controller.GetRoomsByWard(ward.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var rooms = Assert.IsType<List<RoomResponse>>(ok.Value);
        Assert.Equal(2, rooms.Count);
    }

    [Fact]
    public async Task GetRoomById_ExistingId_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var id = ((RoomResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.GetRoomById(id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var room = Assert.IsType<RoomResponse>(ok.Value);
        Assert.Equal(id, room.Id);
        Assert.Equal("R-101", room.RoomNumber);
    }

    [Fact]
    public async Task GetRoomById_NonExistentId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.GetRoomById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRoom_ValidRequest_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101", Type = RoomType.Standard });
        var id = ((RoomResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.UpdateRoom(id, new UpdateRoomRequest
        {
            RoomNumber = "R-101-NEW",
            Type = RoomType.Private,
            Capacity = 4,
            IsActive = true
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var room = Assert.IsType<RoomResponse>(ok.Value);
        Assert.Equal("R-101-NEW", room.RoomNumber);
        Assert.Equal("Private", room.Type);
    }

    [Fact]
    public async Task UpdateRoom_DuplicateNumber_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var r2 = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-102" });
        var id2 = ((RoomResponse)((CreatedResult)r2).Value!).Id;

        var result = await controller.UpdateRoom(id2, new UpdateRoomRequest
        {
            RoomNumber = "R-101",
            Type = RoomType.Standard,
            Capacity = 4,
            IsActive = true
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateRoom_Cascade_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var id = ((RoomResponse)((CreatedResult)created).Value!).Id;

        var bed = new Bed { RoomId = id, BedNumber = "B-01", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        var result = await controller.DeactivateRoom(id);

        Assert.IsType<OkObjectResult>(result);
        var roomEntity = await context.Rooms.FindAsync(id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.False(roomEntity!.IsActive);
        Assert.False(bedEntity!.IsActive);
    }

    [Fact]
    public async Task DeactivateRoom_WithOccupiedBeds_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var id = ((RoomResponse)((CreatedResult)created).Value!).Id;

        context.Beds.Add(new Bed { RoomId = id, BedNumber = "B-01", Status = BedStatus.Occupied, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var result = await controller.DeactivateRoom(id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ActivateRoom_Cascade_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateRoom(new CreateRoomRequest { WardId = ward.Id, RoomNumber = "R-101" });
        var id = ((RoomResponse)((CreatedResult)created).Value!).Id;

        var bed = new Bed { RoomId = id, BedNumber = "B-01", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed);
        await context.SaveChangesAsync();

        await controller.DeactivateRoom(id);
        var result = await controller.ActivateRoom(id);

        Assert.IsType<OkObjectResult>(result);
        var roomEntity = await context.Rooms.FindAsync(id);
        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.True(roomEntity!.IsActive);
        Assert.True(bedEntity!.IsActive);
    }

    [Fact]
    public void RoomController_HasAuthorizeAttribute()
    {
        var attribute = typeof(RoomController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
    }

    [Theory]
    [InlineData(nameof(RoomController.CreateRoom), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(RoomController.UpdateRoom), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(RoomController.DeactivateRoom), "Admin,ResourceAdmin")]
    [InlineData(nameof(RoomController.ActivateRoom), "Admin,ResourceAdmin")]
    public void RoomController_ActionsEnforceRoleRestrictions(string action, string expectedRoles)
    {
        var method = typeof(RoomController).GetMethod(action);
        Assert.NotNull(method);
        var attribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(expectedRoles, attribute!.Roles);
    }
}
