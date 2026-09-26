namespace Application.DTOs;

public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? Details { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime Timestamp => CreatedAtUtc; // compatibility alias
}
