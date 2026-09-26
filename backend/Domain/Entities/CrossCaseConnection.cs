namespace Domain.Entities;

/// <summary>
/// First-class model representing an approved, verified cross-case investigative link.
/// Preserves case provenance without conflating separate case records.
/// </summary>
public class CrossCaseConnection
{
    public string Id { get; set; } = string.Empty;
    public string? CandidateId { get; set; }
    public string SourceCaseId { get; set; } = string.Empty;
    public string TargetCaseId { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;

    /// <summary>
    /// Connection type: SHARED_ENTITY, SHARED_PHONE, SHARED_VEHICLE, SHARED_ACCOUNT, SHARED_LOCATION, MULTI_SIGNAL_CONNECTION.
    /// Neutral investigative terminology: never "criminal association" or "guilty link".
    /// </summary>
    public string ConnectionType { get; set; } = "SHARED_ENTITY";

    /// <summary>
    /// Derived confidence score from verified resolution signals.
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// Status: APPROVED, REJECTED.
    /// </summary>
    public string Status { get; set; } = "APPROVED";

    /// <summary>
    /// Objective, factual explanation of the cross-case nexus.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized citations from both participating cases.
    /// </summary>
    public string? SupportingEvidenceJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }

    // Navigation properties
    public EntityItem? SourceEntity { get; set; }
    public EntityItem? TargetEntity { get; set; }
    public Case? SourceCase { get; set; }
    public Case? TargetCase { get; set; }
}
