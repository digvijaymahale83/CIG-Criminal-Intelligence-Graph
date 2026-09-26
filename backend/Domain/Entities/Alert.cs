namespace Domain.Entities;

/// <summary>
/// Represents an investigative alert generated from unusual patterns in network structure,
/// temporal event streams, geospatial movements, relationships, or data consistency.
/// Note: Alerts represent prioritized investigative leads requiring review, NOT proof of criminal activity.
/// </summary>
public class Alert
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string? AlertRunId { get; set; }

    /// <summary>
    /// Classification: NETWORK_ANOMALY, TEMPORAL_ANOMALY, GEOGRAPHIC_ANOMALY,
    /// RELATIONSHIP_SURGE, CROSS_CASE_PATTERN, ACTIVITY_SPIKE, UNUSUAL_TRAVEL,
    /// DATA_CONSISTENCY, MODEL_SIGNAL
    /// </summary>
    public string AlertType { get; set; } = "NETWORK_ANOMALY";

    /// <summary>
    /// Investigative Priority: LOW, MEDIUM, HIGH, CRITICAL.
    /// Indicates operational review priority, NOT guilt or criminality.
    /// </summary>
    public string Severity { get; set; } = "MEDIUM";

    /// <summary>
    /// Review Lifecycle: NEW, ACKNOWLEDGED, UNDER_REVIEW, RESOLVED, DISMISSED.
    /// </summary>
    public string Status { get; set; } = "NEW";

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Associated Entities / Nodes
    public string? SourceEntityId { get; set; }
    public string? SourceEntityName { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }

    // Associated Location
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }

    // Associated Events and Evidentiary Grounding
    public string? RelatedEventId { get; set; }
    public string? RelatedEvidenceId { get; set; }
    public string? RelatedEvidenceFileName { get; set; }
    public string? RelatedEvidenceSha256 { get; set; }

    /// <summary>
    /// Algorithmic anomaly confidence score between 0.0 and 1.0.
    /// </summary>
    public double Score { get; set; }

    public string DetectionMethod { get; set; } = string.Empty;
    public string DetectionVersion { get; set; } = "v1.0";

    /// <summary>
    /// Detailed structured explanation explaining WHAT, WHEN, WHERE, WHO, WHY, and WHICH evidence was involved.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Deterministic SHA-256 fingerprint used to prevent alert duplication across batch runs.
    /// </summary>
    public string DeduplicationFingerprint { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Review Audit Trails
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // Navigation
    public Case? Case { get; set; }
    public AlertRun? AlertRun { get; set; }
    public EntityItem? SourceEntity { get; set; }
    public EntityItem? TargetEntity { get; set; }
    public LocationItem? LocationItem { get; set; }
    public ExtractedEvent? RelatedEvent { get; set; }
    public Evidence? RelatedEvidence { get; set; }
}
