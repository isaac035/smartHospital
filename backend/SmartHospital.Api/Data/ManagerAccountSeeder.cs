using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Models;

namespace SmartHospital.Api.Data;

/// <summary>
/// Makes sure the four feature-manager logins exist on every startup (idempotent).
/// Existing accounts are never duplicated and their passwords are never overwritten;
/// they are only re-activated and given the expected role.
/// </summary>
public static class ManagerAccountSeeder
{
    public const string PasswordConfigKey = "SeedManagers:Password";
    private const string DefaultPassword = "227740";

    private static readonly (string FirstName, string LastName, string Email, string Phone, UserRole Role)[] Managers =
    {
        ("Appointment", "Manager", "medimate.appointmentmanager@gmail.com", "0770000101", UserRole.AppointmentManager),
        ("Doctor", "Manager", "medimate.doctormanager@gmail.com", "0770000102", UserRole.DoctorManager),
        ("Resource", "Manager", "medimate.resourcemanager@gmail.com", "0770000103", UserRole.ResourceAdmin),
        ("Clinical Care", "Manager", "medimate.clinicalmanager@gmail.com", "0770000104", UserRole.ClinicalCareManager),
    };

    public static async Task SeedAsync(AppDbContext context, IConfiguration configuration, ILogger logger)
    {
        var password = configuration[PasswordConfigKey];
        if (string.IsNullOrWhiteSpace(password))
        {
            password = DefaultPassword;
        }

        foreach (var manager in Managers)
        {
            try
            {
                var email = manager.Email.ToLowerInvariant();
                var existing = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

                if (existing == null)
                {
                    context.Users.Add(new User
                    {
                        FirstName = manager.FirstName,
                        LastName = manager.LastName,
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        PhoneNumber = manager.Phone,
                        Role = manager.Role,
                        Status = UserStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                    logger.LogInformation("[ManagerSeeder] Created {Role} account {Email}.", manager.Role, email);
                    continue;
                }

                if (existing.Role != manager.Role || existing.Status != UserStatus.Active)
                {
                    existing.Role = manager.Role;
                    existing.Status = UserStatus.Active;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    logger.LogInformation("[ManagerSeeder] {Email} already exists; role/status corrected to {Role}/Active.", email, manager.Role);
                }
                else
                {
                    logger.LogInformation("[ManagerSeeder] {Email} already exists; no changes needed.", email);
                }
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                logger.LogError(ex, "[ManagerSeeder] Failed to seed {Role} account {Email}.", manager.Role, manager.Email);
            }
        }
    }
}
