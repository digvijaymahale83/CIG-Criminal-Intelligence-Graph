namespace Domain.Entities;

public class AuditLog
{
    public string Id { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // LOGIN, LOGOUT, CASE_CREATED, CASE_UPDATED, CASE_DELETED, EVIDENCE_UPLOADED, EVIDENCE_VIEWED
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
