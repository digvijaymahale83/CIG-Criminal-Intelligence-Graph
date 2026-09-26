namespace Domain.Entities;

public class EvidenceLedgerBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long BlockIndex { get; set; }
    public string? EvidenceItemId { get; set; }
    public int EvidenceVersion { get; set; } = 1;
    public string EvidenceHash { get; set; } = string.Empty;
    public string PreviousBlockHash { get; set; } = string.Empty;
    public string BlockHash { get; set; } = string.Empty;
    public string Action { get; set; } = "UPLOAD";
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Evidence? EvidenceItem { get; set; }
}
