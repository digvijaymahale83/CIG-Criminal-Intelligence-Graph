using System.Text.Json;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class RelationshipSurgeDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<RelationshipSurgeDetector> _logger;

    public string DetectorType => "RELATIONSHIP_SURGE";
    public string Version => "v1.0";

    public RelationshipSurgeDetector(IAppDbContext dbContext, ILogger<RelationshipSurgeDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();
        int surgeThreshold = request.RelationshipSurgeThreshold > 0 ? request.RelationshipSurgeThreshold : 4;

        var events = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Include(e => e.Evidence)
            .Where(e => e.CaseId == caseId && e.ReviewStatus != "REJECTED" && e.StartTimeUtc != null)
            .OrderBy(e => e.StartTimeUtc)
            .ToListAsync(cancellationToken);

        if (events.Count == 0) return signals;

        // Find events with multiple entities
        var pairEventsMap = new Dictionary<string, List<Domain.Entities.ExtractedEvent>>();

        foreach (var ev in events)
        {
            var names = GetEntityNames(ev.RelatedEntitiesJson).Distinct().OrderBy(n => n).ToList();
            if (names.Count >= 2)
            {
                for (int i = 0; i < names.Count; i++)
                {
                    for (int j = i + 1; j < names.Count; j++)
                    {
                        var pairKey = $"{names[i]}<->{names[j]}";
                        if (!pairEventsMap.TryGetValue(pairKey, out var list))
                        {
                            list = new List<Domain.Entities.ExtractedEvent>();
                            pairEventsMap[pairKey] = list;
                        }
                        list.Add(ev);
                    }
                }
            }
        }

        foreach (var (pairKey, pairEvents) in pairEventsMap)
        {
            if (pairEvents.Count < surgeThreshold) continue;

            // Check rolling 48-hour windows
            for (int i = 0; i < pairEvents.Count; i++)
            {
                var start = pairEvents[i].StartTimeUtc!.Value;
                var end = start.AddHours(48);

                var inWindow = pairEvents.Where(e => e.StartTimeUtc >= start && e.StartTimeUtc <= end).ToList();
                if (inWindow.Count >= surgeThreshold)
                {
                    var parts = pairKey.Split("<->");
                    string entA = parts[0];
                    string entB = parts.Length > 1 ? parts[1] : "Unknown";
                    var firstEv = inWindow[0];

                    double score = Math.Min(0.95, 0.65 + (inWindow.Count * 0.05));
                    string severity = inWindow.Count >= 8 ? "HIGH" : "MEDIUM";

                    signals.Add(new AnomalySignalDto
                    {
                        AlertType = DetectorType,
                        Title = $"Relationship Interaction Surge: {entA} & {entB}",
                        Description = $"Recorded {inWindow.Count} direct joint events/communications within a 48-hour window.",
                        Score = Math.Round(score, 2),
                        Severity = severity,
                        DetectionMethod = "DyadicRollingFrequencySurge",
                        DetectionVersion = Version,
                        Explanation = $"WHAT: Surge in co-recorded interactions.\nENTITIES: {entA} & {entB}\nCOUNT: {inWindow.Count} events within 48h (Threshold: {surgeThreshold})\nEVIDENCE: {firstEv.Evidence?.FileName ?? "Document"}",
                        SourceEntityName = entA,
                        TargetEntityName = entB,
                        RelatedEventId = firstEv.Id,
                        RelatedEvidenceId = firstEv.EvidenceId,
                        RelatedEvidenceFileName = firstEv.Evidence?.FileName,
                        RelatedEvidenceSha256 = firstEv.Evidence?.Sha256Hash,
                        LocationName = firstEv.Location,
                        TimeWindowKey = $"surge-{pairKey}-{start:yyyyMMdd}"
                    });

                    i += inWindow.Count - 1;
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
