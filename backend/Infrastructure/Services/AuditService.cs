using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAppDbContext dbContext, ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogAsync(
        string? actorId,
        string actorName,
        string action,
        string? resourceType,
        string? resourceId,
        string? details,
        string? metadataJson,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = $"aud-{DateTime.UtcNow.Ticks}-{Guid.NewGuid().ToString("N")[..6]}",
                ActorId = actorId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "System" : actorName,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                MetadataJson = metadataJson ?? (details != null ? $"{{\"details\":\"{details.Replace("\"", "\\\"")}\"}}" : null),
                IpAddress = ipAddress,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to persist audit log entry: {Message}", ex.Message);
        }
    }

    public async Task<List<AuditLogDto>> GetLogsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var cappedLimit = Math.Clamp(limit, 1, 500);
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(cappedLimit)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                ActorId = a.ActorId,
                ActorName = a.ActorName,
                Action = a.Action,
                ResourceType = a.ResourceType,
                ResourceId = a.ResourceId,
                Details = a.MetadataJson,
                MetadataJson = a.MetadataJson,
                IpAddress = a.IpAddress,
                CreatedAtUtc = a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
