namespace Domain.Entities;

/// <summary>
/// Represents a candidate cross-case entity match identified by the multi-signal resolution engine.
/// All candidates require explicit human investigator review before becoming verified intelligence.
/// </summary>
public class EntityMatchCandidate
{
    public string Id { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string SourceCaseId { get; set; } = string.Empty;
    public string TargetCaseId { get; set; } = string.Empty;

    /// <summary>
    /// Canonical entity category (e.g. PERSON, PHONE, VEHICLE, ACCOUNT, ORGANIZATION, LOCATION).
    /// </summary>
    public string EntityType { get; set; } = "PERSON";

    /// <summary>
    /// Lifecycle review status: PENDING, APPROVED, REJECTED, SUPERSEDED.
    /// </summary>
    public string MatchStatus { get; set; } = "PENDING";

    /// <summary>
    /// Normalized match score computed by the multi-signal engine (0.0 to 1.0).
    /// </summary>
    public double MatchScore { get; set; } = 0.0;

    /// <summary>
    /// Primary resolution method: MULTI_SIGNAL, EXACT_IDENTIFIER, FUZZY_NAME, CONTEXT_SIMILARITY.
    /// </summary>
    public string MatchMethod { get; set; } = "MULTI_SIGNAL";

    /// <summary>
    /// JSON-serialized array of contributing factors with tangible weights and evidence citations.
    /// </summary>
    public string MatchExplanationJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // Navigation properties
    public EntityItem? SourceEntity { get; set; }
    public EntityItem? TargetEntity { get; set; }
    public Case? SourceCase { get; set; }
    public Case? TargetCase { get; set; }
}
