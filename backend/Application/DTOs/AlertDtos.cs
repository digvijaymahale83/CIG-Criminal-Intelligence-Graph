namespace Application.DTOs;

public class AnomalySignalDto
{
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Severity { get; set; } = "MEDIUM";
    public string DetectionMethod { get; set; } = string.Empty;
    public string DetectionVersion { get; set; } = "v1.0";
    public string Explanation { get; set; } = string.Empty;

    // Contextual associations
    public string? SourceEntityId { get; set; }
    public string? SourceEntityName { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? RelatedEventId { get; set; }
    public string? RelatedEvidenceId { get; set; }
    public string? RelatedEvidenceFileName { get; set; }
    public string? RelatedEvidenceSha256 { get; set; }

    // Deduplication key components
    public string? TimeWindowKey { get; set; }
}

public class AlertDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string? AlertRunId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = "MEDIUM";
    public string Status { get; set; } = "NEW";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? SourceEntityId { get; set; }
    public string? SourceEntityName { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }

    public string? LocationId { get; set; }
    public string? LocationName { get; set; }

    public string? RelatedEventId { get; set; }
    public string? RelatedEvidenceId { get; set; }
    public string? RelatedEvidenceFileName { get; set; }
    public string? RelatedEvidenceSha256 { get; set; }

    public double Score { get; set; }
    public string DetectionMethod { get; set; } = string.Empty;
    public string DetectionVersion { get; set; } = "v1.0";
    public string Explanation { get; set; } = string.Empty;
    public string DeduplicationFingerprint { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
}

public class AlertQueryDto
{
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? AlertType { get; set; }
    public string? EntityId { get; set; }
    public string? Search { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AlertSummaryDto
{
    public string CaseId { get; set; } = string.Empty;
    public int TotalAlerts { get; set; }
    public int NewAlerts { get; set; }
    public int AcknowledgedAlerts { get; set; }
    public int UnderReviewAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int DismissedAlerts { get; set; }

    // Severity Breakdown
    public int CriticalSeverity { get; set; }
    public int HighSeverity { get; set; }
    public int MediumSeverity { get; set; }
    public int LowSeverity { get; set; }

    // Type Breakdown
    public int NetworkAnomalies { get; set; }
    public int TemporalAnomalies { get; set; }
    public int GeographicAnomalies { get; set; }
    public int RelationshipSurges { get; set; }
    public int ActivitySpikes { get; set; }
    public int UnusualTravel { get; set; }
    public int DataConsistency { get; set; }
    public int CrossCasePatterns { get; set; }
    public int ModelSignals { get; set; }
}

public class AlertRunResultDto
{
    public string RunId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = "COMPLETED";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int DetectorsExecuted { get; set; }
    public int SignalsGenerated { get; set; }
    public int AlertsCreated { get; set; }
    public int AlertsDeduplicated { get; set; }
    public long ExecutionDurationMs { get; set; }
    public List<AlertDto> CreatedAlerts { get; set; } = new();
}

public class RunAlertDetectionRequestDto
{
    public List<string>? EnabledDetectors { get; set; }
    public bool IncludeCrossCase { get; set; } = false;
    public List<string>? AuthorizedCaseIds { get; set; }
    public double TemporalSpikeThreshold { get; set; } = 3.0; // multiplier or z-score
    public double GeographicDistanceThresholdKm { get; set; } = 100.0;
    public int RelationshipSurgeThreshold { get; set; } = 5;
}

public class ReviewAlertRequestDto
{
    public string? Notes { get; set; }
}
