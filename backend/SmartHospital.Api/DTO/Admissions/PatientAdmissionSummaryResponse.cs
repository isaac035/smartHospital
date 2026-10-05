namespace SmartHospital.Api.DTOs.Admissions;

public class PatientAdmissionSummaryResponse
{
    public int AdmissionId { get; set; }

    public string AdmissionNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateTime AdmissionDate { get; set; }

    public DateTime? DischargeDate { get; set; }

    public string? DoctorName { get; set; }

    public string? WardName { get; set; }

    public string? WardFloor { get; set; }

    public string? RoomNumber { get; set; }

    public string? BedNumber { get; set; }

    public DateTime? AllocatedAt { get; set; }

    public string ReasonForAdmission { get; set; } = string.Empty;
}
