namespace Domain.Entities;

public class ExtractionJob
{
    public string Id { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string Status { get; set; } = "QUEUED"; // QUEUED, PROCESSING, COMPLETED, FAILED
    public string? ErrorMessage { get; set; }
    public string? RawTextSnippet { get; set; }
    public int TotalEntitiesFound { get; set; }
    public int TotalRelationshipsFound { get; set; }
    public int TotalEventsFound { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;

    // Navigation
    public Evidence? Evidence { get; set; }
    public ICollection<ExtractedEntity> ExtractedEntities { get; set; } = new List<ExtractedEntity>();
    public ICollection<ExtractedRelationship> ExtractedRelationships { get; set; } = new List<ExtractedRelationship>();
    public ICollection<ExtractedEvent> ExtractedEvents { get; set; } = new List<ExtractedEvent>();
}
