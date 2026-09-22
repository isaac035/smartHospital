namespace SmartHospital.Api.Models;

public class BedAllocation
{
    public int Id { get; set; }

    public int AdmissionId { get; set; }

    public int BedId { get; set; }

    public int? AllocatedByStaffId { get; set; }

    public DateTime AllocatedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public BedAllocationStatus Status { get; set; } = BedAllocationStatus.Active;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Admission? Admission { get; set; }

    public Bed? Bed { get; set; }

    public User? AllocatedByStaff { get; set; }
}
