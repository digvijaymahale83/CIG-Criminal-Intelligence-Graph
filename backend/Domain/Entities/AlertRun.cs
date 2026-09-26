namespace Domain.Entities;

/// <summary>
/// Tracks execution metrics and operational audit data for anomaly detector runs.
/// </summary>
public class AlertRun
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle: RUNNING, COMPLETED, FAILED
    /// </summary>
    public string Status { get; set; } = "RUNNING";

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public int DetectorsExecuted { get; set; }
    public int SignalsGenerated { get; set; }
    public int AlertsCreated { get; set; }
    public int AlertsDeduplicated { get; set; }

    public string ExecutedBy { get; set; } = string.Empty;
    public long ExecutionDurationMs { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public Case? Case { get; set; }
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
