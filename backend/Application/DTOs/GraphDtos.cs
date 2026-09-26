namespace Application.DTOs;

public class GraphNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "PERSON";
    public string? CaseId { get; set; }
    public int ConnectionsCount { get; set; }
    public int EvidenceCount { get; set; }
    public bool Verified { get; set; } = true;
    public string? Risk { get; set; } = "MEDIUM";
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class GraphEdgeDto
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = "ASSOCIATED_WITH";
    public string? Label => Type;
    public double Confidence { get; set; } = 1.0;
    public string? SourceEvidenceId { get; set; }
    public string? SourceLocation { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int SupportingEvidenceCount { get; set; } = 1;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class GraphDataDto
{
    public string? CaseId { get; set; }
    public List<GraphNodeDto> Nodes { get; set; } = new();
    public List<GraphEdgeDto> Edges { get; set; } = new();
    public int TotalNodes => Nodes.Count;
    public int TotalEdges => Edges.Count;
}

public class CaseGraphResponseDto : GraphDataDto
{
}

public class EntityNeighborhoodDto : GraphDataDto
{
    public string CenterEntityId { get; set; } = string.Empty;
    public int RequestedDepth { get; set; } = 1;
}

public class ShortestPathDto
{
    public bool Found { get; set; }
    public string StartEntityId { get; set; } = string.Empty;
    public string EndEntityId { get; set; } = string.Empty;
    public int HopsCount { get; set; }
    public List<GraphNodeDto> Nodes { get; set; } = new();
    public List<GraphEdgeDto> Edges { get; set; } = new();
}

public class GraphSearchResultDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public string Type { get; set; } = "PERSON";
    public string? CaseId { get; set; }
    public int ConnectionsCount { get; set; }
    public string? Risk { get; set; } = "MEDIUM";
}

public class ConnectedComponentDto
{
    public string ClusterId { get; set; } = string.Empty;
    public string ClusterLabel { get; set; } = string.Empty; // e.g., "Network Cluster A"
    public int EntityCount { get; set; }
    public int RelationshipCount { get; set; }
    public List<string> EntityIds { get; set; } = new();
}

public class EntityCentralityDto
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = "PERSON";
    public int Degree { get; set; }
    public double CentralityScore { get; set; }
    public string AnalyticalIndicator { get; set; } = "Network Relationship"; // Neutral indicator
}

public class GraphStatisticsDto
{
    public string? CaseId { get; set; }
    public int TotalNodes { get; set; }
    public int TotalEdges { get; set; }
    public Dictionary<string, int> EntityTypeDistribution { get; set; } = new();
    public Dictionary<string, int> RelationshipTypeDistribution { get; set; } = new();
    public List<ConnectedComponentDto> ConnectedComponents { get; set; } = new();
    public List<EntityCentralityDto> CentralityRankings { get; set; } = new();
}

public class SupportingEvidenceDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string VerifiedBy { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
    public string Sha256Hash { get; set; } = string.Empty;
}

public class RelationshipDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string SourceEntityName { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string? PrimaryEvidenceId { get; set; }
    public List<SupportingEvidenceDto> SupportingEvidence { get; set; } = new();
}

public class Neo4jProbeResultDto
{
    public bool Connected { get; set; }
    public string ProbeId { get; set; } = string.Empty;
    public bool NodeCreated { get; set; }
    public bool NodeRetrieved { get; set; }
    public bool NodeCleanedUp { get; set; }
    public double LatencyMs { get; set; }
    public string Message { get; set; } = string.Empty;
}
