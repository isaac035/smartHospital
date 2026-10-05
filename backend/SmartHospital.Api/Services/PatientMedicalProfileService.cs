using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class PatientMedicalProfileService : IPatientMedicalProfileService
{
    private readonly AppDbContext _context;

    public PatientMedicalProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PatientMedicalProfileResponse?> GetByPatientIdAsync(int patientId)
    {
        var profile = await _context.PatientMedicalProfiles
            .AsNoTracking()
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.PatientId == patientId);

        if (profile == null)
        {
            return null;
        }

        return new PatientMedicalProfileResponse
        {
            Id = profile.Id,
            PatientId = profile.PatientId,
            PatientName = $"{profile.Patient?.FirstName} {profile.Patient?.LastName}".Trim(),
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            BloodGroup = profile.BloodGroup.ToString(),
            Allergies = profile.Allergies,
            ChronicDiseases = profile.ChronicDiseases,
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    public async Task<PatientMedicalProfileResponse> UpsertAsync(
        int patientId,
        UpsertPatientMedicalProfileRequest request)
    {
        var profile = await _context.PatientMedicalProfiles
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.PatientId == patientId);

        var now = DateTime.UtcNow;

        if (profile == null)
        {
            profile = new PatientMedicalProfile
            {
                PatientId = patientId,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender.Trim(),
                BloodGroup = request.BloodGroup,
                Allergies = request.Allergies.Trim(),
                ChronicDiseases = request.ChronicDiseases.Trim(),
                EmergencyContactName = request.EmergencyContactName.Trim(),
                EmergencyContactPhone = request.EmergencyContactPhone.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.PatientMedicalProfiles.Add(profile);
        }
        else
        {
            profile.DateOfBirth = request.DateOfBirth;
            profile.Gender = request.Gender.Trim();
            profile.BloodGroup = request.BloodGroup;
            profile.Allergies = request.Allergies.Trim();
            profile.ChronicDiseases = request.ChronicDiseases.Trim();
            profile.EmergencyContactName = request.EmergencyContactName.Trim();
            profile.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
            profile.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        return new PatientMedicalProfileResponse
        {
            Id = profile.Id,
            PatientId = profile.PatientId,
            PatientName = $"{profile.Patient?.FirstName} {profile.Patient?.LastName}".Trim(),
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            BloodGroup = profile.BloodGroup.ToString(),
            Allergies = profile.Allergies,
            ChronicDiseases = profile.ChronicDiseases,
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
