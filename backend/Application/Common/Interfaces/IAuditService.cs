using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string? actorId, string actorName, string action, string? resourceType, string? resourceId, string? details, string? metadataJson, string? ipAddress, CancellationToken cancellationToken = default);
    Task<List<AuditLogDto>> GetLogsAsync(int limit, CancellationToken cancellationToken = default);
}
