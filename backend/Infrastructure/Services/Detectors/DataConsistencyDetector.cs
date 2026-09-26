using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class DataConsistencyDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<DataConsistencyDetector> _logger;

    public string DetectorType => "DATA_CONSISTENCY";
    public string Version => "v1.0";

    public DataConsistencyDetector(IAppDbContext dbContext, ILogger<DataConsistencyDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();

        // 1. Inverted Event Timestamps (EndTime < StartTime)
        var events = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Include(e => e.Evidence)
            .Where(e => e.CaseId == caseId && e.StartTimeUtc != null && e.EndTimeUtc != null && e.EndTimeUtc < e.StartTimeUtc)
            .ToListAsync(cancellationToken);

        foreach (var ev in events)
        {
            signals.Add(new AnomalySignalDto
            {
                AlertType = DetectorType,
                Title = $"Temporal Inversion Anomaly: {ev.Description}",
                Description = $"Event end timestamp ({ev.EndTimeUtc:yyyy-MM-dd HH:mm}) precedes start timestamp ({ev.StartTimeUtc:yyyy-MM-dd HH:mm}).",
                Score = 0.88,
                Severity = "MEDIUM",
                DetectionMethod = "TemporalBoundIntegrityCheck",
                DetectionVersion = Version,
                Explanation = $"WHAT: Chronological bound inversion detected in extracted event.\nEVENT ID: {ev.Id}\nSTART: {ev.StartTimeUtc:yyyy-MM-dd HH:mm} UTC\nEND: {ev.EndTimeUtc:yyyy-MM-dd HH:mm} UTC\nACTION: Timeline review required to correct chronological bounds.",
                RelatedEventId = ev.Id,
                RelatedEvidenceId = ev.EvidenceId,
                RelatedEvidenceFileName = ev.Evidence?.FileName,
                RelatedEvidenceSha256 = ev.Evidence?.Sha256Hash,
                TimeWindowKey = $"inversion-{ev.Id}"
            });
        }

        // 2. Out of bounds coordinates on locations
        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId)
            .ToListAsync(cancellationToken);

        foreach (var loc in locations)
        {
            if (!GeoMath.IsValidCoordinate(loc.Latitude, loc.Longitude))
            {
                signals.Add(new AnomalySignalDto
                {
                    AlertType = DetectorType,
                    Title = $"Geodetic Coordinate Out of Bounds: {loc.Name}",
                    Description = $"Recorded coordinates ({loc.Latitude}, {loc.Longitude}) exceed standard WGS-84 geodesic limits [-90, 90], [-180, 180].",
                    Score = 0.95,
                    Severity = "HIGH",
                    DetectionMethod = "GeodeticCoordinateBoundCheck",
                    DetectionVersion = Version,
                    Explanation = $"WHAT: Invalid latitude or longitude recorded.\nLOCATION: {loc.Name} (ID: {loc.Id})\nCOORDINATES: ({loc.Latitude}, {loc.Longitude})\nACTION: Re-geocode or correct location entry.",
                    LocationId = loc.Id,
                    LocationName = loc.Name,
                    TimeWindowKey = $"coord-{loc.Id}"
                });
            }
        }

        return signals;
    }
}
