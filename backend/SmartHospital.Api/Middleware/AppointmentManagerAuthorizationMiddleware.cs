using Microsoft.AspNetCore.Mvc.Controllers;

namespace SmartHospital.Api.Middleware;

/// <summary>Limits Appointment Manager tokens to the appointment and queue UI API surface.</summary>
public sealed class AppointmentManagerAuthorizationMiddleware
{
    private static readonly HashSet<(string Controller, string Action)> AllowedActions = new()
    {
        ("Appointment", "GetAppointments"),
        ("Appointment", "GetAppointmentById"),
        ("Appointment", "UpdateAppointmentPriority"),
        ("Appointment", "CancelAppointment"),
        ("Appointment", "RescheduleAppointment"),
        ("Appointment", "GetStatusHistory"),
        ("Appointment", "ConfirmAppointment"),
        ("Appointment", "ConfirmEmergency"),
        ("Appointment", "GetDoctorOptions"),
        ("Appointment", "GetConsultationPeriod"),
        ("Appointment", "BookAppointment"),
        ("Users", "SearchPatients"),
        ("Users", "CreateWalkInPatient"),
        ("Queue", "GetQueue"),
        ("Queue", "CallQueueEntry"),
        ("Queue", "MarkNoShow"),
        ("Queue", "MarkCompleted")
    };

    private readonly RequestDelegate _next;

    public AppointmentManagerAuthorizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("AppointmentManager"))
        {
            var isSignalRHub = context.Request.Path.StartsWithSegments("/hubs/hospital");
            var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var isAllowedAction = action != null && AllowedActions.Contains((action.ControllerName, action.ActionName));

            if (!isSignalRHub && !isAllowedAction)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await _next(context);
    }
}
