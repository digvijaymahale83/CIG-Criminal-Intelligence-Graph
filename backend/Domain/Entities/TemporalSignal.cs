namespace Domain.Entities;

public class TemporalSignal
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string? AnalysisRunId { get; set; }

    public string SignalType { get; set; } = "TEMPORAL_OVERLAP"; // TEMPORAL_OVERLAP, CROSS_CASE_TEMPORAL_OVERLAP, ACTIVITY_CLUSTER, TEMPORAL_SEQUENCE
    public string? SourceEntityId { get; set; }
    public string? TargetEntityId { get; set; }

    public string? LocationEntityId { get; set; }
    public string? LocationName { get; set; }

    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public double DurationMinutes { get; set; }

    public double Score { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string SignalsJson { get; set; } = "{}";

    public string Status { get; set; } = "PENDING"; // PENDING, CONFIRMED, DISMISSED

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // Navigation
    public Case? Case { get; set; }
    public TemporalAnalysisRun? AnalysisRun { get; set; }
    public EntityItem? SourceEntity { get; set; }
    public EntityItem? TargetEntity { get; set; }
}
