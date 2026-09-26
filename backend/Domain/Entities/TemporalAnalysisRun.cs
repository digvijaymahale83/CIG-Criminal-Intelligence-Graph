namespace Domain.Entities;

public class TemporalAnalysisRun
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING"; // PENDING, RUNNING, COMPLETED, FAILED
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public int TotalEventsAnalyzed { get; set; }
    public int SignalsGenerated { get; set; }
    public int OverlapsFound { get; set; }
    public int ClustersFound { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Case? Case { get; set; }
    public ICollection<TemporalSignal> Signals { get; set; } = new List<TemporalSignal>();
}
