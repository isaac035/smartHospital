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
        if (patientId <= 0)
        {
            throw new ArgumentException("Patient ID must be a positive integer.", nameof(patientId));
        }

        var patientExists = await _context.Users
            .AnyAsync(u => u.Id == patientId && u.Role == UserRole.Patient);

        if (!patientExists)
        {
            throw new InvalidOperationException($"Patient with ID {patientId} was not found.");
        }

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
        if (id <= 0)
        {
            return null;
        }

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
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (doctorId <= 0)
        {
            throw new ArgumentException("Doctor ID must be a positive integer.", nameof(doctorId));
        }

        if (request.PatientId <= 0)
        {
            throw new ArgumentException("Patient ID must be a positive integer.", nameof(request.PatientId));
        }

        if (string.IsNullOrWhiteSpace(request.TestName))
        {
            throw new ArgumentException("Test name cannot be empty or whitespace.", nameof(request.TestName));
        }

        if (request.TestName.Trim().Length > 120)
        {
            throw new ArgumentException("Test name cannot exceed 120 characters.", nameof(request.TestName));
        }

        if (!Enum.IsDefined(typeof(LabOrderPriority), request.Priority))
        {
            throw new ArgumentException("Invalid lab order priority.", nameof(request.Priority));
        }

        if (!string.IsNullOrEmpty(request.Category) && request.Category.Length > 100)
        {
            throw new ArgumentException("Category cannot exceed 100 characters.", nameof(request.Category));
        }

        if (!string.IsNullOrEmpty(request.ClinicalNotes) && request.ClinicalNotes.Length > 1000)
        {
            throw new ArgumentException("Clinical notes cannot exceed 1000 characters.", nameof(request.ClinicalNotes));
        }

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId && (u.Role == UserRole.Doctor || u.Role == UserRole.Admin) && u.Status == UserStatus.Active);
        if (doctor == null)
        {
            throw new InvalidOperationException("Active ordering doctor not found.");
        }

        var patient = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.Role == UserRole.Patient && u.Status == UserStatus.Active);
        if (patient == null)
        {
            throw new InvalidOperationException("Active patient not found.");
        }

        if (request.MedicalRecordId.HasValue)
        {
            if (request.MedicalRecordId.Value <= 0)
            {
                throw new ArgumentException("Medical record ID must be a positive integer.", nameof(request.MedicalRecordId));
            }

            var medicalRecord = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.Id == request.MedicalRecordId.Value);

            if (medicalRecord == null)
            {
                throw new InvalidOperationException("Medical record not found.");
            }

            if (medicalRecord.PatientId != request.PatientId)
            {
                throw new InvalidOperationException("Medical record does not belong to the specified patient.");
            }
        }

        var order = new LabOrder
        {
            OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PatientId = request.PatientId,
            DoctorId = doctorId,
            MedicalRecordId = request.MedicalRecordId,
            TestName = request.TestName.Trim(),
            Category = (request.Category ?? string.Empty).Trim(),
            Priority = request.Priority,
            Status = LabOrderStatus.Ordered,
            OrderedAt = DateTime.UtcNow,
            ClinicalNotes = (request.ClinicalNotes ?? string.Empty).Trim()
        };

        _context.LabOrders.Add(order);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<LabOrderResponse?> UpdateStatusAsync(int id, UpdateLabOrderStatusRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return await UpdateStatusAsync(id, request.Status);
    }

    public async Task<LabOrderResponse?> UpdateStatusAsync(int id, LabOrderStatus newStatus)
    {
        if (id <= 0)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(LabOrderStatus), newStatus))
        {
            throw new ArgumentException("Invalid lab order status.", nameof(newStatus));
        }

        var order = await _context.LabOrders
            .Include(l => l.Report)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (order == null)
        {
            return null;
        }

        if (order.Status == newStatus)
        {
            throw new InvalidOperationException($"Lab order is already in '{order.Status}' status.");
        }

        if (order.Status == LabOrderStatus.Completed)
        {
            throw new InvalidOperationException("Cannot change status of a completed lab order.");
        }

        if (order.Status == LabOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot change status of a cancelled lab order.");
        }

        if (newStatus == LabOrderStatus.Ordered)
        {
            throw new InvalidOperationException($"Cannot transition lab order from '{order.Status}' back to 'Ordered'.");
        }

        if (order.Status == LabOrderStatus.InProgress && newStatus == LabOrderStatus.SampleCollected)
        {
            throw new InvalidOperationException("Cannot transition lab order from 'InProgress' back to 'SampleCollected'.");
        }

        if (newStatus == LabOrderStatus.Completed)
        {
            var hasReport = order.Report != null || await _context.LabReports.AnyAsync(r => r.LabOrderId == id);
            if (!hasReport)
            {
                throw new InvalidOperationException("Cannot mark lab order as completed without a lab report.");
            }
        }

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<LabReportResponse?> RecordReportAsync(
        int labOrderId,
        int conductedByUserId,
        RecordLabReportRequest request)
    {
        if (labOrderId <= 0)
        {
            throw new ArgumentException("Lab order ID must be a positive integer.", nameof(labOrderId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (conductedByUserId <= 0)
        {
            throw new ArgumentException("Conducted by user ID must be a positive integer.", nameof(conductedByUserId));
        }

        if (string.IsNullOrWhiteSpace(request.ResultSummary))
        {
            throw new ArgumentException("Result summary cannot be empty or whitespace.", nameof(request.ResultSummary));
        }

        if (request.ResultSummary.Trim().Length > 500)
        {
            throw new ArgumentException("Result summary cannot exceed 500 characters.", nameof(request.ResultSummary));
        }

        if (string.IsNullOrWhiteSpace(request.Findings))
        {
            throw new ArgumentException("Findings cannot be empty or whitespace.", nameof(request.Findings));
        }

        if (request.Findings.Trim().Length > 2000)
        {
            throw new ArgumentException("Findings cannot exceed 2000 characters.", nameof(request.Findings));
        }

        if (!string.IsNullOrEmpty(request.ReferenceRange) && request.ReferenceRange.Length > 500)
        {
            throw new ArgumentException("Reference range cannot exceed 500 characters.", nameof(request.ReferenceRange));
        }

        if (!string.IsNullOrEmpty(request.DoctorRemarks) && request.DoctorRemarks.Length > 1000)
        {
            throw new ArgumentException("Doctor remarks cannot exceed 1000 characters.", nameof(request.DoctorRemarks));
        }

        var attachmentUrl = (request.AttachmentUrl ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(attachmentUrl))
        {
            if (attachmentUrl.Length > 500)
            {
                throw new ArgumentException("Attachment URL cannot exceed 500 characters.", nameof(request.AttachmentUrl));
            }

            if (!Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var uriResult)
                || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Attachment URL must be a valid HTTP or HTTPS URL.", nameof(request.AttachmentUrl));
            }
        }

        var conductedByUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == conductedByUserId && u.Status == UserStatus.Active);
        if (conductedByUser == null)
        {
            throw new InvalidOperationException("Active user recording the report not found.");
        }

        if (conductedByUser.Role != UserRole.Staff && conductedByUser.Role != UserRole.Doctor && conductedByUser.Role != UserRole.Admin)
        {
            throw new InvalidOperationException("User recording the report must have Staff, Doctor, or Admin role.");
        }

        var order = await _context.LabOrders
            .Include(l => l.Report)
            .FirstOrDefaultAsync(l => l.Id == labOrderId);

        if (order == null)
        {
            return null;
        }

        if (order.Status == LabOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot record report for a cancelled lab order.");
        }

        if (order.Report != null || await _context.LabReports.AnyAsync(r => r.LabOrderId == labOrderId))
        {
            throw new InvalidOperationException("A report has already been recorded for this lab order.");
        }

        if (order.Status == LabOrderStatus.Completed)
        {
            throw new InvalidOperationException("Cannot record report for an already completed lab order.");
        }

        var reportDate = request.ReportDate ?? DateTime.UtcNow;
        if (reportDate > DateTime.UtcNow.AddMinutes(5))
        {
            throw new ArgumentException("Report date cannot be in the future.", nameof(request.ReportDate));
        }

        if (reportDate < order.OrderedAt)
        {
            throw new ArgumentException("Report date cannot be earlier than the order date.", nameof(request.ReportDate));
        }

        var now = DateTime.UtcNow;
        var report = new LabReport
        {
            LabOrderId = labOrderId,
            ConductedByUserId = conductedByUserId,
            ReportDate = reportDate,
            ResultSummary = request.ResultSummary.Trim(),
            Findings = request.Findings.Trim(),
            ReferenceRange = (request.ReferenceRange ?? string.Empty).Trim(),
            DoctorRemarks = (request.DoctorRemarks ?? string.Empty).Trim(),
            AttachmentUrl = attachmentUrl,
            CreatedAt = now
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
