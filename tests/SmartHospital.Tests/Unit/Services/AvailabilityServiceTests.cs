using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;
using ScheduleDayOfWeek = SmartHospital.Api.Models.DayOfWeek;

namespace SmartHospital.Tests.Unit.Services;

public class AvailabilityServiceTests
{
    [Fact]
    public async Task GetAvailableSlotsAsync_ProfileIdScopesToOneDoctorsSchedule()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);

        var date = DateTime.UtcNow.Date.AddDays(3);
        var user = new User
        {
            Id = 5,
            FirstName = "Nadia",
            LastName = "Perera",
            Email = "doctor@example.test",
            PasswordHash = "test",
            PhoneNumber = "000",
            Role = UserRole.Doctor,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var cardiology = new Department
        {
            Id = 1,
            Name = "Cardiology",
            Description = "Test department",
            Status = DepartmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var orthopedics = new Department
        {
            Id = 2,
            Name = "Orthopedics",
            Description = "Test department",
            Status = DepartmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var ashan = CreateDoctor(11, 5, cardiology, "Ashan", "Wijesekara");
        var roshan = CreateDoctor(12, 5, orthopedics, "Roshan", "Herath");
        var consultationType = new ConsultationType
        {
            Id = 1,
            Name = "General Consultation",
            DurationMinutes = 30,
            Description = "Test consultation",
            Status = ConsultationTypeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var dayOfWeek = ToScheduleDayOfWeek(date.DayOfWeek);

        context.Users.Add(user);
        context.Departments.AddRange(cardiology, orthopedics);
        context.Doctors.AddRange(ashan, roshan);
        context.ConsultationTypes.Add(consultationType);
        context.DoctorSchedules.AddRange(
            CreateSchedule(101, ashan, consultationType, dayOfWeek, "08:00", "12:00"),
            CreateSchedule(102, roshan, consultationType, dayOfWeek, "14:00", "18:00"));
        await context.SaveChangesAsync();

        var service = new AvailabilityService(context);
        var selectedDoctorSlots = await service.GetAvailableSlotsAsync(
            doctorId: 5,
            departmentId: null,
            date: date,
            doctorProfileId: 11);

        Assert.Equal(8, selectedDoctorSlots.Count);
        Assert.All(selectedDoctorSlots, slot =>
        {
            Assert.Equal(11, slot.DoctorProfileId);
            Assert.Equal("Ashan Wijesekara", slot.DoctorName);
            Assert.InRange(slot.SlotStart.TimeOfDay, TimeSpan.FromHours(2.5), TimeSpan.FromHours(6));
        });
        Assert.Equal(TimeSpan.FromHours(2.5), selectedDoctorSlots.First().SlotStart.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(6), selectedDoctorSlots.Last().SlotStart.TimeOfDay);

        var unconfiguredDoctorSlots = await service.GetAvailableSlotsAsync(
            doctorId: 5,
            departmentId: null,
            date: date,
            doctorProfileId: 999);

        Assert.Empty(unconfiguredDoctorSlots);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_DateSpecificSessionOverridesWeeklySession()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);

        var date = DateTime.UtcNow.Date.AddDays(3);
        var user = new User
        {
            Id = 5, FirstName = "Priya", LastName = "Gunawardena", Email = "priya@example.test",
            PasswordHash = "test", PhoneNumber = "000", Role = UserRole.Doctor,
            Status = UserStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var department = new Department
        {
            Id = 1, Name = "General Medicine", Description = "Test", Status = DepartmentStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var doctor = CreateDoctor(11, user.Id, department, "Priya", "Gunawardena");
        var consultationType = new ConsultationType
        {
            Id = 1, Name = "General Consultation", DurationMinutes = 30, Description = "Test",
            Status = ConsultationTypeStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var dayOfWeek = ToScheduleDayOfWeek(date.DayOfWeek);

        context.Users.Add(user);
        context.Departments.Add(department);
        context.Doctors.Add(doctor);
        context.ConsultationTypes.Add(consultationType);
        context.DoctorSchedules.AddRange(
            CreateSchedule(101, doctor, consultationType, dayOfWeek, "09:00", "13:00"),
            new DoctorSchedule
            {
                Id = 102, DoctorId = doctor.Id, Doctor = doctor,
                ConsultationTypeId = consultationType.Id, ConsultationType = consultationType,
                DayOfWeek = dayOfWeek, SpecificDate = DateOnly.FromDateTime(date),
                StartTime = TimeOnly.Parse("08:00"), EndTime = TimeOnly.Parse("12:00"),
                SlotDurationMinutes = 30, Status = ScheduleStatus.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var slots = await new AvailabilityService(context).GetAvailableSlotsAsync(
            doctorId: user.Id, departmentId: null, date: date, doctorProfileId: doctor.Id);

        Assert.Equal(8, slots.Count);
        Assert.Equal(TimeSpan.FromHours(2.5), slots.First().SlotStart.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(6), slots.Last().SlotStart.TimeOfDay);
    }

    private static Doctor CreateDoctor(
        int id,
        int userId,
        Department department,
        string firstName,
        string lastName) => new()
    {
        Id = id,
        UserId = userId,
        DepartmentId = department.Id,
        Department = department,
        FirstName = firstName,
        LastName = lastName,
        Email = $"{firstName.ToLowerInvariant()}@example.test",
        PhoneNumber = "000",
        Specialization = "Test specialization",
        LicenseNumber = $"TEST-{id}",
        Status = DoctorStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static DoctorSchedule CreateSchedule(
        int id,
        Doctor doctor,
        ConsultationType consultationType,
        ScheduleDayOfWeek dayOfWeek,
        string start,
        string end) => new()
    {
        Id = id,
        DoctorId = doctor.Id,
        Doctor = doctor,
        ConsultationTypeId = consultationType.Id,
        ConsultationType = consultationType,
        DayOfWeek = dayOfWeek,
        StartTime = TimeOnly.Parse(start),
        EndTime = TimeOnly.Parse(end),
        SlotDurationMinutes = 30,
        Status = ScheduleStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static ScheduleDayOfWeek ToScheduleDayOfWeek(System.DayOfWeek day) => day switch
    {
        System.DayOfWeek.Monday => ScheduleDayOfWeek.Monday,
        System.DayOfWeek.Tuesday => ScheduleDayOfWeek.Tuesday,
        System.DayOfWeek.Wednesday => ScheduleDayOfWeek.Wednesday,
        System.DayOfWeek.Thursday => ScheduleDayOfWeek.Thursday,
        System.DayOfWeek.Friday => ScheduleDayOfWeek.Friday,
        System.DayOfWeek.Saturday => ScheduleDayOfWeek.Saturday,
        _ => ScheduleDayOfWeek.Sunday
    };
}
