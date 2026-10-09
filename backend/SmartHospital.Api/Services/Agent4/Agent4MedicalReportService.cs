using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Agent4;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services.Agent4;

public class Agent4MedicalReportService : IAgent4MedicalReportService
{
    private const string InternalKeyHeader = "X-Internal-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly Agent4Settings _settings;
    private readonly ILogger<Agent4MedicalReportService> _logger;

    public Agent4MedicalReportService(AppDbContext db, HttpClient http, IOptions<Agent4Settings> settings,
        ILogger<Agent4MedicalReportService> logger)
    { _db = db; _http = http; _settings = settings.Value; _logger = logger; }

    public async Task<MedicalReportResponse> GenerateAsync(int patientId, int? appointmentId, Guid? generationId, bool? checkupRequested, CancellationToken ct)
    {
        if (generationId.HasValue)
        {
            var prior = await _db.AiMedicalReports.AsNoTracking().Include(r => r.Appointment).ThenInclude(a => a!.Doctor)
                .FirstOrDefaultAsync(r => r.PatientId == patientId && r.ClientGenerationId == generationId, ct);
            if (prior != null) return ToResponse(prior);
        }
        var patient = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == patientId && u.Role == UserRole.Patient, ct)
            ?? throw new KeyNotFoundException("Patient was not found.");
        var profile = await _db.PatientMedicalProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == patientId, ct);

        var appointments = await _db.Appointments.AsNoTracking().Include(a => a.Doctor).Include(a => a.Department).Include(a => a.Agent1TriageResult)
            .Where(a => a.PatientId == patientId && (!appointmentId.HasValue || a.Id == appointmentId.Value))
            .OrderByDescending(a => a.ScheduledStart).ToListAsync(ct);
        if (appointmentId.HasValue && appointments.Count == 0) throw new KeyNotFoundException("Appointment was not found for this patient.");
        var appointment = appointments.FirstOrDefault();
        var doctorProfile = appointment == null ? null : await _db.Doctors.AsNoTracking()
            .Where(d => d.UserId == appointment.DoctorId)
            .Select(d => new { d.Specialization, Department = d.Department != null ? d.Department.Name : null })
            .FirstOrDefaultAsync(ct);

        var allMedicalRecords = await _db.MedicalRecords.AsNoTracking().Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId).OrderByDescending(r => r.VisitDate).ToListAsync(ct);
        var encounterRecords = appointment == null ? new List<MedicalRecord>() : allMedicalRecords.Where(r => r.AppointmentId == appointment.Id).ToList();
        var historicalRecords = allMedicalRecords.Where(r => appointment == null || r.AppointmentId != appointment.Id).ToList();

        var vitals = await _db.VitalSigns.AsNoTracking().Where(v => v.PatientId == patientId).OrderByDescending(v => v.RecordedAt).ToListAsync(ct);
        var prescriptions = await _db.Prescriptions.AsNoTracking().Include(p => p.Items).Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssueDate).ToListAsync(ct);
        var labOrders = await _db.LabOrders.AsNoTracking().Include(l => l.Report).Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.OrderedAt).ToListAsync(ct);
        var diagnoses = await _db.ClinicalDiagnoses.AsNoTracking().Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.DiagnosedAt).ToListAsync(ct);
        var treatmentPlans = await _db.ClinicalTreatmentPlans.AsNoTracking().Where(t => t.PatientId == patientId)
            .OrderByDescending(t => t.StartDate).ToListAsync(ct);

        var timeline = new List<(DateTime Date, object Event)>();
        timeline.AddRange(allMedicalRecords.Select(r => (r.VisitDate, (object)new { date = r.VisitDate, type = "MedicalRecord", sourceId = r.Id, summary = r.ChiefComplaint })));
        timeline.AddRange(appointments.Select(a => (a.ScheduledStart, (object)new { date = a.ScheduledStart, type = "Appointment", sourceId = a.Id, summary = $"{a.AppointmentType} appointment ({a.Status})" })));
        timeline.AddRange(vitals.Select(v => (v.RecordedAt, (object)new { date = v.RecordedAt, type = "VitalSign", sourceId = v.Id, summary = v.Notes })));
        timeline.AddRange(prescriptions.Select(p => (p.IssueDate, (object)new { date = p.IssueDate, type = "Prescription", sourceId = p.Id, summary = $"{p.PrescriptionNumber} ({p.Status})" })));
        timeline.AddRange(labOrders.Select(l => (l.OrderedAt, (object)new { date = l.OrderedAt, type = "LabOrder", sourceId = l.Id, summary = $"{l.TestName} ({l.Status})" })));
        timeline.AddRange(diagnoses.Select(d => (d.DiagnosedAt, (object)new { date = d.DiagnosedAt, type = "ClinicalDiagnosis", sourceId = d.Id, summary = $"{d.Type}: {d.Description} ({d.Status})" })));
        timeline.AddRange(treatmentPlans.Select(t => (t.StartDate, (object)new { date = t.StartDate, type = "TreatmentPlan", sourceId = t.Id, summary = $"{t.Title}: {t.Description} ({t.Status})" })));

        var sourceReferences = new
        {
            appointmentIds = appointments.Select(a => a.Id).ToArray(),
            triageResultIds = appointments.Where(a => a.Agent1TriageResult != null).Select(a => a.Agent1TriageResult!.Id).ToArray(),
            medicalRecordIds = allMedicalRecords.Select(r => r.Id).ToArray(),
            vitalSignIds = vitals.Select(v => v.Id).ToArray(),
            prescriptionIds = prescriptions.Select(p => p.Id).ToArray(),
            labOrderIds = labOrders.Select(l => l.Id).ToArray(),
            clinicalDiagnosisIds = diagnoses.Select(d => d.Id).ToArray(),
            treatmentPlanIds = treatmentPlans.Select(t => t.Id).ToArray()
        };

        object? checkup = null;
        if (appointment != null)
        {
            var allocations = await _db.AppointmentResourceAllocations.AsNoTracking()
                .Include(a => a.Bed)!.ThenInclude(b => b!.Room)!.ThenInclude(r => r!.Ward)
                .Include(a => a.MedicalResource)!.ThenInclude(r => r!.Ward)
                .Where(a => a.AppointmentId == appointment.Id)
                .OrderBy(a => a.AllocatedAt).ToListAsync(ct);
            var admission = await _db.Admissions.AsNoTracking().Include(a => a.BedAllocations.Where(b => b.Status == BedAllocationStatus.Active))
                .ThenInclude(b => b.Bed)!.ThenInclude(b => b!.Room)!.ThenInclude(r => r!.Ward)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointment.Id, ct);
            var resources = allocations.Select(a => a.BedId.HasValue
                ? new
                {
                    resourceId = a.BedId,
                    kind = "bed",
                    name = $"Bed {a.Bed?.BedNumber}",
                    code = a.Bed?.BedNumber,
                    type = a.Bed?.Type.ToString(),
                    room = a.Bed?.Room?.RoomNumber,
                    ward = a.Bed?.Room?.Ward?.Name,
                    status = a.Bed?.Status.ToString(),
                    allocatedAt = a.AllocatedAt,
                    sourceId = a.Id
                }
                : (object)new
                {
                    resourceId = a.MedicalResourceId,
                    kind = "equipment",
                    name = a.MedicalResource?.Name,
                    code = a.MedicalResource?.ResourceCode,
                    type = a.MedicalResource?.Category.ToString(),
                    room = (string?)null,
                    ward = a.MedicalResource?.Ward?.Name,
                    status = a.MedicalResource?.Status.ToString(),
                    allocatedAt = a.AllocatedAt,
                    sourceId = a.Id
                }).ToList();
            var activeAdmissionBed = admission?.BedAllocations.FirstOrDefault()?.Bed;
            checkup = new
            {
                status = resources.Count > 0 || admission != null
                    ? "Allocation recorded"
                    : checkupRequested == false
                        ? "Not requested"
                        : checkupRequested == true
                            ? "Checkup requested; no Agent 3 allocation was saved"
                            : "No Agent 3 allocation recorded; Agent 3 may have been skipped, unavailable, or no suitable resource was saved",
                admission = admission == null ? null : new
                {
                    admissionId = admission.Id,
                    admissionNumber = admission.AdmissionNumber,
                    status = admission.Status.ToString(),
                    admissionDate = admission.AdmissionDate,
                    checkupDate = admission.CheckupDate,
                    reasonForAdmission = admission.ReasonForAdmission,
                    bed = activeAdmissionBed == null ? null : new { bedNumber = activeAdmissionBed.BedNumber, room = activeAdmissionBed.Room?.RoomNumber, ward = activeAdmissionBed.Room?.Ward?.Name, status = activeAdmissionBed.Status.ToString() }
                },
                resources
            };
        }

        var sectionsPresent = new List<string> { "Patient Information" };
        if (appointment != null) sectionsPresent.Add("Appointment Summary");
        if (encounterRecords.Any(r => !string.IsNullOrWhiteSpace(r.Symptoms) || !string.IsNullOrWhiteSpace(r.ChiefComplaint))) sectionsPresent.Add("Presenting Symptoms");
        if (encounterRecords.Count > 0) sectionsPresent.Add("Current Encounter");
        if (checkup != null) sectionsPresent.Add("Medical Checkup");
        if (historicalRecords.Count > 0) sectionsPresent.Add("Previous Medical History");
        if (vitals.Count > 0) sectionsPresent.Add("Vital Signs");
        if (prescriptions.Count > 0) sectionsPresent.Add("Prescriptions");
        if (labOrders.Count > 0) sectionsPresent.Add("Lab Reports");
        if (diagnoses.Count > 0) sectionsPresent.Add("Clinical Diagnoses");
        if (treatmentPlans.Count > 0) sectionsPresent.Add("Treatment Plans");
        if (timeline.Count > 0) sectionsPresent.Add("Clinical Timeline");
        if (appointments.Count > 0 || allMedicalRecords.Count > 0) sectionsPresent.Add("Consultations & Encounters");

        var recordedData = new
        {
            patientInformation = new
            {
                patientId = patient.Id,
                name = $"{patient.FirstName} {patient.LastName}".Trim(),
                dateOfBirth = profile?.DateOfBirth,
                age = AgeInYears(profile?.DateOfBirth, DateTime.UtcNow),
                gender = BlankToNull(profile?.Gender),
                bloodGroup = profile == null || profile.BloodGroup == BloodGroup.Unknown ? null : profile.BloodGroup.ToString(),
                allergies = BlankToNull(profile?.Allergies),
                chronicConditions = BlankToNull(profile?.ChronicDiseases)
            },
            appointmentSummary = appointment == null ? null : new
            {
                appointmentId = appointment.Id,
                referenceNumber = appointment.ReferenceNumber,
                doctorId = appointment.DoctorId,
                doctor = appointment.Doctor == null ? null : $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}".Trim(),
                department = appointment.Department?.Name,
                doctorSpecialization = BlankToNull(doctorProfile?.Specialization),
                doctorDepartment = doctorProfile?.Department,
                date = appointment.ScheduledStart,
                durationMinutes = appointment.EstimatedDurationMinutes,
                type = appointment.AppointmentType.ToString(),
                status = appointment.Status.ToString(),
                priority = (appointment.RequestedPriority ?? appointment.Priority).ToString(),
                notes = appointment.Notes,
                agent2Outcome = new { source = "Persisted appointment after patient selected a slot", result = "Booked", appointmentId = appointment.Id, slotStart = appointment.ScheduledStart, durationMinutes = appointment.EstimatedDurationMinutes, status = appointment.Status.ToString(), note = "A separate Agent 2 recommendation or ranking result was not persisted, so none is inferred." }
            },
            presentingSymptoms = appointment?.Agent1TriageResult is { } triage
                ? (object)new { status = "Recorded verbatim from Agent 1 input", records = new object[] { new { source = "Agent1TriageResult", sourceId = triage.Id, visitDate = triage.CreatedAt, chiefComplaint = (string?)null, symptoms = triage.Symptoms } }.Concat(encounterRecords.Select(r => (object)new { source = "MedicalRecord", sourceId = r.Id, visitDate = r.VisitDate, chiefComplaint = r.ChiefComplaint, symptoms = r.Symptoms })) }
                : encounterRecords.Count == 0
                    ? new { status = "No symptoms recorded for this appointment", records = Array.Empty<object>() }
                    : new { status = "Recorded", records = encounterRecords.Select(r => new { source = "MedicalRecord", sourceId = r.Id, visitDate = r.VisitDate, chiefComplaint = r.ChiefComplaint, symptoms = r.Symptoms }) },
            currentEncounter = encounterRecords.Count == 0
                ? (object)new { status = "No consultation notes recorded for this appointment", records = Array.Empty<object>() }
                : new { status = "Recorded", records = encounterRecords.Select(r => new { sourceId = r.Id, recordNumber = r.RecordNumber, visitDate = r.VisitDate, doctor = r.Doctor == null ? null : $"{r.Doctor.FirstName} {r.Doctor.LastName}".Trim(), chiefComplaint = r.ChiefComplaint, symptoms = r.Symptoms, examinationNotes = r.ExaminationNotes, diagnosis = r.Diagnosis, treatmentPlan = r.TreatmentPlan, followUpDate = r.FollowUpDate }) },
            clinicalTriageSummary = new
            {
                status = appointment?.Agent1TriageResult == null ? "No linked Agent 1 result is available for this appointment" : "Persisted Agent 1 result",
                triageResultId = appointment?.Agent1TriageResult?.Id,
                category = appointment?.Agent1TriageResult?.Category,
                priority = appointment?.Agent1TriageResult?.Priority,
                reason = appointment?.Agent1TriageResult?.Reason,
                confidence = appointment?.Agent1TriageResult?.Confidence,
                appointmentPriority = appointment == null ? null : (appointment.RequestedPriority ?? appointment.Priority).ToString(),
                emergencyFlag = appointment?.Agent1TriageResult?.PossibleEmergency,
                emergencyNotice = appointment?.Agent1TriageResult?.EmergencyNotice,
                resultStatus = appointment?.Agent1TriageResult?.Status,
                appointmentDepartment = appointment?.Department?.Name,
                source = appointment?.Agent1TriageResult == null ? "No original Agent 1 result was linked; appointment fields are shown independently." : "Agent1TriageResult linked to this appointment"
            },
            medicalCheckup = checkup ?? new { status = "Not requested", admission = (object?)null, resources = Array.Empty<object>() },
            previousMedicalHistory = historicalRecords.Count == 0
                ? (object)new { status = "No previous medical history recorded", records = Array.Empty<object>() }
                : new { status = "Previously recorded", records = historicalRecords.Select(r => new { sourceId = r.Id, recordNumber = r.RecordNumber, visitDate = r.VisitDate, chiefComplaint = r.ChiefComplaint, symptoms = r.Symptoms, examinationNotes = r.ExaminationNotes, diagnosis = r.Diagnosis, treatmentPlan = r.TreatmentPlan, followUpDate = r.FollowUpDate }) },
            vitalSigns = vitals.Count == 0 ? (object)new { status = "No vital signs recorded", records = Array.Empty<object>() }
                : new { status = "Recorded", records = vitals.Select(v => new { sourceId = v.Id, recordedAt = v.RecordedAt, temperatureCelsius = v.TemperatureCelsius, systolicBloodPressure = v.SystolicBloodPressure, diastolicBloodPressure = v.DiastolicBloodPressure, heartRateBpm = v.HeartRateBpm, respiratoryRateBpm = v.RespiratoryRateBpm, oxygenSaturationSpO2 = v.OxygenSaturationSpO2, weightKg = v.WeightKg, heightCm = v.HeightCm, bmi = v.Bmi, notes = v.Notes }) },
            prescriptions = prescriptions.Count == 0 ? (object)new { status = "No prescriptions recorded", records = Array.Empty<object>() }
                : new { status = "Recorded", records = prescriptions.Select(p => new { sourceId = p.Id, prescriptionNumber = p.PrescriptionNumber, issueDate = p.IssueDate, expiryDate = p.ExpiryDate, status = p.Status.ToString(), generalInstructions = p.GeneralInstructions, items = p.Items.Select(i => new { medicineName = i.MedicineName, dosage = i.Dosage, route = i.Route, frequency = i.Frequency, durationDays = i.DurationDays, specialInstructions = i.SpecialInstructions }) }) },
            labReports = labOrders.Count == 0 ? (object)new { status = "No laboratory orders recorded", records = Array.Empty<object>() }
                : new { status = "Recorded orders and any attached results", records = labOrders.Select(l => new { sourceId = l.Id, orderNumber = l.OrderNumber, testName = l.TestName, category = l.Category, priority = l.Priority.ToString(), status = l.Status.ToString(), orderedAt = l.OrderedAt, clinicalNotes = l.ClinicalNotes, report = l.Report == null ? null : new { sourceId = l.Report.Id, reportDate = l.Report.ReportDate, resultSummary = l.Report.ResultSummary, findings = l.Report.Findings, referenceRange = l.Report.ReferenceRange, doctorRemarks = l.Report.DoctorRemarks } }) },
            clinicalDiagnoses = diagnoses.Count == 0 ? (object)new { status = "No clinical diagnoses recorded", records = Array.Empty<object>() }
                : new { status = "Previously recorded diagnoses", records = diagnoses.Select(d => new { sourceId = d.Id, medicalRecordId = d.MedicalRecordId, diagnosedAt = d.DiagnosedAt, code = d.Code, description = d.Description, type = d.Type.ToString(), status = d.Status.ToString(), severity = d.Severity.ToString(), notes = d.Notes }) },
            treatmentPlans = treatmentPlans.Count == 0 ? (object)new { status = "No treatment plans recorded", records = Array.Empty<object>() }
                : new { status = "Recorded treatment plans", records = treatmentPlans.Select(t => new { sourceId = t.Id, medicalRecordId = t.MedicalRecordId, title = t.Title, category = t.Category.ToString(), description = t.Description, goals = t.Goals, interventions = t.Interventions, status = t.Status.ToString(), startDate = t.StartDate, targetDate = t.TargetDate, reviewDate = t.ReviewDate }) },
            clinicalTimeline = timeline.Count == 0 ? (object)new { status = "No clinical timeline events recorded", events = Array.Empty<object>() }
                : new { status = "Recorded events", events = timeline.OrderByDescending(e => e.Date).Select(e => e.Event) },
            consultationsAndEncounters = new
            {
                status = appointments.Count == 0 && allMedicalRecords.Count == 0 ? "No consultations or encounters recorded" : "Recorded appointments and clinical encounters",
                appointments = appointments.Select(a => new { sourceId = a.Id, date = a.ScheduledStart, type = a.AppointmentType.ToString(), doctor = a.Doctor == null ? null : $"{a.Doctor.FirstName} {a.Doctor.LastName}".Trim(), status = a.Status.ToString() }),
                encounters = allMedicalRecords.Select(r => new { sourceId = r.Id, date = r.VisitDate, recordNumber = r.RecordNumber, doctor = r.Doctor == null ? null : $"{r.Doctor.FirstName} {r.Doctor.LastName}".Trim(), chiefComplaint = r.ChiefComplaint })
            },
            followUp = new { recordedDates = allMedicalRecords.Where(r => r.FollowUpDate.HasValue).Select(r => new { medicalRecordId = r.Id, date = r.FollowUpDate }), suggestedTests = "No tests are reported as completed unless a result is present in Lab Reports." },
            sectionsPresent,
            sourceReferences
        };

        var recordedJson = JsonSerializer.SerializeToElement(recordedData, JsonOptions);
        Agent4ReasonResponse reasoning;
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.InternalApiKey)) throw new InvalidOperationException("Agent 4 internal API key is not configured.");
            using var message = new HttpRequestMessage(HttpMethod.Post, "v1/reports/reason")
            { Content = JsonContent.Create(new Agent4ReasonRequest { RecordedData = recordedJson }, options: JsonOptions) };
            message.Headers.Add(InternalKeyHeader, _settings.InternalApiKey);
            using var reasonerTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 1, 120)));
            using var response = await _http.SendAsync(message, reasonerTimeout.Token);
            response.EnsureSuccessStatusCode();
            reasoning = await response.Content.ReadFromJsonAsync<Agent4ReasonResponse>(JsonOptions, reasonerTimeout.Token)
                ?? throw new InvalidOperationException("Agent 4 returned an empty response.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Agent 4 reasoner unavailable; storing a structured non-AI fallback for patient {PatientId}.", patientId);
            reasoning = FallbackReasoning(appointment, sectionsPresent);
        }

        // Once the source data and reasoning are assembled, finish persistence even if the HTTP
        // caller disconnected. The generation id lets a client retry and retrieve this exact row.
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, CancellationToken.None);
        var version = (await _db.AiMedicalReports.Where(r => r.PatientId == patientId).MaxAsync(r => (int?)r.VersionNumber, CancellationToken.None) ?? 0) + 1;
        var reportId = Guid.NewGuid();
        var reportCreatedAt = DateTime.UtcNow;
        var content = new
        {
            recordedData,
            followUpSuggestedTests = new
            {
                recordedFollowUpDates = allMedicalRecords.Where(r => r.FollowUpDate.HasValue).Select(r => new { medicalRecordId = r.Id, date = r.FollowUpDate }).ToArray(),
                note = "Only lab tests with a recorded LabReport contain results; AI recommendations remain suggestions for clinical review."
            },
            aiSummary = new { text = reasoning.AiSummary, mode = reasoning.Mode, label = reasoning.Mode == "gemini" ? "AI Summary" : "Non-AI fallback summary" },
            aiRecommendations = new { label = "General wellness and priority-based follow-up guidance", advisoryOnly = true, items = SafeRecommendations(appointment) },
            recommendations = BuildRecommendations(reasoning.Recommendations),
            // Contact details are stored with the report for display but never sent to Agent 4.
            patientContact = new { phone = BlankToNull(patient.PhoneNumber), email = patient.Email.EndsWith(UserService.WalkInEmailDomain, StringComparison.OrdinalIgnoreCase) ? null : BlankToNull(patient.Email) },
            reportMetadata = new { reportId, versionNumber = version, generatedAt = reportCreatedAt, sourceAppointmentId = appointment?.Id, formatVersion = 2 }
        };
        var json = JsonSerializer.Serialize(content, JsonOptions);
        // The appointment was loaded AsNoTracking above. Do not put that detached graph
        // on an Added report: EF would traverse it and try to insert the existing
        // appointment (and its doctor/department) again. The FK is sufficient to link it.
        var entity = new AiMedicalReport { ReportId = reportId, PatientId = patientId, AppointmentId = appointment?.Id, VersionNumber = version, CreatedAt = reportCreatedAt, ContentJson = json, ClientGenerationId = generationId };
        _db.AiMedicalReports.Add(entity);
        await _db.SaveChangesAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return ToResponse(entity, appointment);
    }

    public async Task<List<MedicalReportListItem>> GetPatientReportsAsync(int patientId, CancellationToken ct)
    {
        return await _db.AiMedicalReports.AsNoTracking().Where(r => r.PatientId == patientId).OrderByDescending(r => r.CreatedAt)
            .Select(r => new MedicalReportListItem
            {
                ReportId = r.ReportId, PatientId = r.PatientId, AppointmentId = r.AppointmentId,
                VersionNumber = r.VersionNumber, CreatedAt = r.CreatedAt, GenerationId = r.ClientGenerationId,
                AppointmentType = r.Appointment == null ? null : r.Appointment.AppointmentType.ToString(),
                DoctorName = r.Appointment != null && r.Appointment.Doctor != null ? r.Appointment.Doctor.FirstName + " " + r.Appointment.Doctor.LastName : null,
                Priority = r.Appointment == null ? null : (r.Appointment.RequestedPriority ?? r.Appointment.Priority).ToString()
            }).ToListAsync(ct);
    }

    public async Task<MedicalReportResponse?> GetByReportIdAsync(Guid reportId, CancellationToken ct)
    {
        var entity = await _db.AiMedicalReports.AsNoTracking().Include(r => r.Appointment).ThenInclude(a => a!.Doctor)
            .FirstOrDefaultAsync(r => r.ReportId == reportId, ct);
        return entity == null ? null : ToResponse(entity);
    }

    private static MedicalReportResponse ToResponse(AiMedicalReport entity, Appointment? appointmentOverride = null)
    {
        using var doc = JsonDocument.Parse(entity.ContentJson);
        var appointment = appointmentOverride ?? entity.Appointment;
        return new MedicalReportResponse
        {
            ReportId = entity.ReportId, PatientId = entity.PatientId, AppointmentId = entity.AppointmentId,
            VersionNumber = entity.VersionNumber, CreatedAt = entity.CreatedAt, GenerationId = entity.ClientGenerationId,
            AppointmentType = appointment?.AppointmentType.ToString(),
            DoctorName = appointment?.Doctor == null ? null : $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}".Trim(),
            Priority = appointment == null ? null : (appointment.RequestedPriority ?? appointment.Priority).ToString(),
            Content = doc.RootElement.Clone()
        };
    }

    private const int MaxRecommendationItems = 5;
    private const int MaxRecommendationLength = 200;
    public const string RecommendationsUnavailableMessage = "Recommendations are not available for this report.";

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? AgeInYears(DateTime? dateOfBirth, DateTime today)
    {
        if (dateOfBirth == null) return null;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.Date.AddYears(-age)) age--;
        return age is >= 0 and < 150 ? age : null;
    }

    private static string? CleanText(string? value, int max)
    {
        var text = string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length == 0 || text.Length > max || text.IndexOfAny(new[] { '<', '>' }) >= 0 ? null : text;
    }

    private static string? TextOf(JsonElement parent, string name, int max) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? CleanText(value.GetString(), max)
            : null;

    private static string[] ListOf(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();
        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => CleanText(item.GetString(), MaxRecommendationLength))
            .OfType<string>()
            .Distinct()
            .Take(MaxRecommendationItems)
            .ToArray();
    }

    private static JsonElement ChildOf(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : default;

    /// <summary>
    /// Re-validates Agent 4's optional recommendations before they are stored: only the known fields,
    /// plain strings, capped counts and lengths. Anything unusable becomes the "not available" marker.
    /// </summary>
    public static object BuildRecommendations(JsonElement? raw)
    {
        var unavailable = new { available = false, message = RecommendationsUnavailableMessage };
        if (raw is not { ValueKind: JsonValueKind.Object } root) return unavailable;

        var diet = ChildOf(root, "diet");
        var exercise = ChildOf(root, "exercise");
        var followUp = ChildOf(root, "followUp");
        var result = new
        {
            available = true,
            advisoryOnly = true,
            basis = TextOf(root, "basis", 40) == "condition_specific" ? "condition_specific" : "general",
            diet = new { prefer = ListOf(diet, "prefer"), avoid = ListOf(diet, "avoid") },
            exercise = new { suitable = ListOf(exercise, "suitable"), avoid = ListOf(exercise, "avoid") },
            lifestyle = ListOf(root, "lifestyle"),
            warningSigns = ListOf(root, "warningSigns"),
            followUp = new { timeframe = TextOf(followUp, "timeframe", 80), monitoring = ListOf(followUp, "monitoring") },
            note = TextOf(root, "note", 300)
        };
        var itemCount = result.diet.prefer.Length + result.diet.avoid.Length + result.exercise.suitable.Length
            + result.exercise.avoid.Length + result.lifestyle.Length + result.warningSigns.Length + result.followUp.monitoring.Length;
        return itemCount > 0 || result.followUp.timeframe != null ? result : unavailable;
    }

    private static Agent4ReasonResponse FallbackReasoning(Appointment? appointment, List<string> sections)
    {
        return new Agent4ReasonResponse
        {
            Mode = "non_ai_fallback",
            AiSummary = "Non-AI structured summary: this report organizes the recorded patient and encounter information. Recorded sections: " + (sections.Count == 0 ? "none" : string.Join(", ", sections)) + ".",
            AiRecommendations = SafeRecommendations(appointment)
        };
    }

    private static List<string> SafeRecommendations(Appointment? appointment)
    {
        var priority = appointment?.Agent1TriageResult?.Priority
            ?? appointment?.RequestedPriority?.ToString()
            ?? appointment?.Priority.ToString();
        var followUp = priority?.ToLowerInvariant() switch
        {
            "emergency" => "If symptoms worsen or new severe symptoms develop, seek immediate medical attention. Follow up with your doctor as they advise.",
            "urgent" => "A follow-up visit within the timeframe recommended by your doctor is advisable.",
            "normal" => "Routine follow-up as advised by your treating clinician is recommended.",
            _ => "Follow up with your doctor as they advise."
        };

        return new List<string>
        {
            "Staying adequately hydrated may help.",
            "Light regular activity as tolerated by your current condition may help.",
            "Getting adequate rest is generally advisable.",
            "Avoid known triggers if any are documented on file.",
            followUp
        };
    }
}
