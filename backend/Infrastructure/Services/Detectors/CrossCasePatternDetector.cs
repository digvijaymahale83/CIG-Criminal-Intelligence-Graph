using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class CrossCasePatternDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<CrossCasePatternDetector> _logger;

    public string DetectorType => "CROSS_CASE_PATTERN";
    public string Version => "v1.0";

    public CrossCasePatternDetector(IAppDbContext dbContext, ILogger<CrossCasePatternDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();

        if (!request.IncludeCrossCase) return signals;

        // 1. Cross-Case Entity Matches from CrossCaseConnections
        var connections = await _dbContext.CrossCaseConnections
            .AsNoTracking()
            .Include(c => c.SourceEntity)
            .Include(c => c.TargetEntity)
            .Where(c => (c.SourceCaseId == caseId || c.TargetCaseId == caseId) && c.Status != "DISMISSED")
            .ToListAsync(cancellationToken);

        foreach (var conn in connections)
        {
            string otherCaseId = conn.SourceCaseId == caseId ? conn.TargetCaseId : conn.SourceCaseId;
            string? entName = conn.SourceEntity?.CanonicalName ?? conn.TargetEntity?.CanonicalName;

            if (string.IsNullOrEmpty(entName))
            {
                var ent = await _dbContext.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == conn.SourceEntityId || e.Id == conn.TargetEntityId, cancellationToken);
                entName = ent?.CanonicalName ?? "Cross-Case Entity";
            }

            signals.Add(new AnomalySignalDto
            {
                AlertType = DetectorType,
                Title = $"Cross-Case Entity Nexus: {entName}",
                Description = $"Entity '{entName}' appears synchronously in authorized case {otherCaseId} with confidence {conn.Confidence:P0}.",
                Score = conn.Confidence,
                Severity = conn.Confidence >= 0.85 ? "HIGH" : "MEDIUM",
                DetectionMethod = "CrossCaseEntityNexusResolution",
                DetectionVersion = Version,
                Explanation = $"WHAT: Shared entity observed across distinct investigative cases.\nENTITY: {entName}\nMATCHED CASE: {otherCaseId}\nREASON: {conn.Explanation}\nWHY: Cross-case nexus indicates common operational lead.",
                SourceEntityId = conn.SourceEntityId,
                SourceEntityName = entName,
                TargetEntityId = conn.TargetEntityId,
                TimeWindowKey = $"crosscase-ent-{conn.Id}"
            });
        }

        // 2. Shared Canonical Locations across Cases
        var caseLocs = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId)
            .ToListAsync(cancellationToken);

        if (caseLocs.Count > 0)
        {
            var caseLocNames = caseLocs.Select(l => l.NormalizedName).Distinct().ToList();

            var matchingExternalLocs = await _dbContext.Locations
                .AsNoTracking()
                .Where(l => l.CaseId != caseId && l.CaseId != null && caseLocNames.Contains(l.NormalizedName))
                .ToListAsync(cancellationToken);

            foreach (var extLoc in matchingExternalLocs)
            {
                var localLoc = caseLocs.FirstOrDefault(l => l.NormalizedName == extLoc.NormalizedName);
                if (localLoc != null)
                {
                    signals.Add(new AnomalySignalDto
                    {
                        AlertType = DetectorType,
                        Title = $"Cross-Case Geographic Overlap: {localLoc.Name}",
                        Description = $"Canonical location '{localLoc.Name}' is also logged as an active site in case {extLoc.CaseId}.",
                        Score = 0.85,
                        Severity = "MEDIUM",
                        DetectionMethod = "CrossCaseLocationOverlap",
                        DetectionVersion = Version,
                        Explanation = $"WHAT: Shared physical location identified across case boundaries.\nLOCATION: {localLoc.Name} ({localLoc.City ?? "City"})\nOTHER CASE: {extLoc.CaseId}\nWHY: Indicates potential shared staging ground, meeting node, or logistics route.",
                        LocationId = localLoc.Id,
                        LocationName = localLoc.Name,
                        TimeWindowKey = $"crosscase-loc-{localLoc.Id}-{extLoc.CaseId}"
                    });
                }
            }
        }

        return signals;
    }
}
