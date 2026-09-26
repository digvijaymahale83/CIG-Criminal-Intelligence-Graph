using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class NetworkAnomalyDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<NetworkAnomalyDetector> _logger;

    public string DetectorType => "NETWORK_ANOMALY";
    public string Version => "v1.0";

    public NetworkAnomalyDetector(IAppDbContext dbContext, ILogger<NetworkAnomalyDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();

        var entities = await _dbContext.Entities
            .AsNoTracking()
            .Where(e => e.CaseId == caseId)
            .ToListAsync(cancellationToken);

        var relationships = await _dbContext.Relationships
            .AsNoTracking()
            .Where(r => r.CaseId == caseId)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0 || relationships.Count == 0)
        {
            return signals;
        }

        // Calculate node degrees
        var degreeMap = new Dictionary<string, int>();
        foreach (var r in relationships)
        {
            degreeMap[r.SourceEntityId] = degreeMap.GetValueOrDefault(r.SourceEntityId) + 1;
            degreeMap[r.TargetEntityId] = degreeMap.GetValueOrDefault(r.TargetEntityId) + 1;
        }

        double avgDegree = degreeMap.Values.Count > 0 ? degreeMap.Values.Average() : 0.0;

        // 1. High Connectivity Nexus Entity (Degree > 2x average and degree >= 5, or degree >= 10)
        foreach (var entity in entities)
        {
            int degree = degreeMap.GetValueOrDefault(entity.Id);
            if (degree >= 5 && (degree >= avgDegree * 2.0 || degree >= 10))
            {
                double score = Math.Min(0.95, 0.60 + (degree / (avgDegree > 0 ? avgDegree : 1.0) * 0.08));
                string severity = degree >= 10 || score >= 0.85 ? "HIGH" : "MEDIUM";

                signals.Add(new AnomalySignalDto
                {
                    AlertType = DetectorType,
                    Title = $"High Connectivity Nexus Entity: {entity.CanonicalName}",
                    Description = $"Entity recorded {degree} verified relationships, substantially exceeding the case average ({avgDegree:F1}).",
                    Score = Math.Round(score, 2),
                    Severity = severity,
                    DetectionMethod = "DegreeCentralityDisparity",
                    DetectionVersion = Version,
                    Explanation = $"WHAT: High relationship density observed.\nWHO: {entity.CanonicalName} (ID: {entity.Id})\nMETRICS: Degree = {degree} (Case Avg = {avgDegree:F1})\nWHY: Marked structural hub position in the investigation knowledge graph warrants investigative review.",
                    SourceEntityId = entity.Id,
                    SourceEntityName = entity.CanonicalName,
                    TimeWindowKey = $"deg-{entity.Id}"
                });
            }
        }

        // 2. High Betweenness Centrality Bridges from GraphNodeMetrics
        var metrics = await _dbContext.GraphNodeMetrics
            .AsNoTracking()
            .Where(m => m.CaseId == caseId && m.BetweennessCentrality >= 0.35)
            .ToListAsync(cancellationToken);

        foreach (var metric in metrics)
        {
            var ent = entities.FirstOrDefault(e => e.Id == metric.EntityId);
            if (ent != null)
            {
                signals.Add(new AnomalySignalDto
                {
                    AlertType = DetectorType,
                    Title = $"Network Bridge Entity: {ent.CanonicalName}",
                    Description = $"Entity acts as a significant structural bridge with betweenness centrality of {metric.BetweennessCentrality:F3}.",
                    Score = Math.Min(0.92, 0.65 + (metric.BetweennessCentrality * 0.3)),
                    Severity = metric.BetweennessCentrality >= 0.5 ? "HIGH" : "MEDIUM",
                    DetectionMethod = "BetweennessCentralityBridge",
                    DetectionVersion = Version,
                    Explanation = $"WHAT: Critical structural intermediary role identified.\nWHO: {ent.CanonicalName}\nMETRICS: Betweenness Centrality = {metric.BetweennessCentrality:F3}, Community = {metric.CommunityId}\nWHY: Serves as a path bridge between disparate components of the investigation network.",
                    SourceEntityId = ent.Id,
                    SourceEntityName = ent.CanonicalName,
                    TimeWindowKey = $"bridge-{ent.Id}"
                });
            }
        }

        return signals;
    }
}
