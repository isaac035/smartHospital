using Microsoft.AspNetCore.Mvc.Controllers;

namespace SmartHospital.Api.Middleware;

/// <summary>
/// Limits Doctor Manager tokens to the Doctor Management, Doctor Availability Calendar and
/// Leave / Unavailability API surface (plus the read-only department and consultation-type
/// lists those screens need). Every other endpoint returns 403 for this role.
/// </summary>
public sealed class DoctorManagerAuthorizationMiddleware
{
    public const string Role = "DoctorManager";

    private static readonly HashSet<(string Controller, string Action)> AllowedActions = new()
    {
        // Doctor Management
        ("Doctors", "GetAll"),
        ("Doctors", "GetAvailable"),
        ("Doctors", "GetById"),
        ("Doctors", "Create"),
        ("Doctors", "CreateLoginForExistingDoctor"),
        ("Doctors", "Update"),
        ("Doctors", "Deactivate"),
        ("Doctors", "Activate"),
        ("Doctors", "DeletePermanently"),

        // Doctor Availability Calendar
        ("Schedules", "GetAll"),
        ("Schedules", "GetById"),
        ("Schedules", "Create"),
        ("Schedules", "Update"),
        ("Schedules", "Remove"),

        // Leave / Unavailability
        ("Leaves", "GetAll"),
        ("Leaves", "GetById"),
        ("Leaves", "Create"),
        ("Leaves", "Update"),
        ("Leaves", "Cancel"),

        // Lookups used by the forms above (read-only)
        ("Departments", "GetAll"),
        ("Departments", "GetById"),
        ("ConsultationTypes", "GetAll"),
        ("ConsultationTypes", "GetById"),
    };

    private readonly RequestDelegate _next;

    public DoctorManagerAuthorizationMiddleware(RequestDelegate next) => _next = next;

    public static bool IsAllowed(string controller, string action) =>
        AllowedActions.Contains((controller, action));

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole(Role))
        {
            var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (action == null || !IsAllowed(action.ControllerName, action.ActionName))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await _next(context);
    }
}
