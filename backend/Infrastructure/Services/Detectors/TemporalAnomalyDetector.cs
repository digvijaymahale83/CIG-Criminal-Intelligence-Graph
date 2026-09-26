using System.Text.Json;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class TemporalAnomalyDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<TemporalAnomalyDetector> _logger;

    public string DetectorType => "TEMPORAL_ANOMALY";
    public string Version => "v1.0";

    public TemporalAnomalyDetector(IAppDbContext dbContext, ILogger<TemporalAnomalyDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();

        var events = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Include(e => e.Evidence)
            .Where(e => e.CaseId == caseId && e.ReviewStatus != "REJECTED" && e.StartTimeUtc != null)
            .OrderBy(e => e.StartTimeUtc)
            .ToListAsync(cancellationToken);

        if (events.Count == 0) return signals;

        // Group events by entity
        var entityEventsMap = new Dictionary<string, List<Domain.Entities.ExtractedEvent>>();
        foreach (var ev in events)
        {
            var names = GetEntityNames(ev.RelatedEntitiesJson);
            foreach (var name in names)
            {
                if (!entityEventsMap.TryGetValue(name, out var list))
                {
                    list = new List<Domain.Entities.ExtractedEvent>();
                    entityEventsMap[name] = list;
                }
                list.Add(ev);
            }
        }

        // 1. Entity Temporal Burst: >= 4 events within a rolling 48-hour window
        foreach (var (entityName, entEvents) in entityEventsMap)
        {
            if (entEvents.Count < 4) continue;

            for (int i = 0; i < entEvents.Count; i++)
            {
                var windowStart = entEvents[i].StartTimeUtc!.Value;
                var windowEnd = windowStart.AddHours(48);

                var burstEvents = entEvents
                    .Where(e => e.StartTimeUtc >= windowStart && e.StartTimeUtc <= windowEnd)
                    .ToList();

                if (burstEvents.Count >= 4)
                {
                    double burstScore = Math.Min(0.92, 0.60 + (burstEvents.Count * 0.05));
                    string severity = burstEvents.Count >= 8 ? "HIGH" : "MEDIUM";
                    var firstEv = burstEvents[0];

                    signals.Add(new AnomalySignalDto
                    {
                        AlertType = DetectorType,
                        Title = $"Temporal Activity Burst: {entityName}",
                        Description = $"Recorded {burstEvents.Count} distinct events within a 48-hour concentrated window.",
                        Score = Math.Round(burstScore, 2),
                        Severity = severity,
                        DetectionMethod = "RollingWindowBurstAnalysis",
                        DetectionVersion = Version,
                        Explanation = $"WHAT: Unusually concentrated event frequency detected.\nWHO: {entityName}\nWHEN: {windowStart:yyyy-MM-dd HH:mm} UTC to {windowEnd:yyyy-MM-dd HH:mm} UTC\nCOUNT: {burstEvents.Count} events in 48 hours\nEVIDENCE: Source documents ({firstEv.Evidence?.FileName ?? "Document"})",
                        SourceEntityName = entityName,
                        RelatedEventId = firstEv.Id,
                        RelatedEvidenceId = firstEv.EvidenceId,
                        RelatedEvidenceFileName = firstEv.Evidence?.FileName,
                        RelatedEvidenceSha256 = firstEv.Evidence?.Sha256Hash,
                        LocationName = firstEv.Location,
                        TimeWindowKey = $"burst-{entityName}-{windowStart:yyyyMMdd}"
                    });

                    // Skip past this window to avoid duplicate adjacent sub-windows
                    i += burstEvents.Count - 1;
                }
            }
        }

        // 2. Incorporate confirmed or high-scoring Temporal Signals (coincident co-presence)
        var temporalSignals = await _dbContext.TemporalSignals
            .AsNoTracking()
            .Include(t => t.SourceEntity)
            .Include(t => t.TargetEntity)
            .Where(t => t.CaseId == caseId && t.Status != "DISMISSED" && t.Score >= 0.70)
            .ToListAsync(cancellationToken);

        foreach (var ts in temporalSignals)
        {
            var srcName = ts.SourceEntity?.CanonicalName ?? ts.SourceEntityId ?? "Unknown Entity";
            var tgtName = ts.TargetEntity?.CanonicalName ?? ts.TargetEntityId ?? "Unknown Entity";

            signals.Add(new AnomalySignalDto
            {
                AlertType = DetectorType,
                Title = $"Coincident Activity Window: {srcName} & {tgtName}",
                Description = ts.Explanation,
                Score = ts.Score,
                Severity = ts.Score >= 0.85 ? "HIGH" : "MEDIUM",
                DetectionMethod = "TemporalOverlapSignalBridge",
                DetectionVersion = Version,
                Explanation = $"WHAT: Temporal co-occurrence signal staged.\nENTITIES: {srcName} and {tgtName}\nWINDOW: {ts.StartTimeUtc:yyyy-MM-dd HH:mm} to {ts.EndTimeUtc:yyyy-MM-dd HH:mm} UTC ({ts.DurationMinutes:F0} mins)\nWHY: Overlapping presence warrants timeline review.",
                SourceEntityId = ts.SourceEntityId,
                SourceEntityName = srcName,
                TargetEntityId = ts.TargetEntityId,
                TargetEntityName = tgtName,
                LocationId = ts.LocationEntityId,
                LocationName = ts.LocationName,
                TimeWindowKey = $"coincide-{ts.Id}"
            });
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
