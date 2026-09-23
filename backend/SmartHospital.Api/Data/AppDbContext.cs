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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            entity.Property(p => p.Allergies)
                .HasMaxLength(500);

            entity.Property(p => p.ChronicDiseases)
                .HasMaxLength(500);

            entity.Property(p => p.EmergencyContactName)
                .HasMaxLength(100);

            entity.Property(p => p.EmergencyContactPhone)
                .HasMaxLength(20);

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
        });

        // --------------------------------------------------
        // EMR: VitalSign Configuration
        // --------------------------------------------------
        modelBuilder.Entity<VitalSign>(entity =>
        {
            entity.HasKey(v => v.Id);

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

            entity.Property(p => p.GeneralInstructions)
                .HasMaxLength(1000);

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

            entity.HasOne(r => r.ConductedByUser)
                .WithMany()
                .HasForeignKey(r => r.ConductedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}