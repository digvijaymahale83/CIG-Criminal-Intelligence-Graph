using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class ActivitySpikeDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ActivitySpikeDetector> _logger;

    public string DetectorType => "ACTIVITY_SPIKE";
    public string Version => "v1.0";

    public ActivitySpikeDetector(IAppDbContext dbContext, ILogger<ActivitySpikeDetector> logger)
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

        if (events.Count < 5) return signals;

        // Group events by Calendar Date (UTC)
        var dailyCounts = events
            .GroupBy(e => e.StartTimeUtc!.Value.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (dailyCounts.Count < 2) return signals;

        var counts = dailyCounts.Values.Select(v => (double)v.Count).ToList();
        double mean = counts.Average();
        double variance = counts.Select(c => Math.Pow(c - mean, 2)).Average();
        double stdDev = Math.Sqrt(variance);

        foreach (var (date, dayEvents) in dailyCounts)
        {
            double count = dayEvents.Count;
            double zScore = stdDev > 0.0001 ? (count - mean) / stdDev : 0.0;
            double ratio = mean > 0 ? count / mean : count;

            // Spike condition: zScore >= 2.0 or (ratio >= 2.5 and count >= 5)
            if (count >= 5 && (zScore >= 2.0 || ratio >= 2.5))
            {
                double score = Math.Min(0.96, 0.65 + (Math.Max(zScore, 2.0) * 0.08));
                string severity = zScore >= 3.0 || count >= 12 ? "CRITICAL" : "HIGH";
                var sampleEv = dayEvents[0];

                signals.Add(new AnomalySignalDto
                {
                    AlertType = DetectorType,
                    Title = $"Case Activity Spike: {date:yyyy-MM-dd}",
                    Description = $"Recorded {count} events on {date:yyyy-MM-dd}, significantly exceeding the historical mean ({mean:F1} events/day).",
                    Score = Math.Round(score, 2),
                    Severity = severity,
                    DetectionMethod = "ZScoreDailyAggregateSpike",
                    DetectionVersion = Version,
                    Explanation = $"WHAT: Statistically significant case activity spike.\nDATE: {date:yyyy-MM-dd}\nCOUNT: {count} events (Historical Mean = {mean:F1}, StdDev = {stdDev:F2})\nMETRICS: Z-Score = {zScore:F2}, Spike Ratio = {ratio:F1}x baseline\nWHY: Indicates sudden surge in documented interactions or enforcement actions.",
                    RelatedEventId = sampleEv.Id,
                    RelatedEvidenceId = sampleEv.EvidenceId,
                    RelatedEvidenceFileName = sampleEv.Evidence?.FileName,
                    RelatedEvidenceSha256 = sampleEv.Evidence?.Sha256Hash,
                    TimeWindowKey = $"spike-{date:yyyyMMdd}"
                });
            }
        }

        return signals;
    }
}
