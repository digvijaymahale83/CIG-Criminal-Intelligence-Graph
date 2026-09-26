namespace Domain.Entities;

public class ExtractedRelationship
{
    public string Id { get; set; } = string.Empty;
    public string ExtractionJobId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }

    public string? SourceExtractedEntityId { get; set; }
    public string? TargetExtractedEntityId { get; set; }

    public string SourceNormalizedValue { get; set; } = string.Empty;
    public string TargetNormalizedValue { get; set; } = string.Empty;

    /// <summary>
    /// Supported relationship type: CALLED, USED, OWNED, VISITED, LOCATED_AT, WORKED_FOR,
    /// ASSOCIATED_WITH, INVOLVED_IN, MENTIONED_IN, TRANSFERRED_TO, MET, COMMUNICATED_WITH
    /// </summary>
    public string RelationshipType { get; set; } = "ASSOCIATED_WITH";

    public double Confidence { get; set; } = 1.0;
    public string SourceLocation { get; set; } = string.Empty; // e.g. "Page 3", "Row 8"

    /// <summary>
    /// Review status: PENDING, APPROVED, REJECTED
    /// </summary>
    public string ReviewStatus { get; set; } = "PENDING";

    /// <summary>
    /// Identifier of the canonical Relationship created when promoted upon approval.
    /// </summary>
    public string? PromotedRelationshipId { get; set; }

    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ExtractionJob? ExtractionJob { get; set; }
    public Evidence? Evidence { get; set; }
}
