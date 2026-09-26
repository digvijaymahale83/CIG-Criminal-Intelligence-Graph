namespace Application.DTOs;

public class RunAnalyticsRequestDto
{
    public bool IncludeCrossCase { get; set; } = false;
    public List<string>? AuthorizedCaseIds { get; set; }
    public int GatEmbeddingDim { get; set; } = 64;
    public int GatHeads { get; set; } = 4;
    public double CandidateThreshold { get; set; } = 0.50;
    public int MaxCandidates { get; set; } = 25;
}

public class GraphAnalysisRunDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
    public int MetricsGenerated { get; set; }
    public string ModelVersion { get; set; } = "GAT-v1.0.0";
    public double NetworkDensity { get; set; }
    public double AverageDegree { get; set; }
    public double AveragePathLength { get; set; }
    public int ConnectedComponentsCount { get; set; }
    public int CommunitiesCount { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
}

public class EntityCentralityMetricDto
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int Degree { get; set; }
    public int InDegree { get; set; }
    public int OutDegree { get; set; }
    public double NormalizedDegree { get; set; }
    public double BetweennessCentrality { get; set; }
    public double ClosenessCentrality { get; set; }
    public double PageRank { get; set; }
    public string AnalyticalIndicator { get; set; } = "Connected Entity";
    public string CommunityId { get; set; } = string.Empty;
    public string ComponentId { get; set; } = string.Empty;
}

public class CentralityResultsDto
{
    public string CaseId { get; set; } = string.Empty;
    public int TotalNodes { get; set; }
    public string SortedBy { get; set; } = "Betweenness";
    public List<EntityCentralityMetricDto> Metrics { get; set; } = new();
}

public class CommunityClusterDto
{
    public string CommunityId { get; set; } = string.Empty;
    public string ClusterLabel { get; set; } = string.Empty;
    public int EntityCount { get; set; }
    public int RelationshipCount { get; set; }
    public double InternalDensity { get; set; }
    public Dictionary<string, int> EntityTypeDistribution { get; set; } = new();
    public List<string> EntityIds { get; set; } = new();
    public List<string> SampleEntities { get; set; } = new();
}

public class ConnectedComponentDetailDto
{
    public string ComponentId { get; set; } = string.Empty;
    public string ComponentLabel { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
    public Dictionary<string, int> EntityTypeDistribution { get; set; } = new();
    public List<string> TopNexusEntities { get; set; } = new();
    public List<string> EntityIds { get; set; } = new();
}

public class NetworkStatisticsDto
{
    public string CaseId { get; set; } = string.Empty;
    public int TotalEntities { get; set; }
    public int TotalRelationships { get; set; }
    public int ConnectedComponents { get; set; }
    public int CommunitiesCount { get; set; }
    public double AverageDegree { get; set; }
    public double NetworkDensity { get; set; }
    public double AveragePathLength { get; set; }
    public Dictionary<string, int> EntityTypeDistribution { get; set; } = new();
    public Dictionary<string, int> RelationshipTypeDistribution { get; set; } = new();
}

public class ContributingSignalDto
{
    public string SignalName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Contribution { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class LeadSignalBreakdownDto
{
    public double CosineSimilarity { get; set; }
    public int SharedNeighborsCount { get; set; }
    public List<string> SharedNeighborNames { get; set; } = new();
    public double AttentionWeight { get; set; }
    public List<ContributingSignalDto> ContributingSignals { get; set; } = new();
}

public class GraphAnalyticalLeadDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string AnalysisRunId { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string SourceEntityName { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public string LeadType { get; set; } = "POTENTIAL_RELATIONSHIP";
    public string SuggestedRelationshipType { get; set; } = "ASSOCIATE_OF";
    public double Score { get; set; }
    public string Status { get; set; } = "PENDING";
    public string ModelVersion { get; set; } = "GAT-v1.0.0";
    public LeadSignalBreakdownDto Signals { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ResultingRelationshipId { get; set; }
}

public class LeadReviewRequestDto
{
    public string Status { get; set; } = "CONFIRMED"; // CONFIRMED, DISMISSED
    public string? ReviewNotes { get; set; }
    public string? SuggestedRelationshipType { get; set; } // override e.g. ASSOCIATE_OF, COMMUNICATED_WITH
}

public class EntityAnalyticsProfileDto
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public EntityCentralityMetricDto? Metrics { get; set; }
    public CommunityClusterDto? CommunityCluster { get; set; }
    public ConnectedComponentDetailDto? ConnectedComponent { get; set; }
    public List<GraphAnalyticalLeadDto> AdjacentLeads { get; set; } = new();
}
