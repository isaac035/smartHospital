using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Departments;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class DepartmentServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesActiveDepartmentWithTrimmedFields()
    {
        await using var context = CreateContext();

        var result = await new DepartmentService(context).CreateAsync(new CreateDepartmentRequest
        {
            Name = "  Cardiology ",
            Description = " Heart care "
        });

        Assert.True(result.Id > 0);
        Assert.Equal("Cardiology", result.Name);
        Assert.Equal("Heart care", result.Description);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateName()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);
        await service.CreateAsync(new CreateDepartmentRequest { Name = "Neurology", Description = "" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateDepartmentRequest { Name = " Neurology ", Description = "" }));

        Assert.Equal("A department with this name already exists.", ex.Message);
        Assert.Equal(1, await context.Departments.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDepartmentsOrderedByName()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);
        await service.CreateAsync(new CreateDepartmentRequest { Name = "Radiology", Description = "" });
        await service.CreateAsync(new CreateDepartmentRequest { Name = "Cardiology", Description = "" });
        await service.CreateAsync(new CreateDepartmentRequest { Name = "Orthopaedics", Description = "" });

        var all = await service.GetAllAsync();

        Assert.Equal(new[] { "Cardiology", "Orthopaedics", "Radiology" }, all.Select(d => d.Name));
    }

    [Fact]
    public async Task GetAllAsync_IncludesInactiveDepartments()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);
        var dept = await service.CreateAsync(new CreateDepartmentRequest { Name = "Old", Description = "" });
        await service.DeactivateAsync(dept.Id);

        var all = await service.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("Inactive", all[0].Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForUnknownId()
    {
        await using var context = CreateContext();

        Assert.Null(await new DepartmentService(context).GetByIdAsync(404));
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAndDescription()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);
        var dept = await service.CreateAsync(new CreateDepartmentRequest { Name = "ENT", Description = "" });

        var result = await service.UpdateAsync(dept.Id, new UpdateDepartmentRequest
        {
            Name = " Ear, Nose & Throat ",
            Description = " Updated "
        });

        Assert.Equal("Ear, Nose & Throat", result!.Name);
        Assert.Equal("Updated", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullForUnknownId()
    {
        await using var context = CreateContext();

        var result = await new DepartmentService(context).UpdateAsync(
            404, new UpdateDepartmentRequest { Name = "X", Description = "" });

        Assert.Null(result);
    }

    // Regression: BUG-003
    [Fact]
    public async Task UpdateAsync_RejectsRenamingToAnExistingDepartmentName()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);
        await service.CreateAsync(new CreateDepartmentRequest { Name = "Cardiology", Description = "" });
        var neuro = await service.CreateAsync(new CreateDepartmentRequest { Name = "Neurology", Description = "" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(neuro.Id, new UpdateDepartmentRequest { Name = " Cardiology ", Description = "" }));

        Assert.Equal("A department with this name already exists.", ex.Message);
        Assert.Equal("Neurology", (await service.GetByIdAsync(neuro.Id))!.Name);
    }

    [Fact]
    public async Task UpdateAsync_AllowsKeepingOwnName()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);
        var dept = await service.CreateAsync(new CreateDepartmentRequest { Name = "Cardiology", Description = "" });

        var result = await service.UpdateAsync(dept.Id, new UpdateDepartmentRequest { Name = "Cardiology", Description = "New" });

        Assert.Equal("New", result!.Description);
    }

    [Fact]
    public async Task DeactivateThenActivate_TogglesStatus()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);
        var dept = await service.CreateAsync(new CreateDepartmentRequest { Name = "Oncology", Description = "" });

        Assert.True(await service.DeactivateAsync(dept.Id));
        Assert.Equal(DepartmentStatus.Inactive, (await context.Departments.SingleAsync()).Status);

        Assert.True(await service.ActivateAsync(dept.Id));
        Assert.Equal(DepartmentStatus.Active, (await context.Departments.SingleAsync()).Status);
    }

    [Fact]
    public async Task DeactivateAndActivate_ReturnFalseForUnknownId()
    {
        await using var context = CreateContext();
        var service = new DepartmentService(context);

        Assert.False(await service.DeactivateAsync(404));
        Assert.False(await service.ActivateAsync(404));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
