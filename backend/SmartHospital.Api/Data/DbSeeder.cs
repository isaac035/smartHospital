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

        if (adminExists)
        {
            return;
        }

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
}