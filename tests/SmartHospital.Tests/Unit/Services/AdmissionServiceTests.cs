using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Admissions;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class AdmissionServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static User SeedUser(AppDbContext context, string email = "patient@test.com", UserRole role = UserRole.Patient)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = role.ToString(),
            Email = email,
            PasswordHash = "hash",
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private static (Ward ward, Room room, Bed bed) SeedHierarchy(AppDbContext context, int wardCapacity = 10, int roomCapacity = 4)
    {
        var ward = new Ward
        {
            Name = "General Ward",
            Code = "GW-" + Guid.NewGuid().ToString()[..4].ToUpper(),
            Floor = "1",
            Capacity = wardCapacity,
            Type = WardType.General,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Wards.Add(ward);
        context.SaveChanges();

        var room = new Room
        {
            WardId = ward.Id,
            RoomNumber = "R-" + Guid.NewGuid().ToString()[..4].ToUpper(),
            Capacity = roomCapacity,
            Type = RoomType.Standard,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Rooms.Add(room);
        context.SaveChanges();

        var bed = new Bed
        {
            RoomId = room.Id,
            BedNumber = "B-01",
            Status = BedStatus.Available,
            Type = BedType.Standard,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Beds.Add(bed);
        context.SaveChanges();

        return (ward, room, bed);
    }

    [Fact]
    public async Task CreateAdmissionAsync_ValidRequest_CreatesAdmissionWithUniqueAdmissionNumber()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient1@test.com", UserRole.Patient);
        var doctor = SeedUser(context, "doc1@test.com", UserRole.Doctor);
        var service = new AdmissionService(context);

        var response = await service.CreateAdmissionAsync(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            AdmittingDoctorId = doctor.Id,
            Priority = AdmissionPriority.Urgent,
            ReasonForAdmission = "Severe respiratory distress",
            Diagnosis = "Acute Bronchitis"
        });

        Assert.True(response.Id > 0);
        Assert.StartsWith("ADM-", response.AdmissionNumber);
        Assert.Equal(patient.Id, response.PatientId);
        Assert.Equal("Admitted", response.Status);
        Assert.Equal("Urgent", response.Priority);
        Assert.Equal("Severe respiratory distress", response.ReasonForAdmission);
        Assert.Equal("Acute Bronchitis", response.Diagnosis);
        Assert.Equal(doctor.Id, response.AdmittingDoctorId);
    }

    [Fact]
    public async Task CreateAdmissionAsync_PatientNotFound_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var service = new AdmissionService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAdmissionAsync(new CreateAdmissionRequest
            {
                PatientId = 999,
                ReasonForAdmission = "Checkup"
            }));

        Assert.Contains("Patient user not found", ex.Message);
    }

    [Fact]
    public async Task CreateAdmissionAsync_AdmittingDoctorNotFoundOrNotDoctorRole_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient1@test.com", UserRole.Patient);
        var notADoctor = SeedUser(context, "staff1@test.com", UserRole.Staff);
        var service = new AdmissionService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAdmissionAsync(new CreateAdmissionRequest
            {
                PatientId = patient.Id,
                AdmittingDoctorId = notADoctor.Id,
                ReasonForAdmission = "Observation"
            }));

        Assert.Contains("Admitting doctor not found", ex.Message);
    }

    [Fact]
    public async Task CreateAdmissionAsync_PatientAlreadyHasActiveAdmission_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient1@test.com", UserRole.Patient);
        var service = new AdmissionService(context);

        await service.CreateAdmissionAsync(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            ReasonForAdmission = "First admission"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAdmissionAsync(new CreateAdmissionRequest
            {
                PatientId = patient.Id,
                ReasonForAdmission = "Second concurrent admission"
            }));

        Assert.Contains("already has an active inpatient admission", ex.Message);
    }

    [Fact]
    public async Task AllocateBedAsync_ValidRequest_AllocatesBedAndUpdatesBedStatusToOccupied()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient1@test.com", UserRole.Patient);
        var (_, _, bed) = SeedHierarchy(context);
        var service = new AdmissionService(context);

        var admission = await service.CreateAdmissionAsync(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            ReasonForAdmission = "Inpatient care"
        });

        var response = await service.AllocateBedAsync(new AllocateBedRequest
        {
            AdmissionId = admission.Id,
            BedId = bed.Id,
            Notes = "Allocated by ER triage"
        });

        Assert.Equal(bed.Id, response.ActiveBedId);
        Assert.Equal("B-01", response.ActiveBedNumber);

        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.Equal(BedStatus.Occupied, bedEntity!.Status);

        var allocation = await context.BedAllocations.SingleAsync(a => a.AdmissionId == admission.Id);
        Assert.Equal(BedAllocationStatus.Active, allocation.Status);
        Assert.Equal("Allocated by ER triage", allocation.Notes);
    }

    [Fact]
    public async Task AllocateBedAsync_BedAlreadyOccupied_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "patient1@test.com", UserRole.Patient);
        var (_, _, bed) = SeedHierarchy(context);
        bed.Status = BedStatus.Occupied;
        await context.SaveChangesAsync();

        var service = new AdmissionService(context);
        var admission = await service.CreateAdmissionAsync(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            ReasonForAdmission = "Inpatient care"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AllocateBedAsync(new AllocateBedRequest
            {
                AdmissionId = admission.Id,
                BedId = bed.Id
            }));

        Assert.Contains("Selected bed is not available", ex.Message);
    }

    [Fact]
    public async Task AllocateBedAsync_BedHasActiveAllocation_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var p1 = SeedUser(context, "p1@test.com", UserRole.Patient);
        var p2 = SeedUser(context, "p2@test.com", UserRole.Patient);
        var (_, _, bed) = SeedHierarchy(context);
        var service = new AdmissionService(context);

        var adm1 = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = p1.Id, ReasonForAdmission = "Stay 1" });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm1.Id, BedId = bed.Id });

        // Manually reset bed status to Available to test allocation guard
        bed.Status = BedStatus.Available;
        await context.SaveChangesAsync();

        var adm2 = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = p2.Id, ReasonForAdmission = "Stay 2" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm2.Id, BedId = bed.Id }));

        Assert.Contains("already has an active allocation", ex.Message);
    }

    [Fact]
    public async Task AllocateBedAsync_PatientAlreadyHasActiveBed_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var (_, room, bed1) = SeedHierarchy(context);

        var bed2 = new Bed { RoomId = room.Id, BedNumber = "B-02", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed2);
        await context.SaveChangesAsync();

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Stay" });

        // Create an allocation under a different admission to simulate patient having another active bed
        var oldAdm = new Admission
        {
            AdmissionNumber = "ADM-OLD-01",
            PatientId = patient.Id,
            Status = AdmissionStatus.Discharged,
            Priority = AdmissionPriority.Normal,
            ReasonForAdmission = "Old",
            AdmissionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Admissions.Add(oldAdm);
        await context.SaveChangesAsync();

        context.BedAllocations.Add(new BedAllocation
        {
            AdmissionId = oldAdm.Id,
            BedId = bed1.Id,
            AllocatedAt = DateTime.UtcNow,
            Status = BedAllocationStatus.Active, // Active bed allocation on another admission
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed2.Id }));

        Assert.Contains("already has an active bed allocation", ex.Message);
    }

    [Fact]
    public async Task AllocateBedAsync_WardCapacityLimitReached_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        // Ward capacity = 1
        var (ward, room, bed1) = SeedHierarchy(context, wardCapacity: 1);

        var bed2 = new Bed { RoomId = room.Id, BedNumber = "B-02", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed2);
        // Mark bed1 as Occupied in same ward
        bed1.Status = BedStatus.Occupied;
        await context.SaveChangesAsync();

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Stay" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed2.Id }));

        Assert.Contains("capacity limit", ex.Message);
    }

    [Fact]
    public async Task TransferPatientAsync_ValidTransfer_ReleasesOldBedAndOccupiesNewBed()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var (_, room, bed1) = SeedHierarchy(context);

        var bed2 = new Bed { RoomId = room.Id, BedNumber = "B-02", Status = BedStatus.Available, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed2);
        await context.SaveChangesAsync();

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Care" });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed1.Id });

        var transferResponse = await service.TransferPatientAsync(adm.Id, new TransferPatientRequest
        {
            NewBedId = bed2.Id,
            TransferReason = "Condition upgraded to stable"
        });

        Assert.Equal(bed2.Id, transferResponse.ActiveBedId);
        Assert.Equal("B-02", transferResponse.ActiveBedNumber);

        var oldBedEntity = await context.Beds.FindAsync(bed1.Id);
        var newBedEntity = await context.Beds.FindAsync(bed2.Id);

        Assert.Equal(BedStatus.Available, oldBedEntity!.Status);
        Assert.Equal(BedStatus.Occupied, newBedEntity!.Status);

        var allocations = await context.BedAllocations.Where(a => a.AdmissionId == adm.Id).OrderBy(a => a.Id).ToListAsync();
        Assert.Equal(2, allocations.Count);
        Assert.Equal(BedAllocationStatus.Transferred, allocations[0].Status);
        Assert.NotNull(allocations[0].ReleasedAt);
        Assert.Equal(BedAllocationStatus.Active, allocations[1].Status);
        Assert.Equal("Condition upgraded to stable", allocations[1].Notes);
    }

    [Fact]
    public async Task TransferPatientAsync_SameBedAsCurrent_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var (_, _, bed) = SeedHierarchy(context);

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Care" });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed.Id });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransferPatientAsync(adm.Id, new TransferPatientRequest
            {
                NewBedId = bed.Id,
                TransferReason = "Transfer to same bed"
            }));

        Assert.Contains("destination bed cannot be the same", ex.Message);
    }

    [Fact]
    public async Task TransferPatientAsync_DestinationBedUnavailable_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var (_, room, bed1) = SeedHierarchy(context);

        var bed2 = new Bed { RoomId = room.Id, BedNumber = "B-02", Status = BedStatus.Maintenance, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Beds.Add(bed2);
        await context.SaveChangesAsync();

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Care" });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed1.Id });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransferPatientAsync(adm.Id, new TransferPatientRequest
            {
                NewBedId = bed2.Id,
                TransferReason = "Transfer"
            }));

        Assert.Contains("Destination bed is not available", ex.Message);
    }

    [Fact]
    public async Task DischargePatientAsync_ValidDischarge_ReleasesBedAndSetsAdmissionDischarged()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var (_, _, bed) = SeedHierarchy(context);

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Inpatient stay" });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed.Id });

        var dischargeResponse = await service.DischargePatientAsync(adm.Id, new DischargePatientRequest
        {
            DischargeSummary = "Patient recovered successfully"
        });

        Assert.Equal("Discharged", dischargeResponse.Status);
        Assert.NotNull(dischargeResponse.DischargeDate);
        Assert.Equal("Patient recovered successfully", dischargeResponse.DischargeSummary);

        var bedEntity = await context.Beds.FindAsync(bed.Id);
        Assert.Equal(BedStatus.Available, bedEntity!.Status);

        var allocation = await context.BedAllocations.SingleAsync(a => a.AdmissionId == adm.Id);
        Assert.Equal(BedAllocationStatus.Released, allocation.Status);
        Assert.NotNull(allocation.ReleasedAt);
    }

    [Fact]
    public async Task DischargePatientAsync_AlreadyDischargedStay_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var (_, _, bed) = SeedHierarchy(context);

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Inpatient stay" });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed.Id });
        await service.DischargePatientAsync(adm.Id, new DischargePatientRequest { DischargeSummary = "Discharged" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DischargePatientAsync(adm.Id, new DischargePatientRequest { DischargeSummary = "Discharge again" }));

        Assert.Contains("Active admitted patient stay not found", ex.Message);
    }

    [Fact]
    public async Task GetActiveAdmissionByPatientIdAsync_ReturnsActiveStayWithBedAndWardDetails()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var doctor = SeedUser(context, "doc1@test.com", UserRole.Doctor);
        var (ward, room, bed) = SeedHierarchy(context);

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            AdmittingDoctorId = doctor.Id,
            ReasonForAdmission = "Observation"
        });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed.Id });

        var activeStay = await service.GetActiveAdmissionByPatientIdAsync(patient.Id);

        Assert.NotNull(activeStay);
        Assert.Equal(adm.Id, activeStay.AdmissionId);
        Assert.Equal("Admitted", activeStay.Status);
        Assert.Equal(ward.Name, activeStay.WardName);
        Assert.Equal(room.RoomNumber, activeStay.RoomNumber);
        Assert.Equal(bed.BedNumber, activeStay.BedNumber);
    }

    [Fact]
    public async Task GetPatientAdmissionHistoryAsync_ReturnsDischargedStays()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var (_, _, bed) = SeedHierarchy(context);

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest { PatientId = patient.Id, ReasonForAdmission = "Observation" });
        await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed.Id });
        await service.DischargePatientAsync(adm.Id, new DischargePatientRequest { DischargeSummary = "Resolved" });

        var history = await service.GetPatientAdmissionHistoryAsync(patient.Id);

        Assert.Single(history);
        Assert.Equal("Discharged", history[0].Status);
        Assert.Equal(adm.Id, history[0].AdmissionId);
    }

    [Fact]
    public async Task UpdateAdmissionAsync_ValidUpdate_UpdatesPriorityAndDiagnosis()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "p1@test.com", UserRole.Patient);
        var service = new AdmissionService(context);

        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            Priority = AdmissionPriority.Normal,
            ReasonForAdmission = "Initial Reason"
        });

        var updated = await service.UpdateAdmissionAsync(adm.Id, new UpdateAdmissionRequest
        {
            Priority = AdmissionPriority.Emergency,
            ReasonForAdmission = "Condition deteriorated",
            Diagnosis = "Septic shock"
        });

        Assert.NotNull(updated);
        Assert.Equal("Emergency", updated.Priority);
        Assert.Equal("Condition deteriorated", updated.ReasonForAdmission);
        Assert.Equal("Septic shock", updated.Diagnosis);
    }

    [Fact]
    public async Task AllocateBedAsync_AdmissionAlreadyHasActiveBed_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        var patient = SeedUser(context, "dup_alloc@test.com", UserRole.Patient);
        var (_, room, bed1) = SeedHierarchy(context);

        var bed2 = new Bed
        {
            RoomId = room.Id,
            BedNumber = "B-02",
            Status = BedStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Beds.Add(bed2);
        await context.SaveChangesAsync();

        var service = new AdmissionService(context);
        var adm = await service.CreateAdmissionAsync(new CreateAdmissionRequest
        {
            PatientId = patient.Id,
            ReasonForAdmission = "Observation"
        });

        // 1. First bed allocation succeeds
        var firstAlloc = await service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed1.Id });
        Assert.Equal(bed1.Id, firstAlloc.ActiveBedId);
        var bed1AfterAlloc = await context.Beds.FindAsync(bed1.Id);
        Assert.Equal(BedStatus.Occupied, bed1AfterAlloc!.Status);

        // 2. Second normal allocation to the same admission is rejected
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AllocateBedAsync(new AllocateBedRequest { AdmissionId = adm.Id, BedId = bed2.Id }));
        Assert.Contains("already has an active bed allocation", ex.Message, StringComparison.OrdinalIgnoreCase);

        // 3. The rejected allocation does not occupy the second bed
        var bed2AfterRejected = await context.Beds.FindAsync(bed2.Id);
        Assert.Equal(BedStatus.Available, bed2AfterRejected!.Status);

        // 4. Discharging the patient releases the correctly allocated bed
        await service.DischargePatientAsync(adm.Id, new DischargePatientRequest { DischargeSummary = "Discharged" });
        var bed1AfterDischarge = await context.Beds.FindAsync(bed1.Id);
        Assert.Equal(BedStatus.Available, bed1AfterDischarge!.Status);

        // 5. No orphaned active BedAllocation remains
        var activeAllocations = await context.BedAllocations
            .Where(ba => ba.AdmissionId == adm.Id && ba.Status == BedAllocationStatus.Active)
            .ToListAsync();
        Assert.Empty(activeAllocations);
    }
}

