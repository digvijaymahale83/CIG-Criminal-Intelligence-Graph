namespace Domain.Entities;

public class ExtractedEvent
{
    public string Id { get; set; } = string.Empty;
    public string ExtractionJobId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }

    public string EventType { get; set; } = "INCIDENT";
    public string Description { get; set; } = string.Empty;

    // Temporal fields
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public DateTime? EventTimestampUtc
    {
        get => StartTimeUtc;
        set => StartTimeUtc = value;
    }
    public string TimePrecision { get; set; } = "EXACT"; // EXACT, MINUTE, HOUR, DAY, DATE_ONLY, MONTH, YEAR, UNKNOWN

    public string? Location { get; set; }
    public string? LocationEntityId { get; set; }
    public string? RelatedEntitiesJson { get; set; }
    public double Confidence { get; set; } = 1.0;
    public string SourceLocation { get; set; } = string.Empty;
    public int? SourcePage { get; set; }

    public string ReviewStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string VerificationStatus
    {
        get => ReviewStatus;
        set => ReviewStatus = value;
    }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ExtractionJob? ExtractionJob { get; set; }
    public Evidence? Evidence { get; set; }
    public Case? Case { get; set; }
}
