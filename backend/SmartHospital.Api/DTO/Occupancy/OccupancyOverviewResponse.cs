namespace SmartHospital.Api.DTOs.Occupancy;

public class OccupancyOverviewResponse
{
    public int TotalBeds { get; set; }

    public int AvailableBeds { get; set; }

    public int OccupiedBeds { get; set; }

    public int ReservedBeds { get; set; }

    public int MaintenanceBeds { get; set; }

    public int BlockedBeds { get; set; }

    public double HospitalOccupancyRate { get; set; }

    public int TotalWards { get; set; }

    public int ActiveWards { get; set; }

    public int LowAvailabilityWardCount { get; set; }

    public int TotalMedicalResources { get; set; }

    public int AvailableMedicalResources { get; set; }

    public int InUseMedicalResources { get; set; }

    public int MaintenanceMedicalResources { get; set; }
}
