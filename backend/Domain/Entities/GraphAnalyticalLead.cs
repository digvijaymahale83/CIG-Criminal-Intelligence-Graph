namespace Domain.Entities;

public class GraphAnalyticalLead
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string AnalysisRunId { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string LeadType { get; set; } = "POTENTIAL_RELATIONSHIP"; // POTENTIAL_RELATIONSHIP, COMMUNICATION_LINK, ASSOCIATION_LEAD
    public string SuggestedRelationshipType { get; set; } = "ASSOCIATE_OF";
    public double Score { get; set; } = 0.0;
    public string Status { get; set; } = "PENDING"; // PENDING, CONFIRMED, DISMISSED
    public string ModelVersion { get; set; } = "GAT-v1.0.0";
    public string ExplanationJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ResultingRelationshipId { get; set; }

    // Navigation
    public Case? Case { get; set; }
    public GraphAnalysisRun? AnalysisRun { get; set; }
    public EntityItem? SourceEntity { get; set; }
    public EntityItem? TargetEntity { get; set; }
    public Relationship? ResultingRelationship { get; set; }
}
