using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Doctors;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;
using ScheduleDayOfWeek = SmartHospital.Api.Models.DayOfWeek;

namespace SmartHospital.Tests.Unit.Services;

public class DoctorServiceTests
{
    // ── Create ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesActiveDoctorWithLinkedLoginAccount()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);

        var result = await new DoctorService(context).CreateAsync(NewCreateRequest(dept.Id, "kamal@example.test"));

        Assert.Equal("Active", result.Doctor.Status);
        Assert.NotNull(result.Doctor.UserId);
        var user = await context.Users.SingleAsync(u => u.Id == result.Doctor.UserId);
        Assert.Equal(UserRole.Doctor, user.Role);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal("kamal@example.test", user.Email);
    }

    [Fact]
    public async Task CreateAsync_ReturnsTemporaryPasswordThatMatchesStoredHash()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);

        var result = await new DoctorService(context).CreateAsync(NewCreateRequest(dept.Id, "temp@example.test"));

        Assert.True(result.TemporaryPassword.Length >= 16);
        var user = await context.Users.SingleAsync();
        Assert.NotEqual(result.TemporaryPassword, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(result.TemporaryPassword, user.PasswordHash));
    }

    [Fact]
    public async Task CreateAsync_GeneratesDifferentTemporaryPasswordsPerDoctor()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var service = new DoctorService(context);

        var first = await service.CreateAsync(NewCreateRequest(dept.Id, "one@example.test"));
        var second = await service.CreateAsync(NewCreateRequest(dept.Id, "two@example.test"));

        Assert.NotEqual(first.TemporaryPassword, second.TemporaryPassword);
    }

    [Fact]
    public async Task CreateAsync_NormalisesEmailAndTrimsFields()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var request = NewCreateRequest(dept.Id, "  Kamal.Silva@Example.TEST ");
        request.FirstName = "  Kamal ";
        request.Specialization = " Cardiology ";

        var result = await new DoctorService(context).CreateAsync(request);

        Assert.Equal("kamal.silva@example.test", result.Doctor.Email);
        Assert.Equal("Kamal", result.Doctor.FirstName);
        Assert.Equal("Cardiology", result.Doctor.Specialization);
    }

    [Fact]
    public async Task CreateAsync_IncludesDepartmentName()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context, "Cardiology");

        var result = await new DoctorService(context).CreateAsync(NewCreateRequest(dept.Id, "dept@example.test"));

        Assert.Equal("Cardiology", result.Doctor.DepartmentName);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmailAlreadyRegisteredToAnyUser()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        context.Users.Add(new User
        {
            FirstName = "P", LastName = "Q", Email = "taken@example.test", PasswordHash = "x",
            Role = UserRole.Patient, Status = UserStatus.Active
        });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).CreateAsync(NewCreateRequest(dept.Id, "TAKEN@example.test")));

        Assert.Equal("This email is already registered to a user.", ex.Message);
        Assert.Empty(context.Doctors);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmailOfExistingDoctorProfile()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "legacy@example.test", userId: null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).CreateAsync(NewCreateRequest(dept.Id, "legacy@example.test")));

        Assert.Equal("A doctor profile with this email already exists.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownDepartmentAndCreatesNoUser()
    {
        await using var context = CreateContext();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).CreateAsync(NewCreateRequest(departmentId: 999, "x@example.test")));

        Assert.Equal("The specified department does not exist.", ex.Message);
        Assert.Empty(context.Users);
        Assert.Empty(context.Doctors);
    }

    // ── Read ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsDoctorOrNull()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "get@example.test");
        var service = new DoctorService(context);

        Assert.Equal("get@example.test", (await service.GetByIdAsync(doctor.Id))!.Email);
        Assert.Null(await service.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetAllAsync_ExcludesInactiveByDefault()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "active@example.test");
        SeedDoctor(context, dept, "inactive@example.test", status: DoctorStatus.Inactive);

        var doctors = await new DoctorService(context).GetAllAsync(new DoctorFilterRequest());

        Assert.Single(doctors);
        Assert.Equal("active@example.test", doctors[0].Email);
    }

    [Fact]
    public async Task GetAllAsync_IncludesInactiveWhenRequested()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "active@example.test");
        SeedDoctor(context, dept, "inactive@example.test", status: DoctorStatus.Inactive);

        var doctors = await new DoctorService(context).GetAllAsync(new DoctorFilterRequest { IncludeInactive = true });

        Assert.Equal(2, doctors.Count);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDepartment()
    {
        await using var context = CreateContext();
        var cardio = SeedDepartment(context, "Cardiology");
        var neuro = SeedDepartment(context, "Neurology");
        SeedDoctor(context, cardio, "c@example.test");
        SeedDoctor(context, neuro, "n@example.test");

        var doctors = await new DoctorService(context).GetAllAsync(new DoctorFilterRequest { DepartmentId = neuro.Id });

        Assert.Single(doctors);
        Assert.Equal("Neurology", doctors[0].DepartmentName);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByMinimumExperience()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "junior@example.test", experience: 2);
        SeedDoctor(context, dept, "senior@example.test", experience: 15);

        var doctors = await new DoctorService(context).GetAllAsync(new DoctorFilterRequest { MinExperience = 10 });

        Assert.Single(doctors);
        Assert.Equal("senior@example.test", doctors[0].Email);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByConsultationTypeOfActiveSchedules()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var withType = SeedDoctor(context, dept, "with@example.test");
        var inactiveSchedule = SeedDoctor(context, dept, "inactive-sched@example.test");
        SeedDoctor(context, dept, "none@example.test");
        SeedSchedule(context, withType, consultationTypeId: 5, ScheduleDayOfWeek.Monday);
        SeedSchedule(context, inactiveSchedule, consultationTypeId: 5, ScheduleDayOfWeek.Monday, ScheduleStatus.Inactive);

        var doctors = await new DoctorService(context).GetAllAsync(new DoctorFilterRequest { ConsultationTypeId = 5 });

        Assert.Single(doctors);
        Assert.Equal(withType.Id, doctors[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByLastName()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "z@example.test", lastName: "Wijesinghe");
        SeedDoctor(context, dept, "a@example.test", lastName: "Abeysekara");
        SeedDoctor(context, dept, "m@example.test", lastName: "Mendis");

        var doctors = await new DoctorService(context).GetAllAsync(new DoctorFilterRequest());

        Assert.Equal(new[] { "Abeysekara", "Mendis", "Wijesinghe" }, doctors.Select(d => d.LastName));
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesAllEditableFields()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context, "General");
        var newDept = SeedDepartment(context, "Paediatrics");
        var doctor = SeedDoctor(context, dept, "old@example.test");

        var result = await new DoctorService(context).UpdateAsync(doctor.Id, new UpdateDoctorRequest
        {
            FirstName = " Ama ",
            LastName = "Ratnayake",
            Email = " NEW@example.test ",
            PhoneNumber = "0710000000",
            DepartmentId = newDept.Id,
            Specialization = "Paediatrics",
            LicenseNumber = "SLMC-999",
            YearsOfExperience = 12,
            Bio = "Updated bio"
        });

        Assert.NotNull(result);
        Assert.Equal("Ama", result!.FirstName);
        Assert.Equal("new@example.test", result.Email);
        Assert.Equal("Paediatrics", result.DepartmentName);
        Assert.Equal(12, result.YearsOfExperience);
        Assert.Equal("SLMC-999", result.LicenseNumber);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullForUnknownDoctor()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);

        Assert.Null(await new DoctorService(context).UpdateAsync(404, NewUpdateRequest(dept.Id, "x@example.test")));
    }

    [Fact]
    public async Task UpdateAsync_RejectsEmailUsedByAnotherDoctor()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "first@example.test");
        var second = SeedDoctor(context, dept, "second@example.test");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).UpdateAsync(second.Id, NewUpdateRequest(dept.Id, "first@example.test")));

        Assert.Equal("A doctor with this email already exists.", ex.Message);
    }

    // Regression: BUG-004
    [Fact]
    public async Task UpdateAsync_RejectsEmailRegisteredToAnotherUserAccount()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "doc@example.test");
        context.Users.Add(new User
        {
            FirstName = "P", LastName = "Q", Email = "patient@example.test", PasswordHash = "x",
            Role = UserRole.Patient, Status = UserStatus.Active
        });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).UpdateAsync(doctor.Id, NewUpdateRequest(dept.Id, "patient@example.test")));

        Assert.Equal("This email is already registered to another user.", ex.Message);
    }

    // Regression: BUG-004
    [Fact]
    public async Task UpdateAsync_ChangingEmailAlsoUpdatesLinkedLoginEmail()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var created = await new DoctorService(context).CreateAsync(NewCreateRequest(dept.Id, "old@example.test"));

        await new DoctorService(context).UpdateAsync(created.Doctor.Id, NewUpdateRequest(dept.Id, "new@example.test"));

        var user = await context.Users.SingleAsync(u => u.Id == created.Doctor.UserId);
        Assert.Equal("new@example.test", user.Email);
    }

    [Fact]
    public async Task UpdateAsync_AllowsKeepingOwnEmail()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "same@example.test");

        var result = await new DoctorService(context).UpdateAsync(doctor.Id, NewUpdateRequest(dept.Id, "same@example.test"));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUnknownDepartment()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "d@example.test");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).UpdateAsync(doctor.Id, NewUpdateRequest(departmentId: 999, "d@example.test")));
    }

    [Fact]
    public async Task UpdateAsync_KeepsExistingUserLinkWhenRequestOmitsIt()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "linked@example.test", userId: 77);

        var result = await new DoctorService(context).UpdateAsync(doctor.Id, NewUpdateRequest(dept.Id, "linked@example.test"));

        Assert.Equal(77, result!.UserId);
    }

    // ── Activate / deactivate / delete ───────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_SetsInactiveAndHidesFromDefaultList()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "bye@example.test");
        var service = new DoctorService(context);

        Assert.True(await service.DeactivateAsync(doctor.Id));
        Assert.Equal("Inactive", (await service.GetByIdAsync(doctor.Id))!.Status);
        Assert.Empty(await service.GetAllAsync(new DoctorFilterRequest()));
    }

    [Fact]
    public async Task DeactivateAsync_ReturnsFalseForUnknownDoctor()
    {
        await using var context = CreateContext();

        Assert.False(await new DoctorService(context).DeactivateAsync(404));
    }

    [Fact]
    public async Task ActivateAsync_ReactivatesInactiveDoctor()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "back@example.test", status: DoctorStatus.Inactive);

        Assert.True(await new DoctorService(context).ActivateAsync(doctor.Id));
        Assert.Equal(DoctorStatus.Active, (await context.Doctors.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(DoctorStatus.Active)]
    [InlineData(DoctorStatus.OnLeave)]
    public async Task ActivateAsync_RejectsDoctorThatIsNotInactive(DoctorStatus status)
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "busy@example.test", status: status);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).ActivateAsync(doctor.Id));

        Assert.Equal("Only an inactive doctor can be activated.", ex.Message);
    }

    [Fact]
    public async Task ActivateAsync_ThrowsNotFoundForUnknownDoctor()
    {
        await using var context = CreateContext();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => new DoctorService(context).ActivateAsync(404));
    }

    [Fact]
    public async Task DeletePermanentlyAsync_RemovesProfileAndDisablesLinkedLogin()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var created = await new DoctorService(context).CreateAsync(NewCreateRequest(dept.Id, "del@example.test"));

        Assert.True(await new DoctorService(context).DeletePermanentlyAsync(created.Doctor.Id));

        Assert.Empty(context.Doctors);
        var user = await context.Users.SingleAsync();
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public async Task DeletePermanentlyAsync_ReturnsFalseForUnknownDoctor()
    {
        await using var context = CreateContext();

        Assert.False(await new DoctorService(context).DeletePermanentlyAsync(404));
    }

    // ── Login account for legacy profiles ────────────────────────────────

    [Fact]
    public async Task CreateAccountForExistingAsync_LinksNewDoctorLogin()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "legacy@example.test", userId: null);

        var result = await new DoctorService(context).CreateAccountForExistingAsync(doctor.Id);

        Assert.NotNull(result.Doctor.UserId);
        var user = await context.Users.SingleAsync();
        Assert.Equal(UserRole.Doctor, user.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify(result.TemporaryPassword, user.PasswordHash));
    }

    [Fact]
    public async Task CreateAccountForExistingAsync_RejectsProfileThatAlreadyHasLogin()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "has@example.test", userId: 5);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).CreateAccountForExistingAsync(doctor.Id));
    }

    [Fact]
    public async Task CreateAccountForExistingAsync_RejectsInactiveProfile()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "off@example.test", userId: null, status: DoctorStatus.Inactive);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DoctorService(context).CreateAccountForExistingAsync(doctor.Id));
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task CreateAccountForExistingAsync_ThrowsNotFoundForUnknownDoctor()
    {
        await using var context = CreateContext();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new DoctorService(context).CreateAccountForExistingAsync(404));
    }

    // ── Availability ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableAsync_ReturnsOnlyActiveDoctors()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "a@example.test");
        SeedDoctor(context, dept, "i@example.test", status: DoctorStatus.Inactive);
        SeedDoctor(context, dept, "l@example.test", status: DoctorStatus.OnLeave);

        var doctors = await new DoctorService(context).GetAvailableAsync(new AvailableDoctorFilterRequest());

        Assert.Single(doctors);
        Assert.Equal("a@example.test", doctors[0].Email);
    }

    [Fact]
    public async Task GetAvailableAsync_DateRequiresScheduleOnThatWeekday()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var monday = SeedDoctor(context, dept, "mon@example.test");
        var friday = SeedDoctor(context, dept, "fri@example.test");
        SeedSchedule(context, monday, 1, ScheduleDayOfWeek.Monday);
        SeedSchedule(context, friday, 1, ScheduleDayOfWeek.Friday);

        var doctors = await new DoctorService(context).GetAvailableAsync(
            new AvailableDoctorFilterRequest { Date = new DateOnly(2026, 10, 5) }); // Monday

        Assert.Single(doctors);
        Assert.Equal(monday.Id, doctors[0].Id);
    }

    [Fact]
    public async Task GetAvailableAsync_SundayMapsToSundaySchedule()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var sunday = SeedDoctor(context, dept, "sun@example.test");
        SeedSchedule(context, sunday, 1, ScheduleDayOfWeek.Sunday);

        var doctors = await new DoctorService(context).GetAvailableAsync(
            new AvailableDoctorFilterRequest { Date = new DateOnly(2026, 10, 4) }); // Sunday

        Assert.Single(doctors);
    }

    [Fact]
    public async Task GetAvailableAsync_ExcludesDoctorOnApprovedLeave()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "leave@example.test");
        SeedSchedule(context, doctor, 1, ScheduleDayOfWeek.Monday);
        SeedLeave(context, doctor, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 10), LeaveStatus.Approved);

        var doctors = await new DoctorService(context).GetAvailableAsync(
            new AvailableDoctorFilterRequest { Date = new DateOnly(2026, 10, 5) });

        Assert.Empty(doctors);
    }

    [Theory]
    [InlineData(LeaveStatus.Pending)]
    [InlineData(LeaveStatus.Rejected)]
    [InlineData(LeaveStatus.Cancelled)]
    public async Task GetAvailableAsync_IgnoresLeaveThatIsNotApproved(LeaveStatus status)
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "leave@example.test");
        SeedSchedule(context, doctor, 1, ScheduleDayOfWeek.Monday);
        SeedLeave(context, doctor, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 10), status);

        var doctors = await new DoctorService(context).GetAvailableAsync(
            new AvailableDoctorFilterRequest { Date = new DateOnly(2026, 10, 5) });

        Assert.Single(doctors);
    }

    [Fact]
    public async Task GetAvailableAsync_LeaveOutsideRequestedDateDoesNotExclude()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "leave@example.test");
        SeedSchedule(context, doctor, 1, ScheduleDayOfWeek.Monday);
        SeedLeave(context, doctor, new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 9), LeaveStatus.Approved);

        var doctors = await new DoctorService(context).GetAvailableAsync(
            new AvailableDoctorFilterRequest { Date = new DateOnly(2026, 10, 5) });

        Assert.Single(doctors);
    }

    // ── Doctor self-service profile ──────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_FindsDoctorLinkedToLogin()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        var doctor = SeedDoctor(context, dept, "me@example.test", userId: 50);
        var service = new DoctorService(context);

        Assert.Equal(doctor.Id, (await service.GetByUserIdAsync(50))!.Id);
        Assert.Null(await service.GetByUserIdAsync(51));
    }

    [Fact]
    public async Task UpdateMyProfileAsync_UpdatesOnlyPhoneAndBio()
    {
        await using var context = CreateContext();
        var dept = SeedDepartment(context);
        SeedDoctor(context, dept, "me@example.test", userId: 50);

        var result = await new DoctorService(context).UpdateMyProfileAsync(50, new UpdateMyDoctorProfileRequest
        {
            PhoneNumber = " 0751231234 ",
            Bio = " New bio "
        });

        Assert.Equal("0751231234", result!.PhoneNumber);
        Assert.Equal("New bio", result.Bio);
        Assert.Equal("me@example.test", result.Email);
        Assert.Equal("General Medicine", result.Specialization);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task UpdateMyProfileAsync_ReturnsNullWhenNoProfileLinked()
    {
        await using var context = CreateContext();

        var result = await new DoctorService(context).UpdateMyProfileAsync(
            99, new UpdateMyDoctorProfileRequest { PhoneNumber = "1", Bio = "" });

        Assert.Null(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Department SeedDepartment(AppDbContext context, string name = "General Medicine")
    {
        var department = new Department
        {
            Name = name,
            Description = "Test",
            Status = DepartmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Departments.Add(department);
        context.SaveChanges();
        return department;
    }

    private static Doctor SeedDoctor(
        AppDbContext context,
        Department department,
        string email,
        int? userId = null,
        DoctorStatus status = DoctorStatus.Active,
        int experience = 5,
        string lastName = "Silva")
    {
        var doctor = new Doctor
        {
            UserId = userId,
            DepartmentId = department.Id,
            FirstName = "Kamal",
            LastName = lastName,
            Email = email,
            PhoneNumber = "000",
            Specialization = "General Medicine",
            LicenseNumber = "SLMC-" + email,
            YearsOfExperience = experience,
            Bio = "",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Doctors.Add(doctor);
        context.SaveChanges();
        return doctor;
    }

    private static void SeedSchedule(
        AppDbContext context,
        Doctor doctor,
        int consultationTypeId,
        ScheduleDayOfWeek day,
        ScheduleStatus status = ScheduleStatus.Active)
    {
        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = doctor.Id,
            ConsultationTypeId = consultationTypeId,
            DayOfWeek = day,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private static void SeedLeave(AppDbContext context, Doctor doctor, DateOnly start, DateOnly end, LeaveStatus status)
    {
        context.DoctorLeaves.Add(new DoctorLeave
        {
            DoctorId = doctor.Id,
            StartDate = start,
            EndDate = end,
            Reason = "Test",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private static CreateDoctorRequest NewCreateRequest(int departmentId, string email) => new()
    {
        FirstName = "Kamal",
        LastName = "Silva",
        Email = email,
        PhoneNumber = "0771234567",
        DepartmentId = departmentId,
        Specialization = "General Medicine",
        LicenseNumber = "SLMC-12345",
        YearsOfExperience = 8,
        Bio = "Experienced physician"
    };

    private static UpdateDoctorRequest NewUpdateRequest(int departmentId, string email) => new()
    {
        FirstName = "Kamal",
        LastName = "Silva",
        Email = email,
        PhoneNumber = "0771234567",
        DepartmentId = departmentId,
        Specialization = "General Medicine",
        LicenseNumber = "SLMC-12345",
        YearsOfExperience = 8,
        Bio = ""
    };
}
