using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PatientMedicalProfile> PatientMedicalProfiles => Set<PatientMedicalProfile>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabReport> LabReports => Set<LabReport>();
    public DbSet<ClinicalDiagnosis> ClinicalDiagnoses => Set<ClinicalDiagnosis>();
    public DbSet<ClinicalTreatmentPlan> ClinicalTreatmentPlans => Set<ClinicalTreatmentPlan>();
    public DbSet<MedicalRecordVersion> MedicalRecordVersions => Set<MedicalRecordVersion>();
    public DbSet<EmrAuditLog> EmrAuditLogs => Set<EmrAuditLog>();

    // ── Smart Appointment & Queue Management ──────────────────────────────────
    public DbSet<Department>               Departments                => Set<Department>();
    public DbSet<DoctorSchedule>           DoctorSchedules            => Set<DoctorSchedule>();
    public DbSet<Appointment>              Appointments               => Set<Appointment>();
    public DbSet<QueueEntry>               QueueEntries               => Set<QueueEntry>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistories => Set<AppointmentStatusHistory>();
    public DbSet<AppointmentNotification>  AppointmentNotifications   => Set<AppointmentNotification>();

    // ── Hospital Resource & Bed Management ────────────────────────────────────
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<BedAllocation> BedAllocations => Set<BedAllocation>();
    public DbSet<MedicalResource> MedicalResources => Set<MedicalResource>();
    public DbSet<ResourceMaintenance> ResourceMaintenances => Set<ResourceMaintenance>();

    // ── Doctor & Clinical Schedule Management ─────────────────────────────────
    public DbSet<ConsultationType> ConsultationTypes => Set<ConsultationType>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorLeave> DoctorLeaves => Set<DoctorLeave>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --------------------------------------------------
        // Existing User Entity Configuration (Preserved)
        // --------------------------------------------------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(u => u.Role)
                .IsRequired();

            entity.Property(u => u.Status)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        // --------------------------------------------------
        // EMR: PatientMedicalProfile Configuration
        // --------------------------------------------------
        modelBuilder.Entity<PatientMedicalProfile>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Gender)
                .HasMaxLength(20);

            entity.Property(p => p.BloodGroup)
                .IsRequired();

            entity.Property(p => p.Allergies)
                .HasMaxLength(500);

            entity.Property(p => p.ChronicDiseases)
                .HasMaxLength(500);

            entity.Property(p => p.EmergencyContactName)
                .HasMaxLength(100);

            entity.Property(p => p.EmergencyContactPhone)
                .HasMaxLength(20);

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .IsRequired();

            entity.HasIndex(p => p.PatientId)
                .IsUnique();

            entity.HasOne(p => p.Patient)
                .WithOne()
                .HasForeignKey<PatientMedicalProfile>(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // EMR: MedicalRecord Configuration
        // --------------------------------------------------
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.RecordNumber)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(m => m.VisitDate)
                .IsRequired();

            entity.Property(m => m.ChiefComplaint)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(m => m.Symptoms)
                .HasMaxLength(1000);

            entity.Property(m => m.ExaminationNotes)
                .HasMaxLength(2000);

            entity.Property(m => m.Diagnosis)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(m => m.TreatmentPlan)
                .HasMaxLength(2000);

            entity.Property(m => m.CreatedAt)
                .IsRequired();

            entity.Property(m => m.UpdatedAt)
                .IsRequired();

            entity.HasIndex(m => m.RecordNumber)
                .IsUnique();

            entity.HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Doctor)
                .WithMany()
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(m => m.Prescriptions)
                .WithOne(p => p.MedicalRecord)
                .HasForeignKey(p => p.MedicalRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(m => m.VitalSigns)
                .WithOne(v => v.MedicalRecord)
                .HasForeignKey(v => v.MedicalRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(m => m.LabOrders)
                .WithOne(l => l.MedicalRecord)
                .HasForeignKey(l => l.MedicalRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(m => m.Diagnoses)
                .WithOne(d => d.MedicalRecord)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(m => m.TreatmentPlans)
                .WithOne(t => t.MedicalRecord)
                .HasForeignKey(t => t.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(m => m.Versions)
                .WithOne(v => v.MedicalRecord)
                .HasForeignKey(v => v.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --------------------------------------------------
        // EMR: VitalSign Configuration
        // --------------------------------------------------
        modelBuilder.Entity<VitalSign>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.Property(v => v.RecordedAt)
                .IsRequired();

            entity.Property(v => v.TemperatureCelsius)
                .HasPrecision(4, 1);

            entity.Property(v => v.OxygenSaturationSpO2)
                .HasPrecision(4, 1);

            entity.Property(v => v.WeightKg)
                .HasPrecision(5, 2);

            entity.Property(v => v.HeightCm)
                .HasPrecision(5, 1);

            entity.Property(v => v.Bmi)
                .HasPrecision(4, 1);

            entity.Property(v => v.Notes)
                .HasMaxLength(500);

            entity.Property(v => v.CreatedAt)
                .IsRequired();

            entity.HasOne(v => v.Patient)
                .WithMany()
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.RecordedByUser)
                .WithMany()
                .HasForeignKey(v => v.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // EMR: Prescription Configuration
        // --------------------------------------------------
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PrescriptionNumber)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(p => p.IssueDate)
                .IsRequired();

            entity.Property(p => p.Status)
                .IsRequired();

            entity.Property(p => p.GeneralInstructions)
                .HasMaxLength(1000);

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .IsRequired();

            entity.HasIndex(p => p.PrescriptionNumber)
                .IsUnique();

            entity.HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Items)
                .WithOne(i => i.Prescription)
                .HasForeignKey(i => i.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.MedicineName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(i => i.Dosage)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(i => i.Route)
                .HasMaxLength(50);

            entity.Property(i => i.Frequency)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(i => i.DurationDays)
                .IsRequired();

            entity.Property(i => i.SpecialInstructions)
                .HasMaxLength(500);
        });

        // --------------------------------------------------
        // EMR: LabOrder & LabReport Configuration
        // --------------------------------------------------
        modelBuilder.Entity<LabOrder>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.OrderNumber)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(l => l.TestName)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(l => l.Category)
                .HasMaxLength(100);

            entity.Property(l => l.Priority)
                .IsRequired();

            entity.Property(l => l.Status)
                .IsRequired();

            entity.Property(l => l.OrderedAt)
                .IsRequired();

            entity.Property(l => l.ClinicalNotes)
                .HasMaxLength(1000);

            entity.HasIndex(l => l.OrderNumber)
                .IsUnique();

            entity.HasOne(l => l.Patient)
                .WithMany()
                .HasForeignKey(l => l.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Doctor)
                .WithMany()
                .HasForeignKey(l => l.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Report)
                .WithOne(r => r.LabOrder)
                .HasForeignKey<LabReport>(r => r.LabOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LabReport>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.ReportDate)
                .IsRequired();

            entity.Property(r => r.ResultSummary)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(r => r.Findings)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(r => r.ReferenceRange)
                .HasMaxLength(500);

            entity.Property(r => r.DoctorRemarks)
                .HasMaxLength(1000);

            entity.Property(r => r.AttachmentUrl)
                .HasMaxLength(500);

            entity.Property(r => r.CreatedAt)
                .IsRequired();

            entity.HasOne(r => r.ConductedByUser)
                .WithMany()
                .HasForeignKey(r => r.ConductedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // EMR: ClinicalDiagnosis Configuration
        // --------------------------------------------------
        modelBuilder.Entity<ClinicalDiagnosis>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(d => d.Code)
                .HasMaxLength(50);

            entity.Property(d => d.Notes)
                .HasMaxLength(2000);

            entity.Property(d => d.Type)
                .IsRequired();

            entity.Property(d => d.Status)
                .IsRequired();

            entity.Property(d => d.Severity)
                .IsRequired();

            entity.Property(d => d.DiagnosedAt)
                .IsRequired();

            entity.Property(d => d.CreatedAt)
                .IsRequired();

            entity.Property(d => d.UpdatedAt)
                .IsRequired();

            entity.HasOne(d => d.MedicalRecord)
                .WithMany(m => m.Diagnoses)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Patient)
                .WithMany()
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Doctor)
                .WithMany()
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // EMR: ClinicalTreatmentPlan Configuration
        // --------------------------------------------------
        modelBuilder.Entity<ClinicalTreatmentPlan>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(t => t.Category)
                .IsRequired();

            entity.Property(t => t.Goals)
                .HasMaxLength(1000);

            entity.Property(t => t.Interventions)
                .HasMaxLength(2000);

            entity.Property(t => t.Status)
                .IsRequired();

            entity.Property(t => t.StartDate)
                .IsRequired();

            entity.Property(t => t.CreatedAt)
                .IsRequired();

            entity.Property(t => t.UpdatedAt)
                .IsRequired();

            entity.HasOne(t => t.MedicalRecord)
                .WithMany(m => m.TreatmentPlans)
                .HasForeignKey(t => t.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Patient)
                .WithMany()
                .HasForeignKey(t => t.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Doctor)
                .WithMany()
                .HasForeignKey(t => t.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // EMR: MedicalRecordVersion Configuration
        // --------------------------------------------------
        modelBuilder.Entity<MedicalRecordVersion>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.Property(v => v.ChangeType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.ChangeSummary)
                .HasMaxLength(1000);

            entity.Property(v => v.ChiefComplaint)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(v => v.Symptoms)
                .HasMaxLength(1000);

            entity.Property(v => v.ExaminationNotes)
                .HasMaxLength(2000);

            entity.Property(v => v.Diagnosis)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(v => v.TreatmentPlan)
                .HasMaxLength(2000);

            entity.Property(v => v.PreviousChiefComplaint)
                .HasMaxLength(500);

            entity.Property(v => v.PreviousSymptoms)
                .HasMaxLength(1000);

            entity.Property(v => v.PreviousExaminationNotes)
                .HasMaxLength(2000);

            entity.Property(v => v.PreviousDiagnosis)
                .HasMaxLength(500);

            entity.Property(v => v.PreviousTreatmentPlan)
                .HasMaxLength(2000);

            entity.HasIndex(v => new { v.MedicalRecordId, v.VersionNumber })
                .IsUnique();

            entity.HasOne(v => v.MedicalRecord)
                .WithMany(m => m.Versions)
                .HasForeignKey(v => v.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.ChangedByUser)
                .WithMany()
                .HasForeignKey(v => v.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // EMR: EmrAuditLog Configuration
        // --------------------------------------------------
        modelBuilder.Entity<EmrAuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Metadata)
                .HasMaxLength(1000);

            entity.Property(a => a.Timestamp)
                .IsRequired();

            entity.Property(a => a.IsSuccess)
                .IsRequired();

            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.PatientId);
            entity.HasIndex(a => a.UserId);

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Smart Appointment & Queue Management ──────────────────────────────
        // Department and DoctorSchedule are configured below, under
        // "Doctor & Clinical Schedule Management Configuration" — that module owns them.

        // Appointment
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.ReferenceNumber)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(a => a.Notes)
                .HasMaxLength(1000);

            entity.Property(a => a.CancelledReason)
                .HasMaxLength(500);

            entity.HasIndex(a => a.ReferenceNumber)
                .IsUnique();

            entity.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Department)
                .WithMany()
                .HasForeignKey(a => a.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.RescheduledFrom)
                .WithMany()
                .HasForeignKey(a => a.RescheduledFromId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.QueueEntry)
                .WithOne(q => q.Appointment)
                .HasForeignKey<QueueEntry>(q => q.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.StatusHistories)
                .WithOne(h => h.Appointment)
                .HasForeignKey(h => h.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.Notifications)
                .WithOne(n => n.Appointment)
                .HasForeignKey(n => n.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QueueEntry
        modelBuilder.Entity<QueueEntry>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.HasOne(q => q.Doctor)
                .WithMany()
                .HasForeignKey(q => q.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment ↔ QueueEntry 1:1 is configured from the Appointment side above
        });

        // AppointmentStatusHistory
        modelBuilder.Entity<AppointmentStatusHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.Reason)
                .HasMaxLength(500);

            entity.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment relationship configured from Appointment side
        });

        // AppointmentNotification
        modelBuilder.Entity<AppointmentNotification>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment relationship configured from Appointment side
        });

        // --------------------------------------------------
        // Ward Configuration
        // --------------------------------------------------
        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(w => w.Code)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(w => w.Floor)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(w => w.BuildingBlock)
                .HasMaxLength(50);

            entity.Property(w => w.Capacity)
                .IsRequired();

            entity.Property(w => w.Type)
                .IsRequired();

            entity.Property(w => w.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(w => w.CreatedAt)
                .IsRequired();

            entity.Property(w => w.UpdatedAt)
                .IsRequired();

            entity.HasIndex(w => w.Code)
                .IsUnique();

            entity.HasMany(w => w.Rooms)
                .WithOne(r => r.Ward)
                .HasForeignKey(r => r.WardId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // Room Configuration
        // --------------------------------------------------
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.RoomNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(r => r.Type)
                .IsRequired();

            entity.Property(r => r.Capacity)
                .IsRequired();

            entity.Property(r => r.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(r => r.CreatedAt)
                .IsRequired();

            entity.Property(r => r.UpdatedAt)
                .IsRequired();

            entity.HasMany(r => r.Beds)
                .WithOne(b => b.Room)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // Bed Configuration
        // --------------------------------------------------
        modelBuilder.Entity<Bed>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.BedNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(b => b.Type)
                .IsRequired();

            entity.Property(b => b.Status)
                .IsRequired();

            entity.Property(b => b.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(b => b.CreatedAt)
                .IsRequired();

            entity.Property(b => b.UpdatedAt)
                .IsRequired();

            entity.HasIndex(b => new { b.RoomId, b.BedNumber })
                .IsUnique();

            entity.HasMany(b => b.Allocations)
                .WithOne(a => a.Bed)
                .HasForeignKey(a => a.BedId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // Admission Configuration
        // --------------------------------------------------
        modelBuilder.Entity<Admission>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.AdmissionNumber)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(a => a.ReasonForAdmission)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(a => a.Diagnosis)
                .HasMaxLength(1000);

            entity.Property(a => a.DischargeSummary)
                .HasMaxLength(1000);

            entity.Property(a => a.Status)
                .IsRequired();

            entity.Property(a => a.Priority)
                .IsRequired();

            entity.Property(a => a.AdmissionDate)
                .IsRequired();

            entity.Property(a => a.CreatedAt)
                .IsRequired();

            entity.Property(a => a.UpdatedAt)
                .IsRequired();

            entity.HasIndex(a => a.AdmissionNumber)
                .IsUnique();

            entity.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AdmittingDoctor)
                .WithMany()
                .HasForeignKey(a => a.AdmittingDoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(a => a.BedAllocations)
                .WithOne(ba => ba.Admission)
                .HasForeignKey(ba => ba.AdmissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------
        // BedAllocation Configuration
        // --------------------------------------------------
        modelBuilder.Entity<BedAllocation>(entity =>
        {
            entity.HasKey(ba => ba.Id);

            entity.Property(ba => ba.Status)
                .IsRequired();

            entity.Property(ba => ba.AllocatedAt)
                .IsRequired();

            entity.Property(ba => ba.Notes)
                .HasMaxLength(500);

            entity.Property(ba => ba.CreatedAt)
                .IsRequired();

            entity.Property(ba => ba.UpdatedAt)
                .IsRequired();

            // PostgreSQL filtered unique index: only 1 active allocation per physical bed
            entity.HasIndex(ba => ba.BedId)
                .IsUnique()
                .HasFilter("\"Status\" = 1"); // 1 = BedAllocationStatus.Active

            entity.HasOne(ba => ba.Bed)
                .WithMany(b => b.Allocations)
                .HasForeignKey(ba => ba.BedId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ba => ba.Admission)
                .WithMany(a => a.BedAllocations)
                .HasForeignKey(ba => ba.AdmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ba => ba.AllocatedByStaff)
                .WithMany()
                .HasForeignKey(ba => ba.AllocatedByStaffId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --------------------------------------------------
        // MedicalResource Configuration
        // --------------------------------------------------
        modelBuilder.Entity<MedicalResource>(entity =>
        {
            entity.HasKey(mr => mr.Id);

            entity.Property(mr => mr.ResourceCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(mr => mr.Name)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(mr => mr.LocationDescription)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(mr => mr.SerialNumber)
                .HasMaxLength(100);

            entity.Property(mr => mr.Manufacturer)
                .HasMaxLength(100);

            entity.Property(mr => mr.ModelNumber)
                .HasMaxLength(100);

            entity.Property(mr => mr.Category)
                .IsRequired();

            entity.Property(mr => mr.Status)
                .IsRequired();

            entity.Property(mr => mr.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(mr => mr.CreatedAt)
                .IsRequired();

            entity.Property(mr => mr.UpdatedAt)
                .IsRequired();

            entity.HasIndex(mr => mr.ResourceCode)
                .IsUnique();

            entity.HasOne(mr => mr.Ward)
                .WithMany()
                .HasForeignKey(mr => mr.WardId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(mr => mr.Room)
                .WithMany()
                .HasForeignKey(mr => mr.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(mr => mr.Bed)
                .WithMany()
                .HasForeignKey(mr => mr.BedId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --------------------------------------------------
        // ResourceMaintenance Configuration
        // --------------------------------------------------
        modelBuilder.Entity<ResourceMaintenance>(entity =>
        {
            entity.HasKey(rm => rm.Id);

            entity.Property(rm => rm.MaintenanceCode)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(rm => rm.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(rm => rm.ResolutionNotes)
                .HasMaxLength(1000);

            entity.Property(rm => rm.Type)
                .IsRequired();

            entity.Property(rm => rm.Status)
                .IsRequired();

            entity.Property(rm => rm.ScheduledStart)
                .IsRequired();

            entity.Property(rm => rm.CreatedAt)
                .IsRequired();

            entity.Property(rm => rm.UpdatedAt)
                .IsRequired();

            entity.HasIndex(rm => rm.MaintenanceCode)
                .IsUnique();

            // PostgreSQL check constraint: exactly one target must be present
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_ResourceMaintenance_SingleTarget",
                "(\"BedId\" IS NOT NULL AND \"MedicalResourceId\" IS NULL) OR (\"BedId\" IS NULL AND \"MedicalResourceId\" IS NOT NULL)"
            ));

            entity.HasOne(rm => rm.Bed)
                .WithMany()
                .HasForeignKey(rm => rm.BedId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rm => rm.MedicalResource)
                .WithMany(mr => mr.Maintenances)
                .HasForeignKey(rm => rm.MedicalResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rm => rm.PerformedByStaff)
                .WithMany()
                .HasForeignKey(rm => rm.PerformedByStaffId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --------------------------------------------------
        // Doctor & Clinical Schedule Management Configuration
        // --------------------------------------------------
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.Description)
                .HasMaxLength(500);

            entity.Property(d => d.Status)
                .IsRequired();

            entity.HasIndex(d => d.Name)
                .IsUnique();
        });

        modelBuilder.Entity<ConsultationType>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.DurationMinutes)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.Status)
                .IsRequired();

            entity.HasIndex(c => c.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(d => d.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(d => d.Specialization)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(d => d.LicenseNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.Bio)
                .HasMaxLength(2000);

            entity.Property(d => d.Status)
                .IsRequired();

            entity.HasIndex(d => d.Email)
                .IsUnique();

            entity.HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.DayOfWeek)
                .IsRequired();

            entity.Property(s => s.StartTime)
                .IsRequired();

            entity.Property(s => s.EndTime)
                .IsRequired();

            entity.Property(s => s.SlotDurationMinutes)
                .IsRequired()
                .HasDefaultValue(30);

            entity.Property(s => s.MaxPatientsPerDay)
                .IsRequired()
                .HasDefaultValue(20);

            entity.Property(s => s.Status)
                .IsRequired();

            entity.HasIndex(s => new { s.DoctorId, s.DayOfWeek });

            entity.HasOne(s => s.Doctor)
                .WithMany()
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.ConsultationType)
                .WithMany()
                .HasForeignKey(s => s.ConsultationTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DoctorLeave>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.StartDate)
                .IsRequired();

            entity.Property(l => l.EndDate)
                .IsRequired();

            entity.Property(l => l.Reason)
                .HasMaxLength(500);

            entity.Property(l => l.Status)
                .IsRequired();

            entity.HasOne(l => l.Doctor)
                .WithMany()
                .HasForeignKey(l => l.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}