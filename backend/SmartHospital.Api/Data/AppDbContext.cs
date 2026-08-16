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
    }
}