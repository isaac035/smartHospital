namespace SmartHospital.Api.DTOs.Dashboard;

public class DashboardSummaryResponse
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalStaff { get; set; }
    public int TodaysAppointments { get; set; }
    public int ActiveAdmissions { get; set; }
}
