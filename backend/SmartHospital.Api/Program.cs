using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartHospital.Api.BackgroundServices;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.Middleware;
using SmartHospital.Api.Services;
using SmartHospital.Api.Services.Agent1;
using SmartHospital.Api.Services.Agent2;
using SmartHospital.Api.Services.Agent3;
using SmartHospital.Api.Services.Agent4;
using SmartHospital.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Controllers
// --------------------------------------------------

builder.Services
    .AddControllers(options =>
    {
        // Zone-less dates in query strings / routes are treated as UTC (see UtcDateTimeHandling.cs)
        options.ModelBinderProviders.Insert(0, new UtcDateTimeModelBinderProvider());
        // Readable, consistent messages for the built-in validation attributes (400 field errors).
        options.ModelMetadataDetailsProviders.Add(new SmartHospital.Api.Validation.FriendlyValidationMessages());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });
builder.Services.AddSignalR();


// --------------------------------------------------
// Database
// --------------------------------------------------

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    ));


// --------------------------------------------------
// JWT Configuration
// --------------------------------------------------

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt")
);

var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>();

if (jwtSettings == null ||
    string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "JWT configuration is missing."
    );
}


// --------------------------------------------------
// Authentication
// --------------------------------------------------

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/hospital"))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Key)
        ),

        ClockSkew = TimeSpan.Zero
    };
});


// --------------------------------------------------
// Authorization
// --------------------------------------------------

builder.Services.AddAuthorization();


// --------------------------------------------------
// Services
// --------------------------------------------------

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientMedicalProfileService, PatientMedicalProfileService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<IVitalSignService, VitalSignService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<ILabOrderService, LabOrderService>();
builder.Services.AddScoped<IMedicalHistoryService, MedicalHistoryService>();
builder.Services.AddScoped<IEmrAuditService, EmrAuditService>();

// Hospital Resource & Bed Management Services
builder.Services.AddScoped<IWardService, WardService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBedService, BedService>();
builder.Services.AddScoped<IAdmissionService, AdmissionService>();
builder.Services.AddScoped<IMedicalResourceService, MedicalResourceService>();
builder.Services.AddScoped<IResourceMaintenanceService, ResourceMaintenanceService>();
builder.Services.AddScoped<IOccupancyService, OccupancyService>();
builder.Services.AddHostedService<MaintenanceAutoStartBackgroundService>();

// Doctor & Clinical Schedule Management Services
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IConsultationTypeService, ConsultationTypeService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();

// Smart Appointment & Queue Management Services
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IQueueService, QueueService>();

// AI Agent 1 - Clinical Triage + Doctor Matching (internal Python service)
builder.Services.Configure<Agent1Settings>(builder.Configuration.GetSection(Agent1Settings.SectionName));
var agent1Settings = builder.Configuration.GetSection(Agent1Settings.SectionName).Get<Agent1Settings>() ?? new Agent1Settings();
builder.Services.AddHttpClient<IAgent1TriageService, Agent1TriageService>(client =>
{
    client.BaseAddress = new Uri(agent1Settings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(agent1Settings.TimeoutSeconds);
});

// AI Agent 2 - Appointment Optimization (internal Python service)
builder.Services.Configure<Agent2Settings>(builder.Configuration.GetSection(Agent2Settings.SectionName));
var agent2Settings = builder.Configuration.GetSection(Agent2Settings.SectionName).Get<Agent2Settings>() ?? new Agent2Settings();
builder.Services.AddHttpClient<IAgent2OptimizationService, Agent2OptimizationService>(client =>
{
    client.BaseAddress = new Uri(agent2Settings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(agent2Settings.TimeoutSeconds);
});

// AI Agent 3 - post-booking resource recommendations (ASP.NET retains database ownership).
builder.Services.Configure<Agent3Settings>(builder.Configuration.GetSection(Agent3Settings.SectionName));
var agent3Settings = builder.Configuration.GetSection(Agent3Settings.SectionName).Get<Agent3Settings>() ?? new Agent3Settings();
builder.Services.AddHttpClient<IAgent3ResourceAllocationService, Agent3ResourceAllocationService>(client =>
{
    client.BaseAddress = new Uri(agent3Settings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(agent3Settings.TimeoutSeconds);
});

// AI Agent 4 - medical report reasoning (API assembles persisted facts and owns report storage).
builder.Services.Configure<Agent4Settings>(builder.Configuration.GetSection(Agent4Settings.SectionName));
var agent4Settings = builder.Configuration.GetSection(Agent4Settings.SectionName).Get<Agent4Settings>() ?? new Agent4Settings();
builder.Services.AddHttpClient<IAgent4MedicalReportService, Agent4MedicalReportService>(client =>
{
    client.BaseAddress = new Uri(agent4Settings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(agent4Settings.TimeoutSeconds);
});

// Per-patient limit on the Agent 1 endpoint only (controls LLM cost and abuse).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\":\"You've reached the Smart Care limit for now. Please try again in a few minutes, or browse doctors directly.\"}",
            cancellationToken);
    };
    options.AddPolicy(SmartHospital.Api.Controllers.Agent1Controller.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = agent1Settings.RateLimitPermits,
                Window = TimeSpan.FromMinutes(agent1Settings.RateLimitWindowMinutes),
                QueueLimit = 0,
            }));
    options.AddPolicy(SmartHospital.Api.Controllers.Agent2Controller.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = agent2Settings.RateLimitPermits,
                Window = TimeSpan.FromMinutes(agent2Settings.RateLimitWindowMinutes),
                QueueLimit = 0,
            }));
});


// --------------------------------------------------
// CORS
// --------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// --------------------------------------------------
// Swagger
// --------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Smart Hospital API",
            Version = "v1",
            Description =
                "Smart Hospital Appointment & Medical Management System API"
        }
    );

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter JWT token as: Bearer {token}"
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type =
                            ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        }
    );
});


var app = builder.Build();


// --------------------------------------------------
// Middleware
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();
app.UseMiddleware<AppointmentManagerAuthorizationMiddleware>();
app.UseMiddleware<DoctorManagerAuthorizationMiddleware>();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<SmartHospital.Api.Hubs.HospitalHub>("/hubs/hospital");

using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();

    await DbSeeder.SeedAsync(dbContext);

    if (app.Environment.IsDevelopment())
    {
        await SampleDataSeeder.SeedAsync(dbContext);
    }
}

app.Run();
