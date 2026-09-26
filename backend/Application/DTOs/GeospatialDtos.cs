using Application.DTOs;

namespace Application.DTOs;

public class LocationDto
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string? EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "India";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string GeocodePrecision { get; set; } = "CITY";
    public string Source { get; set; } = "SOURCE_DATA";
    public int EventCount { get; set; } = 0;
    public int EntityCount { get; set; } = 0;
    public int EvidenceCount { get; set; } = 0;
    public DateTime CreatedAtUtc { get; set; }
}

public class SpatialClusterDto
{
    public string ClusterId { get; set; } = string.Empty;
    public string ClusterLabel { get; set; } = string.Empty;
    public double CentroidLatitude { get; set; }
    public double CentroidLongitude { get; set; }
    public int LocationCount { get; set; }
    public int EventCount { get; set; }
    public int EntityCount { get; set; }
    public List<LocationDto> Locations { get; set; } = new();
    public List<string> KeyEntities { get; set; } = new();
}

public class CaseMapDto
{
    public string CaseId { get; set; } = string.Empty;
    public int TotalLocations { get; set; }
    public int TotalEvents { get; set; }
    public int TotalSignals { get; set; }
    public List<LocationDto> Locations { get; set; } = new();
    public List<SpatialClusterDto> Clusters { get; set; } = new();
    public List<SpatialSignalDto> ActiveSignals { get; set; } = new();
}

public class LocationActivityEntitySummaryDto
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int RecordedVisits { get; set; }
    public DateTime? FirstObservedUtc { get; set; }
    public DateTime? LastObservedUtc { get; set; }
}

public class LocationEvidenceCitationDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string? SourceLocation { get; set; }
    public int? Page { get; set; }
    public bool EvidenceIntegrityVerified { get; set; }
}

public class LocationActivityDto
{
    public LocationDto Location { get; set; } = new();
    public List<TimelineEventDto> Events { get; set; } = new();
    public List<LocationActivityEntitySummaryDto> Entities { get; set; } = new();
    public List<LocationEvidenceCitationDto> EvidenceRecords { get; set; } = new();
    public DateTime? ActivePeriodStartUtc { get; set; }
    public DateTime? ActivePeriodEndUtc { get; set; }
}

public class SpatialProximityResultDto
{
    public string LocationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceKm { get; set; }
    public string GeocodePrecision { get; set; } = "CITY";
    public int EventCount { get; set; }
    public int EntityCount { get; set; }
}

public class TravelSequenceStepDto
{
    public int StepIndex { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceKmFromPrevious { get; set; }
    public double ElapsedHoursFromPrevious { get; set; }
    public string ElapsedFormatted { get; set; } = "Origin";
    public double ImpliedSpeedKmh { get; set; }
    public bool IsImplausibleSpeed { get; set; }
}

public class TravelSequenceDto
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int TotalSteps { get; set; }
    public double TotalDistanceKm { get; set; }
    public DateTime? SequenceStartUtc { get; set; }
    public DateTime? SequenceEndUtc { get; set; }
    public List<TravelSequenceStepDto> Steps { get; set; } = new();
}

public class SpatialSignalDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string? AnalysisRunId { get; set; }
    public string SignalType { get; set; } = string.Empty;
    public string? SourceEntityId { get; set; }
    public string? SourceEntityName { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public double DistanceKm { get; set; }
    public double Score { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public List<string> SupportingEvidenceIds { get; set; } = new();
    public List<string> SupportingEvidenceFileNames { get; set; } = new();
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
}

public class SpatialAnalysisResultDto
{
    public string RunId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int TotalLocationsAnalyzed { get; set; }
    public int TotalEventsAnalyzed { get; set; }
    public int SignalsGenerated { get; set; }
    public int OverlapsFound { get; set; }
    public int ClustersFound { get; set; }
    public int VelocityWarningsFound { get; set; }
    public List<SpatialSignalDto> Signals { get; set; } = new();
    public List<SpatialClusterDto> Clusters { get; set; } = new();
    public string ExecutedBy { get; set; } = string.Empty;
}

public class RunSpatialAnalysisRequestDto
{
    public bool IncludeCrossCase { get; set; } = false;
    public List<string>? AuthorizedCaseIds { get; set; }
    public double ClusterRadiusKm { get; set; } = 25.0;
    public double VelocityWarningThresholdKmh { get; set; } = 900.0;
}

public class ReviewSpatialSignalRequestDto
{
    public string Status { get; set; } = "CONFIRMED"; // CONFIRMED, DISMISSED
    public string? ReviewNotes { get; set; }
}
