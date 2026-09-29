using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Dashboard;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Staff")]
public class DashboardController : ControllerBase
{
    private static readonly TimeZoneInfo HospitalTimeZone = ResolveHospitalTimeZone();
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary()
    {
        var localDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, HospitalTimeZone));
        var localDayStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utcDayStart = TimeZoneInfo.ConvertTimeToUtc(localDayStart, HospitalTimeZone);
        var utcNextDayStart = TimeZoneInfo.ConvertTimeToUtc(localDayStart.AddDays(1), HospitalTimeZone);

        var totalPatients = await _context.Users
            .CountAsync(user => user.Role == UserRole.Patient);
        var totalStaff = await _context.Users
            .CountAsync(user => user.Role == UserRole.Staff);
        var totalDoctors = await _context.Doctors.CountAsync();
        var todaysAppointments = await _context.Appointments
            .CountAsync(appointment =>
                appointment.ScheduledStart >= utcDayStart &&
                appointment.ScheduledStart < utcNextDayStart &&
                appointment.Status != AppointmentStatus.Cancelled &&
                appointment.Status != AppointmentStatus.Rescheduled);

        return Ok(new DashboardSummaryResponse
        {
            TotalPatients = totalPatients,
            TotalDoctors = totalDoctors,
            TotalStaff = totalStaff,
            TodaysAppointments = todaysAppointments,
        });
    }

    private static TimeZoneInfo ResolveHospitalTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
        }
    }
}
