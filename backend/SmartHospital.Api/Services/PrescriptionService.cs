using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly AppDbContext _context;

    public PrescriptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PrescriptionResponse>> GetByPatientIdAsync(int patientId)
    {
        return await _context.Prescriptions
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssueDate)
            .Select(p => new PrescriptionResponse
            {
                Id = p.Id,
                PrescriptionNumber = p.PrescriptionNumber,
                MedicalRecordId = p.MedicalRecordId,
                PatientId = p.PatientId,
                PatientName = $"{p.Patient!.FirstName} {p.Patient.LastName}".Trim(),
                DoctorId = p.DoctorId,
                DoctorName = $"Dr. {p.Doctor!.FirstName} {p.Doctor.LastName}".Trim(),
                IssueDate = p.IssueDate,
                ExpiryDate = p.ExpiryDate,
                Status = p.Status.ToString(),
                GeneralInstructions = p.GeneralInstructions,
                CreatedAt = p.CreatedAt,
                Items = p.Items.Select(i => new PrescriptionItemResponse
                {
                    Id = i.Id,
                    MedicineName = i.MedicineName,
                    Dosage = i.Dosage,
                    Route = i.Route,
                    Frequency = i.Frequency,
                    DurationDays = i.DurationDays,
                    SpecialInstructions = i.SpecialInstructions
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<PrescriptionResponse?> GetByIdAsync(int id)
    {
        var prescription = await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null)
        {
            return null;
        }

        return new PrescriptionResponse
        {
            Id = prescription.Id,
            PrescriptionNumber = prescription.PrescriptionNumber,
            MedicalRecordId = prescription.MedicalRecordId,
            PatientId = prescription.PatientId,
            PatientName = $"{prescription.Patient?.FirstName} {prescription.Patient?.LastName}".Trim(),
            DoctorId = prescription.DoctorId,
            DoctorName = $"Dr. {prescription.Doctor?.FirstName} {prescription.Doctor?.LastName}".Trim(),
            IssueDate = prescription.IssueDate,
            ExpiryDate = prescription.ExpiryDate,
            Status = prescription.Status.ToString(),
            GeneralInstructions = prescription.GeneralInstructions,
            CreatedAt = prescription.CreatedAt,
            Items = prescription.Items.Select(i => new PrescriptionItemResponse
            {
                Id = i.Id,
                MedicineName = i.MedicineName,
                Dosage = i.Dosage,
                Route = i.Route,
                Frequency = i.Frequency,
                DurationDays = i.DurationDays,
                SpecialInstructions = i.SpecialInstructions
            }).ToList()
        };
    }

    public async Task<PrescriptionResponse> CreateAsync(int doctorId, CreatePrescriptionRequest request)
    {
        var now = DateTime.UtcNow;
        var prescription = new Prescription
        {
            PrescriptionNumber = $"RX-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PatientId = request.PatientId,
            DoctorId = doctorId,
            MedicalRecordId = request.MedicalRecordId,
            IssueDate = now,
            ExpiryDate = request.ExpiryDate ?? now.AddDays(30),
            Status = PrescriptionStatus.Active,
            GeneralInstructions = request.GeneralInstructions.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Items = request.Items.Select(i => new PrescriptionItem
            {
                MedicineName = i.MedicineName.Trim(),
                Dosage = i.Dosage.Trim(),
                Route = i.Route.Trim(),
                Frequency = i.Frequency.Trim(),
                DurationDays = i.DurationDays,
                SpecialInstructions = i.SpecialInstructions.Trim()
            }).ToList()
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(prescription.Id))!;
    }
}
