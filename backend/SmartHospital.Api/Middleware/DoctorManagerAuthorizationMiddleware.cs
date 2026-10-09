using Microsoft.AspNetCore.Mvc.Controllers;

namespace SmartHospital.Api.Middleware;

/// <summary>
/// Limits Doctor Manager tokens to the Doctor Management, Department Management,
/// Consultation Types, Doctor Availability Calendar and Leave / Unavailability API surface.
/// Every other endpoint returns 403 for this role.
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

        // Department Management
        ("Departments", "GetAll"),
        ("Departments", "GetById"),
        ("Departments", "Create"),
        ("Departments", "Update"),
        ("Departments", "Deactivate"),
        ("Departments", "Activate"),

        // Consultation Types
        ("ConsultationTypes", "GetAll"),
        ("ConsultationTypes", "GetById"),
        ("ConsultationTypes", "Create"),
        ("ConsultationTypes", "Update"),
        ("ConsultationTypes", "Deactivate"),
        ("ConsultationTypes", "Activate"),
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
