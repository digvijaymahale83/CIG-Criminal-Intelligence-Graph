namespace Application.DTOs;

/// <summary>
/// Detailed breakdown of an individual matching signal contributing to the resolution score.
/// </summary>
public class MatchFactorDto
{
    public string Type { get; set; } = string.Empty; // SHARED_PHONE, SHARED_VEHICLE, SHARED_ACCOUNT, SHARED_EMAIL, NAME_SIMILARITY, ALIAS_MATCH, CONTEXT_SIMILARITY, CONTRADICTORY_ATTRIBUTE
    public double Weight { get; set; } = 0.0;
    public double Score { get; set; } = 0.0;
    public string Description { get; set; } = string.Empty;
    public List<SupportingEvidenceCitationDto> EvidenceCitations { get; set; } = new();
}

public class SupportingEvidenceCitationDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string EvidenceType { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string ExtractedQuote { get; set; } = string.Empty;
}

public class EntityMatchCandidateDto
{
    public string Id { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string SourceCaseId { get; set; } = string.Empty;
    public string TargetCaseId { get; set; } = string.Empty;
    public string SourceCaseNumber { get; set; } = string.Empty;
    public string TargetCaseNumber { get; set; } = string.Empty;
    public string EntityType { get; set; } = "PERSON";
    public string MatchStatus { get; set; } = "PENDING";
    public double MatchScore { get; set; } = 0.0;
    public string MatchMethod { get; set; } = "MULTI_SIGNAL";
    public List<MatchFactorDto> Factors { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // Summary of source and target entities
    public EntitySummaryDto SourceEntity { get; set; } = new();
    public EntitySummaryDto TargetEntity { get; set; } = new();
}

public class EntitySummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public string EntityType { get; set; } = "PERSON";
    public List<string> Aliases { get; set; } = new();
    public string? PhoneNumber { get; set; }
    public string? VehicleNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Comprehensive side-by-side comparison payload for investigator review.
/// </summary>
public class CandidateComparisonDto
{
    public string CandidateId { get; set; } = string.Empty;
    public double MatchScore { get; set; }
    public string MatchStatus { get; set; } = "PENDING";
    public string EntityType { get; set; } = "PERSON";
    public List<MatchFactorDto> Factors { get; set; } = new();

    public EntityComparisonSideDto SideA { get; set; } = new();
    public EntityComparisonSideDto SideB { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNotes { get; set; }
}

public class EntityComparisonSideDto
{
    public string EntityId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string CaseTitle { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public string EntityType { get; set; } = "PERSON";
    public List<string> Aliases { get; set; } = new();
    public string? PhoneNumber { get; set; }
    public string? VehicleNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public string Location { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public List<SupportingEvidenceCitationDto> SupportingEvidence { get; set; } = new();
    public List<string> ConnectedRelationships { get; set; } = new();
}

public class CandidateReviewRequestDto
{
    public string Status { get; set; } = "APPROVED"; // APPROVED, REJECTED
    public string? ReviewNotes { get; set; }
}

public class CrossCaseConnectionDto
{
    public string Id { get; set; } = string.Empty;
    public string? CandidateId { get; set; }
    public string SourceCaseId { get; set; } = string.Empty;
    public string TargetCaseId { get; set; } = string.Empty;
    public string SourceCaseNumber { get; set; } = string.Empty;
    public string TargetCaseNumber { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string SourceEntityName { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = "PERSON";
    public string ConnectionType { get; set; } = "SHARED_ENTITY";
    public double Confidence { get; set; } = 1.0;
    public string Status { get; set; } = "APPROVED";
    public string Explanation { get; set; } = string.Empty;
    public List<SupportingEvidenceCitationDto> SupportingEvidence { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
}

public class CrossCaseNetworkDto
{
    public string FocusCaseId { get; set; } = string.Empty;
    public List<GraphNodeDto> Nodes { get; set; } = new();
    public List<GraphEdgeDto> Edges { get; set; } = new();
    public List<CrossCaseConnectionDto> CrossCaseLinks { get; set; } = new();
    public int TotalCrossCaseLinks { get; set; }
    public int ConnectedCasesCount { get; set; }
}

public class BatchResolutionRequestDto
{
    public string? CaseId { get; set; }
    public List<string>? EntityTypes { get; set; }
    public double MinimumScore { get; set; } = 0.60;
}

public class BatchResolutionResultDto
{
    public int EntitiesCompared { get; set; }
    public int PotentialMatchesFound { get; set; }
    public int HighConfidenceCandidates { get; set; }
    public int CandidatesCreated { get; set; }
    public TimeSpan Duration { get; set; }
}
