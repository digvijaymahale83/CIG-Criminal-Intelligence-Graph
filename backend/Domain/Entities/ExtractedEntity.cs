namespace Domain.Entities;

public class ExtractedEntity
{
    public string Id { get; set; } = string.Empty;
    public string ExtractionJobId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }

    /// <summary>
    /// Canonical entity type: PERSON, PHONE, VEHICLE, LOCATION, ORGANIZATION, ACCOUNT, DEVICE, DOCUMENT, CASE, EVENT
    /// </summary>
    public string EntityType { get; set; } = "PERSON";

    public string RawValue { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string SourceLocation { get; set; } = string.Empty; // e.g. "Page 2", "Row 15", "Line 42"

    /// <summary>
    /// Review status: PENDING, APPROVED, REJECTED
    /// </summary>
    public string ReviewStatus { get; set; } = "PENDING";

    /// <summary>
    /// Identifier of the canonical EntityItem created when promoted upon approval.
    /// </summary>
    public string? PromotedEntityId { get; set; }

    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ExtractionJob? ExtractionJob { get; set; }
    public Evidence? Evidence { get; set; }
}
