using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Agent1;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Agent1;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class Agent1TriageServiceTests
{
    private const string InternalKey = "test-internal-key";
    private const int PatientId = 42;

    // User ids (what appointments book against)
    private const int NadiaUserId = 10;        // General Medicine, bookable
    private const int AshanUserId = 11;        // Cardiology, bookable
    private const int PriyaUserId = 12;        // Cardiology, user account suspended
    private const int RoshanUserId = 13;       // Cardiology, no active schedule
    private const int InactiveDocUserId = 14;  // Cardiology, doctor profile inactive

    // ── Test infrastructure ───────────────────────────────────────────────────

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request, body));
            return _respond(request);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object body) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
    };

    private static object AgentOk(string category, string priority, bool emergency = false, params int[] doctorIds) => new
    {
        patientId = PatientId,
        category,
        priority,
        reason = "Recommended specialty based on your description.",
        confidence = 0.8,
        possibleEmergency = emergency,
        usedDefaultCategory = false,
        recommendedDoctors = doctorIds.Select(id => new
        {
            doctorId = id,
            doctorProfileId = 999,
            name = "AI Invented Name",
            specialization = "made up",
            department = category,
        }),
        message = (string?)null,
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var now = DateTime.UtcNow;

        var general = new Department { Id = 1, Name = "General Medicine", Status = DepartmentStatus.Active, CreatedAt = now, UpdatedAt = now };
        var cardiology = new Department { Id = 2, Name = "Cardiology", Status = DepartmentStatus.Active, CreatedAt = now, UpdatedAt = now };
        var neurology = new Department { Id = 3, Name = "Neurology", Status = DepartmentStatus.Active, CreatedAt = now, UpdatedAt = now };
        var ent = new Department { Id = 4, Name = "ENT", Status = DepartmentStatus.Inactive, CreatedAt = now, UpdatedAt = now };
        db.Departments.AddRange(general, cardiology, neurology, ent);

        User DoctorUser(int id, UserStatus status = UserStatus.Active) => new()
        {
            Id = id, FirstName = "U", LastName = id.ToString(), Email = $"doc{id}@test.local",
            PasswordHash = "x", Role = UserRole.Doctor, Status = status, CreatedAt = now, UpdatedAt = now,
        };
        db.Users.AddRange(
            DoctorUser(NadiaUserId), DoctorUser(AshanUserId), DoctorUser(PriyaUserId, UserStatus.Suspended),
            DoctorUser(RoshanUserId), DoctorUser(InactiveDocUserId),
            new User { Id = PatientId, FirstName = "Pat", LastName = "Ient", Email = "p@test.local", PasswordHash = "x", Role = UserRole.Patient, Status = UserStatus.Active, CreatedAt = now, UpdatedAt = now });

        Doctor Doc(int id, int? userId, Department dept, string first, string last, DoctorStatus status = DoctorStatus.Active) => new()
        {
            Id = id, UserId = userId, DepartmentId = dept.Id, FirstName = first, LastName = last,
            Email = $"{first}@test.local", Specialization = dept.Name, Status = status, CreatedAt = now, UpdatedAt = now,
        };
        db.Doctors.AddRange(
            Doc(1, NadiaUserId, general, "Nadia", "Perera"),
            Doc(2, AshanUserId, cardiology, "Ashan", "Wijesekara"),
            Doc(3, PriyaUserId, cardiology, "Priya", "Gunawardena"),
            Doc(4, RoshanUserId, cardiology, "Roshan", "Herath"),
            Doc(5, InactiveDocUserId, cardiology, "Old", "Doctor", DoctorStatus.Inactive),
            Doc(6, null, neurology, "Chaminda", "Ekanayake"));

        db.ConsultationTypes.Add(new ConsultationType { Id = 1, Name = "General Consultation", DurationMinutes = 30, Status = ConsultationTypeStatus.Active, CreatedAt = now, UpdatedAt = now });
        DoctorSchedule Schedule(int id, int doctorId, ScheduleStatus status = ScheduleStatus.Active) => new()
        {
            Id = id, DoctorId = doctorId, ConsultationTypeId = 1, DayOfWeek = SmartHospital.Api.Models.DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(12, 0), Status = status, CreatedAt = now, UpdatedAt = now,
        };
        db.DoctorSchedules.AddRange(
            Schedule(1, 1), Schedule(2, 2), Schedule(3, 3), Schedule(4, 4, ScheduleStatus.Inactive), Schedule(5, 5), Schedule(6, 6));

        db.SaveChanges();
        return db;
    }

    private static (Agent1TriageService Service, StubHandler Handler) CreateService(
        AppDbContext db, Func<HttpRequestMessage, HttpResponseMessage> respond, string internalKey = InternalKey)
    {
        var handler = new StubHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://agent1.test/") };
        var settings = Options.Create(new Agent1Settings { InternalApiKey = internalKey });
        return (new Agent1TriageService(db, http, settings, NullLogger<Agent1TriageService>.Instance), handler);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sends_real_vocabulary_and_only_bookable_doctors_with_internal_key()
    {
        using var db = CreateContext();
        var (service, handler) = CreateService(db, _ => Json(HttpStatusCode.OK, AgentOk("Cardiology", "Urgent", false, AshanUserId)));

        await service.TriageAndMatchAsync(PatientId, "  chest discomfort on stairs  ");

        var (request, body) = Assert.Single(handler.Calls);
        Assert.Equal("http://agent1.test/v1/triage-doctor-match", request.RequestUri!.ToString());
        Assert.Equal(InternalKey, request.Headers.GetValues(Agent1TriageService.InternalKeyHeader).Single());

        var sent = JsonSerializer.Deserialize<Agent1ServiceRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(PatientId, sent.PatientId);
        Assert.Equal("chest discomfort on stairs", sent.Text);
        Assert.Equal(new[] { "Cardiology", "General Medicine", "Neurology" }, sent.Categories); // ENT is inactive
        Assert.Equal("General Medicine", sent.DefaultCategory);
        // Excluded: suspended user, no active schedule, inactive profile, no linked user.
        Assert.Equal(
            new[] { NadiaUserId, AshanUserId }.OrderBy(id => id),
            sent.CandidateDoctors.Select(d => d.DoctorId).OrderBy(id => id));
    }

    [Fact]
    public async Task Re_verification_drops_stale_invalid_and_out_of_category_ids_and_uses_database_values()
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => Json(HttpStatusCode.OK,
            AgentOk("Cardiology", "Urgent", false, AshanUserId, 999, PriyaUserId, RoshanUserId, InactiveDocUserId, NadiaUserId)));

        var result = await service.TriageAndMatchAsync(PatientId, "chest discomfort on stairs");

        Assert.Equal(TriageStatus.Ok, result.Status);
        var doctor = Assert.Single(result.RecommendedDoctors);
        Assert.Equal(AshanUserId, doctor.DoctorId);
        Assert.Equal(2, doctor.DoctorProfileId);              // from the database, not the AI's 999
        Assert.Equal("Ashan Wijesekara", doctor.Name);        // not "AI Invented Name"
        Assert.Equal("Cardiology", doctor.Department);
        Assert.Equal("Urgent", result.Priority);
        Assert.Equal(PatientId, result.PatientId);
    }

    [Fact]
    public async Task All_ids_failing_verification_returns_no_doctors_with_explanation()
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => Json(HttpStatusCode.OK, AgentOk("Cardiology", "Normal", false, 999)));

        var result = await service.TriageAndMatchAsync(PatientId, "palpitations");

        Assert.Equal(TriageStatus.NoDoctors, result.Status);
        Assert.Empty(result.RecommendedDoctors);
        Assert.Contains("No Cardiology doctors are available", result.Message);
    }

    [Fact]
    public async Task Category_with_zero_bookable_doctors_returns_no_doctors()
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => Json(HttpStatusCode.OK, AgentOk("Neurology", "Normal")));

        var result = await service.TriageAndMatchAsync(PatientId, "numb fingers");

        Assert.Equal(TriageStatus.NoDoctors, result.Status);
        Assert.Equal("Neurology", result.Category);
        Assert.Contains("browse all doctors", result.Message);
    }

    [Fact]
    public async Task Category_outside_active_list_falls_back_to_default()
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => Json(HttpStatusCode.OK, AgentOk("ENT", "Normal", false, NadiaUserId)));

        var result = await service.TriageAndMatchAsync(PatientId, "ear ache");

        Assert.Equal("General Medicine", result.Category);
        Assert.True(result.UsedDefaultCategory);
        Assert.Equal(NadiaUserId, Assert.Single(result.RecommendedDoctors).DoctorId);
    }

    [Theory]
    [InlineData("Critical", false, "Normal")]
    [InlineData("urgent", false, "Normal")]   // exact enum names only
    [InlineData("Normal", true, "Emergency")]
    public async Task Priority_is_constrained_to_the_existing_enum(string agentPriority, bool emergency, string expected)
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => Json(HttpStatusCode.OK, AgentOk("Cardiology", agentPriority, emergency, AshanUserId)));

        var result = await service.TriageAndMatchAsync(PatientId, "something is wrong");

        Assert.Equal(expected, result.Priority);
        Assert.Equal(emergency, result.PossibleEmergency);
        Assert.Equal(emergency, result.EmergencyNotice != null);
    }

    [Fact]
    public async Task Service_503_is_graceful_and_keeps_its_emergency_flag()
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => Json(HttpStatusCode.ServiceUnavailable, new { detail = "The AI service timed out.", possibleEmergency = true }));

        var result = await service.TriageAndMatchAsync(PatientId, "something unusual");

        Assert.Equal(TriageStatus.AiUnavailable, result.Status);
        Assert.True(result.PossibleEmergency);
        Assert.Equal(EmergencyRedFlags.Notice, result.EmergencyNotice);
        Assert.Empty(result.RecommendedDoctors);
        Assert.Contains("browse doctors", result.Message);
    }

    [Theory]
    [InlineData("my husband collapsed and is unresponsive", true)]
    [InlineData("a rash on my arm", false)]
    public async Task Unreachable_service_still_applies_red_flag_floor(string text, bool expectedEmergency)
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => throw new HttpRequestException("connection refused"));

        var result = await service.TriageAndMatchAsync(PatientId, text);

        Assert.Equal(TriageStatus.AiUnavailable, result.Status);
        Assert.Equal(expectedEmergency, result.PossibleEmergency);
    }

    [Fact]
    public async Task Timeout_is_graceful()
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => throw new TaskCanceledException("timeout"));

        var result = await service.TriageAndMatchAsync(PatientId, "back pain");

        Assert.Equal(TriageStatus.AiUnavailable, result.Status);
    }

    [Fact]
    public async Task Missing_internal_key_disables_agent_without_calling_it()
    {
        using var db = CreateContext();
        var (service, handler) = CreateService(db, _ => Json(HttpStatusCode.OK, AgentOk("Cardiology", "Normal")), internalKey: "");

        var result = await service.TriageAndMatchAsync(PatientId, "back pain");

        Assert.Equal(TriageStatus.AiUnavailable, result.Status);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task Unreadable_service_response_is_graceful()
    {
        using var db = CreateContext();
        var (service, _) = CreateService(db, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json"),
        });

        var result = await service.TriageAndMatchAsync(PatientId, "back pain");

        Assert.Equal(TriageStatus.AiUnavailable, result.Status);
    }
}
