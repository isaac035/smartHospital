using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class MedicalHistoryService : IMedicalHistoryService
{
    private readonly AppDbContext _context;

    public MedicalHistoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PatientMedicalTimelineResponse> GetPatientTimelineAsync(int patientId)
    {
        if (patientId <= 0)
        {
            throw new ArgumentException("Patient ID must be a positive integer.", nameof(patientId));
        }

        var patient = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == patientId && u.Role == UserRole.Patient);

        if (patient == null)
        {
            throw new InvalidOperationException($"Patient with ID {patientId} was not found.");
        }

        var patientName = $"{patient.FirstName} {patient.LastName}".Trim();
        var allEvents = new List<MedicalTimelineEventResponse>();

        // 1. Medical Records (Encounters)
        var medicalRecords = await _context.MedicalRecords
            .AsNoTracking()
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId)
            .ToListAsync();

        foreach (var r in medicalRecords)
        {
            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "MedicalRecord",
                EventDate = r.VisitDate,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = r.DoctorId,
                DoctorName = r.Doctor != null ? $"Dr. {r.Doctor.FirstName} {r.Doctor.LastName}".Trim() : null,
                Summary = $"Clinical Encounter {r.RecordNumber}: Chief complaint - {r.ChiefComplaint}",
                SourceRecordId = r.Id,
                Category = "ClinicalEncounter",
                Status = "Recorded"
            });
        }

        // 2. Clinical Visits (Appointments)
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patientId)
            .ToListAsync();

        foreach (var a in appointments)
        {
            var note = !string.IsNullOrWhiteSpace(a.Notes) ? a.Notes : $"{a.AppointmentType} Consultation";
            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "ClinicalVisit",
                EventDate = a.ScheduledStart,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor != null ? $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}".Trim() : null,
                Summary = $"Clinical Visit {a.ReferenceNumber}: {note} [{a.Status}]",
                SourceRecordId = a.Id,
                Category = a.AppointmentType.ToString(),
                Status = a.Status.ToString()
            });
        }

        // 3. Clinical Diagnoses
        var diagnoses = await _context.ClinicalDiagnoses
            .AsNoTracking()
            .Include(d => d.Doctor)
            .Where(d => d.PatientId == patientId)
            .ToListAsync();

        foreach (var d in diagnoses)
        {
            var codeText = !string.IsNullOrWhiteSpace(d.Code) ? $" [{d.Code}]" : string.Empty;
            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "Diagnosis",
                EventDate = d.DiagnosedAt,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = d.DoctorId,
                DoctorName = d.Doctor != null ? $"Dr. {d.Doctor.FirstName} {d.Doctor.LastName}".Trim() : null,
                Summary = $"{d.Type} Diagnosis: {d.Description}{codeText} ({d.Severity}, {d.Status})",
                SourceRecordId = d.Id,
                Category = d.Type.ToString(),
                Status = d.Status.ToString()
            });
        }

        // 4. Prescriptions
        var prescriptions = await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .Where(p => p.PatientId == patientId)
            .ToListAsync();

        foreach (var p in prescriptions)
        {
            var meds = p.Items.Any()
                ? string.Join("; ", p.Items.Select(i => $"{i.MedicineName} {i.Dosage} ({i.Frequency})"))
                : "No medications listed";

            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "Prescription",
                EventDate = p.IssueDate,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = p.DoctorId,
                DoctorName = p.Doctor != null ? $"Dr. {p.Doctor.FirstName} {p.Doctor.LastName}".Trim() : null,
                Summary = $"Prescription {p.PrescriptionNumber}: {meds} [{p.Status}]",
                SourceRecordId = p.Id,
                Category = "Medication",
                Status = p.Status.ToString()
            });
        }

        // 5. Lab Orders
        var labOrders = await _context.LabOrders
            .AsNoTracking()
            .Include(l => l.Doctor)
            .Where(l => l.PatientId == patientId)
            .ToListAsync();

        foreach (var l in labOrders)
        {
            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "LabOrder",
                EventDate = l.OrderedAt,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = l.DoctorId,
                DoctorName = l.Doctor != null ? $"Dr. {l.Doctor.FirstName} {l.Doctor.LastName}".Trim() : null,
                Summary = $"Lab Order {l.OrderNumber}: {l.TestName} ({l.Category}, {l.Priority} priority) [{l.Status}]",
                SourceRecordId = l.Id,
                Category = l.Category,
                Status = l.Status.ToString()
            });
        }

        // 6. Lab Reports
        var labReports = await _context.LabReports
            .AsNoTracking()
            .Include(r => r.LabOrder)
            .Include(r => r.ConductedByUser)
            .Where(r => r.LabOrder != null && r.LabOrder.PatientId == patientId)
            .ToListAsync();

        foreach (var r in labReports)
        {
            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "LabReport",
                EventDate = r.ReportDate,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = r.ConductedByUserId,
                DoctorName = r.ConductedByUser != null ? $"{r.ConductedByUser.FirstName} {r.ConductedByUser.LastName}".Trim() : null,
                Summary = $"Lab Report for {r.LabOrder?.TestName ?? "Lab Test"}: {r.ResultSummary}",
                SourceRecordId = r.Id,
                Category = r.LabOrder?.Category ?? "Diagnostic",
                Status = "Completed"
            });
        }

        // 7. Vital Signs
        var vitalSigns = await _context.VitalSigns
            .AsNoTracking()
            .Include(v => v.RecordedByUser)
            .Where(v => v.PatientId == patientId)
            .ToListAsync();

        foreach (var v in vitalSigns)
        {
            var readings = new List<string>();
            if (v.SystolicBloodPressure.HasValue && v.DiastolicBloodPressure.HasValue)
            {
                readings.Add($"BP: {v.SystolicBloodPressure}/{v.DiastolicBloodPressure} mmHg");
            }
            if (v.HeartRateBpm.HasValue)
            {
                readings.Add($"HR: {v.HeartRateBpm} bpm");
            }
            if (v.TemperatureCelsius.HasValue)
            {
                readings.Add($"Temp: {v.TemperatureCelsius:F1}°C");
            }
            if (v.OxygenSaturationSpO2.HasValue)
            {
                readings.Add($"SpO2: {v.OxygenSaturationSpO2:F1}%");
            }
            if (v.RespiratoryRateBpm.HasValue)
            {
                readings.Add($"RR: {v.RespiratoryRateBpm} bpm");
            }

            var vitalsSummary = readings.Any() ? string.Join(", ", readings) : "Vital signs recorded";

            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "VitalSign",
                EventDate = v.RecordedAt,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = v.RecordedByUserId,
                DoctorName = v.RecordedByUser != null ? $"{v.RecordedByUser.FirstName} {v.RecordedByUser.LastName}".Trim() : null,
                Summary = $"Vital Signs: {vitalsSummary}",
                SourceRecordId = v.Id,
                Category = "Vitals",
                Status = "Recorded"
            });
        }

        // 8. Treatment Plans
        var treatmentPlans = await _context.ClinicalTreatmentPlans
            .AsNoTracking()
            .Include(t => t.Doctor)
            .Where(t => t.PatientId == patientId)
            .ToListAsync();

        foreach (var t in treatmentPlans)
        {
            allEvents.Add(new MedicalTimelineEventResponse
            {
                EventType = "TreatmentPlan",
                EventDate = t.StartDate,
                PatientId = patientId,
                PatientName = patientName,
                DoctorId = t.DoctorId,
                DoctorName = t.Doctor != null ? $"Dr. {t.Doctor.FirstName} {t.Doctor.LastName}".Trim() : null,
                Summary = $"Treatment Plan: {t.Title} - {t.Description} ({t.Category}) [{t.Status}]",
                SourceRecordId = t.Id,
                Category = t.Category.ToString(),
                Status = t.Status.ToString()
            });
        }

        // Chronologically sort with newest clinical events available first
        var sortedEvents = allEvents
            .OrderByDescending(e => e.EventDate)
            .ThenByDescending(e => e.SourceRecordId)
            .ToList();

        return new PatientMedicalTimelineResponse
        {
            PatientId = patient.Id,
            PatientName = patientName,
            TotalEvents = sortedEvents.Count,
            Events = sortedEvents
        };
    }
}
