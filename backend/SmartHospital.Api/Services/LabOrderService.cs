using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class LabOrderService : ILabOrderService
{
    private readonly AppDbContext _context;

    public LabOrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LabOrderResponse>> GetByPatientIdAsync(int patientId)
    {
        return await _context.LabOrders
            .AsNoTracking()
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.OrderedAt)
            .Select(l => new LabOrderResponse
            {
                Id = l.Id,
                OrderNumber = l.OrderNumber,
                PatientId = l.PatientId,
                PatientName = $"{l.Patient!.FirstName} {l.Patient.LastName}".Trim(),
                DoctorId = l.DoctorId,
                DoctorName = $"Dr. {l.Doctor!.FirstName} {l.Doctor.LastName}".Trim(),
                MedicalRecordId = l.MedicalRecordId,
                TestName = l.TestName,
                Category = l.Category,
                Priority = l.Priority.ToString(),
                Status = l.Status.ToString(),
                OrderedAt = l.OrderedAt,
                ClinicalNotes = l.ClinicalNotes,
                Report = l.Report == null ? null : new LabReportResponse
                {
                    Id = l.Report.Id,
                    LabOrderId = l.Report.LabOrderId,
                    ConductedByUserId = l.Report.ConductedByUserId,
                    ConductedByUserName = $"{l.Report.ConductedByUser!.FirstName} {l.Report.ConductedByUser.LastName}".Trim(),
                    ReportDate = l.Report.ReportDate,
                    ResultSummary = l.Report.ResultSummary,
                    Findings = l.Report.Findings,
                    ReferenceRange = l.Report.ReferenceRange,
                    DoctorRemarks = l.Report.DoctorRemarks,
                    AttachmentUrl = l.Report.AttachmentUrl
                }
            })
            .ToListAsync();
    }

    public async Task<LabOrderResponse?> GetByIdAsync(int id)
    {
        var order = await _context.LabOrders
            .AsNoTracking()
            .Include(l => l.Patient)
            .Include(l => l.Doctor)
            .Include(l => l.Report).ThenInclude(r => r!.ConductedByUser)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (order == null)
        {
            return null;
        }

        return new LabOrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            PatientId = order.PatientId,
            PatientName = $"{order.Patient?.FirstName} {order.Patient?.LastName}".Trim(),
            DoctorId = order.DoctorId,
            DoctorName = $"Dr. {order.Doctor?.FirstName} {order.Doctor?.LastName}".Trim(),
            MedicalRecordId = order.MedicalRecordId,
            TestName = order.TestName,
            Category = order.Category,
            Priority = order.Priority.ToString(),
            Status = order.Status.ToString(),
            OrderedAt = order.OrderedAt,
            ClinicalNotes = order.ClinicalNotes,
            Report = order.Report == null ? null : new LabReportResponse
            {
                Id = order.Report.Id,
                LabOrderId = order.Report.LabOrderId,
                ConductedByUserId = order.Report.ConductedByUserId,
                ConductedByUserName = $"{order.Report.ConductedByUser?.FirstName} {order.Report.ConductedByUser?.LastName}".Trim(),
                ReportDate = order.Report.ReportDate,
                ResultSummary = order.Report.ResultSummary,
                Findings = order.Report.Findings,
                ReferenceRange = order.Report.ReferenceRange,
                DoctorRemarks = order.Report.DoctorRemarks,
                AttachmentUrl = order.Report.AttachmentUrl
            }
        };
    }

    public async Task<LabOrderResponse> CreateOrderAsync(int doctorId, CreateLabOrderRequest request)
    {
        var order = new LabOrder
        {
            OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PatientId = request.PatientId,
            DoctorId = doctorId,
            MedicalRecordId = request.MedicalRecordId,
            TestName = request.TestName.Trim(),
            Category = request.Category.Trim(),
            Priority = request.Priority,
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow,
            ClinicalNotes = request.ClinicalNotes.Trim()
        };

        _context.LabOrders.Add(order);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<LabReportResponse?> RecordReportAsync(
        int labOrderId,
        int conductedByUserId,
        RecordLabReportRequest request)
    {
        var order = await _context.LabOrders
            .Include(l => l.Report)
            .FirstOrDefaultAsync(l => l.Id == labOrderId);

        if (order == null)
        {
            return null;
        }

        var report = new LabReport
        {
            LabOrderId = labOrderId,
            ConductedByUserId = conductedByUserId,
            ReportDate = DateTime.UtcNow,
            ResultSummary = request.ResultSummary.Trim(),
            Findings = request.Findings.Trim(),
            ReferenceRange = request.ReferenceRange.Trim(),
            DoctorRemarks = request.DoctorRemarks.Trim(),
            AttachmentUrl = request.AttachmentUrl.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        order.Status = LabOrderStatus.Completed;
        _context.LabReports.Add(report);

        await _context.SaveChangesAsync();

        var createdReport = await _context.LabReports
            .AsNoTracking()
            .Include(r => r.ConductedByUser)
            .FirstAsync(r => r.Id == report.Id);

        return new LabReportResponse
        {
            Id = createdReport.Id,
            LabOrderId = createdReport.LabOrderId,
            ConductedByUserId = createdReport.ConductedByUserId,
            ConductedByUserName = $"{createdReport.ConductedByUser?.FirstName} {createdReport.ConductedByUser?.LastName}".Trim(),
            ReportDate = createdReport.ReportDate,
            ResultSummary = createdReport.ResultSummary,
            Findings = createdReport.Findings,
            ReferenceRange = createdReport.ReferenceRange,
            DoctorRemarks = createdReport.DoctorRemarks,
            AttachmentUrl = createdReport.AttachmentUrl
        };
    }
}
