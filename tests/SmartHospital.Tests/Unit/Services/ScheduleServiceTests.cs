using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Schedules;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;
using ScheduleDayOfWeek = SmartHospital.Api.Models.DayOfWeek;

namespace SmartHospital.Tests.Unit.Services;

public class ScheduleServiceTests
{
    [Fact]
    public async Task CreateAsync_AllowsDateSpecificSessionThatOverlapsWeeklyTemplate()
    {
        await using var context = CreateContext();
        var (doctor, consultationType) = SeedDoctorAndConsultationType(context);
        var date = new DateOnly(2026, 9, 29); // Tuesday
        context.DoctorSchedules.Add(CreateSchedule(
            doctor, consultationType, ScheduleDayOfWeek.Tuesday, null, "08:00", "13:00"));
        await context.SaveChangesAsync();

        var service = new ScheduleService(context);
        var created = await service.CreateAsync(new CreateScheduleRequest
        {
            DoctorId = doctor.Id,
            ConsultationTypeId = consultationType.Id,
            DayOfWeek = "Tuesday",
            SpecificDate = date,
            StartTime = TimeOnly.Parse("08:00"),
            EndTime = TimeOnly.Parse("12:00")
        });

        Assert.Equal(date, created.SpecificDate);
        Assert.Equal("08:00", created.StartTime.ToString("HH:mm"));
        Assert.Equal("12:00", created.EndTime.ToString("HH:mm"));
    }

    [Fact]
    public async Task CreateAsync_AllowsDateSpecificSessionWithoutRecurringAvailability()
    {
        await using var context = CreateContext();
        var (doctor, consultationType) = SeedDoctorAndConsultationType(context);
        var date = new DateOnly(2026, 9, 28); // Monday

        var created = await new ScheduleService(context).CreateAsync(new CreateScheduleRequest
        {
            DoctorId = doctor.Id,
            ConsultationTypeId = consultationType.Id,
            DayOfWeek = "Monday",
            SpecificDate = date,
            StartTime = TimeOnly.Parse("00:00"),
            EndTime = TimeOnly.Parse("02:00")
        });

        Assert.Equal(date, created.SpecificDate);
        Assert.Equal("00:00", created.StartTime.ToString("HH:mm"));
        Assert.Equal("02:00", created.EndTime.ToString("HH:mm"));
    }

    [Fact]
    public async Task CreateAsync_RejectsOverlappingSecondSessionForSameDate()
    {
        await using var context = CreateContext();
        var (doctor, consultationType) = SeedDoctorAndConsultationType(context);
        var date = new DateOnly(2026, 9, 29);
        context.DoctorSchedules.Add(CreateSchedule(
            doctor, consultationType, ScheduleDayOfWeek.Tuesday, date, "08:00", "12:00"));
        await context.SaveChangesAsync();

        var service = new ScheduleService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new CreateScheduleRequest
        {
            DoctorId = doctor.Id,
            ConsultationTypeId = consultationType.Id,
            DayOfWeek = "Tuesday",
            SpecificDate = date,
            StartTime = TimeOnly.Parse("11:00"),
            EndTime = TimeOnly.Parse("13:00")
        }));
    }

    [Fact]
    public async Task CreateAsync_AllowsSamePeriodOnAnotherDate()
    {
        await using var context = CreateContext();
        var (doctor, consultationType) = SeedDoctorAndConsultationType(context);
        context.DoctorSchedules.Add(CreateSchedule(
            doctor, consultationType, ScheduleDayOfWeek.Tuesday, null, "08:00", "17:00"));
        context.DoctorSchedules.Add(CreateSchedule(
            doctor, consultationType, ScheduleDayOfWeek.Tuesday,
            new DateOnly(2026, 9, 29), "08:00", "12:00"));
        await context.SaveChangesAsync();

        var created = await new ScheduleService(context).CreateAsync(new CreateScheduleRequest
        {
            DoctorId = doctor.Id,
            ConsultationTypeId = consultationType.Id,
            DayOfWeek = "Tuesday",
            SpecificDate = new DateOnly(2026, 10, 6),
            StartTime = TimeOnly.Parse("08:00"),
            EndTime = TimeOnly.Parse("12:00")
        });

        Assert.Equal(new DateOnly(2026, 10, 6), created.SpecificDate);
    }

    [Fact]
    public async Task AddSlotsAsync_ExtendsOnlyTheSelectedDateForARecurringSchedule()
    {
        await using var context = CreateContext();
        var (doctor, consultationType) = SeedDoctorAndConsultationType(context);
        var recurring = CreateSchedule(doctor, consultationType, ScheduleDayOfWeek.Tuesday, null, "08:00", "10:00");
        var otherSession = CreateSchedule(doctor, consultationType, ScheduleDayOfWeek.Tuesday, null, "11:00", "12:00");
        context.DoctorSchedules.AddRange(recurring, otherSession);
        await context.SaveChangesAsync();
        var date = new DateOnly(2026, 10, 6);

        var extended = await new ScheduleService(context).AddSlotsAsync(recurring.Id, date, 2);

        Assert.NotNull(extended);
        Assert.Equal(date, extended!.SpecificDate);
        Assert.Equal("11:00", extended.EndTime.ToString("HH:mm"));
        Assert.Equal("10:00", (await context.DoctorSchedules.FindAsync(recurring.Id))!.EndTime.ToString("HH:mm"));
        var dateSchedules = await context.DoctorSchedules.Where(s => s.SpecificDate == date).OrderBy(s => s.StartTime).ToListAsync();
        Assert.Equal(2, dateSchedules.Count);
        Assert.Equal(TimeOnly.Parse("11:00"), dateSchedules[1].StartTime);
        Assert.Equal(TimeOnly.Parse("12:00"), dateSchedules[1].EndTime);
    }

    [Fact]
    public async Task AddSlotsAsync_ExtendsAnExistingDateSpecificSchedule()
    {
        await using var context = CreateContext();
        var (doctor, consultationType) = SeedDoctorAndConsultationType(context);
        var date = new DateOnly(2026, 10, 6);
        var schedule = CreateSchedule(doctor, consultationType, ScheduleDayOfWeek.Tuesday, date, "08:00", "10:00");
        context.DoctorSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var extended = await new ScheduleService(context).AddSlotsAsync(schedule.Id, date, 3);

        Assert.NotNull(extended);
        Assert.Equal("11:30", extended!.EndTime.ToString("HH:mm"));
        Assert.Equal(1, await context.DoctorSchedules.CountAsync(s => s.SpecificDate == date));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (Doctor Doctor, ConsultationType ConsultationType) SeedDoctorAndConsultationType(
        AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var department = new Department
        {
            Id = 1,
            Name = "General Medicine",
            Description = "Test",
            Status = DepartmentStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var doctor = new Doctor
        {
            Id = 10,
            DepartmentId = department.Id,
            Department = department,
            FirstName = "Priya",
            LastName = "Gunawardena",
            Email = "priya@example.test",
            PhoneNumber = "000",
            Specialization = "General Medicine",
            LicenseNumber = "TEST-10",
            Status = DoctorStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var consultationType = new ConsultationType
        {
            Id = 20,
            Name = "General Consultation",
            DurationMinutes = 30,
            Description = "Test",
            Status = ConsultationTypeStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Departments.Add(department);
        context.Doctors.Add(doctor);
        context.ConsultationTypes.Add(consultationType);
        context.SaveChanges();
        return (doctor, consultationType);
    }

    private static DoctorSchedule CreateSchedule(
        Doctor doctor,
        ConsultationType consultationType,
        ScheduleDayOfWeek day,
        DateOnly? specificDate,
        string start,
        string end) => new()
    {
        DoctorId = doctor.Id,
        Doctor = doctor,
        ConsultationTypeId = consultationType.Id,
        ConsultationType = consultationType,
        DayOfWeek = day,
        SpecificDate = specificDate,
        StartTime = TimeOnly.Parse(start),
        EndTime = TimeOnly.Parse(end),
        SlotDurationMinutes = consultationType.DurationMinutes,
        Status = ScheduleStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
