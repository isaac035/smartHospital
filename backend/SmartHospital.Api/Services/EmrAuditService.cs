using Microsoft.EntityFrameworkCore;
using SmartHospital.Api.Data;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Services;

public class EmrAuditService : IEmrAuditService
{
    private readonly AppDbContext _context;

    public EmrAuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmrAuditLogResponse> LogAsync(
        int userId,
        string action,
        string entityType,
        int? entityId,
        int? patientId = null,
        string? metadata = null,
        bool isSuccess = true)
    {
        var log = new EmrAuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PatientId = patientId,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata,
            IsSuccess = isSuccess
        };

        _context.EmrAuditLogs.Add(log);
        await _context.SaveChangesAsync();

        string userName = string.Empty;
        string userRole = string.Empty;

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            userName = $"{user.FirstName} {user.LastName}".Trim();
            userRole = user.Role.ToString();
        }

        return new EmrAuditLogResponse
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = userName,
            UserRole = userRole,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            PatientId = log.PatientId,
            Timestamp = log.Timestamp,
            Metadata = log.Metadata,
            IsSuccess = log.IsSuccess
        };
    }

    public async Task<List<EmrAuditLogResponse>> GetAuditLogsAsync(
        int? patientId = null,
        string? entityType = null,
        int? userId = null,
        int limit = 100)
    {
        if (limit <= 0) limit = 100;
        if (limit > 500) limit = 500;

        var query = _context.EmrAuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .AsQueryable();

        if (patientId.HasValue && patientId.Value > 0)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new EmrAuditLogResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : string.Empty,
                UserRole = a.User != null ? a.User.Role.ToString() : string.Empty,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                PatientId = a.PatientId,
                Timestamp = a.Timestamp,
                Metadata = a.Metadata,
                IsSuccess = a.IsSuccess
            })
            .ToListAsync();
    }

    public async Task<EmrAuditLogResponse?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;

        var a = await _context.EmrAuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return null;

        return new EmrAuditLogResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : string.Empty,
            UserRole = a.User != null ? a.User.Role.ToString() : string.Empty,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            PatientId = a.PatientId,
            Timestamp = a.Timestamp,
            Metadata = a.Metadata,
            IsSuccess = a.IsSuccess
        };
    }

    public async Task<List<EmrAuditLogResponse>> GetByPatientIdAsync(int patientId, int limit = 100)
    {
        return await GetAuditLogsAsync(patientId: patientId, entityType: null, userId: null, limit: limit);
    }
}
