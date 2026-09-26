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
            .Include(r => r.Diagnoses)
            .Include(r => r.TreatmentPlans)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null)
        {
            return null;
        }

        return MapToResponse(record);
    }

    public async Task<MedicalRecordResponse?> GetByAppointmentIdAsync(int appointmentId)
    {
        if (appointmentId <= 0)
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
            .Include(r => r.Diagnoses)
            .Include(r => r.TreatmentPlans)
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);

        if (record == null)
        {
            return null;
        }

        return MapToResponse(record);
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

        var now = DateTime.UtcNow;

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

            if (appointment.DoctorId != doctorId)
            {
                throw new InvalidOperationException("Appointment does not belong to the specified doctor.");
            }

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Rescheduled)
            {
                throw new InvalidOperationException($"Cannot create a medical record for an appointment with status '{appointment.Status}'.");
            }

            var existingRecordForAppointment = await _context.MedicalRecords
                .AnyAsync(m => m.AppointmentId == request.AppointmentId.Value);

            if (existingRecordForAppointment)
            {
                throw new InvalidOperationException("A medical record has already been created for this appointment.");
            }

            // Completing the appointment associated with this clinical visit
            if (appointment.Status != AppointmentStatus.Completed)
            {
                appointment.Status = AppointmentStatus.Completed;
                appointment.UpdatedAt = now;
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

        if (!string.IsNullOrWhiteSpace(request.Diagnosis))
        {
            var initialDiagnosis = new ClinicalDiagnosis
            {
                MedicalRecord = record,
                PatientId = request.PatientId,
                DoctorId = doctorId,
                Description = request.Diagnosis.Trim(),
                Type = DiagnosisType.Primary,
                Status = DiagnosisStatus.Active,
                Severity = DiagnosisSeverity.Moderate,
                DiagnosedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.ClinicalDiagnoses.Add(initialDiagnosis);
        }

        if (!string.IsNullOrWhiteSpace(request.TreatmentPlan))
        {
            var initialTreatmentPlan = new ClinicalTreatmentPlan
            {
                MedicalRecord = record,
                PatientId = request.PatientId,
                DoctorId = doctorId,
                Title = "Primary Treatment Plan",
                Category = TreatmentPlanCategory.General,
                Description = request.TreatmentPlan.Trim(),
                Status = TreatmentPlanStatus.Active,
                StartDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.ClinicalTreatmentPlans.Add(initialTreatmentPlan);
        }

        var initialVersion = new MedicalRecordVersion
        {
            MedicalRecord = record,
            VersionNumber = 1,
            ChangedByUserId = doctorId,
            ChangedAt = now,
            ChangeType = "Initial",
            ChangeSummary = "Initial medical record created",
            ChiefComplaint = record.ChiefComplaint,
            Symptoms = record.Symptoms,
            ExaminationNotes = record.ExaminationNotes,
            Diagnosis = record.Diagnosis,
            TreatmentPlan = record.TreatmentPlan,
            FollowUpDate = record.FollowUpDate
        };
        _context.MedicalRecordVersions.Add(initialVersion);

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

            if (appointment.DoctorId != doctorId)
            {
                throw new InvalidOperationException("Appointment does not belong to the specified doctor.");
            }

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Rescheduled)
            {
                throw new InvalidOperationException($"Cannot link a medical record to an appointment with status '{appointment.Status}'.");
            }

            var existingRecordForAppointment = await _context.MedicalRecords
                .AnyAsync(m => m.AppointmentId == request.AppointmentId.Value && m.Id != id);

            if (existingRecordForAppointment)
            {
                throw new InvalidOperationException("A medical record has already been created for this appointment.");
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                appointment.Status = AppointmentStatus.Completed;
                appointment.UpdatedAt = DateTime.UtcNow;
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

        var prevChiefComplaint = record.ChiefComplaint;
        var prevSymptoms = record.Symptoms;
        var prevExamNotes = record.ExaminationNotes;
        var prevDiagnosis = record.Diagnosis;
        var prevTreatmentPlan = record.TreatmentPlan;
        var prevFollowUpDate = record.FollowUpDate;

        record.ChiefComplaint = request.ChiefComplaint.Trim();
        record.Symptoms = (request.Symptoms ?? string.Empty).Trim();
        record.ExaminationNotes = (request.ExaminationNotes ?? string.Empty).Trim();
        
        var trimmedDiagnosis = request.Diagnosis.Trim();
        if (record.Diagnosis != trimmedDiagnosis)
        {
            record.Diagnosis = trimmedDiagnosis;
            var primaryDiag = await _context.ClinicalDiagnoses
                .FirstOrDefaultAsync(d => d.MedicalRecordId == id && d.Type == DiagnosisType.Primary);
            if (primaryDiag != null)
            {
                primaryDiag.Description = trimmedDiagnosis;
                primaryDiag.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ClinicalDiagnoses.Add(new ClinicalDiagnosis
                {
                    MedicalRecordId = id,
                    PatientId = record.PatientId,
                    DoctorId = doctorId,
                    Description = trimmedDiagnosis,
                    Type = DiagnosisType.Primary,
                    Status = DiagnosisStatus.Active,
                    Severity = DiagnosisSeverity.Moderate,
                    DiagnosedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        var trimmedTreatmentPlan = (request.TreatmentPlan ?? string.Empty).Trim();
        if (record.TreatmentPlan != trimmedTreatmentPlan)
        {
            record.TreatmentPlan = trimmedTreatmentPlan;
            if (!string.IsNullOrWhiteSpace(trimmedTreatmentPlan))
            {
                var primaryPlan = await _context.ClinicalTreatmentPlans
                    .FirstOrDefaultAsync(t => t.MedicalRecordId == id);
                if (primaryPlan != null)
                {
                    primaryPlan.Description = trimmedTreatmentPlan;
                    primaryPlan.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.ClinicalTreatmentPlans.Add(new ClinicalTreatmentPlan
                    {
                        MedicalRecordId = id,
                        PatientId = record.PatientId,
                        DoctorId = doctorId,
                        Title = "Primary Treatment Plan",
                        Category = TreatmentPlanCategory.General,
                        Description = trimmedTreatmentPlan,
                        Status = TreatmentPlanStatus.Active,
                        StartDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        record.FollowUpDate = request.FollowUpDate;
        record.UpdatedAt = DateTime.UtcNow;

        var changes = new List<string>();
        if (prevChiefComplaint != record.ChiefComplaint) changes.Add("Chief complaint updated");
        if (prevSymptoms != record.Symptoms) changes.Add("Symptoms updated");
        if (prevExamNotes != record.ExaminationNotes) changes.Add("Examination notes updated");
        if (prevDiagnosis != record.Diagnosis) changes.Add("Diagnosis updated");
        if (prevTreatmentPlan != record.TreatmentPlan) changes.Add("Treatment plan updated");
        if (prevFollowUpDate != record.FollowUpDate) changes.Add("Follow-up date updated");
        if (request.AppointmentId.HasValue && record.AppointmentId == request.AppointmentId.Value) changes.Add("Appointment linked");
        if (request.AdmissionId.HasValue && record.AdmissionId == request.AdmissionId.Value) changes.Add("Admission linked");

        var changeSummary = changes.Count > 0 ? string.Join("; ", changes) : "Medical record updated";

        var latestVersion = await _context.MedicalRecordVersions
            .Where(v => v.MedicalRecordId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();

        int nextVersionNumber;
        if (latestVersion == null)
        {
            var initialSnapshot = new MedicalRecordVersion
            {
                MedicalRecordId = id,
                VersionNumber = 1,
                ChangedByUserId = record.DoctorId,
                ChangedAt = record.CreatedAt,
                ChangeType = "Initial",
                ChangeSummary = "Initial medical record created",
                ChiefComplaint = prevChiefComplaint,
                Symptoms = prevSymptoms,
                ExaminationNotes = prevExamNotes,
                Diagnosis = prevDiagnosis,
                TreatmentPlan = prevTreatmentPlan,
                FollowUpDate = prevFollowUpDate
            };
            _context.MedicalRecordVersions.Add(initialSnapshot);
            nextVersionNumber = 2;
        }
        else
        {
            nextVersionNumber = latestVersion.VersionNumber + 1;
        }

        var newVersion = new MedicalRecordVersion
        {
            MedicalRecordId = id,
            VersionNumber = nextVersionNumber,
            ChangedByUserId = doctorId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = "Update",
            ChangeSummary = changeSummary,
            ChiefComplaint = record.ChiefComplaint,
            Symptoms = record.Symptoms,
            ExaminationNotes = record.ExaminationNotes,
            Diagnosis = record.Diagnosis,
            TreatmentPlan = record.TreatmentPlan,
            FollowUpDate = record.FollowUpDate,
            PreviousChiefComplaint = prevChiefComplaint,
            PreviousSymptoms = prevSymptoms,
            PreviousExaminationNotes = prevExamNotes,
            PreviousDiagnosis = prevDiagnosis,
            PreviousTreatmentPlan = prevTreatmentPlan,
            PreviousFollowUpDate = prevFollowUpDate
        };
        _context.MedicalRecordVersions.Add(newVersion);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    private static MedicalRecordResponse MapToResponse(MedicalRecord record)
    {
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
            }).ToList(),
            Diagnoses = record.Diagnoses.Select(d => new DiagnosisResponse
            {
                Id = d.Id,
                MedicalRecordId = d.MedicalRecordId,
                PatientId = d.PatientId,
                PatientName = $"{record.Patient?.FirstName} {record.Patient?.LastName}".Trim(),
                DoctorId = d.DoctorId,
                DoctorName = $"Dr. {record.Doctor?.FirstName} {record.Doctor?.LastName}".Trim(),
                Code = d.Code,
                Description = d.Description,
                Type = d.Type.ToString(),
                Status = d.Status.ToString(),
                Severity = d.Severity.ToString(),
                Notes = d.Notes,
                DiagnosedAt = d.DiagnosedAt,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList(),
            TreatmentPlans = record.TreatmentPlans.Select(t => new TreatmentPlanResponse
            {
                Id = t.Id,
                MedicalRecordId = t.MedicalRecordId,
                PatientId = t.PatientId,
                PatientName = $"{record.Patient?.FirstName} {record.Patient?.LastName}".Trim(),
                DoctorId = t.DoctorId,
                DoctorName = $"Dr. {record.Doctor?.FirstName} {record.Doctor?.LastName}".Trim(),
                Title = t.Title,
                Category = t.Category.ToString(),
                Description = t.Description,
                Goals = t.Goals,
                Interventions = t.Interventions,
                Status = t.Status.ToString(),
                StartDate = t.StartDate,
                TargetDate = t.TargetDate,
                EndDate = t.EndDate,
                ReviewDate = t.ReviewDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList()
        };
    }

    public async Task<DiagnosisResponse> AddDiagnosisAsync(int medicalRecordId, int doctorId, AddDiagnosisRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        if (doctorId <= 0)
        {
            throw new ArgumentException("Doctor ID must be a positive integer.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Diagnosis description cannot be empty or whitespace.", nameof(request.Description));
        }

        if (request.Description.Length > 500)
        {
            throw new ArgumentException("Diagnosis description cannot exceed 500 characters.", nameof(request.Description));
        }

        if (!string.IsNullOrEmpty(request.Code) && request.Code.Length > 50)
        {
            throw new ArgumentException("Diagnosis code cannot exceed 50 characters.", nameof(request.Code));
        }

        if (!string.IsNullOrEmpty(request.Notes) && request.Notes.Length > 2000)
        {
            throw new ArgumentException("Notes cannot exceed 2000 characters.", nameof(request.Notes));
        }

        if (!Enum.IsDefined(typeof(DiagnosisType), request.Type))
        {
            throw new ArgumentException("Invalid diagnosis type.", nameof(request.Type));
        }

        if (!Enum.IsDefined(typeof(DiagnosisStatus), request.Status))
        {
            throw new ArgumentException("Invalid diagnosis status.", nameof(request.Status));
        }

        if (!Enum.IsDefined(typeof(DiagnosisSeverity), request.Severity))
        {
            throw new ArgumentException("Invalid diagnosis severity.", nameof(request.Severity));
        }

        if (request.DiagnosedAt.HasValue)
        {
            if (request.DiagnosedAt.Value > DateTime.UtcNow.AddDays(1))
            {
                throw new ArgumentException("Diagnosed date cannot be in the future.", nameof(request.DiagnosedAt));
            }
            if (request.DiagnosedAt.Value < DateTime.UtcNow.AddYears(-100))
            {
                throw new ArgumentException("Diagnosed date cannot be more than 100 years in the past.", nameof(request.DiagnosedAt));
            }
        }

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor && u.Status == UserStatus.Active);
        if (doctor == null)
        {
            throw new UnauthorizedAccessException("Active doctor not found or unauthorized.");
        }

        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == medicalRecordId);
        if (record == null)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        if (record.DoctorId != doctorId)
        {
            throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
        }

        var now = DateTime.UtcNow;
        var diagnosedAt = request.DiagnosedAt ?? now;

        var diagnosis = new ClinicalDiagnosis
        {
            MedicalRecordId = medicalRecordId,
            PatientId = record.PatientId,
            DoctorId = doctorId,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Description = request.Description.Trim(),
            Type = request.Type,
            Status = request.Status,
            Severity = request.Severity,
            Notes = (request.Notes ?? string.Empty).Trim(),
            DiagnosedAt = diagnosedAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.Type == DiagnosisType.Primary)
        {
            record.Diagnosis = diagnosis.Description;
            record.UpdatedAt = now;
        }

        _context.ClinicalDiagnoses.Add(diagnosis);
        await _context.SaveChangesAsync();

        return new DiagnosisResponse
        {
            Id = diagnosis.Id,
            MedicalRecordId = diagnosis.MedicalRecordId,
            PatientId = diagnosis.PatientId,
            PatientName = $"{record.Patient?.FirstName} {record.Patient?.LastName}".Trim(),
            DoctorId = doctor.Id,
            DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}".Trim(),
            Code = diagnosis.Code,
            Description = diagnosis.Description,
            Type = diagnosis.Type.ToString(),
            Status = diagnosis.Status.ToString(),
            Severity = diagnosis.Severity.ToString(),
            Notes = diagnosis.Notes,
            DiagnosedAt = diagnosis.DiagnosedAt,
            CreatedAt = diagnosis.CreatedAt,
            UpdatedAt = diagnosis.UpdatedAt
        };
    }

    public async Task<DiagnosisResponse?> UpdateDiagnosisAsync(int medicalRecordId, int diagnosisId, int doctorId, UpdateDiagnosisRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        if (diagnosisId <= 0)
        {
            throw new ArgumentException("Diagnosis ID must be a positive integer.", nameof(diagnosisId));
        }

        if (doctorId <= 0)
        {
            throw new ArgumentException("Doctor ID must be a positive integer.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Diagnosis description cannot be empty or whitespace.", nameof(request.Description));
        }

        if (request.Description.Length > 500)
        {
            throw new ArgumentException("Diagnosis description cannot exceed 500 characters.", nameof(request.Description));
        }

        if (!string.IsNullOrEmpty(request.Code) && request.Code.Length > 50)
        {
            throw new ArgumentException("Diagnosis code cannot exceed 50 characters.", nameof(request.Code));
        }

        if (!string.IsNullOrEmpty(request.Notes) && request.Notes.Length > 2000)
        {
            throw new ArgumentException("Notes cannot exceed 2000 characters.", nameof(request.Notes));
        }

        if (!Enum.IsDefined(typeof(DiagnosisType), request.Type))
        {
            throw new ArgumentException("Invalid diagnosis type.", nameof(request.Type));
        }

        if (!Enum.IsDefined(typeof(DiagnosisStatus), request.Status))
        {
            throw new ArgumentException("Invalid diagnosis status.", nameof(request.Status));
        }

        if (!Enum.IsDefined(typeof(DiagnosisSeverity), request.Severity))
        {
            throw new ArgumentException("Invalid diagnosis severity.", nameof(request.Severity));
        }

        if (request.DiagnosedAt.HasValue)
        {
            if (request.DiagnosedAt.Value > DateTime.UtcNow.AddDays(1))
            {
                throw new ArgumentException("Diagnosed date cannot be in the future.", nameof(request.DiagnosedAt));
            }
            if (request.DiagnosedAt.Value < DateTime.UtcNow.AddYears(-100))
            {
                throw new ArgumentException("Diagnosed date cannot be more than 100 years in the past.", nameof(request.DiagnosedAt));
            }
        }

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor && u.Status == UserStatus.Active);
        if (doctor == null)
        {
            throw new UnauthorizedAccessException("Active doctor not found or unauthorized.");
        }

        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == medicalRecordId);
        if (record == null)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        if (record.DoctorId != doctorId)
        {
            throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
        }

        var diagnosis = await _context.ClinicalDiagnoses
            .FirstOrDefaultAsync(d => d.Id == diagnosisId && d.MedicalRecordId == medicalRecordId);
        if (diagnosis == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        diagnosis.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        diagnosis.Description = request.Description.Trim();
        diagnosis.Type = request.Type;
        diagnosis.Status = request.Status;
        diagnosis.Severity = request.Severity;
        diagnosis.Notes = (request.Notes ?? string.Empty).Trim();
        if (request.DiagnosedAt.HasValue)
        {
            diagnosis.DiagnosedAt = request.DiagnosedAt.Value;
        }
        diagnosis.DoctorId = doctorId;
        diagnosis.UpdatedAt = now;

        if (diagnosis.Type == DiagnosisType.Primary)
        {
            record.Diagnosis = diagnosis.Description;
            record.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        return new DiagnosisResponse
        {
            Id = diagnosis.Id,
            MedicalRecordId = diagnosis.MedicalRecordId,
            PatientId = diagnosis.PatientId,
            PatientName = $"{record.Patient?.FirstName} {record.Patient?.LastName}".Trim(),
            DoctorId = doctor.Id,
            DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}".Trim(),
            Code = diagnosis.Code,
            Description = diagnosis.Description,
            Type = diagnosis.Type.ToString(),
            Status = diagnosis.Status.ToString(),
            Severity = diagnosis.Severity.ToString(),
            Notes = diagnosis.Notes,
            DiagnosedAt = diagnosis.DiagnosedAt,
            CreatedAt = diagnosis.CreatedAt,
            UpdatedAt = diagnosis.UpdatedAt
        };
    }

    public async Task<DiagnosisResponse?> UpdatePrimaryDiagnosisAsync(int medicalRecordId, int doctorId, UpdateDiagnosisRequest request)
    {
        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        var primaryDiag = await _context.ClinicalDiagnoses
            .FirstOrDefaultAsync(d => d.MedicalRecordId == medicalRecordId && d.Type == DiagnosisType.Primary);

        if (primaryDiag != null)
        {
            return await UpdateDiagnosisAsync(medicalRecordId, primaryDiag.Id, doctorId, request);
        }

        var addRequest = new AddDiagnosisRequest
        {
            Code = request.Code,
            Description = request.Description,
            Type = DiagnosisType.Primary,
            Status = request.Status,
            Severity = request.Severity,
            Notes = request.Notes,
            DiagnosedAt = request.DiagnosedAt
        };

        return await AddDiagnosisAsync(medicalRecordId, doctorId, addRequest);
    }

    public async Task<List<DiagnosisResponse>> GetDiagnosesAsync(int medicalRecordId)
    {
        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        var recordExists = await _context.MedicalRecords.AnyAsync(r => r.Id == medicalRecordId);
        if (!recordExists)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        return await _context.ClinicalDiagnoses
            .AsNoTracking()
            .Where(d => d.MedicalRecordId == medicalRecordId)
            .OrderBy(d => d.Type)
            .ThenByDescending(d => d.DiagnosedAt)
            .Select(d => new DiagnosisResponse
            {
                Id = d.Id,
                MedicalRecordId = d.MedicalRecordId,
                PatientId = d.PatientId,
                PatientName = $"{d.Patient!.FirstName} {d.Patient.LastName}".Trim(),
                DoctorId = d.DoctorId,
                DoctorName = $"Dr. {d.Doctor!.FirstName} {d.Doctor.LastName}".Trim(),
                Code = d.Code,
                Description = d.Description,
                Type = d.Type.ToString(),
                Status = d.Status.ToString(),
                Severity = d.Severity.ToString(),
                Notes = d.Notes,
                DiagnosedAt = d.DiagnosedAt,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<TreatmentPlanResponse> RecordTreatmentPlanAsync(int medicalRecordId, int doctorId, RecordTreatmentPlanRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        if (doctorId <= 0)
        {
            throw new ArgumentException("Doctor ID must be a positive integer.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Treatment plan title cannot be empty or whitespace.", nameof(request.Title));
        }

        if (request.Title.Length > 200)
        {
            throw new ArgumentException("Treatment plan title cannot exceed 200 characters.", nameof(request.Title));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Treatment plan description cannot be empty or whitespace.", nameof(request.Description));
        }

        if (request.Description.Length > 2000)
        {
            throw new ArgumentException("Treatment plan description cannot exceed 2000 characters.", nameof(request.Description));
        }

        if (!string.IsNullOrEmpty(request.Goals) && request.Goals.Length > 1000)
        {
            throw new ArgumentException("Goals cannot exceed 1000 characters.", nameof(request.Goals));
        }

        if (!string.IsNullOrEmpty(request.Interventions) && request.Interventions.Length > 2000)
        {
            throw new ArgumentException("Interventions cannot exceed 2000 characters.", nameof(request.Interventions));
        }

        if (!Enum.IsDefined(typeof(TreatmentPlanCategory), request.Category))
        {
            throw new ArgumentException("Invalid treatment plan category.", nameof(request.Category));
        }

        if (!Enum.IsDefined(typeof(TreatmentPlanStatus), request.Status))
        {
            throw new ArgumentException("Invalid treatment plan status.", nameof(request.Status));
        }

        var effectiveStart = request.StartDate ?? DateTime.UtcNow;

        if (request.TargetDate.HasValue && request.TargetDate.Value < effectiveStart)
        {
            throw new ArgumentException("Target date must be on or after start date.", nameof(request.TargetDate));
        }

        if (request.EndDate.HasValue && request.EndDate.Value < effectiveStart)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(request.EndDate));
        }

        if (request.ReviewDate.HasValue && request.ReviewDate.Value < effectiveStart)
        {
            throw new ArgumentException("Review date must be on or after start date.", nameof(request.ReviewDate));
        }

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor && u.Status == UserStatus.Active);
        if (doctor == null)
        {
            throw new UnauthorizedAccessException("Active doctor not found or unauthorized.");
        }

        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == medicalRecordId);
        if (record == null)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        if (record.DoctorId != doctorId)
        {
            throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
        }

        var now = DateTime.UtcNow;

        var plan = new ClinicalTreatmentPlan
        {
            MedicalRecordId = medicalRecordId,
            PatientId = record.PatientId,
            DoctorId = doctorId,
            Title = request.Title.Trim(),
            Category = request.Category,
            Description = request.Description.Trim(),
            Goals = (request.Goals ?? string.Empty).Trim(),
            Interventions = (request.Interventions ?? string.Empty).Trim(),
            Status = request.Status,
            StartDate = effectiveStart,
            TargetDate = request.TargetDate,
            EndDate = request.EndDate,
            ReviewDate = request.ReviewDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        record.TreatmentPlan = plan.Description;
        record.UpdatedAt = now;

        _context.ClinicalTreatmentPlans.Add(plan);
        await _context.SaveChangesAsync();

        return new TreatmentPlanResponse
        {
            Id = plan.Id,
            MedicalRecordId = plan.MedicalRecordId,
            PatientId = plan.PatientId,
            PatientName = $"{record.Patient?.FirstName} {record.Patient?.LastName}".Trim(),
            DoctorId = doctor.Id,
            DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}".Trim(),
            Title = plan.Title,
            Category = plan.Category.ToString(),
            Description = plan.Description,
            Goals = plan.Goals,
            Interventions = plan.Interventions,
            Status = plan.Status.ToString(),
            StartDate = plan.StartDate,
            TargetDate = plan.TargetDate,
            EndDate = plan.EndDate,
            ReviewDate = plan.ReviewDate,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }

    public async Task<TreatmentPlanResponse?> UpdateTreatmentPlanAsync(int medicalRecordId, int treatmentPlanId, int doctorId, UpdateTreatmentPlanRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        if (treatmentPlanId <= 0)
        {
            throw new ArgumentException("Treatment plan ID must be a positive integer.", nameof(treatmentPlanId));
        }

        if (doctorId <= 0)
        {
            throw new ArgumentException("Doctor ID must be a positive integer.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Treatment plan title cannot be empty or whitespace.", nameof(request.Title));
        }

        if (request.Title.Length > 200)
        {
            throw new ArgumentException("Treatment plan title cannot exceed 200 characters.", nameof(request.Title));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Treatment plan description cannot be empty or whitespace.", nameof(request.Description));
        }

        if (request.Description.Length > 2000)
        {
            throw new ArgumentException("Treatment plan description cannot exceed 2000 characters.", nameof(request.Description));
        }

        if (!string.IsNullOrEmpty(request.Goals) && request.Goals.Length > 1000)
        {
            throw new ArgumentException("Goals cannot exceed 1000 characters.", nameof(request.Goals));
        }

        if (!string.IsNullOrEmpty(request.Interventions) && request.Interventions.Length > 2000)
        {
            throw new ArgumentException("Interventions cannot exceed 2000 characters.", nameof(request.Interventions));
        }

        if (!Enum.IsDefined(typeof(TreatmentPlanCategory), request.Category))
        {
            throw new ArgumentException("Invalid treatment plan category.", nameof(request.Category));
        }

        if (!Enum.IsDefined(typeof(TreatmentPlanStatus), request.Status))
        {
            throw new ArgumentException("Invalid treatment plan status.", nameof(request.Status));
        }

        var effectiveStart = request.StartDate ?? DateTime.UtcNow;

        if (request.TargetDate.HasValue && request.TargetDate.Value < effectiveStart)
        {
            throw new ArgumentException("Target date must be on or after start date.", nameof(request.TargetDate));
        }

        if (request.EndDate.HasValue && request.EndDate.Value < effectiveStart)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(request.EndDate));
        }

        if (request.ReviewDate.HasValue && request.ReviewDate.Value < effectiveStart)
        {
            throw new ArgumentException("Review date must be on or after start date.", nameof(request.ReviewDate));
        }

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor && u.Status == UserStatus.Active);
        if (doctor == null)
        {
            throw new UnauthorizedAccessException("Active doctor not found or unauthorized.");
        }

        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == medicalRecordId);
        if (record == null)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        if (record.DoctorId != doctorId)
        {
            throw new UnauthorizedAccessException("Doctor is not authorized to modify this medical record.");
        }

        var plan = await _context.ClinicalTreatmentPlans
            .FirstOrDefaultAsync(t => t.Id == treatmentPlanId && t.MedicalRecordId == medicalRecordId);
        if (plan == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        plan.Title = request.Title.Trim();
        plan.Category = request.Category;
        plan.Description = request.Description.Trim();
        plan.Goals = (request.Goals ?? string.Empty).Trim();
        plan.Interventions = (request.Interventions ?? string.Empty).Trim();
        plan.Status = request.Status;
        if (request.StartDate.HasValue)
        {
            plan.StartDate = request.StartDate.Value;
        }
        plan.TargetDate = request.TargetDate;
        plan.EndDate = request.EndDate;
        plan.ReviewDate = request.ReviewDate;
        plan.DoctorId = doctorId;
        plan.UpdatedAt = now;

        record.TreatmentPlan = plan.Description;
        record.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return new TreatmentPlanResponse
        {
            Id = plan.Id,
            MedicalRecordId = plan.MedicalRecordId,
            PatientId = plan.PatientId,
            PatientName = $"{record.Patient?.FirstName} {record.Patient?.LastName}".Trim(),
            DoctorId = doctor.Id,
            DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}".Trim(),
            Title = plan.Title,
            Category = plan.Category.ToString(),
            Description = plan.Description,
            Goals = plan.Goals,
            Interventions = plan.Interventions,
            Status = plan.Status.ToString(),
            StartDate = plan.StartDate,
            TargetDate = plan.TargetDate,
            EndDate = plan.EndDate,
            ReviewDate = plan.ReviewDate,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }

    public async Task<TreatmentPlanResponse?> UpdatePrimaryTreatmentPlanAsync(int medicalRecordId, int doctorId, UpdateTreatmentPlanRequest request)
    {
        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        var primaryPlan = await _context.ClinicalTreatmentPlans
            .FirstOrDefaultAsync(t => t.MedicalRecordId == medicalRecordId);

        if (primaryPlan != null)
        {
            return await UpdateTreatmentPlanAsync(medicalRecordId, primaryPlan.Id, doctorId, request);
        }

        var recordRequest = new RecordTreatmentPlanRequest
        {
            Title = request.Title,
            Category = request.Category,
            Description = request.Description,
            Goals = request.Goals,
            Interventions = request.Interventions,
            Status = request.Status,
            StartDate = request.StartDate,
            TargetDate = request.TargetDate,
            EndDate = request.EndDate,
            ReviewDate = request.ReviewDate
        };

        return await RecordTreatmentPlanAsync(medicalRecordId, doctorId, recordRequest);
    }

    public async Task<List<TreatmentPlanResponse>> GetTreatmentPlansAsync(int medicalRecordId)
    {
        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        var recordExists = await _context.MedicalRecords.AnyAsync(r => r.Id == medicalRecordId);
        if (!recordExists)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        return await _context.ClinicalTreatmentPlans
            .AsNoTracking()
            .Where(t => t.MedicalRecordId == medicalRecordId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TreatmentPlanResponse
            {
                Id = t.Id,
                MedicalRecordId = t.MedicalRecordId,
                PatientId = t.PatientId,
                PatientName = $"{t.Patient!.FirstName} {t.Patient.LastName}".Trim(),
                DoctorId = t.DoctorId,
                DoctorName = $"Dr. {t.Doctor!.FirstName} {t.Doctor.LastName}".Trim(),
                Title = t.Title,
                Category = t.Category.ToString(),
                Description = t.Description,
                Goals = t.Goals,
                Interventions = t.Interventions,
                Status = t.Status.ToString(),
                StartDate = t.StartDate,
                TargetDate = t.TargetDate,
                EndDate = t.EndDate,
                ReviewDate = t.ReviewDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<MedicalRecordVersionResponse>> GetVersionHistoryAsync(int medicalRecordId)
    {
        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        var recordExists = await _context.MedicalRecords.AnyAsync(r => r.Id == medicalRecordId);
        if (!recordExists)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        return await _context.MedicalRecordVersions
            .AsNoTracking()
            .Include(v => v.ChangedByUser)
            .Where(v => v.MedicalRecordId == medicalRecordId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new MedicalRecordVersionResponse
            {
                Id = v.Id,
                MedicalRecordId = v.MedicalRecordId,
                VersionNumber = v.VersionNumber,
                ChangedByUserId = v.ChangedByUserId,
                ChangedByUserName = v.ChangedByUser != null ? $"{v.ChangedByUser.FirstName} {v.ChangedByUser.LastName}".Trim() : string.Empty,
                ChangedAt = v.ChangedAt,
                ChangeType = v.ChangeType,
                ChangeSummary = v.ChangeSummary,
                ChiefComplaint = v.ChiefComplaint,
                Symptoms = v.Symptoms,
                ExaminationNotes = v.ExaminationNotes,
                Diagnosis = v.Diagnosis,
                TreatmentPlan = v.TreatmentPlan,
                FollowUpDate = v.FollowUpDate,
                PreviousChiefComplaint = v.PreviousChiefComplaint,
                PreviousSymptoms = v.PreviousSymptoms,
                PreviousExaminationNotes = v.PreviousExaminationNotes,
                PreviousDiagnosis = v.PreviousDiagnosis,
                PreviousTreatmentPlan = v.PreviousTreatmentPlan,
                PreviousFollowUpDate = v.PreviousFollowUpDate
            })
            .ToListAsync();
    }

    public async Task<MedicalRecordVersionResponse?> GetVersionAsync(int medicalRecordId, int versionNumber)
    {
        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Medical record ID must be a positive integer.", nameof(medicalRecordId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentException("Version number must be a positive integer.", nameof(versionNumber));
        }

        var recordExists = await _context.MedicalRecords.AnyAsync(r => r.Id == medicalRecordId);
        if (!recordExists)
        {
            throw new InvalidOperationException($"Medical record with ID {medicalRecordId} was not found.");
        }

        var version = await _context.MedicalRecordVersions
            .AsNoTracking()
            .Include(v => v.ChangedByUser)
            .FirstOrDefaultAsync(v => v.MedicalRecordId == medicalRecordId && v.VersionNumber == versionNumber);

        if (version == null)
        {
            return null;
        }

        return new MedicalRecordVersionResponse
        {
            Id = version.Id,
            MedicalRecordId = version.MedicalRecordId,
            VersionNumber = version.VersionNumber,
            ChangedByUserId = version.ChangedByUserId,
            ChangedByUserName = version.ChangedByUser != null ? $"{version.ChangedByUser.FirstName} {version.ChangedByUser.LastName}".Trim() : string.Empty,
            ChangedAt = version.ChangedAt,
            ChangeType = version.ChangeType,
            ChangeSummary = version.ChangeSummary,
            ChiefComplaint = version.ChiefComplaint,
            Symptoms = version.Symptoms,
            ExaminationNotes = version.ExaminationNotes,
            Diagnosis = version.Diagnosis,
            TreatmentPlan = version.TreatmentPlan,
            FollowUpDate = version.FollowUpDate,
            PreviousChiefComplaint = version.PreviousChiefComplaint,
            PreviousSymptoms = version.PreviousSymptoms,
            PreviousExaminationNotes = version.PreviousExaminationNotes,
            PreviousDiagnosis = version.PreviousDiagnosis,
            PreviousTreatmentPlan = version.PreviousTreatmentPlan,
            PreviousFollowUpDate = version.PreviousFollowUpDate
        };
    }
}
