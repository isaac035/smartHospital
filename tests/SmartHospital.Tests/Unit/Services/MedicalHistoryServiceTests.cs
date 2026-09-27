using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class MedicalHistoryServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private (User patient, User doctor) SeedDefaultUsers(AppDbContext context, int patientId = 1, int doctorId = 2)
    {
        var patient = new User
        {
            Id = patientId,
            FirstName = "Emily",
            LastName = "Watson",
            Email = $"emily{patientId}@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor = new User
        {
            Id = doctorId,
            FirstName = "Stephen",
            LastName = "Strange",
            Email = $"doctor{doctorId}@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient, doctor);
        context.SaveChanges();

        return (patient, doctor);
    }

    [Fact]
    public async Task GetPatientTimelineAsync_WithFullClinicalHistory_AggregatesAndSortsNewestFirst()
    {
        using var context = CreateInMemoryDbContext();
        var (patient, doctor) = SeedDefaultUsers(context);
        var service = new MedicalHistoryService(context);

        var baseTime = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);

        // 1. MedicalRecord (Event date: baseTime + 1 day)
        var record = new MedicalRecord
        {
            Id = 101,
            RecordNumber = "REC-101",
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            VisitDate = baseTime.AddDays(1),
            ChiefComplaint = "Chest congestion and cough",
            Diagnosis = "Acute Bronchitis",
            TreatmentPlan = "Inhaler therapy",
            CreatedAt = baseTime.AddDays(1),
            UpdatedAt = baseTime.AddDays(1)
        };
        context.MedicalRecords.Add(record);

        // 2. Appointment / ClinicalVisit (Event date: baseTime)
        var appointment = new Appointment
        {
            Id = 201,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ScheduledStart = baseTime,
            Status = AppointmentStatus.Completed,
            ReferenceNumber = "APT-201",
            Notes = "Routine follow-up"
        };
        context.Appointments.Add(appointment);

        // 3. ClinicalDiagnosis (Event date: baseTime + 2 days)
        var diagnosis = new ClinicalDiagnosis
        {
            Id = 301,
            MedicalRecordId = record.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Code = "J20.9",
            Description = "Acute bronchitis, unspecified",
            Type = DiagnosisType.Primary,
            Status = DiagnosisStatus.Active,
            Severity = DiagnosisSeverity.Moderate,
            DiagnosedAt = baseTime.AddDays(2),
            CreatedAt = baseTime.AddDays(2),
            UpdatedAt = baseTime.AddDays(2)
        };
        context.ClinicalDiagnoses.Add(diagnosis);

        // 4. Prescription (Event date: baseTime + 3 days)
        var prescription = new Prescription
        {
            Id = 401,
            PrescriptionNumber = "RX-401",
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            MedicalRecordId = record.Id,
            IssueDate = baseTime.AddDays(3),
            Status = PrescriptionStatus.Active,
            CreatedAt = baseTime.AddDays(3),
            UpdatedAt = baseTime.AddDays(3),
            Items = new List<PrescriptionItem>
            {
                new()
                {
                    Id = 1,
                    MedicineName = "Albuterol",
                    Dosage = "90mcg",
                    Frequency = "Every 4-6h",
                    DurationDays = 7
                }
            }
        };
        context.Prescriptions.Add(prescription);

        // 5. LabOrder (Event date: baseTime + 4 days)
        var labOrder = new LabOrder
        {
            Id = 501,
            OrderNumber = "ORD-501",
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            MedicalRecordId = record.Id,
            TestName = "Chest X-Ray",
            Category = "Radiology",
            Priority = LabOrderPriority.Routine,
            Status = LabOrderStatus.Completed,
            OrderedAt = baseTime.AddDays(4)
        };
        context.LabOrders.Add(labOrder);

        // 6. LabReport (Event date: baseTime + 5 days)
        var labReport = new LabReport
        {
            Id = 601,
            LabOrderId = labOrder.Id,
            ReportDate = baseTime.AddDays(5),
            ResultSummary = "Clear lung fields, no infiltrates",
            Findings = "Normal study",
            ConductedByUserId = doctor.Id,
            CreatedAt = baseTime.AddDays(5)
        };
        context.LabReports.Add(labReport);

        // 7. VitalSign (Event date: baseTime + 6 days)
        var vitalSign = new VitalSign
        {
            Id = 701,
            PatientId = patient.Id,
            RecordedByUserId = doctor.Id,
            RecordedAt = baseTime.AddDays(6),
            TemperatureCelsius = 37.1m,
            SystolicBloodPressure = 118,
            DiastolicBloodPressure = 78,
            HeartRateBpm = 72,
            OxygenSaturationSpO2 = 98.5m,
            CreatedAt = baseTime.AddDays(6)
        };
        context.VitalSigns.Add(vitalSign);

        // 8. ClinicalTreatmentPlan (Event date: baseTime + 7 days - NEWEST)
        var treatmentPlan = new ClinicalTreatmentPlan
        {
            Id = 801,
            MedicalRecordId = record.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Title = "Pulmonary Rehabilitation",
            Category = TreatmentPlanCategory.Rehabilitative,
            Description = "Incentive spirometry and deep breathing",
            Status = TreatmentPlanStatus.Active,
            StartDate = baseTime.AddDays(7),
            CreatedAt = baseTime.AddDays(7),
            UpdatedAt = baseTime.AddDays(7)
        };
        context.ClinicalTreatmentPlans.Add(treatmentPlan);

        await context.SaveChangesAsync();

        // Act
        var timeline = await service.GetPatientTimelineAsync(patient.Id);

        // Assert
        Assert.NotNull(timeline);
        Assert.Equal(patient.Id, timeline.PatientId);
        Assert.Equal("Emily Watson", timeline.PatientName);
        Assert.Equal(8, timeline.TotalEvents);
        Assert.Equal(8, timeline.Events.Count);

        // Chronological order verification: newest first
        for (int i = 0; i < timeline.Events.Count - 1; i++)
        {
            Assert.True(timeline.Events[i].EventDate >= timeline.Events[i + 1].EventDate,
                $"Event at index {i} ({timeline.Events[i].EventType}, {timeline.Events[i].EventDate}) should be >= event at index {i + 1} ({timeline.Events[i + 1].EventType}, {timeline.Events[i + 1].EventDate})");
        }

        // Newest event should be TreatmentPlan (baseTime + 7 days)
        Assert.Equal("TreatmentPlan", timeline.Events[0].EventType);
        Assert.Equal(801, timeline.Events[0].SourceRecordId);
        Assert.Equal(doctor.Id, timeline.Events[0].DoctorId);
        Assert.Equal("Dr. Stephen Strange", timeline.Events[0].DoctorName);
        Assert.Contains("Pulmonary Rehabilitation", timeline.Events[0].Summary);

        // Second newest should be VitalSign (baseTime + 6 days)
        Assert.Equal("VitalSign", timeline.Events[1].EventType);
        Assert.Equal(701, timeline.Events[1].SourceRecordId);
        Assert.Contains("118/78 mmHg", timeline.Events[1].Summary);

        // Third should be LabReport (baseTime + 5 days)
        Assert.Equal("LabReport", timeline.Events[2].EventType);
        Assert.Equal(601, timeline.Events[2].SourceRecordId);
        Assert.Contains("Clear lung fields", timeline.Events[2].Summary);

        // Oldest should be ClinicalVisit (baseTime)
        Assert.Equal("ClinicalVisit", timeline.Events[7].EventType);
        Assert.Equal(201, timeline.Events[7].SourceRecordId);
        Assert.Equal(baseTime, timeline.Events[7].EventDate);
    }

    [Fact]
    public async Task GetPatientTimelineAsync_WhenPatientHasNoRecords_ReturnsEmptyTimelineWithZeroTotal()
    {
        using var context = CreateInMemoryDbContext();
        var (patient, _) = SeedDefaultUsers(context);
        var service = new MedicalHistoryService(context);

        var timeline = await service.GetPatientTimelineAsync(patient.Id);

        Assert.NotNull(timeline);
        Assert.Equal(patient.Id, timeline.PatientId);
        Assert.Equal("Emily Watson", timeline.PatientName);
        Assert.Equal(0, timeline.TotalEvents);
        Assert.Empty(timeline.Events);
    }

    [Fact]
    public async Task GetPatientTimelineAsync_WithNonExistentPatient_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        SeedDefaultUsers(context);
        var service = new MedicalHistoryService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetPatientTimelineAsync(999));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPatientTimelineAsync_WithInvalidPatientId_ThrowsArgumentException(int invalidId)
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicalHistoryService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetPatientTimelineAsync(invalidId));
    }

    [Fact]
    public async Task GetPatientTimelineAsync_ExcludesOtherPatientsClinicalData()
    {
        using var context = CreateInMemoryDbContext();
        var (patient1, doctor) = SeedDefaultUsers(context, 1, 2);

        var patient2 = new User
        {
            Id = 10,
            FirstName = "Mark",
            LastName = "Ruffalo",
            Email = "mark@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };
        context.Users.Add(patient2);

        // Data for patient 2
        context.MedicalRecords.Add(new MedicalRecord
        {
            Id = 500,
            RecordNumber = "REC-500",
            PatientId = patient2.Id,
            DoctorId = doctor.Id,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Other patient complaint",
            Diagnosis = "Other diagnosis"
        });

        // Data for patient 1
        context.MedicalRecords.Add(new MedicalRecord
        {
            Id = 501,
            RecordNumber = "REC-501",
            PatientId = patient1.Id,
            DoctorId = doctor.Id,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = "Patient 1 complaint",
            Diagnosis = "Patient 1 diagnosis"
        });

        await context.SaveChangesAsync();

        var service = new MedicalHistoryService(context);

        var timeline1 = await service.GetPatientTimelineAsync(patient1.Id);

        Assert.Single(timeline1.Events);
        Assert.Equal(501, timeline1.Events[0].SourceRecordId);
        Assert.Equal("Patient 1 complaint", timeline1.Events[0].Summary.Split("Chief complaint - ")[1]);
        Assert.DoesNotContain(timeline1.Events, e => e.PatientId == patient2.Id);
    }
}
