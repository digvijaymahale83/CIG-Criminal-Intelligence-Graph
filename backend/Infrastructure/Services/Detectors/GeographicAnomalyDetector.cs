using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class GeographicAnomalyDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<GeographicAnomalyDetector> _logger;

    public string DetectorType => "GEOGRAPHIC_ANOMALY";
    public string Version => "v1.0";

    public GeographicAnomalyDetector(IAppDbContext dbContext, ILogger<GeographicAnomalyDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();
        double distThreshold = request.GeographicDistanceThresholdKm > 0 ? request.GeographicDistanceThresholdKm : 100.0;

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId || l.CaseId == null)
            .ToListAsync(cancellationToken);

        var events = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Include(e => e.Evidence)
            .Where(e => e.CaseId == caseId && e.ReviewStatus != "REJECTED" && e.StartTimeUtc != null)
            .OrderBy(e => e.StartTimeUtc)
            .ToListAsync(cancellationToken);

        if (locations.Count == 0 || events.Count == 0) return signals;

        // Map events to locations
        var entityLocationsMap = new Dictionary<string, List<(Domain.Entities.ExtractedEvent Ev, Domain.Entities.LocationItem Loc)>>();

        foreach (var ev in events)
        {
            Domain.Entities.LocationItem? matchedLoc = null;
            if (!string.IsNullOrEmpty(ev.LocationEntityId))
            {
                matchedLoc = locations.FirstOrDefault(l => l.Id == ev.LocationEntityId);
            }
            if (matchedLoc == null && !string.IsNullOrEmpty(ev.Location))
            {
                var evLocName = ev.Location.ToLower();
                matchedLoc = locations.FirstOrDefault(l => l.Name.ToLower().Contains(evLocName) || evLocName.Contains(l.Name.ToLower()));
            }

            if (matchedLoc != null)
            {
                var names = GetEntityNames(ev.RelatedEntitiesJson);
                foreach (var name in names)
                {
                    if (!entityLocationsMap.TryGetValue(name, out var list))
                    {
                        list = new List<(Domain.Entities.ExtractedEvent, Domain.Entities.LocationItem)>();
                        entityLocationsMap[name] = list;
                    }
                    list.Add((ev, matchedLoc));
                }
            }
        }

        // 1. Detect activity outside observed historical geographic cluster
        foreach (var (entityName, locVisits) in entityLocationsMap)
        {
            if (locVisits.Count < 2) continue;

            // Compute baseline cluster from earlier visits
            var originLoc = locVisits[0].Loc;

            for (int i = 1; i < locVisits.Count; i++)
            {
                var currVisit = locVisits[i];
                double distance = GeoMath.HaversineDistanceKm(originLoc.Latitude, originLoc.Longitude, currVisit.Loc.Latitude, currVisit.Loc.Longitude);

                if (distance >= distThreshold)
                {
                    double score = Math.Min(0.94, 0.65 + (distance / 500.0 * 0.2));
                    string severity = distance >= 300.0 ? "HIGH" : "MEDIUM";

                    signals.Add(new AnomalySignalDto
                    {
                        AlertType = DetectorType,
                        Title = $"Geographic Outlier Activity: {entityName}",
                        Description = $"Recorded activity at '{currVisit.Loc.Name}' is {distance:F1} km away from baseline hub '{originLoc.Name}'.",
                        Score = Math.Round(score, 2),
                        Severity = severity,
                        DetectionMethod = "CentroidGeodesicDisparity",
                        DetectionVersion = Version,
                        Explanation = $"WHAT: Activity recorded substantially outside historical geographic baseline.\nWHO: {entityName}\nWHERE: {currVisit.Loc.Name} ({currVisit.Loc.City ?? "Unknown City"})\nBASELINE: {originLoc.Name} ({originLoc.City ?? "Unknown City"})\nDISTANCE: {distance:F1} km (Threshold: {distThreshold:F1} km)\nEVIDENCE: {currVisit.Ev.Evidence?.FileName ?? "Document"}",
                        SourceEntityName = entityName,
                        LocationId = currVisit.Loc.Id,
                        LocationName = currVisit.Loc.Name,
                        RelatedEventId = currVisit.Ev.Id,
                        RelatedEvidenceId = currVisit.Ev.EvidenceId,
                        RelatedEvidenceFileName = currVisit.Ev.Evidence?.FileName,
                        RelatedEvidenceSha256 = currVisit.Ev.Evidence?.Sha256Hash,
                        TimeWindowKey = $"geo-{entityName}-{currVisit.Loc.Id}"
                    });
                }
            }
        }

        // 2. Incorporate staged SpatialSignals
        var spatialSignals = await _dbContext.SpatialSignals
            .AsNoTracking()
            .Where(s => s.CaseId == caseId && s.Status != "DISMISSED" && s.Score >= 0.70)
            .ToListAsync(cancellationToken);

        if (spatialSignals.Count > 0)
        {
            var entityMap = await _dbContext.Entities
                .AsNoTracking()
                .Where(e => e.CaseId == caseId)
                .ToDictionaryAsync(e => e.Id, e => e.CanonicalName, cancellationToken);

            foreach (var ss in spatialSignals)
            {
                var srcName = ss.SourceEntityId != null && entityMap.TryGetValue(ss.SourceEntityId, out var sName) ? sName : (ss.SourceEntityId ?? "Unknown");
                var tgtName = ss.TargetEntityId != null && entityMap.TryGetValue(ss.TargetEntityId, out var tName) ? tName : (ss.TargetEntityId ?? "Unknown");

                signals.Add(new AnomalySignalDto
                {
                    AlertType = DetectorType,
                    Title = $"Spatial Co-Presence Signal: {ss.LocationName ?? "Site"}",
                    Description = ss.Explanation,
                    Score = ss.Score,
                    Severity = ss.Score >= 0.85 ? "HIGH" : "MEDIUM",
                    DetectionMethod = "SpatialSignalBridge",
                    DetectionVersion = Version,
                    Explanation = $"WHAT: Spatial co-presence detected.\nWHERE: {ss.LocationName ?? "Unknown Location"}\nENTITIES: {srcName} & {tgtName}\nSCORE: {ss.Score:F2}",
                    SourceEntityId = ss.SourceEntityId,
                    SourceEntityName = srcName,
                    TargetEntityId = ss.TargetEntityId,
                    TargetEntityName = tgtName,
                    LocationId = ss.LocationId,
                    LocationName = ss.LocationName,
                    TimeWindowKey = $"spatial-{ss.Id}"
                });
            }
        }

        return signals;
    }

    private static List<string> GetEntityNames(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
