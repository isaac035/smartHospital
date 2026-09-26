using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Data;
using SmartHospital.Api.Services;
using SmartHospital.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Controllers
// --------------------------------------------------

builder.Services.AddControllers();


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

// Hospital Resource & Bed Management Services
builder.Services.AddScoped<IWardService, WardService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBedService, BedService>();
builder.Services.AddScoped<IAdmissionService, AdmissionService>();
builder.Services.AddScoped<IMedicalResourceService, MedicalResourceService>();
builder.Services.AddScoped<IResourceMaintenanceService, ResourceMaintenanceService>();
builder.Services.AddScoped<IOccupancyService, OccupancyService>();

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

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await DbSeeder.SeedAsync(dbContext);
}

app.Run();