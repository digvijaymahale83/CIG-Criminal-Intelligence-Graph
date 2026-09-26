namespace Domain.Entities;

public class Evidence
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string? UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public string ProcessingStatus { get; set; } = "UPLOADED"; 
    // Status states: UPLOADED, QUEUED, PROCESSING, EXTRACTED, REVIEW_REQUIRED, APPROVED, REJECTED, FAILED
    public string Clearance { get; set; } = "RESTRICTED";
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? ParentEvidenceId { get; set; }

    // Navigation
    public Case? Case { get; set; }
    public ICollection<ExtractionJob> ExtractionJobs { get; set; } = new List<ExtractionJob>();
    public ICollection<ExtractedEntity> ExtractedEntities { get; set; } = new List<ExtractedEntity>();
    public ICollection<ExtractedRelationship> ExtractedRelationships { get; set; } = new List<ExtractedRelationship>();
    public ICollection<ExtractedEvent> ExtractedEvents { get; set; } = new List<ExtractedEvent>();
}
