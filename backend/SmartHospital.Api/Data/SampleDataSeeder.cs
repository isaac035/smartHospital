using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Data;

// Development-only demo data. Each record is added only if its unique key
// (name or email) is missing, so restarts never duplicate or overwrite data.
public static class SampleDataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;

        // Departments
        var departmentSeeds = new[]
        {
            ("Cardiology", "Heart and cardiovascular care.", DepartmentStatus.Active),
            ("Pediatrics", "Medical care for infants, children and adolescents.", DepartmentStatus.Active),
            ("Orthopedics", "Bones, joints, muscles and sports injuries.", DepartmentStatus.Active),
            ("Dermatology", "Skin, hair and nail conditions.", DepartmentStatus.Active),
            ("Neurology", "Brain, spinal cord and nervous system disorders.", DepartmentStatus.Active),
            ("ENT", "Ear, nose and throat care. Temporarily closed for renovation.", DepartmentStatus.Inactive)
        };

        foreach (var (name, description, status) in departmentSeeds)
        {
            if (!await context.Departments.AnyAsync(d => d.Name == name))
            {
                context.Departments.Add(new Department
                {
                    Name = name,
                    Description = description,
                    Status = status,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // Consultation types
        var consultationSeeds = new[]
        {
            ("Specialist Consultation", 45, "In-depth consultation with a specialist.", ConsultationTypeStatus.Active),
            ("Pediatric Check-up", 30, "Routine check-up for children.", ConsultationTypeStatus.Active),
            ("Telemedicine", 20, "Remote video consultation.", ConsultationTypeStatus.Active),
            ("Procedure Review", 60, "Pre- or post-procedure review. Currently unavailable.", ConsultationTypeStatus.Inactive)
        };

        foreach (var (name, duration, description, status) in consultationSeeds)
        {
            if (!await context.ConsultationTypes.AnyAsync(c => c.Name == name))
            {
                context.ConsultationTypes.Add(new ConsultationType
                {
                    Name = name,
                    DurationMinutes = duration,
                    Description = description,
                    Status = status,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // Login users (staff and patients)
        var userSeeds = new[]
        {
            ("Kasun", "Fernando", "staff@smarthospital.local", "Staff123!", "0772223344", UserRole.Staff, UserStatus.Active),
            ("Amaya", "Silva", "patient@smarthospital.local", "Patient123!", "0715550101", UserRole.Patient, UserStatus.Active),
            ("Ravi", "Jayasinghe", "ravi.jayasinghe@example.com", "Patient123!", "0715550102", UserRole.Patient, UserStatus.Active),
            ("Tharushi", "Wickramasinghe", "tharushi.w@example.com", "Patient123!", "0715550103", UserRole.Patient, UserStatus.Active),
            ("Dinesh", "Kumar", "dinesh.kumar@example.com", "Patient123!", "0715550104", UserRole.Patient, UserStatus.Inactive),
            ("Malini", "Rajapaksha", "malini.r@example.com", "Patient123!", "0715550105", UserRole.Patient, UserStatus.Suspended)
        };

        foreach (var (firstName, lastName, email, password, phone, role, status) in userSeeds)
        {
            if (!await context.Users.AnyAsync(u => u.Email == email))
            {
                context.Users.Add(new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    PhoneNumber = phone,
                    Role = role,
                    Status = status,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
