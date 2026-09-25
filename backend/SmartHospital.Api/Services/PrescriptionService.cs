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
        if (patientId <= 0)
        {
            throw new ArgumentException("Patient ID must be a positive integer.", nameof(patientId));
        }

        var patientExists = await _context.Users
            .AnyAsync(u => u.Id == patientId && u.Role == UserRole.Patient);

        if (!patientExists)
        {
            throw new InvalidOperationException($"Patient with ID {patientId} was not found.");
        }

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
        if (id <= 0)
        {
            return null;
        }

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
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (doctorId <= 0)
        {
            throw new ArgumentException("Doctor ID must be a positive integer.", nameof(doctorId));
        }

        if (request.PatientId <= 0)
        {
            throw new ArgumentException("Patient ID must be a positive integer.", nameof(request.PatientId));
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new ArgumentException("At least one prescription item is required.", nameof(request.Items));
        }

        if (request.ExpiryDate.HasValue)
        {
            if (request.ExpiryDate.Value <= DateTime.UtcNow)
            {
                throw new ArgumentException("Expiry date must be in the future.", nameof(request.ExpiryDate));
            }

            if (request.ExpiryDate.Value > DateTime.UtcNow.AddYears(1))
            {
                throw new ArgumentException("Expiry date cannot be more than 1 year in the future.", nameof(request.ExpiryDate));
            }
        }

        if (!string.IsNullOrEmpty(request.GeneralInstructions) && request.GeneralInstructions.Length > 1000)
        {
            throw new ArgumentException("General instructions cannot exceed 1000 characters.", nameof(request.GeneralInstructions));
        }

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor && u.Status == UserStatus.Active);
        if (doctor == null)
        {
            throw new InvalidOperationException("Active doctor not found.");
        }

        var patient = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.Role == UserRole.Patient && u.Status == UserStatus.Active);
        if (patient == null)
        {
            throw new InvalidOperationException("Active patient not found.");
        }

        if (request.MedicalRecordId.HasValue)
        {
            if (request.MedicalRecordId.Value <= 0)
            {
                throw new ArgumentException("Medical record ID must be a positive integer.", nameof(request.MedicalRecordId));
            }

            var medicalRecord = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.Id == request.MedicalRecordId.Value);

            if (medicalRecord == null)
            {
                throw new InvalidOperationException("Medical record not found.");
            }

            if (medicalRecord.PatientId != request.PatientId)
            {
                throw new InvalidOperationException("Medical record does not belong to the specified patient.");
            }
        }

        foreach (var item in request.Items)
        {
            if (item == null)
            {
                throw new ArgumentException("Prescription item cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(item.MedicineName))
            {
                throw new ArgumentException("Medicine name is required.");
            }

            if (item.MedicineName.Length > 150)
            {
                throw new ArgumentException("Medicine name cannot exceed 150 characters.");
            }

            if (string.IsNullOrWhiteSpace(item.Dosage))
            {
                throw new ArgumentException("Dosage is required.");
            }

            if (item.Dosage.Length > 50)
            {
                throw new ArgumentException("Dosage cannot exceed 50 characters.");
            }

            if (!string.IsNullOrEmpty(item.Route) && item.Route.Length > 50)
            {
                throw new ArgumentException("Route cannot exceed 50 characters.");
            }

            if (string.IsNullOrWhiteSpace(item.Frequency))
            {
                throw new ArgumentException("Frequency is required.");
            }

            if (item.Frequency.Length > 50)
            {
                throw new ArgumentException("Frequency cannot exceed 50 characters.");
            }

            if (item.DurationDays < 1 || item.DurationDays > 365)
            {
                throw new ArgumentException("Duration must be between 1 and 365 days.");
            }

            if (!string.IsNullOrEmpty(item.SpecialInstructions) && item.SpecialInstructions.Length > 500)
            {
                throw new ArgumentException("Special instructions cannot exceed 500 characters.");
            }
        }

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
            GeneralInstructions = (request.GeneralInstructions ?? string.Empty).Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Items = request.Items.Select(i => new PrescriptionItem
            {
                MedicineName = i.MedicineName.Trim(),
                Dosage = i.Dosage.Trim(),
                Route = (string.IsNullOrWhiteSpace(i.Route) ? "Oral" : i.Route).Trim(),
                Frequency = i.Frequency.Trim(),
                DurationDays = i.DurationDays,
                SpecialInstructions = (i.SpecialInstructions ?? string.Empty).Trim()
            }).ToList()
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(prescription.Id))!;
    }
}
