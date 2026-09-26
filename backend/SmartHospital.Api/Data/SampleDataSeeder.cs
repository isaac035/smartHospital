using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Models;
using DayOfWeek = SmartHospital.Api.Models.DayOfWeek;

namespace SmartHospital.Api.Data;

// Development-only demo data. Each record is added only if its unique key
// (name or email) is missing, so restarts never duplicate or overwrite data.
public static class SampleDataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

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

        var departments = await context.Departments
            .ToDictionaryAsync(d => d.Name, d => d.Id);

        var consultationTypes = await context.ConsultationTypes
            .ToDictionaryAsync(c => c.Name, c => c.Id);

        // Doctor profiles (no login accounts)
        var doctorSeeds = new[]
        {
            ("Ashan", "Wijesekara", "ashan.wijesekara@smarthospital.local", "0773000001", "Cardiology", "Interventional Cardiology", "SLMC-10231", 15, "Consultant cardiologist specialising in angioplasty and heart failure.", DoctorStatus.Active),
            ("Priya", "Gunawardena", "priya.gunawardena@smarthospital.local", "0773000002", "Cardiology", "Cardiac Electrophysiology", "SLMC-10874", 9, "Treats heart rhythm disorders and manages pacemaker patients.", DoctorStatus.Active),
            ("Sanjeewa", "Bandara", "sanjeewa.bandara@smarthospital.local", "0773000003", "Pediatrics", "General Pediatrics", "SLMC-11502", 12, "Paediatrician focused on child development and vaccinations.", DoctorStatus.Active),
            ("Ishara", "de Silva", "ishara.desilva@smarthospital.local", "0773000004", "Pediatrics", "Pediatric Allergy", "SLMC-12045", 5, "Manages childhood asthma, allergies and eczema.", DoctorStatus.OnLeave),
            ("Roshan", "Herath", "roshan.herath@smarthospital.local", "0773000005", "Orthopedics", "Sports Medicine", "SLMC-09877", 18, "Orthopaedic surgeon treating sports injuries and joint replacements.", DoctorStatus.Active),
            ("Nimali", "Karunaratne", "nimali.karunaratne@smarthospital.local", "0773000006", "Dermatology", "Clinical Dermatology", "SLMC-11233", 7, "Treats acne, psoriasis and other chronic skin conditions.", DoctorStatus.Active),
            ("Chaminda", "Ekanayake", "chaminda.ekanayake@smarthospital.local", "0773000007", "Neurology", "Neurology", "SLMC-08765", 21, "Senior neurologist with a focus on epilepsy and stroke care.", DoctorStatus.Active),
            ("Lakshan", "Senanayake", "lakshan.senanayake@smarthospital.local", "0773000008", "General Medicine", "Internal Medicine", "SLMC-13310", 3, "Physician managing diabetes, hypertension and general illness.", DoctorStatus.Inactive)
        };

        var newDoctors = new Dictionary<string, Doctor>();

        foreach (var (firstName, lastName, email, phone, department, specialization, license, years, bio, status) in doctorSeeds)
        {
            if (!departments.TryGetValue(department, out var departmentId) ||
                await context.Doctors.AnyAsync(d => d.Email == email))
            {
                continue;
            }

            var doctor = new Doctor
            {
                DepartmentId = departmentId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phone,
                Specialization = specialization,
                LicenseNumber = license,
                YearsOfExperience = years,
                Bio = bio,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Doctors.Add(doctor);
            newDoctors[email] = doctor;
        }

        await context.SaveChangesAsync();

        // Weekly schedules, only for doctors created above
        var scheduleSeeds = new[]
        {
            ("ashan.wijesekara@smarthospital.local", DayOfWeek.Monday, "08:00", "12:00", "Specialist Consultation"),
            ("ashan.wijesekara@smarthospital.local", DayOfWeek.Wednesday, "14:00", "17:00", "Follow-up"),
            ("ashan.wijesekara@smarthospital.local", DayOfWeek.Friday, "08:00", "12:00", "Specialist Consultation"),
            ("priya.gunawardena@smarthospital.local", DayOfWeek.Tuesday, "09:00", "13:00", "Specialist Consultation"),
            ("priya.gunawardena@smarthospital.local", DayOfWeek.Thursday, "18:00", "20:00", "Telemedicine"),
            ("sanjeewa.bandara@smarthospital.local", DayOfWeek.Monday, "09:00", "12:00", "Pediatric Check-up"),
            ("sanjeewa.bandara@smarthospital.local", DayOfWeek.Tuesday, "09:00", "12:00", "Pediatric Check-up"),
            ("sanjeewa.bandara@smarthospital.local", DayOfWeek.Thursday, "14:00", "16:00", "Follow-up"),
            ("sanjeewa.bandara@smarthospital.local", DayOfWeek.Saturday, "08:00", "11:00", "Pediatric Check-up"),
            ("ishara.desilva@smarthospital.local", DayOfWeek.Wednesday, "09:00", "12:00", "Pediatric Check-up"),
            ("roshan.herath@smarthospital.local", DayOfWeek.Monday, "14:00", "18:00", "Specialist Consultation"),
            ("roshan.herath@smarthospital.local", DayOfWeek.Thursday, "08:00", "12:00", "Specialist Consultation"),
            ("nimali.karunaratne@smarthospital.local", DayOfWeek.Tuesday, "14:00", "17:00", "General Consultation"),
            ("nimali.karunaratne@smarthospital.local", DayOfWeek.Friday, "14:00", "17:00", "General Consultation"),
            ("nimali.karunaratne@smarthospital.local", DayOfWeek.Saturday, "09:00", "11:00", "Telemedicine"),
            ("chaminda.ekanayake@smarthospital.local", DayOfWeek.Wednesday, "08:00", "12:00", "Specialist Consultation"),
            ("chaminda.ekanayake@smarthospital.local", DayOfWeek.Friday, "13:00", "16:00", "Follow-up"),
            ("lakshan.senanayake@smarthospital.local", DayOfWeek.Monday, "08:00", "11:00", "General Consultation")
        };

        foreach (var (email, day, start, end, consultationType) in scheduleSeeds)
        {
            if (!newDoctors.TryGetValue(email, out var doctor) ||
                !consultationTypes.TryGetValue(consultationType, out var consultationTypeId))
            {
                continue;
            }

            context.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id,
                ConsultationTypeId = consultationTypeId,
                DayOfWeek = day,
                StartTime = TimeOnly.Parse(start),
                EndTime = TimeOnly.Parse(end),
                // Inactive doctors keep their slots, but switched off
                Status = doctor.Status == DoctorStatus.Inactive ? ScheduleStatus.Inactive : ScheduleStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Leave requests, dated relative to today
        var leaveSeeds = new[]
        {
            ("ishara.desilva@smarthospital.local", -3, 10, "Maternity leave.", LeaveStatus.Approved),
            ("ashan.wijesekara@smarthospital.local", 14, 18, "Attending international cardiology conference.", LeaveStatus.Approved),
            ("roshan.herath@smarthospital.local", 7, 8, "Personal matters.", LeaveStatus.Pending),
            ("nimali.karunaratne@smarthospital.local", 21, 25, "Annual family vacation.", LeaveStatus.Pending),
            ("chaminda.ekanayake@smarthospital.local", 2, 4, "Short break requested during peak clinic week.", LeaveStatus.Rejected),
            ("sanjeewa.bandara@smarthospital.local", -30, -28, "Medical leave.", LeaveStatus.Approved),
            ("priya.gunawardena@smarthospital.local", 10, 11, "Training workshop (rescheduled).", LeaveStatus.Cancelled)
        };

        foreach (var (email, startOffset, endOffset, reason, status) in leaveSeeds)
        {
            if (!newDoctors.TryGetValue(email, out var doctor))
            {
                continue;
            }

            context.DoctorLeaves.Add(new DoctorLeave
            {
                DoctorId = doctor.Id,
                StartDate = today.AddDays(startOffset),
                EndDate = today.AddDays(endOffset),
                Reason = reason,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Give the original seeded doctor (Nadia Perera) a schedule if she has none
        var nadia = await context.Doctors
            .FirstOrDefaultAsync(d => d.Email == "nadia.perera@smarthospital.local");

        if (nadia != null &&
            !await context.DoctorSchedules.AnyAsync(s => s.DoctorId == nadia.Id) &&
            consultationTypes.TryGetValue("General Consultation", out var generalId) &&
            consultationTypes.TryGetValue("Follow-up", out var followUpId))
        {
            var nadiaSlots = new[]
            {
                (DayOfWeek.Monday, "09:00", "13:00", generalId),
                (DayOfWeek.Wednesday, "09:00", "13:00", generalId),
                (DayOfWeek.Friday, "15:00", "17:00", followUpId)
            };

            foreach (var (day, start, end, consultationTypeId) in nadiaSlots)
            {
                context.DoctorSchedules.Add(new DoctorSchedule
                {
                    DoctorId = nadia.Id,
                    ConsultationTypeId = consultationTypeId,
                    DayOfWeek = day,
                    StartTime = TimeOnly.Parse(start),
                    EndTime = TimeOnly.Parse(end),
                    Status = ScheduleStatus.Active,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
