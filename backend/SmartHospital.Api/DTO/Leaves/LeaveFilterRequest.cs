namespace SmartHospital.Api.DTOs.Leaves;

public class LeaveFilterRequest
{
    public int? DoctorId { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }
}
