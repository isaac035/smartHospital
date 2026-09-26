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
        if (id <= 0)
        {
            return null;
        }

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

        if (string.IsNullOrWhiteSpace(request.ChiefComplaint))
        {
            throw new ArgumentException("Chief complaint cannot be empty or whitespace.", nameof(request.ChiefComplaint));
        }

        if (request.ChiefComplaint.Length > 500)
        {
            throw new ArgumentException("Chief complaint cannot exceed 500 characters.", nameof(request.ChiefComplaint));
        }

        if (string.IsNullOrWhiteSpace(request.Diagnosis))
        {
            throw new ArgumentException("Diagnosis cannot be empty or whitespace.", nameof(request.Diagnosis));
        }

        if (request.Diagnosis.Length > 500)
        {
            throw new ArgumentException("Diagnosis cannot exceed 500 characters.", nameof(request.Diagnosis));
        }

        if (!string.IsNullOrEmpty(request.Symptoms) && request.Symptoms.Length > 1000)
        {
            throw new ArgumentException("Symptoms cannot exceed 1000 characters.", nameof(request.Symptoms));
        }

        if (!string.IsNullOrEmpty(request.ExaminationNotes) && request.ExaminationNotes.Length > 2000)
        {
            throw new ArgumentException("Examination notes cannot exceed 2000 characters.", nameof(request.ExaminationNotes));
        }

        if (!string.IsNullOrEmpty(request.TreatmentPlan) && request.TreatmentPlan.Length > 2000)
        {
            throw new ArgumentException("Treatment plan cannot exceed 2000 characters.", nameof(request.TreatmentPlan));
        }

        if (request.FollowUpDate.HasValue)
        {
            if (request.FollowUpDate.Value <= DateTime.UtcNow)
            {
                throw new ArgumentException("Follow-up date must be in the future.", nameof(request.FollowUpDate));
            }

            if (request.FollowUpDate.Value > DateTime.UtcNow.AddYears(5))
            {
                throw new ArgumentException("Follow-up date cannot be more than 5 years in the future.", nameof(request.FollowUpDate));
            }
        }

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor && u.Status == UserStatus.Active);
        if (doctor == null)
        {
            throw new InvalidOperationException("Active doctor not found.");
        }

        var patient = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.Role == UserRole.Patient && u.Status == UserStatus.Active);
        if (patient == null)
        {
            throw new InvalidOperationException("Active patient not found.");
        }

        if (request.AppointmentId.HasValue)
        {
            if (request.AppointmentId.Value <= 0)
            {
                throw new ArgumentException("Appointment ID must be a positive integer.", nameof(request.AppointmentId));
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId.Value);

            if (appointment == null)
            {
                throw new InvalidOperationException("Appointment not found.");
            }

            if (appointment.PatientId != request.PatientId)
            {
                throw new InvalidOperationException("Appointment does not belong to the specified patient.");
            }
        }

        if (request.AdmissionId.HasValue)
        {
            if (request.AdmissionId.Value <= 0)
            {
                throw new ArgumentException("Admission ID must be a positive integer.", nameof(request.AdmissionId));
            }

            var admission = await _context.Admissions
                .FirstOrDefaultAsync(a => a.Id == request.AdmissionId.Value);

            if (admission == null)
            {
                throw new InvalidOperationException("Admission not found.");
            }

            if (admission.PatientId != request.PatientId)
            {
                throw new InvalidOperationException("Admission does not belong to the specified patient.");
            }
        }

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
            Symptoms = (request.Symptoms ?? string.Empty).Trim(),
            ExaminationNotes = (request.ExaminationNotes ?? string.Empty).Trim(),
            Diagnosis = request.Diagnosis.Trim(),
            TreatmentPlan = (request.TreatmentPlan ?? string.Empty).Trim(),
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
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (id <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(id));
        }

        if (doctorId <= 0)
        {
            throw new ArgumentException("Doctor ID must be a positive integer.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(request.ChiefComplaint))
        {
            throw new ArgumentException("Chief complaint cannot be empty or whitespace.", nameof(request.ChiefComplaint));
        }

        if (request.ChiefComplaint.Length > 500)
        {
            throw new ArgumentException("Chief complaint cannot exceed 500 characters.", nameof(request.ChiefComplaint));
        }

        if (string.IsNullOrWhiteSpace(request.Diagnosis))
        {
            throw new ArgumentException("Diagnosis cannot be empty or whitespace.", nameof(request.Diagnosis));
        }

        if (request.Diagnosis.Length > 500)
        {
            throw new ArgumentException("Diagnosis cannot exceed 500 characters.", nameof(request.Diagnosis));
        }

        if (!string.IsNullOrEmpty(request.Symptoms) && request.Symptoms.Length > 1000)
        {
            throw new ArgumentException("Symptoms cannot exceed 1000 characters.", nameof(request.Symptoms));
        }

        if (!string.IsNullOrEmpty(request.ExaminationNotes) && request.ExaminationNotes.Length > 2000)
        {
            throw new ArgumentException("Examination notes cannot exceed 2000 characters.", nameof(request.ExaminationNotes));
        }

        if (!string.IsNullOrEmpty(request.TreatmentPlan) && request.TreatmentPlan.Length > 2000)
        {
            throw new ArgumentException("Treatment plan cannot exceed 2000 characters.", nameof(request.TreatmentPlan));
        }

        if (request.FollowUpDate.HasValue)
        {
            if (request.FollowUpDate.Value <= DateTime.UtcNow)
            {
                throw new ArgumentException("Follow-up date must be in the future.", nameof(request.FollowUpDate));
            }

            if (request.FollowUpDate.Value > DateTime.UtcNow.AddYears(5))
            {
                throw new ArgumentException("Follow-up date cannot be more than 5 years in the future.", nameof(request.FollowUpDate));
            }
        }

        var record = await _context.MedicalRecords
            .FirstOrDefaultAsync(r => r.Id == id && r.DoctorId == doctorId);

        if (record == null)
        {
            return null;
        }

        if (request.AppointmentId.HasValue)
        {
            if (request.AppointmentId.Value <= 0)
            {
                throw new ArgumentException("Appointment ID must be a positive integer.", nameof(request.AppointmentId));
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId.Value);

            if (appointment == null)
            {
                throw new InvalidOperationException("Appointment not found.");
            }

            if (appointment.PatientId != record.PatientId)
            {
                throw new InvalidOperationException("Appointment does not belong to the specified patient.");
            }

            record.AppointmentId = request.AppointmentId.Value;
        }

        if (request.AdmissionId.HasValue)
        {
            if (request.AdmissionId.Value <= 0)
            {
                throw new ArgumentException("Admission ID must be a positive integer.", nameof(request.AdmissionId));
            }

            var admission = await _context.Admissions
                .FirstOrDefaultAsync(a => a.Id == request.AdmissionId.Value);

            if (admission == null)
            {
                throw new InvalidOperationException("Admission not found.");
            }

            if (admission.PatientId != record.PatientId)
            {
                throw new InvalidOperationException("Admission does not belong to the specified patient.");
            }

            record.AdmissionId = request.AdmissionId.Value;
        }

        record.ChiefComplaint = request.ChiefComplaint.Trim();
        record.Symptoms = (request.Symptoms ?? string.Empty).Trim();
        record.ExaminationNotes = (request.ExaminationNotes ?? string.Empty).Trim();
        record.Diagnosis = request.Diagnosis.Trim();
        record.TreatmentPlan = (request.TreatmentPlan ?? string.Empty).Trim();
        record.FollowUpDate = request.FollowUpDate;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }
}
