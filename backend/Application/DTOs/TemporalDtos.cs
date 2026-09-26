namespace Application.DTOs;

public class TimelineEventDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string EventType { get; set; } = "INCIDENT";
    public string Description { get; set; } = string.Empty;

    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public string TimePrecision { get; set; } = "EXACT"; // EXACT, MINUTE, HOUR, DAY, DATE_ONLY, MONTH, YEAR, UNKNOWN

    public string? Location { get; set; }
    public string? LocationEntityId { get; set; }
    public List<string> RelatedEntityNames { get; set; } = new();
    public List<string> RelatedEntityIds { get; set; } = new();

    public double Confidence { get; set; } = 1.0;
    public string VerificationStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

    // Provenance
    public string? SourceEvidenceId { get; set; }
    public string? SourceEvidenceFileName { get; set; }
    public string? SourceEvidenceSha256 { get; set; }
    public bool EvidenceIntegrityVerified { get; set; }
    public string? SourceLocation { get; set; }
    public int? SourcePage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class TimelineQueryDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? EventType { get; set; }
    public string? EntityId { get; set; }
    public string? Location { get; set; }
    public string? EvidenceId { get; set; }
    public double? MinConfidence { get; set; }
    public string? VerificationStatus { get; set; } // ALL, PENDING, APPROVED, REJECTED
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class TimelineDateRangeDto
{
    public string CaseId { get; set; } = string.Empty;
    public DateTime? EarliestEventUtc { get; set; }
    public DateTime? LatestEventUtc { get; set; }
    public int TotalEvents { get; set; }
}

public class TemporalOverlapDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string? SourceEntityId { get; set; }
    public string? SourceEntityName { get; set; }
    public string? SourceEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public string? TargetEntityType { get; set; }

    public string? LocationEntityId { get; set; }
    public string? LocationName { get; set; }

    public DateTime OverlapStartUtc { get; set; }
    public DateTime OverlapEndUtc { get; set; }
    public double DurationMinutes { get; set; }

    public double Score { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string SignalType { get; set; } = "TEMPORAL_OVERLAP"; // TEMPORAL_OVERLAP, CROSS_CASE_TEMPORAL_OVERLAP
    public string Status { get; set; } = "PENDING"; // PENDING, CONFIRMED, DISMISSED

    public List<string> SupportingEvidenceIds { get; set; } = new();
    public List<string> SupportingEvidenceFileNames { get; set; } = new();

    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNotes { get; set; }
}

public class TemporalClusterDto
{
    public string ClusterId { get; set; } = string.Empty;
    public string ClusterLabel { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int EventCount { get; set; }
    public int DistinctEntitiesCount { get; set; }
    public int DistinctLocationsCount { get; set; }
    public string Intensity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH
    public List<string> KeyEntities { get; set; } = new();
    public List<string> KeyLocations { get; set; } = new();
    public List<TimelineEventDto> Events { get; set; } = new();
}

public class TemporalSequenceItemDto
{
    public int StepIndex { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string? Location { get; set; }
    public List<string> InvolvedEntities { get; set; } = new();
    public string ElapsedFromPrevious { get; set; } = string.Empty;
    public double ElapsedMinutesFromPrevious { get; set; }
}

public class TemporalSequenceDto
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int TotalSteps { get; set; }
    public DateTime? SequenceStartUtc { get; set; }
    public DateTime? SequenceEndUtc { get; set; }
    public List<TemporalSequenceItemDto> Steps { get; set; } = new();
}

public class TimelineResponseDto
{
    public string CaseId { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public DateTime? ActivePeriodStartUtc { get; set; }
    public DateTime? ActivePeriodEndUtc { get; set; }
    public List<TimelineEventDto> Events { get; set; } = new();
    public List<TemporalClusterDto> Clusters { get; set; } = new();
    public List<TemporalSequenceItemDto> SequenceHighlights { get; set; } = new();
}

public class TemporalAnalysisResultDto
{
    public string RunId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = "COMPLETED";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int TotalEventsAnalyzed { get; set; }
    public int SignalsGenerated { get; set; }
    public int OverlapsFound { get; set; }
    public int ClustersFound { get; set; }
    public List<TemporalOverlapDto> Overlaps { get; set; } = new();
    public List<TemporalClusterDto> Clusters { get; set; } = new();
    public string ExecutedBy { get; set; } = string.Empty;
}

public class ReviewTemporalSignalRequestDto
{
    public string Status { get; set; } = "CONFIRMED"; // CONFIRMED, DISMISSED
    public string? ReviewNotes { get; set; }
}

public class TemporalSummaryDto
{
    public string CaseId { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public DateTime? ActivePeriodStartUtc { get; set; }
    public DateTime? ActivePeriodEndUtc { get; set; }
    public int ActivityPeaks { get; set; }
    public int TemporalSignals { get; set; }
    public int PendingSignals { get; set; }
    public int ConfirmedSignals { get; set; }
}

public class RunTemporalAnalysisRequestDto
{
    public bool IncludeCrossCase { get; set; } = false;
    public List<string>? AuthorizedCaseIds { get; set; }
    public double MinOverlapDurationMinutes { get; set; } = 1.0;
}
