using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.MedicalResources;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Integration.Controllers;

public class MedicalResourceControllerIntegrationTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static MedicalResourceController CreateController(AppDbContext context, string role = "Admin", int userId = 1) =>
        new(new MedicalResourceService(context))
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

    private static MedicalResource SeedResource(
        AppDbContext context,
        string code = "VENT-001",
        string name = "Mechanical Ventilator",
        ResourceStatus status = ResourceStatus.Available,
        bool isActive = true)
    {
        var resource = new MedicalResource
        {
            ResourceCode = code,
            Name = name,
            Category = ResourceCategory.Ventilator,
            Status = status,
            LocationDescription = "ICU Equipment Bay",
            SerialNumber = "SN-998877",
            Manufacturer = "MedTech",
            ModelNumber = "V-2000",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.MedicalResources.Add(resource);
        context.SaveChanges();
        return resource;
    }

    [Fact]
    public async Task CreateResource_ValidRequest_Returns201CreatedWithLocationHeader()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.CreateResource(new CreateResourceRequest
        {
            ResourceCode = "DEFIB-001",
            Name = "Automated External Defibrillator",
            Category = ResourceCategory.Defibrillator,
            LocationDescription = "Emergency Room Bay 1"
        });

        var created = Assert.IsType<CreatedResult>(result);
        var resource = Assert.IsType<MedicalResourceResponse>(created.Value);
        Assert.Equal($"/api/medical-resources/{resource.Id}", created.Location);
        Assert.Equal("DEFIB-001", resource.ResourceCode);
        Assert.Equal("Available", resource.Status);
    }

    [Fact]
    public async Task CreateResource_DuplicateCode_Returns400BadRequest()
    {
        await using var context = CreateContext();
        SeedResource(context, "VENT-001");
        var controller = CreateController(context, "Staff");

        var result = await controller.CreateResource(new CreateResourceRequest
        {
            ResourceCode = "VENT-001",
            Name = "Duplicate Ventilator",
            Category = ResourceCategory.Ventilator,
            LocationDescription = "Storage"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task CreateResource_InvalidLocationHierarchy_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "ResourceAdmin");

        var result = await controller.CreateResource(new CreateResourceRequest
        {
            ResourceCode = "MON-001",
            Name = "Patient Monitor",
            Category = ResourceCategory.Monitor,
            LocationDescription = "Ward 1",
            WardId = 999,
            RoomId = 888
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetResources_WithFilter_Returns200OkWithList()
    {
        await using var context = CreateContext();
        SeedResource(context, "VENT-001", "Ventilator 1");
        SeedResource(context, "VENT-002", "Ventilator 2");
        var controller = CreateController(context);

        var result = await controller.GetResources(new ResourceQueryFilter
        {
            Category = ResourceCategory.Ventilator
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<MedicalResourceResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetResourceById_ExistingId_Returns200Ok()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, "PUMP-001", "Infusion Pump");
        var controller = CreateController(context);

        var result = await controller.GetResourceById(resource.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MedicalResourceResponse>(ok.Value);
        Assert.Equal(resource.Id, response.Id);
        Assert.Equal("PUMP-001", response.ResourceCode);
    }

    [Fact]
    public async Task GetResourceById_NonExistingId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetResourceById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateResource_ExistingResource_Returns200Ok()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, "ECG-001", "ECG Machine");
        var controller = CreateController(context, "ResourceAdmin");

        var result = await controller.UpdateResource(resource.Id, new UpdateResourceRequest
        {
            Name = "Advanced ECG Machine",
            Category = ResourceCategory.Other,
            Status = ResourceStatus.Maintenance,
            LocationDescription = "Cardiology Suite",
            IsActive = true
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<MedicalResourceResponse>(ok.Value);
        Assert.Equal("Advanced ECG Machine", updated.Name);
        Assert.Equal("Maintenance", updated.Status);
    }

    [Fact]
    public async Task UpdateResource_NonExistingId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.UpdateResource(999, new UpdateResourceRequest
        {
            Name = "Non-existing",
            Category = ResourceCategory.Other,
            Status = ResourceStatus.Available,
            LocationDescription = "Nowhere",
            IsActive = true
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateResource_InvalidHierarchy_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, "XRAY-001", "Portable X-Ray");
        var controller = CreateController(context, "Admin");

        var result = await controller.UpdateResource(resource.Id, new UpdateResourceRequest
        {
            Name = "Portable X-Ray",
            Category = ResourceCategory.Other,
            Status = ResourceStatus.Available,
            LocationDescription = "Radiology",
            WardId = 999,
            RoomId = 888,
            IsActive = true
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task AssignResource_ValidHierarchy_Returns200Ok()
    {
        await using var context = CreateContext();
        var (ward, room, bed) = SeedHierarchy(context);
        var resource = SeedResource(context, "MON-002", "Monitor");
        var controller = CreateController(context, "Staff");

        var result = await controller.AssignResource(resource.Id, new AssignResourceRequest
        {
            WardId = ward.Id,
            RoomId = room.Id,
            BedId = bed.Id,
            LocationDescription = "Bedside ICU-B1"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var assigned = Assert.IsType<MedicalResourceResponse>(ok.Value);
        Assert.Equal(ward.Id, assigned.WardId);
        Assert.Equal(room.Id, assigned.RoomId);
        Assert.Equal(bed.Id, assigned.BedId);
    }

    [Fact]
    public async Task AssignResource_NonExistingResource_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Staff");

        var result = await controller.AssignResource(999, new AssignResourceRequest
        {
            LocationDescription = "Room 1"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AssignResource_InvalidHierarchy_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, "VENT-003", "Ventilator");
        var controller = CreateController(context, "ResourceAdmin");

        var result = await controller.AssignResource(resource.Id, new AssignResourceRequest
        {
            WardId = 999,
            RoomId = 888
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task DeactivateResource_AvailableResource_Returns200Ok()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, "DIAL-001", "Dialysis Machine", ResourceStatus.Available);
        var controller = CreateController(context, "Admin");

        var result = await controller.DeactivateResource(resource.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var reloaded = await context.MedicalResources.FindAsync(resource.Id);
        Assert.False(reloaded!.IsActive);
    }

    [Fact]
    public async Task DeactivateResource_NonExistingId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.DeactivateResource(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateResource_InUseResource_Returns400BadRequest()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, "VENT-INUSE", "In-Use Ventilator", ResourceStatus.InUse);
        var controller = CreateController(context, "Admin");

        var result = await controller.DeactivateResource(resource.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task ActivateResource_InactiveResource_Returns200Ok()
    {
        await using var context = CreateContext();
        var resource = SeedResource(context, "PUMP-INACTIVE", "Infusion Pump", isActive: false);
        var controller = CreateController(context, "ResourceAdmin");

        var result = await controller.ActivateResource(resource.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var reloaded = await context.MedicalResources.FindAsync(resource.Id);
        Assert.True(reloaded!.IsActive);
    }

    [Fact]
    public async Task ActivateResource_NonExistingId_Returns404NotFound()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "ResourceAdmin");

        var result = await controller.ActivateResource(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Controller_HasExpectedRouteAndAuthorizeAttribute()
    {
        var type = typeof(MedicalResourceController);
        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        var authAttr = type.GetCustomAttribute<AuthorizeAttribute>();
        var apiControllerAttr = type.GetCustomAttribute<ApiControllerAttribute>();

        Assert.NotNull(routeAttr);
        Assert.Equal("api/medical-resources", routeAttr.Template);
        Assert.NotNull(authAttr);
        Assert.NotNull(apiControllerAttr);
    }

    [Theory]
    [InlineData(nameof(MedicalResourceController.CreateResource), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(MedicalResourceController.UpdateResource), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(MedicalResourceController.AssignResource), "Admin,Staff,ResourceAdmin")]
    [InlineData(nameof(MedicalResourceController.DeactivateResource), "Admin,ResourceAdmin")]
    [InlineData(nameof(MedicalResourceController.ActivateResource), "Admin,ResourceAdmin")]
    public void Actions_HaveExpectedRoleAuthorizeAttributes(string actionName, string expectedRoles)
    {
        var method = typeof(MedicalResourceController).GetMethod(actionName);
        Assert.NotNull(method);

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal(expectedRoles, authAttr.Roles);
    }
}
