using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class DashboardDto
{
    public CaseDto Case { get; set; } = new();
    public DashboardSummaryDto Summary { get; set; } = new();
    public DashboardNetworkDto Network { get; set; } = new();
    public List<DashboardSignalDto> Signals { get; set; } = new();
    public List<DashboardTimelineEventDto> Timeline { get; set; } = new();
    public List<DashboardLocationDto> Locations { get; set; } = new();
    public DashboardAlertsDto Alerts { get; set; } = new();
    public DashboardCrossCaseDto CrossCase { get; set; } = new();
    public DashboardIntegrityDto Integrity { get; set; } = new();
    public List<DashboardActionItemDto> Actions { get; set; } = new();
    public List<DashboardActivityDto> RecentActivity { get; set; } = new();
    public List<DashboardDataQualityWarningDto> DataQualityWarnings { get; set; } = new();
    public string ResponsibleAiNotice { get; set; } = "Analytical and model-generated signals are investigative leads and require human verification. They do not establish guilt or wrongdoing.";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DashboardSummaryDto
{
    public int EntityCount { get; set; }
    public int RelationshipCount { get; set; }
    public int EvidenceCount { get; set; }
    public int AlertCount { get; set; }
    public int HighRiskAlerts { get; set; }
    public int CrossCaseTotalCount { get; set; }
    public int CrossCaseConfirmedCount { get; set; }
    public int CrossCasePotentialCount { get; set; }
    public int CrossCaseModelCount { get; set; }
    public int ModelSignalCount { get; set; }
    public int IntegrityVerifiedCount { get; set; }
    public int IntegrityModifiedCount { get; set; }
    public int IntegrityUnreconciledCount { get; set; }
}

public class DashboardNetworkDto
{
    public int NodeCount { get; set; }
    public int RelationshipCount { get; set; }
    public int ComponentCount { get; set; }
    public List<TopConnectedEntityDto> TopConnectedEntities { get; set; } = new();
    public List<BridgeEntityDto> BridgeEntities { get; set; } = new();
    public List<NetworkNodePreviewDto> Nodes { get; set; } = new();
    public List<NetworkEdgePreviewDto> Edges { get; set; } = new();
}

public class TopConnectedEntityDto
{
    public string EntityId { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int Degree { get; set; }
}

public class BridgeEntityDto
{
    public string EntityId { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public double BetweennessScore { get; set; }
}

public class NetworkNodePreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "VERIFIED";
    public int Degree { get; set; }
}

public class NetworkEdgePreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string Status { get; set; } = "VERIFIED";
}

public class DashboardSignalDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // ALERT, CROSS_CASE, MODEL_SIGNAL, SPATIAL_ANOMALY, TEMPORAL_ANOMALY
    public string Priority { get; set; } = "HIGH"; // CRITICAL, HIGH, MEDIUM, LOW
    public string WhyItMatters { get; set; } = string.Empty;
    public int EvidenceCount { get; set; }
    public string Status { get; set; } = "PENDING_REVIEW";
    public double? ModelScore { get; set; }
    public string? ModelName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ActionUrl { get; set; } = string.Empty;
}

public class DashboardTimelineEventDto
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTimestampUtc { get; set; }
    public string Precision { get; set; } = "DATETIME"; // DATETIME, DATE_ONLY, TIME_UNAVAILABLE
    public string FormattedTime { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public List<string> EvidenceIds { get; set; } = new();
}

public class DashboardLocationDto
{
    public string LocationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool HasCoordinates { get; set; }
    public string CoordinateDisplay { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
    public DateTime? LastActivityUtc { get; set; }
}

public class DashboardAlertsDto
{
    public DashboardAlertSeverityCountsDto BySeverity { get; set; } = new();
    public DashboardAlertStatusCountsDto ByStatus { get; set; } = new();
    public List<AlertDto> HighPriorityAlerts { get; set; } = new();
}

public class DashboardAlertSeverityCountsDto
{
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
}

public class DashboardAlertStatusCountsDto
{
    public int NewCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int ResolvedCount { get; set; }
}

public class DashboardCrossCaseDto
{
    public int ConfirmedCount { get; set; }
    public int PotentialCount { get; set; }
    public int ModelPredictedCount { get; set; }
    public List<CrossCaseItemDto> Connections { get; set; } = new();
}

public class CrossCaseItemDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public string SourceCaseId { get; set; } = string.Empty;
    public string TargetCaseId { get; set; } = string.Empty;
    public string TargetCaseNumber { get; set; } = string.Empty;
    public string TargetCaseTitle { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int SupportingEvidenceCount { get; set; }
    public string Status { get; set; } = "PENDING_REVIEW"; // CONFIRMED, POTENTIAL, MODEL_PREDICTED
    public double Confidence { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
}

public class DashboardIntegrityDto
{
    public int VerifiedCount { get; set; }
    public int ModifiedCount { get; set; }
    public int UnreconciledCount { get; set; }
    public int TotalEvidenceCount { get; set; }
    public string LedgerHealthStatus { get; set; } = "VALID"; // VALID, WARNING, COMPROMISED
    public List<IntegrityWarningItemDto> Warnings { get; set; } = new();
}

public class IntegrityWarningItemDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ExpectedSha256 { get; set; } = string.Empty;
    public string CurrentSha256 { get; set; } = string.Empty;
    public long? LedgerBlockIndex { get; set; }
    public string Status { get; set; } = "EVIDENCE_MODIFIED";
    public string Explanation { get; set; } = string.Empty;
}

public class DashboardActionItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // ENTITY_MATCH, ALERT_VERIFICATION, MODEL_SIGNAL, EVIDENCE_REVIEW, INTEGRITY_VERIFY, CROSS_CASE
    public string Priority { get; set; } = "HIGH";
    public string Description { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string ActionLabel { get; set; } = "Review";
    public string ActionUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DashboardActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class DashboardDataQualityWarningDto
{
    public string Type { get; set; } = string.Empty; // TIMESTAMP_CONFLICT, MISSING_COORDINATES, UNVERIFIED_RELATIONSHIP, MISSING_PROVENANCE
    public string Message { get; set; } = string.Empty;
    public string? AffectedEntityId { get; set; }
    public string? AffectedEvidenceId { get; set; }
    public string ActionUrl { get; set; } = string.Empty;
}
