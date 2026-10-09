using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using SmartHospital.Api.Controllers;
using SmartHospital.Api.Middleware;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class DoctorManagerAccessTests
{
    private static readonly Assembly ApiAssembly = typeof(DoctorsController).Assembly;

    private static HttpContext ContextFor(string role, string controller, string action)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test")),
        };
        var descriptor = new ControllerActionDescriptor { ControllerName = controller, ActionName = action };
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(descriptor), "test"));
        return context;
    }

    private static async Task<(int Status, bool NextCalled)> Run(string role, string controller, string action)
    {
        var nextCalled = false;
        var middleware = new DoctorManagerAuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = ContextFor(role, controller, action);
        await middleware.InvokeAsync(context);
        return (context.Response.StatusCode, nextCalled);
    }

    [Theory]
    [InlineData("Doctors", "GetAll")]
    [InlineData("Doctors", "Create")]
    [InlineData("Doctors", "Deactivate")]
    [InlineData("Schedules", "Create")]
    [InlineData("Schedules", "Remove")]
    [InlineData("Leaves", "Update")]
    [InlineData("Leaves", "Cancel")]
    [InlineData("Departments", "GetAll")]
    [InlineData("ConsultationTypes", "GetAll")]
    [InlineData("Departments", "Create")]
    [InlineData("ConsultationTypes", "Update")]
    public async Task Doctor_manager_can_reach_doctor_availability_and_leave_endpoints(string controller, string action)
    {
        var (status, nextCalled) = await Run(DoctorManagerAuthorizationMiddleware.Role, controller, action);
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, status);
    }

    [Theory]
    [InlineData("Appointment", "GetAppointments")]
    [InlineData("Queue", "GetQueue")]
    [InlineData("Users", "GetAll")]
    [InlineData("Users", "CreateDoctorManager")]
    [InlineData("Bed", "GetAll")]
    [InlineData("MedicalRecords", "GetById")]
    [InlineData("Agent1", "TriageDoctorMatch")]
    public async Task Doctor_manager_is_blocked_everywhere_else(string controller, string action)
    {
        var (status, nextCalled) = await Run(DoctorManagerAuthorizationMiddleware.Role, controller, action);
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Staff")]
    [InlineData("AppointmentManager")]
    public async Task Other_roles_are_not_affected(string role)
    {
        var (_, nextCalled) = await Run(role, "Appointment", "GetAppointments");
        Assert.True(nextCalled);
    }

    [Fact]
    public void Every_allowlisted_action_exists_on_its_controller()
    {
        var actions = ApiAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => (Controller: t.Name[..^"Controller".Length], Action: m.Name)))
            .ToHashSet();

        var allowlist = typeof(DoctorManagerAuthorizationMiddleware)
            .GetField("AllowedActions", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as HashSet<(string Controller, string Action)>;

        Assert.NotNull(allowlist);
        Assert.All(allowlist!, entry => Assert.Contains(entry, actions));
    }

    [Fact]
    public void Every_action_granted_to_DoctorManager_is_on_the_allowlist()
    {
        var granted = ApiAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<AuthorizeAttribute>()
                    .Any(a => (a.Roles ?? string.Empty).Split(',').Contains(DoctorManagerAuthorizationMiddleware.Role)))
                .Select(m => (Controller: t.Name[..^"Controller".Length], Action: m.Name)))
            .ToList();

        Assert.NotEmpty(granted);
        Assert.All(granted, g => Assert.True(
            DoctorManagerAuthorizationMiddleware.IsAllowed(g.Controller, g.Action),
            $"{g.Controller}.{g.Action} grants DoctorManager but is not on the middleware allowlist"));
    }
}
