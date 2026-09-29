using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SmartHospital.Api.Hubs;

[Authorize]
public class HospitalHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId)) await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        if (Context.User?.IsInRole("Doctor") == true && !string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor:{userId}");
        if (Context.User?.IsInRole("Staff") == true) await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        if (Context.User?.IsInRole("Admin") == true) await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
        if (Context.User?.IsInRole("AppointmentManager") == true) await Groups.AddToGroupAsync(Context.ConnectionId, "appointment-manager");
        await base.OnConnectedAsync();
    }
    // Clients will listen to these events:
    // "SlotBooked", "SlotReleased", "SlotUpdated"
    // "AppointmentCreated", "AppointmentUpdated", "AppointmentCancelled"
    // "PatientCheckedIn", "QueueUpdated", "PatientCalled", "ConsultationStarted", "ConsultationCompleted"
}
