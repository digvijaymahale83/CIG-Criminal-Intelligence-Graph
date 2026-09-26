namespace Domain.Entities;

/// <summary>
/// Tracks execution lifecycle, metrics, and parameters of automated spatial intelligence runs.
/// </summary>
public class SpatialAnalysisRun
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string ExecutedBy { get; set; } = string.Empty;

    /// <summary>
    /// Status: PENDING, RUNNING, COMPLETED, FAILED
    /// </summary>
    public string Status { get; set; } = "COMPLETED";

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public int TotalLocationsAnalyzed { get; set; } = 0;
    public int TotalEventsAnalyzed { get; set; } = 0;
    public int SignalsGenerated { get; set; } = 0;
    public int OverlapsFound { get; set; } = 0;
    public int ClustersFound { get; set; } = 0;
    public int VelocityWarningsFound { get; set; } = 0;

    public string? ConfigurationJson { get; set; }

    // Navigation
    public Case? Case { get; set; }
}
