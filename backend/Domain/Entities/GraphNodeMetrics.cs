namespace Domain.Entities;

public class GraphNodeMetrics
{
    public string Id { get; set; } = string.Empty;
    public string AnalysisRunId { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    
    public int Degree { get; set; } = 0;
    public int InDegree { get; set; } = 0;
    public int OutDegree { get; set; } = 0;
    public double NormalizedDegree { get; set; } = 0.0;
    public double BetweennessCentrality { get; set; } = 0.0;
    public double ClosenessCentrality { get; set; } = 0.0;
    public double PageRank { get; set; } = 0.0;
    
    public string ComponentId { get; set; } = string.Empty;
    public string CommunityId { get; set; } = string.Empty;
    public string AnalyticalIndicator { get; set; } = "Connected Entity"; // "Key Nexus Entity", "High Connectivity Lead", "Network Association", "Connected Entity"
    
    public string? EmbeddingJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public GraphAnalysisRun? AnalysisRun { get; set; }
    public EntityItem? Entity { get; set; }
}
