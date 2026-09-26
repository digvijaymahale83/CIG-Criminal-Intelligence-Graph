namespace Domain.Entities;

public class GraphAnalysisRun
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING"; // PENDING, RUNNING, COMPLETED, FAILED
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int NodeCount { get; set; } = 0;
    public int EdgeCount { get; set; } = 0;
    public int MetricsGenerated { get; set; } = 0;
    public string ModelVersion { get; set; } = "GAT-v1.0.0";
    public double NetworkDensity { get; set; } = 0.0;
    public double AverageDegree { get; set; } = 0.0;
    public double AveragePathLength { get; set; } = 0.0;
    public int ConnectedComponentsCount { get; set; } = 0;
    public int CommunitiesCount { get; set; } = 0;
    public string? ConfigurationJson { get; set; }
    public string? ErrorMessage { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;

    // Navigation
    public Case? Case { get; set; }
    public ICollection<GraphNodeMetrics> NodeMetrics { get; set; } = new List<GraphNodeMetrics>();
    public ICollection<GraphAnalyticalLead> Leads { get; set; } = new List<GraphAnalyticalLead>();
}
