using Microsoft.AspNetCore.SignalR;

namespace SmartHospital.Api.Hubs;

public class HospitalHub : Hub
{
    // Clients will listen to these events:
    // "SlotBooked", "SlotReleased", "SlotUpdated"
    // "AppointmentCreated", "AppointmentUpdated", "AppointmentCancelled"
    // "PatientCheckedIn", "QueueUpdated", "PatientCalled", "ConsultationStarted", "ConsultationCompleted"
}
