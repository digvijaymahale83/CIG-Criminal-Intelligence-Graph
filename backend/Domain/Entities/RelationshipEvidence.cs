namespace Domain.Entities;

public class RelationshipEvidence
{
    public string Id { get; set; } = string.Empty;
    public string RelationshipId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public double Confidence { get; set; } = 1.0;
    public string SourceLocation { get; set; } = string.Empty;
    public string VerifiedBy { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Relationship? Relationship { get; set; }
    public Evidence? Evidence { get; set; }
}
