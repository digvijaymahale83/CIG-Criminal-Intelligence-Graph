namespace Domain.Entities;

/// <summary>
/// Represents a surfaced geospatial or spatial-temporal correlation candidate requiring investigator review.
/// </summary>
public class SpatialSignal
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string? AnalysisRunId { get; set; }

    /// <summary>
    /// Signal category: SPATIAL_TEMPORAL_OVERLAP, CROSS_CASE_SPATIAL_OVERLAP, PROXIMITY_CLUSTER, IMPLAUSIBLE_TRAVEL_SPEED
    /// </summary>
    public string SignalType { get; set; } = "SPATIAL_TEMPORAL_OVERLAP";

    public string? SourceEntityId { get; set; }
    public string? TargetEntityId { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }

    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }

    /// <summary>
    /// Geodesic distance in kilometers.
    /// </summary>
    public double DistanceKm { get; set; } = 0.0;

    /// <summary>
    /// Correlation / analytical confidence score (0.0 to 1.0).
    /// </summary>
    public double Score { get; set; } = 0.0;

    /// <summary>
    /// Human-readable neutral explanation.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of supporting evidence IDs and file citations.
    /// </summary>
    public string? SupportingEvidenceJson { get; set; }

    /// <summary>
    /// Staged review status: PENDING, CONFIRMED, DISMISSED.
    /// Default is PENDING (zero automated knowledge graph mutation).
    /// </summary>
    public string Status { get; set; } = "PENDING";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // Navigation
    public Case? Case { get; set; }
}
