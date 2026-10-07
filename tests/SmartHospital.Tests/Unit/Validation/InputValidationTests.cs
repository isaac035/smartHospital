using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.DTOs.Admissions;
using SmartHospital.Api.DTOs.Agent1;
using SmartHospital.Api.DTOs.Appointments;
using SmartHospital.Api.DTOs.Auth;
using SmartHospital.Api.DTOs.Beds;
using SmartHospital.Api.DTOs.Doctors;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.DTOs.Leaves;
using SmartHospital.Api.DTOs.Maintenance;
using SmartHospital.Api.DTOs.Schedules;
using SmartHospital.Api.DTOs.Users;
using SmartHospital.Api.DTOs.Wards;
using SmartHospital.Api.Models;
using Xunit;

namespace SmartHospital.Tests.Unit.Validation;

/// <summary>
/// Request-DTO validation across modules: what the API returns as a 400 with field errors
/// (ASP.NET runs these same DataAnnotations before a controller action executes).
/// </summary>
public class InputValidationTests
{
    // ── User management ──────────────────────────────────────────────────

    [Fact]
    public void Register_ValidRequestPasses() => Assert.Empty(Validate(ValidRegister()));

    [Theory]
    [InlineData("J0hn", "FirstName")]          // digits
    [InlineData("A", "FirstName")]             // too short
    [InlineData("   ", "FirstName")]           // whitespace only
    [InlineData("<b>Bob</b>", "FirstName")]    // markup
    public void Register_InvalidFirstNameFails(string firstName, string member)
    {
        var request = ValidRegister();
        request.FirstName = firstName;

        AssertFails(request, member);
    }

    [Theory]
    [InlineData("Mary-Jane")]
    [InlineData("O'Brien")]
    [InlineData("Dr. Silva")]
    [InlineData("Thilakarathne")]
    public void Register_RealisticNamesPass(string name)
    {
        var request = ValidRegister();
        request.LastName = name;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("0771234567")]
    [InlineData("+94 77 123 4567")]
    [InlineData("+1-555-1234")]
    [InlineData("(011) 2345678")]
    public void Register_ValidPhoneFormatsPass(string phone)
    {
        var request = ValidRegister();
        request.PhoneNumber = phone;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345")]               // too few digits
    [InlineData("1234567890123456")]    // too many digits
    [InlineData("077-ABC-1234")]
    public void Register_InvalidPhoneFails(string phone)
    {
        var request = ValidRegister();
        request.PhoneNumber = phone;

        AssertFails(request, nameof(RegisterRequest.PhoneNumber));
    }

    [Theory]
    [InlineData("password")]     // no upper, digit, special
    [InlineData("Password1")]    // no special
    [InlineData("Pa1!")]         // too short
    [InlineData("PASSWORD1!")]   // no lower
    public void Register_WeakPasswordFails(string password)
    {
        var request = ValidRegister();
        request.Password = password;

        AssertFails(request, nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Register_EmailLongerThanColumnFails()
    {
        var request = ValidRegister();
        request.Email = new string('a', 250) + "@x.com";

        AssertFails(request, nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Login_DoesNotEnforcePasswordStrength()
    {
        // Existing accounts may have passwords created before the strength rule.
        Assert.Empty(Validate(new LoginRequest { Email = "user@example.test", Password = "short" }));
    }

    [Fact]
    public void UpdateUser_InvalidPhoneFails() =>
        AssertFails(new UpdateUserRequest { FirstName = "Amaya", LastName = "Silva", PhoneNumber = "phone" },
            nameof(UpdateUserRequest.PhoneNumber));

    [Fact]
    public void CreateDoctorManager_WeakPasswordAndLongPhoneFail()
    {
        var errors = Validate(new CreateDoctorManagerRequest
        {
            FirstName = "Dilani", LastName = "Abeysekera", Email = "dm@example.test",
            Password = "secret1", PhoneNumber = "+94 (77) 123 4567 89 01"
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateDoctorManagerRequest.Password)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateDoctorManagerRequest.PhoneNumber)));
    }

    // ── Doctor management ────────────────────────────────────────────────

    [Fact]
    public void CreateDoctor_ValidRequestPasses() => Assert.Empty(Validate(ValidDoctor()));

    [Fact]
    public void CreateDoctor_MissingDepartmentSaysPleaseSelect()
    {
        var request = ValidDoctor();
        request.DepartmentId = 0;

        var error = AssertFails(request, nameof(CreateDoctorRequest.DepartmentId));
        Assert.Equal("Please select a department.", error.ErrorMessage);
    }

    [Fact]
    public void CreateDoctor_LicenseWithMarkupFails()
    {
        var request = ValidDoctor();
        request.LicenseNumber = "<script>alert(1)</script>";

        AssertFails(request, nameof(CreateDoctorRequest.LicenseNumber));
    }

    [Fact]
    public void CreateLeave_EndBeforeStartFailsOnEndDate()
    {
        var error = AssertFails(new CreateLeaveRequest
        {
            DoctorId = 1, StartDate = new DateOnly(2026, 10, 10), EndDate = new DateOnly(2026, 10, 9)
        }, nameof(CreateLeaveRequest.EndDate));

        Assert.Equal("End date must be on or after start date.", error.ErrorMessage);
    }

    [Fact]
    public void CreateLeave_SameDayPasses() =>
        Assert.Empty(Validate(new CreateLeaveRequest
        {
            DoctorId = 1, StartDate = new DateOnly(2026, 10, 10), EndDate = new DateOnly(2026, 10, 10)
        }));

    [Theory]
    [InlineData("Approved", true)]
    [InlineData("cancelled", true)]
    [InlineData("Bogus", false)]
    [InlineData("2", false)]
    public void UpdateLeave_StatusMustBeAKnownName(string status, bool valid)
    {
        var request = new UpdateLeaveRequest
        {
            StartDate = new DateOnly(2026, 10, 10), EndDate = new DateOnly(2026, 10, 11), Status = status
        };

        if (valid) Assert.Empty(Validate(request));
        else AssertFails(request, nameof(UpdateLeaveRequest.Status));
    }

    [Theory]
    [InlineData("Monday", "09:00", "12:00", null)]
    [InlineData("monday", "09:00", "12:00", null)]
    [InlineData("Funday", "09:00", "12:00", "DayOfWeek")]
    [InlineData("1", "09:00", "12:00", "DayOfWeek")]
    [InlineData("Monday", "12:00", "09:00", "EndTime")]
    [InlineData("Monday", "09:00", "09:00", "EndTime")]
    public void CreateSchedule_DayAndTimeRules(string day, string start, string end, string? failingMember)
    {
        var request = new CreateScheduleRequest
        {
            DoctorId = 1, ConsultationTypeId = 1, DayOfWeek = day,
            StartTime = TimeOnly.Parse(start), EndTime = TimeOnly.Parse(end)
        };

        if (failingMember is null) Assert.Empty(Validate(request));
        else AssertFails(request, failingMember);
    }

    // ── Appointments & Smart Care ────────────────────────────────────────

    [Fact]
    public void CreateAppointment_ValidRequestPasses() => Assert.Empty(Validate(ValidAppointment()));

    [Fact]
    public void CreateAppointment_UnknownPriorityFails()
    {
        var request = ValidAppointment();
        request.Priority = (AppointmentPriority)99;

        AssertFails(request, nameof(CreateAppointmentRequest.Priority));
    }

    [Fact]
    public void CreateAppointment_MissingPatientFails()
    {
        var request = ValidAppointment();
        request.PatientId = 0;

        AssertFails(request, nameof(CreateAppointmentRequest.PatientId));
    }

    [Fact]
    public void CreateAppointment_NotesWithScriptFail()
    {
        var request = ValidAppointment();
        request.Notes = "<script>alert('x')</script>";

        AssertFails(request, nameof(CreateAppointmentRequest.Notes));
    }

    [Fact]
    public void CancelAppointment_WhitespaceReasonFails() =>
        AssertFails(new CancelAppointmentRequest { Reason = "    " }, nameof(CancelAppointmentRequest.Reason));

    [Theory]
    [InlineData("fever", true)]
    [InlineData("I get chest discomfort when I climb stairs", true)]
    [InlineData("!!!???", false)]
    [InlineData("12345", false)]
    [InlineData("ab", false)]
    [InlineData("<img src=x onerror=alert(1)> headache", false)]
    public void SmartCareSymptoms(string symptoms, bool valid)
    {
        var request = new TriageDoctorMatchRequest { Symptoms = symptoms };

        if (valid) Assert.Empty(Validate(request));
        else AssertFails(request, nameof(TriageDoctorMatchRequest.Symptoms));
    }

    // ── Hospital resources ───────────────────────────────────────────────

    [Fact]
    public void CreateWard_ValidRequestPasses() => Assert.Empty(Validate(ValidWard()));

    [Fact]
    public void CreateWard_CapacityOutOfRangeAndUnknownTypeFail()
    {
        var request = ValidWard();
        request.Capacity = 0;
        request.Type = (WardType)99;

        var errors = Validate(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateWardRequest.Capacity)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateWardRequest.Type)));
    }

    [Fact]
    public void CreateBed_MissingRoomFails() =>
        AssertFails(new CreateBedRequest { RoomId = 0, BedNumber = "BED-01", Type = BedType.Standard },
            nameof(CreateBedRequest.RoomId));

    [Fact]
    public void CreateMaintenance_EndBeforeStartFailsOnScheduledEnd()
    {
        var start = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

        AssertFails(new CreateMaintenanceRequest
        {
            BedId = 1, Type = MaintenanceType.RoutineInspection, Description = "Check rails",
            ScheduledStart = start, ScheduledEnd = start.AddHours(-1)
        }, nameof(CreateMaintenanceRequest.ScheduledEnd));
    }

    [Fact]
    public void CreateAdmission_ReasonWithMarkupFails() =>
        AssertFails(new CreateAdmissionRequest
        {
            PatientId = 1, Priority = AdmissionPriority.Normal, ReasonForAdmission = "<iframe src=evil>"
        }, nameof(CreateAdmissionRequest.ReasonForAdmission));

    // ── Clinical care ────────────────────────────────────────────────────

    [Fact]
    public void PatientProfile_FutureDateOfBirthFails() =>
        AssertFails(new UpsertPatientMedicalProfileRequest { DateOfBirth = DateTime.UtcNow.AddDays(10) },
            nameof(UpsertPatientMedicalProfileRequest.DateOfBirth));

    [Fact]
    public void PatientProfile_DateOfBirthBefore1900Fails() =>
        AssertFails(new UpsertPatientMedicalProfileRequest { DateOfBirth = new DateTime(1890, 1, 1) },
            nameof(UpsertPatientMedicalProfileRequest.DateOfBirth));

    [Fact]
    public void PatientProfile_ExistingStyleEmergencyContactStillPasses()
    {
        // Values shaped like the UI placeholders must keep saving.
        Assert.Empty(Validate(new UpsertPatientMedicalProfileRequest
        {
            DateOfBirth = new DateTime(1990, 5, 20),
            EmergencyContactName = "Jane Doe (Spouse)",
            EmergencyContactPhone = "+1-555-1234"
        }));
    }

    [Fact]
    public void PatientProfile_InvalidEmergencyPhoneFails() =>
        AssertFails(new UpsertPatientMedicalProfileRequest { EmergencyContactPhone = "12" },
            nameof(UpsertPatientMedicalProfileRequest.EmergencyContactPhone));

    [Fact]
    public void Diagnosis_ClinicalComparisonTextIsNotTreatedAsHtml()
    {
        // "<" followed by a number is normal clinical shorthand, not markup.
        Assert.Empty(Validate(new AddDiagnosisRequest
        {
            Description = "Hypertension", Notes = "Target BP < 130/80, HbA1c <7%",
            DiagnosedAt = DateTime.UtcNow.AddDays(-1)
        }));
    }

    [Fact]
    public void Diagnosis_DescriptionWithMarkupFails() =>
        AssertFails(new AddDiagnosisRequest { Description = "<b>Flu</b>", DiagnosedAt = DateTime.UtcNow.AddDays(-1) },
            nameof(AddDiagnosisRequest.Description));

    [Fact]
    public void Vitals_NotesWithMarkupFail() =>
        AssertFails(new RecordVitalSignRequest { PatientId = 1, Notes = "<script>x</script>" },
            nameof(RecordVitalSignRequest.Notes));

    // ── Helpers ──────────────────────────────────────────────────────────

    private static RegisterRequest ValidRegister() => new()
    {
        FirstName = "Amaya",
        LastName = "Silva",
        Email = "amaya@example.test",
        Password = "Patient123!",
        PhoneNumber = "0715550101"
    };

    private static CreateDoctorRequest ValidDoctor() => new()
    {
        FirstName = "Kamal",
        LastName = "Silva",
        Email = "kamal@example.test",
        PhoneNumber = "0771234567",
        DepartmentId = 1,
        Specialization = "Cardiology",
        LicenseNumber = "SLMC-12345",
        YearsOfExperience = 8,
        Bio = "Consultant cardiologist"
    };

    private static CreateAppointmentRequest ValidAppointment() => new()
    {
        PatientId = 1,
        DoctorId = 2,
        AppointmentType = AppointmentType.General,
        ScheduledStart = DateTime.UtcNow.AddDays(1),
        EstimatedDurationMinutes = 30,
        Priority = AppointmentPriority.Normal,
        Notes = "Follow-up for BP < 140/90"
    };

    private static CreateWardRequest ValidWard() => new()
    {
        Name = "Cardiology Ward",
        Code = "WD1",
        Floor = "Ground Floor",
        Capacity = 20,
        Type = WardType.General
    };

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static ValidationResult AssertFails(object model, string member)
    {
        var errors = Validate(model);
        var match = errors.FirstOrDefault(e => e.MemberNames.Contains(member));
        Assert.True(match is not null,
            $"Expected a validation error on {member}, got: {string.Join(" | ", errors.Select(e => $"{string.Join(",", e.MemberNames)}: {e.ErrorMessage}"))}");
        return match!;
    }
}
