using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext context)
    {
        await context.Database.MigrateAsync();

        var adminExists = await context.Users
            .AnyAsync(u =>
                u.Email == "admin@smarthospital.local");

        if (!adminExists)
        {
            var admin = new User
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@smarthospital.local",

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        "Admin123!"
                    ),

                PhoneNumber = "0770000000",

                Role = UserRole.Admin,

                Status = UserStatus.Active,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);

            await context.SaveChangesAsync();
        }

        var departmentExists = await context.Departments
            .AnyAsync(d => d.Name == "General Medicine");

        if (departmentExists)
        {
            return;
        }

        var department = new Department
        {
            Name = "General Medicine",
            Description = "General medical consultations and primary care.",
            Status = DepartmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);

        var generalConsultation = new ConsultationType
        {
            Name = "General Consultation",
            DurationMinutes = 30,
            Description = "Standard consultation session.",
            Status = ConsultationTypeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var followUp = new ConsultationType
        {
            Name = "Follow-up",
            DurationMinutes = 15,
            Description = "Short follow-up session.",
            Status = ConsultationTypeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.ConsultationTypes.AddRange(generalConsultation, followUp);

        await context.SaveChangesAsync();
    }
}
