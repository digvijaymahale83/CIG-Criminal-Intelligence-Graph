namespace Application.DTOs;

public class CopilotQueryRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public int MaxResults { get; set; } = 10;
    public bool IncludeCrossCase { get; set; } = true;
    public bool IncludeAlerts { get; set; } = true;
    public bool IncludeTimeline { get; set; } = true;
    public bool IncludeLocations { get; set; } = true;
    public bool IncludeGraph { get; set; } = true;
    public bool IncludeEvidence { get; set; } = true;
    public string? Language { get; set; } = "en";
}

public class EvidenceCitationDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public int EvidenceVersion { get; set; } = 1;
    public string FileName { get; set; } = string.Empty;
    public string SourceType { get; set; } = "DOCUMENT";
    public string Snippet { get; set; } = string.Empty;
    public int? Page { get; set; }
    public int? Row { get; set; }
    public string? RecordId { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string IntegrityStatus { get; set; } = "VERIFIED"; 
    // VERIFIED, EVIDENCE_MODIFIED, UNVERIFIED, MISSING_FILE
}

public class EntityCitationDto
{
    public string EntityId { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public double Confidence { get; set; } = 1.0;
}

public class RelationshipCitationDto
{
    public string RelationshipId { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string SourceEntityName { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string Status { get; set; } = "VERIFIED"; // VERIFIED, PREDICTED
    public List<string> EvidenceIds { get; set; } = new();
}

public class TimelineCitationDto
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTimestampUtc { get; set; }
    public string Precision { get; set; } = "DATETIME"; // DATETIME, DATE_ONLY
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public List<string> EvidenceIds { get; set; } = new();
}

public class LocationCitationDto
{
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
}

public class AlertCitationDto
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = "MEDIUM";
    public double Score { get; set; }
    public double Threshold { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string Status { get; set; } = "NEW";
}

public class ModelSignalDto
{
    public string SignalType { get; set; } = "GAT_EDGE_PREDICTION";
    public string SourceEntityId { get; set; } = string.Empty;
    public string SourceEntityName { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public string PredictedType { get; set; } = "ASSOCIATED_WITH";
    public double Score { get; set; }
    public string Status { get; set; } = "PENDING_REVIEW";
    public string Note { get; set; } = "Graph Attention Network algorithmic prediction. Requires human investigator review.";
}

public class CopilotClaimDto
{
    public string Text { get; set; } = string.Empty;
    public string ClaimType { get; set; } = "FACT"; // FACT, MODEL_PREDICTION, UNSUPPORTED
    public List<string> SourceIds { get; set; } = new();
    public bool IsSupported { get; set; } = true;
}

public class CopilotResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public string Confidence { get; set; } = "HIGH"; // HIGH, MEDIUM, LOW, MODEL_SIGNAL, UNKNOWN
    public double ConfidenceScore { get; set; } = 0.95;
    public string Intent { get; set; } = "GENERAL";
    public List<CopilotClaimDto> Claims { get; set; } = new();
    public List<EvidenceCitationDto> EvidenceCitations { get; set; } = new();
    public List<EntityCitationDto> EntityCitations { get; set; } = new();
    public List<RelationshipCitationDto> RelationshipCitations { get; set; } = new();
    public List<TimelineCitationDto> TimelineCitations { get; set; } = new();
    public List<TimelineCitationDto> TimelineEvents => TimelineCitations;
    public List<LocationCitationDto> LocationCitations { get; set; } = new();
    public List<AlertCitationDto> AlertCitations { get; set; } = new();
    public List<ModelSignalDto> ModelSignals { get; set; } = new();
    public List<ModelSignalDto> ModelPredictions => ModelSignals;
    public List<CrossCaseConnectionDto> CrossCaseConnections { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> IntegrityWarnings => Warnings;
    public string ConfidenceStatus => Confidence;
    public List<string> SuggestedFollowUps { get; set; } = new();
    public string ConversationId { get; set; } = string.Empty;
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CopilotGroundedContext
{
    public string CaseId { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string CaseTitle { get; set; } = string.Empty;
    public string QueryIntent { get; set; } = "GENERAL";
    public List<string> ExtractedTokens { get; set; } = new();
    public List<EntityCitationDto> Entities { get; set; } = new();
    public List<RelationshipCitationDto> VerifiedRelationships { get; set; } = new();
    public List<ModelSignalDto> ModelPredictions { get; set; } = new();
    public List<TimelineCitationDto> TimelineEvents { get; set; } = new();
    public List<LocationCitationDto> Locations { get; set; } = new();
    public List<AlertCitationDto> Alerts { get; set; } = new();
    public List<EvidenceCitationDto> EvidenceItems { get; set; } = new();
    public List<CrossCaseConnectionDto> CrossCaseConnections { get; set; } = new();
    public ShortestPathDto? ShortestPath { get; set; }
    public string? DisambiguationNote { get; set; }
    public string Language { get; set; } = "en";
}

public class CopilotConversationMessageDto
{
    public string Role { get; set; } = "user"; // user, assistant
    public string Content { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class CopilotConversationDto
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public List<CopilotConversationMessageDto> Messages { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
