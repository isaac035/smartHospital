using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly AppDbContext _context;

    public MedicalRecordService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MedicalRecordSummaryResponse>> GetByPatientIdAsync(int patientId)
    {
        return await _context.MedicalRecords
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.VisitDate)
            .Select(r => new MedicalRecordSummaryResponse
            {
                Id = r.Id,
                RecordNumber = r.RecordNumber,
                PatientId = r.PatientId,
                PatientName = $"{r.Patient!.FirstName} {r.Patient.LastName}".Trim(),
                DoctorId = r.DoctorId,
                DoctorName = $"Dr. {r.Doctor!.FirstName} {r.Doctor.LastName}".Trim(),
                VisitDate = r.VisitDate,
                ChiefComplaint = r.ChiefComplaint,
                Diagnosis = r.Diagnosis,
                FollowUpDate = r.FollowUpDate
            })
            .ToListAsync();
    }

    public async Task<MedicalRecordResponse?> GetByIdAsync(int id)
    {
        var record = await _context.MedicalRecords
            .AsNoTracking()
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.VitalSigns)
            .Include(r => r.Prescriptions).ThenInclude(p => p.Items)
            .Include(r => r.LabOrders).ThenInclude(l => l.Report)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null)
        {
            return null;
        }

        return new MedicalRecordResponse
        {
            Id = record.Id,
            RecordNumber = record.RecordNumber,
            PatientId = record.PatientId,
            PatientName = $"{record.Patient?.FirstName} {record.Patient?.LastName}".Trim(),
            DoctorId = record.DoctorId,
            DoctorName = $"Dr. {record.Doctor?.FirstName} {record.Doctor?.LastName}".Trim(),
            AppointmentId = record.AppointmentId,
            AdmissionId = record.AdmissionId,
            VisitDate = record.VisitDate,
            ChiefComplaint = record.ChiefComplaint,
            Symptoms = record.Symptoms,
            ExaminationNotes = record.ExaminationNotes,
            Diagnosis = record.Diagnosis,
            TreatmentPlan = record.TreatmentPlan,
            FollowUpDate = record.FollowUpDate,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            VitalSigns = record.VitalSigns.Select(v => new VitalSignResponse
            {
                Id = v.Id,
                PatientId = v.PatientId,
                TemperatureCelsius = v.TemperatureCelsius,
                SystolicBloodPressure = v.SystolicBloodPressure,
                DiastolicBloodPressure = v.DiastolicBloodPressure,
                HeartRateBpm = v.HeartRateBpm,
                RespiratoryRateBpm = v.RespiratoryRateBpm,
                OxygenSaturationSpO2 = v.OxygenSaturationSpO2,
                WeightKg = v.WeightKg,
                HeightCm = v.HeightCm,
                Bmi = v.Bmi,
                Notes = v.Notes,
                RecordedAt = v.RecordedAt
            }).ToList(),
            Prescriptions = record.Prescriptions.Select(p => new PrescriptionResponse
            {
                Id = p.Id,
                PrescriptionNumber = p.PrescriptionNumber,
                PatientId = p.PatientId,
                DoctorId = p.DoctorId,
                IssueDate = p.IssueDate,
                ExpiryDate = p.ExpiryDate,
                Status = p.Status.ToString(),
                GeneralInstructions = p.GeneralInstructions,
                CreatedAt = p.CreatedAt,
                Items = p.Items.Select(i => new PrescriptionItemResponse
                {
                    Id = i.Id,
                    MedicineName = i.MedicineName,
                    Dosage = i.Dosage,
                    Route = i.Route,
                    Frequency = i.Frequency,
                    DurationDays = i.DurationDays,
                    SpecialInstructions = i.SpecialInstructions
                }).ToList()
            }).ToList(),
            LabOrders = record.LabOrders.Select(l => new LabOrderResponse
            {
                Id = l.Id,
                OrderNumber = l.OrderNumber,
                PatientId = l.PatientId,
                DoctorId = l.DoctorId,
                TestName = l.TestName,
                Category = l.Category,
                Priority = l.Priority.ToString(),
                Status = l.Status.ToString(),
                OrderedAt = l.OrderedAt,
                ClinicalNotes = l.ClinicalNotes
            }).ToList()
        };
    }

    public async Task<MedicalRecordResponse> CreateAsync(int doctorId, CreateMedicalRecordRequest request)
    {
        var now = DateTime.UtcNow;
        var record = new MedicalRecord
        {
            RecordNumber = $"REC-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PatientId = request.PatientId,
            DoctorId = doctorId,
            AppointmentId = request.AppointmentId,
            AdmissionId = request.AdmissionId,
            VisitDate = now,
            ChiefComplaint = request.ChiefComplaint.Trim(),
            Symptoms = request.Symptoms.Trim(),
            ExaminationNotes = request.ExaminationNotes.Trim(),
            Diagnosis = request.Diagnosis.Trim(),
            TreatmentPlan = request.TreatmentPlan.Trim(),
            FollowUpDate = request.FollowUpDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(record.Id))!;
    }

    public async Task<MedicalRecordResponse?> UpdateAsync(int id, int doctorId, UpdateMedicalRecordRequest request)
    {
        var record = await _context.MedicalRecords
            .FirstOrDefaultAsync(r => r.Id == id && r.DoctorId == doctorId);

        if (record == null)
        {
            return null;
        }

        record.ChiefComplaint = request.ChiefComplaint.Trim();
        record.Symptoms = request.Symptoms.Trim();
        record.ExaminationNotes = request.ExaminationNotes.Trim();
        record.Diagnosis = request.Diagnosis.Trim();
        record.TreatmentPlan = request.TreatmentPlan.Trim();
        record.FollowUpDate = request.FollowUpDate;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }
}
