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
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<BedAllocation> BedAllocations => Set<BedAllocation>();
    public DbSet<MedicalResource> MedicalResources => Set<MedicalResource>();
    public DbSet<ResourceMaintenance> ResourceMaintenances => Set<ResourceMaintenance>();

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
    }
}