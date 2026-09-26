using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class MedicalRecordSearchFilterTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private async Task SeedDataAsync(AppDbContext context)
    {
        var patient1 = new User
        {
            Id = 10,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var patient2 = new User
        {
            Id = 20,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };

        var doctor1 = new User
        {
            Id = 30,
            FirstName = "Gregory",
            LastName = "House",
            Email = "dr.house@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        var doctor2 = new User
        {
            Id = 40,
            FirstName = "James",
            LastName = "Wilson",
            Email = "dr.wilson@hospital.com",
            PasswordHash = "hash",
            Role = UserRole.Doctor,
            Status = UserStatus.Active
        };

        context.Users.AddRange(patient1, patient2, doctor1, doctor2);

        var r1 = new MedicalRecord
        {
            Id = 1,
            RecordNumber = "REC-2026-001",
            PatientId = 10,
            DoctorId = 30,
            VisitDate = new DateTime(2026, 1, 10, 9, 30, 0, DateTimeKind.Utc),
            ChiefComplaint = "High blood pressure check",
            Diagnosis = "Hypertension Stage 1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var r2 = new MedicalRecord
        {
            Id = 2,
            RecordNumber = "REC-2026-002",
            PatientId = 10,
            DoctorId = 40,
            VisitDate = new DateTime(2026, 2, 15, 14, 0, 0, DateTimeKind.Utc),
            ChiefComplaint = "Routine endocrine checkup",
            Diagnosis = "Type 2 Diabetes Mellitus",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var r3 = new MedicalRecord
        {
            Id = 3,
            RecordNumber = "REC-2026-003",
            PatientId = 20,
            DoctorId = 30,
            VisitDate = new DateTime(2026, 3, 20, 11, 15, 0, DateTimeKind.Utc),
            ChiefComplaint = "Cough and mild fever",
            Diagnosis = "Acute Bronchitis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Diagnoses = new List<ClinicalDiagnosis>
            {
                new()
                {
                    Id = 101,
                    Description = "Viral Bronchitis",
                    Code = "J20.9",
                    Type = DiagnosisType.Secondary,
                    Status = DiagnosisStatus.Active,
                    Severity = DiagnosisSeverity.Moderate,
                    PatientId = 20,
                    DoctorId = 30,
                    DiagnosedAt = DateTime.UtcNow
                }
            }
        };

        var r4 = new MedicalRecord
        {
            Id = 4,
            RecordNumber = "REC-2026-004",
            PatientId = 20,
            DoctorId = 40,
            VisitDate = new DateTime(2026, 4, 25, 16, 45, 0, DateTimeKind.Utc),
            ChiefComplaint = "Chest tightness and palpitations",
            Diagnosis = "Hypertension Stage 2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var r5 = new MedicalRecord
        {
            Id = 5,
            RecordNumber = "REC-2026-005",
            PatientId = 10,
            DoctorId = 30,
            VisitDate = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            ChiefComplaint = "Nasal congestion",
            Diagnosis = "Seasonal Allergic Rhinitis",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.MedicalRecords.AddRange(r1, r2, r3, r4, r5);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAsync_FilterByPatientId_ReturnsOnlyMatchingPatientRecords()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter { PatientId = 10 };
        var result = await service.SearchAsync(filter);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.All(result.Items, r => Assert.Equal(10, r.PatientId));
    }

    [Fact]
    public async Task SearchAsync_FilterByPatientSearch_MatchesPatientNameSubstring()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter { PatientSearch = "smith" };
        var result = await service.SearchAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, r => Assert.Equal(20, r.PatientId));
        Assert.All(result.Items, r => Assert.Contains("Jane Smith", r.PatientName));
    }

    [Fact]
    public async Task SearchAsync_FilterByDoctorId_ReturnsOnlyMatchingDoctorRecords()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter { DoctorId = 30 };
        var result = await service.SearchAsync(filter);

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, r => Assert.Equal(30, r.DoctorId));
    }

    [Fact]
    public async Task SearchAsync_FilterByDoctorSearch_MatchesDoctorNameSubstring()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter { DoctorSearch = "wilson" };
        var result = await service.SearchAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, r => Assert.Equal(40, r.DoctorId));
        Assert.All(result.Items, r => Assert.Contains("Wilson", r.DoctorName));
    }

    [Fact]
    public async Task SearchAsync_FilterByRecordNumber_MatchesExactOrSubstring()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter { RecordNumber = "003" };
        var result = await service.SearchAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("REC-2026-003", result.Items[0].RecordNumber);
    }

    [Fact]
    public async Task SearchAsync_FilterByDiagnosis_MatchesPrimaryOrStructuredDiagnosis()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        // Matches primary diagnosis
        var filterPrimary = new MedicalRecordQueryFilter { Diagnosis = "hypertension" };
        var resultPrimary = await service.SearchAsync(filterPrimary);
        Assert.Equal(2, resultPrimary.TotalCount);

        // Matches structured diagnosis code/description
        var filterStructured = new MedicalRecordQueryFilter { Diagnosis = "J20.9" };
        var resultStructured = await service.SearchAsync(filterStructured);
        Assert.Single(resultStructured.Items);
        Assert.Equal("REC-2026-003", resultStructured.Items[0].RecordNumber);
    }

    [Fact]
    public async Task SearchAsync_FilterByVisitDate_MatchesExactCalendarDay()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        // VisitDate in DB is 2026-02-15 14:00:00 UTC. Filter with midnight date.
        var filter = new MedicalRecordQueryFilter { VisitDate = new DateTime(2026, 2, 15) };
        var result = await service.SearchAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("REC-2026-002", result.Items[0].RecordNumber);
    }

    [Fact]
    public async Task SearchAsync_FilterByDateRange_ReturnsRecordsBetweenStartAndEndDate()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter
        {
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 4, 1)
        };
        var result = await service.SearchAsync(filter);

        // Records from Feb and Mar (REC-2, REC-3)
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, r => r.RecordNumber == "REC-2026-002");
        Assert.Contains(result.Items, r => r.RecordNumber == "REC-2026-003");
    }

    [Fact]
    public async Task SearchAsync_FilterBySearchTerm_MatchesAcrossMultipleFields()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        // Matches chief complaint "palpitations"
        var filterComplaint = new MedicalRecordQueryFilter { SearchTerm = "palpitations" };
        var resultComplaint = await service.SearchAsync(filterComplaint);
        Assert.Single(resultComplaint.Items);
        Assert.Equal("REC-2026-004", resultComplaint.Items[0].RecordNumber);

        // Matches record number
        var filterRec = new MedicalRecordQueryFilter { SearchTerm = "REC-2026-005" };
        var resultRec = await service.SearchAsync(filterRec);
        Assert.Single(resultRec.Items);
        Assert.Equal("REC-2026-005", resultRec.Items[0].RecordNumber);
    }

    [Fact]
    public async Task SearchAsync_CombinedFilters_NarrowsResultsPrecisely()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        // Patient 10 AND Doctor 30 (r1 and r5) AND Diagnosis "Hypertension" (r1 only)
        var filter = new MedicalRecordQueryFilter
        {
            PatientId = 10,
            DoctorId = 30,
            Diagnosis = "Hypertension"
        };
        var result = await service.SearchAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("REC-2026-001", result.Items[0].RecordNumber);
    }

    [Fact]
    public async Task SearchAsync_NoMatchingRecords_ReturnsEmptyItemsAndZeroTotalCount()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter { Diagnosis = "NonExistentConditionXYZ" };
        var result = await service.SearchAsync(filter);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task SearchAsync_Pagination_FirstPage_ReturnsCorrectSubsetAndMetadata()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter
        {
            Page = 1,
            PageSize = 2
        };
        var result = await service.SearchAsync(filter);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        // Ordered newest first (May 30, April 25)
        Assert.Equal("REC-2026-005", result.Items[0].RecordNumber);
        Assert.Equal("REC-2026-004", result.Items[1].RecordNumber);
    }

    [Fact]
    public async Task SearchAsync_Pagination_SecondPage_ReturnsNextSubset()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter
        {
            Page = 2,
            PageSize = 2
        };
        var result = await service.SearchAsync(filter);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
        // 3rd and 4th newest records (March 20, Feb 15)
        Assert.Equal("REC-2026-003", result.Items[0].RecordNumber);
        Assert.Equal("REC-2026-002", result.Items[1].RecordNumber);
    }

    [Fact]
    public async Task SearchAsync_Pagination_PageBeyondTotal_ReturnsEmptyListWithCorrectTotal()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter
        {
            Page = 10,
            PageSize = 10
        };
        var result = await service.SearchAsync(filter);

        Assert.Equal(5, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ClampsNegativeOrZeroPageAndPageSize()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter
        {
            Page = -5,
            PageSize = 0
        };
        var result = await service.SearchAsync(filter);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ClampsExcessivePageSizeToMax100()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter
        {
            Page = 1,
            PageSize = 500
        };
        var result = await service.SearchAsync(filter);

        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task SearchAsync_InvalidDateRange_StartDateAfterEndDate_ThrowsArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        await SeedDataAsync(context);
        var service = new MedicalRecordService(context);

        var filter = new MedicalRecordQueryFilter
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 1, 1)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync(filter));
    }

    [Fact]
    public async Task SearchAsync_NullFilter_ThrowsArgumentNullException()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicalRecordService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SearchAsync(null!));
    }
}
