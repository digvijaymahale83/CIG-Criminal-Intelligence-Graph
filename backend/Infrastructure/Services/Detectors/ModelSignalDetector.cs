using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Detectors;

public class ModelSignalDetector : IAnomalyDetector
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ModelSignalDetector> _logger;

    public string DetectorType => "MODEL_SIGNAL";
    public string Version => "v1.0";

    public ModelSignalDetector(IAppDbContext dbContext, ILogger<ModelSignalDetector> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var signals = new List<AnomalySignalDto>();

        var modelLeads = await _dbContext.GraphAnalyticalLeads
            .AsNoTracking()
            .Include(l => l.SourceEntity)
            .Include(l => l.TargetEntity)
            .Where(l => l.CaseId == caseId && l.Status == "PENDING" && l.Score >= 0.70)
            .ToListAsync(cancellationToken);

        foreach (var lead in modelLeads)
        {
            var srcName = lead.SourceEntity?.CanonicalName ?? lead.SourceEntityId;
            var tgtName = lead.TargetEntity?.CanonicalName ?? lead.TargetEntityId;

            signals.Add(new AnomalySignalDto
            {
                AlertType = DetectorType,
                Title = $"GAT Model Investigative Lead: {srcName} ↔ {tgtName}",
                Description = lead.ExplanationJson,
                Score = lead.Score,
                Severity = lead.Score >= 0.85 ? "HIGH" : "MEDIUM",
                DetectionMethod = $"GraphAttentionNetwork-{lead.ModelVersion}",
                DetectionVersion = lead.ModelVersion,
                Explanation = $"WHAT: Model-generated network relationship lead.\nENTITIES: {srcName} (ID: {lead.SourceEntityId}) and {tgtName} (ID: {lead.TargetEntityId})\nSUGGESTED RELATIONSHIP: {lead.SuggestedRelationshipType}\nCONFIDENCE: {lead.Score:P1}\nEXPLANATION: {lead.ExplanationJson}\nNOTE: Model-generated leads do NOT automatically create graph edges.",
                SourceEntityId = lead.SourceEntityId,
                SourceEntityName = srcName,
                TargetEntityId = lead.TargetEntityId,
                TargetEntityName = tgtName,
                TimeWindowKey = $"model-lead-{lead.Id}"
            });
        }

        return signals;
    }
}
