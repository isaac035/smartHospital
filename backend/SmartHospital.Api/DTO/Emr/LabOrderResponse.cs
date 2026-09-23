namespace SmartHospital.Api.DTOs.Emr;

public class LabOrderResponse
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int? MedicalRecordId { get; set; }

    public string TestName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime OrderedAt { get; set; }

    public string ClinicalNotes { get; set; } = string.Empty;

    public LabReportResponse? Report { get; set; }
}

public class LabReportResponse
{
    public int Id { get; set; }

    public int LabOrderId { get; set; }

    public int ConductedByUserId { get; set; }

    public string ConductedByUserName { get; set; } = string.Empty;

    public DateTime ReportDate { get; set; }

    public string ResultSummary { get; set; } = string.Empty;

    public string Findings { get; set; } = string.Empty;

    public string ReferenceRange { get; set; } = string.Empty;

    public string DoctorRemarks { get; set; } = string.Empty;

    public string AttachmentUrl { get; set; } = string.Empty;
}
