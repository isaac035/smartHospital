using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class VitalSignService : IVitalSignService
{
    private readonly AppDbContext _context;

    public VitalSignService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<VitalSignResponse>> GetByPatientIdAsync(int patientId)
    {
        return await _context.VitalSigns
            .AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedAt)
            .Select(v => new VitalSignResponse
            {
                Id = v.Id,
                PatientId = v.PatientId,
                PatientName = $"{v.Patient!.FirstName} {v.Patient.LastName}".Trim(),
                RecordedByUserId = v.RecordedByUserId,
                RecordedByUserName = $"{v.RecordedByUser!.FirstName} {v.RecordedByUser.LastName}".Trim(),
                MedicalRecordId = v.MedicalRecordId,
                RecordedAt = v.RecordedAt,
                TemperatureCelsius = v.TemperatureCelsius,
                SystolicBloodPressure = v.SystolicBloodPressure,
                DiastolicBloodPressure = v.DiastolicBloodPressure,
                HeartRateBpm = v.HeartRateBpm,
                RespiratoryRateBpm = v.RespiratoryRateBpm,
                OxygenSaturationSpO2 = v.OxygenSaturationSpO2,
                WeightKg = v.WeightKg,
                HeightCm = v.HeightCm,
                Bmi = v.Bmi,
                Notes = v.Notes
            })
            .ToListAsync();
    }

    public async Task<VitalSignResponse> RecordAsync(int recordedByUserId, RecordVitalSignRequest request)
    {
        decimal? calculatedBmi = null;
        if (request.WeightKg.HasValue && request.HeightCm.HasValue && request.HeightCm.Value > 0)
        {
            var heightInMeters = request.HeightCm.Value / 100m;
            calculatedBmi = Math.Round(request.WeightKg.Value / (heightInMeters * heightInMeters), 2);
        }

        var vital = new VitalSign
        {
            PatientId = request.PatientId,
            RecordedByUserId = recordedByUserId,
            MedicalRecordId = request.MedicalRecordId,
            RecordedAt = DateTime.UtcNow,
            TemperatureCelsius = request.TemperatureCelsius,
            SystolicBloodPressure = request.SystolicBloodPressure,
            DiastolicBloodPressure = request.DiastolicBloodPressure,
            HeartRateBpm = request.HeartRateBpm,
            RespiratoryRateBpm = request.RespiratoryRateBpm,
            OxygenSaturationSpO2 = request.OxygenSaturationSpO2,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            Bmi = calculatedBmi,
            Notes = request.Notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.VitalSigns.Add(vital);
        await _context.SaveChangesAsync();

        var created = await _context.VitalSigns
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.RecordedByUser)
            .FirstAsync(v => v.Id == vital.Id);

        return new VitalSignResponse
        {
            Id = created.Id,
            PatientId = created.PatientId,
            PatientName = $"{created.Patient?.FirstName} {created.Patient?.LastName}".Trim(),
            RecordedByUserId = created.RecordedByUserId,
            RecordedByUserName = $"{created.RecordedByUser?.FirstName} {created.RecordedByUser?.LastName}".Trim(),
            MedicalRecordId = created.MedicalRecordId,
            RecordedAt = created.RecordedAt,
            TemperatureCelsius = created.TemperatureCelsius,
            SystolicBloodPressure = created.SystolicBloodPressure,
            DiastolicBloodPressure = created.DiastolicBloodPressure,
            HeartRateBpm = created.HeartRateBpm,
            RespiratoryRateBpm = created.RespiratoryRateBpm,
            OxygenSaturationSpO2 = created.OxygenSaturationSpO2,
            WeightKg = created.WeightKg,
            HeightCm = created.HeightCm,
            Bmi = created.Bmi,
            Notes = created.Notes
        };
    }
}
