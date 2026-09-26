namespace Domain.Entities;

public class Relationship
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string Type { get; set; } = "ASSOCIATED_WITH"; // CALLED, USED, VISITED, OWNED, MET, WORKED_FOR, INVOLVED_IN, LOCATED_AT, MENTIONED_IN, ASSOCIATED_WITH
    public string? SourceEvidenceId { get; set; }
    public double Confidence { get; set; } = 1.0;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RelationshipEvidence> SupportingEvidence { get; set; } = new List<RelationshipEvidence>();
}
