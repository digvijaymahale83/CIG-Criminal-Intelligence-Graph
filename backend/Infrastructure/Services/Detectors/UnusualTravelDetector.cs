using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class UnusualTravelDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<UnusualTravelDetector> _logger;

    public string DetectorType => "UNUSUAL_TRAVEL";
    public string Version => "v1.0";

    public UnusualTravelDetector(IAppDbContext dbContext, ILogger<UnusualTravelDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();

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

        // Group by entity
        var entityEventsMap = new Dictionary<string, List<(Domain.Entities.ExtractedEvent Ev, Domain.Entities.LocationItem Loc)>>();
        foreach (var ev in events)
        {
            Domain.Entities.LocationItem? loc = null;
            if (!string.IsNullOrEmpty(ev.LocationEntityId))
            {
                loc = locations.FirstOrDefault(l => l.Id == ev.LocationEntityId);
            }
            if (loc == null && !string.IsNullOrEmpty(ev.Location))
            {
                var evLoc = ev.Location.ToLower();
                loc = locations.FirstOrDefault(l => l.Name.ToLower().Contains(evLoc) || evLoc.Contains(l.Name.ToLower()));
            }

            if (loc != null)
            {
                var names = GetEntityNames(ev.RelatedEntitiesJson);
                foreach (var name in names)
                {
                    if (!entityEventsMap.TryGetValue(name, out var list))
                    {
                        list = new List<(Domain.Entities.ExtractedEvent, Domain.Entities.LocationItem)>();
                        entityEventsMap[name] = list;
                    }
                    list.Add((ev, loc));
                }
            }
        }

        foreach (var (entityName, visits) in entityEventsMap)
        {
            if (visits.Count < 2) continue;

            for (int i = 0; i < visits.Count - 1; i++)
            {
                var v1 = visits[i];
                var v2 = visits[i + 1];

                double distKm = GeoMath.HaversineDistanceKm(v1.Loc.Latitude, v1.Loc.Longitude, v2.Loc.Latitude, v2.Loc.Longitude);
                double elapsedHours = (v2.Ev.StartTimeUtc!.Value - (v1.Ev.EndTimeUtc ?? v1.Ev.StartTimeUtc!.Value)).TotalHours;

                if (distKm > 80.0 && elapsedHours >= 0.0)
                {
                    double speedKmh = GeoMath.CalculateSpeedKmh(distKm, Math.Max(0.01, elapsedHours));
                    bool isSupersonic = distKm > 100.0 && speedKmh > 900.0;

                    if (isSupersonic || (speedKmh > 400.0 && elapsedHours < 1.0))
                    {
                        double score = isSupersonic ? 0.95 : 0.78;
                        string severity = isSupersonic ? "HIGH" : "MEDIUM";

                        signals.Add(new AnomalySignalDto
                        {
                            AlertType = DetectorType,
                            Title = $"Implausible Travel Velocity: {entityName}",
                            Description = $"Recorded transition of {distKm:F1} km in {elapsedHours:F2} hours implies {speedKmh:F0} km/h travel velocity.",
                            Score = score,
                            Severity = severity,
                            DetectionMethod = "GeodesicVelocityCalculation",
                            DetectionVersion = Version,
                            Explanation = $"WHAT: Physically implausible or unusual recorded travel interval.\nWHO: {entityName}\nTRANSITION: '{v1.Loc.Name}' ({v1.Ev.StartTimeUtc:HH:mm} UTC) → '{v2.Loc.Name}' ({v2.Ev.StartTimeUtc:HH:mm} UTC)\nDISTANCE: {distKm:F1} km, ELAPSED: {elapsedHours:F2} hours, IMPLIED SPEED: {speedKmh:F0} km/h\nNOTE: May indicate timestamp inaccuracy, approximate geocoding, or simultaneous observations.",
                            SourceEntityName = entityName,
                            LocationId = v2.Loc.Id,
                            LocationName = v2.Loc.Name,
                            RelatedEventId = v2.Ev.Id,
                            RelatedEvidenceId = v2.Ev.EvidenceId,
                            RelatedEvidenceFileName = v2.Ev.Evidence?.FileName,
                            RelatedEvidenceSha256 = v2.Ev.Evidence?.Sha256Hash,
                            TimeWindowKey = $"travel-{entityName}-{v1.Loc.Id}-{v2.Loc.Id}"
                        });
                    }
                }
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
