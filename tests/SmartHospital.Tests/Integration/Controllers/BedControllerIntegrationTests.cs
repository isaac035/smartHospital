using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Beds;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class BedControllerIntegrationTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static BedController CreateController(AppDbContext context, string role = "Admin", int userId = 1) =>
        new(new BedService(context))
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

    private static Ward SeedWard(AppDbContext context, string code = "GW01")
    {
        var ward = new Ward
        {
            Name = "General Ward",
            Code = code,
            Floor = "1st Floor",
            Capacity = 10,
            Type = WardType.General,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward);
        context.SaveChanges();
        return ward;
    }

    private static Room SeedRoom(AppDbContext context, int wardId, string roomNumber = "R-101", int capacity = 4)
    {
        var room = new Room
        {
            WardId = wardId,
            RoomNumber = roomNumber,
            Type = RoomType.Standard,
            Capacity = capacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Rooms.Add(room);
        context.SaveChanges();
        return room;
    }

    [Fact]
    public async Task CreateBed_ValidRequest_Returns201CreatedWithLocationHeader()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateBed(new CreateBedRequest
        {
            RoomId = room.Id,
            BedNumber = "B-01",
            Type = BedType.Standard
        });

        var created = Assert.IsType<CreatedResult>(result);
        var bed = Assert.IsType<BedResponse>(created.Value);
        Assert.Equal($"/api/beds/{bed.Id}", created.Location);
        Assert.Equal("B-01", bed.BedNumber);
        Assert.Equal("Available", bed.Status);
    }

    [Fact]
    public async Task CreateBed_DuplicateBedNumberInRoom_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var result = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "b-01" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task CreateBed_RoomExceedsCapacity_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id, capacity: 1);
        var controller = CreateController(context, "Admin");

        await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var result = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBeds_Returns200WithBedList()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02" });

        var result = await controller.GetBeds(new BedQueryFilter());

        var ok = Assert.IsType<OkObjectResult>(result);
        var beds = Assert.IsType<List<BedResponse>>(ok.Value);
        Assert.Equal(2, beds.Count);
    }

    [Fact]
    public async Task GetAvailableBeds_Returns200OnlyAvailableBeds()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        var bed1 = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var bed2 = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-02" });
        var b2Response = (BedResponse)((CreatedResult)bed2).Value!;

        await controller.UpdateBedStatus(b2Response.Id, new UpdateBedStatusRequest { Status = BedStatus.Maintenance });

        var result = await controller.GetAvailableBeds(wardId: ward.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var beds = Assert.IsType<List<BedResponse>>(ok.Value);
        Assert.Single(beds);
        Assert.Equal("B-01", beds[0].BedNumber);
    }

    [Fact]
    public async Task GetBedById_ExistingId_Returns200WithDetails()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var id = ((BedResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.GetBedById(id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var bed = Assert.IsType<BedResponse>(ok.Value);
        Assert.Equal(id, bed.Id);
        Assert.Equal("B-01", bed.BedNumber);
    }

    [Fact]
    public async Task GetBedById_NonExistentId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.GetBedById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBed_ValidRequest_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01", Type = BedType.Standard });
        var id = ((BedResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.UpdateBed(id, new UpdateBedRequest
        {
            BedNumber = "B-01-ICU",
            Type = BedType.ICU,
            IsActive = true
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var bed = Assert.IsType<BedResponse>(ok.Value);
        Assert.Equal("B-01-ICU", bed.BedNumber);
        Assert.Equal("ICU", bed.Type);
    }

    [Fact]
    public async Task UpdateBed_NonExistentId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.UpdateBed(999, new UpdateBedRequest
        {
            BedNumber = "B-999",
            Type = BedType.Standard,
            IsActive = true
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBedStatus_AvailableToMaintenance_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Staff");

        var created = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var id = ((BedResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.UpdateBedStatus(id, new UpdateBedStatusRequest { Status = BedStatus.Maintenance });

        var ok = Assert.IsType<OkObjectResult>(result);
        var bed = Assert.IsType<BedResponse>(ok.Value);
        Assert.Equal("Maintenance", bed.Status);
    }

    [Fact]
    public async Task UpdateBedStatus_CannotManuallyMarkOccupied_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Staff");

        var created = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var id = ((BedResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.UpdateBedStatus(id, new UpdateBedStatusRequest { Status = BedStatus.Occupied });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateBed_UnoccupiedBed_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var id = ((BedResponse)((CreatedResult)created).Value!).Id;

        var result = await controller.DeactivateBed(id);

        Assert.IsType<OkObjectResult>(result);
        var bedEntity = await context.Beds.FindAsync(id);
        Assert.False(bedEntity!.IsActive);
    }

    [Fact]
    public async Task DeactivateBed_OccupiedBed_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var id = ((BedResponse)((CreatedResult)created).Value!).Id;

        var bedEntity = await context.Beds.FindAsync(id);
        bedEntity!.Status = BedStatus.Occupied;
        await context.SaveChangesAsync();

        var result = await controller.DeactivateBed(id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateBed_NonExistentId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.DeactivateBed(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ActivateBed_DeactivatedBed_Returns200()
    {
        await using var context = CreateContext();
        var ward = SeedWard(context);
        var room = SeedRoom(context, ward.Id);
        var controller = CreateController(context, "Admin");

        var created = await controller.CreateBed(new CreateBedRequest { RoomId = room.Id, BedNumber = "B-01" });
        var id = ((BedResponse)((CreatedResult)created).Value!).Id;

        await controller.DeactivateBed(id);
        var result = await controller.ActivateBed(id);

        Assert.IsType<OkObjectResult>(result);
        var bedEntity = await context.Beds.FindAsync(id);
        Assert.True(bedEntity!.IsActive);
    }

    [Fact]
    public void BedController_HasAuthorizeAttribute()
    {
        var attribute = typeof(BedController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
    }

    [Theory]
    [InlineData(nameof(BedController.CreateBed), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(BedController.UpdateBed), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(BedController.UpdateBedStatus), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(BedController.DeactivateBed), "Admin,ResourceAdmin")]
    [InlineData(nameof(BedController.ActivateBed), "Admin,ResourceAdmin")]
    public void BedController_ActionsEnforceRoleRestrictions(string action, string expectedRoles)
    {
        var method = typeof(BedController).GetMethod(action);
        Assert.NotNull(method);
        var attribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(expectedRoles, attribute!.Roles);
    }
}
