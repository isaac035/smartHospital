using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.ConsultationTypes;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class ConsultationTypeService : IConsultationTypeService
{
    private readonly AppDbContext _context;

    public ConsultationTypeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ConsultationTypeResponse>> GetAllAsync()
    {
        return await _context.ConsultationTypes
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ConsultationTypeResponse
            {
                Id = c.Id,
                Name = c.Name,
                DurationMinutes = c.DurationMinutes,
                Description = c.Description,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ConsultationTypeResponse?> GetByIdAsync(int id)
    {
        return await _context.ConsultationTypes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ConsultationTypeResponse
            {
                Id = c.Id,
                Name = c.Name,
                DurationMinutes = c.DurationMinutes,
                Description = c.Description,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ConsultationTypeResponse> CreateAsync(CreateConsultationTypeRequest request)
    {
        var name = request.Name.Trim();

        var existing = await _context.ConsultationTypes
            .FirstOrDefaultAsync(c => c.Name == name);

        if (existing != null)
        {
            throw new InvalidOperationException(
                "A consultation type with this name already exists."
            );
        }

        var consultationType = new ConsultationType
        {
            Name = name,
            DurationMinutes = request.DurationMinutes,
            Description = request.Description.Trim(),
            Status = ConsultationTypeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ConsultationTypes.Add(consultationType);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(consultationType.Id)
            ?? throw new InvalidOperationException("Failed to create consultation type.");
    }

    public async Task<ConsultationTypeResponse?> UpdateAsync(
        int id,
        UpdateConsultationTypeRequest request)
    {
        var consultationType = await _context.ConsultationTypes
            .FirstOrDefaultAsync(c => c.Id == id);

        if (consultationType == null)
        {
            return null;
        }

        consultationType.Name = request.Name.Trim();
        consultationType.DurationMinutes = request.DurationMinutes;
        consultationType.Description = request.Description.Trim();
        consultationType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var consultationType = await _context.ConsultationTypes
            .FirstOrDefaultAsync(c => c.Id == id);

        if (consultationType == null)
        {
            return false;
        }

        consultationType.Status = ConsultationTypeStatus.Inactive;
        consultationType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}
