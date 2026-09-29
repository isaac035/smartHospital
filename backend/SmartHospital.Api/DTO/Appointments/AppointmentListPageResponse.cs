namespace SmartHospital.Api.DTOs.Appointments;

public class AppointmentListPageResponse
{
    public List<AppointmentResponse> Appointments { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public Dictionary<string, int> StatusCounts { get; set; } = new();

    public Dictionary<string, int> PriorityCounts { get; set; } = new();
}
