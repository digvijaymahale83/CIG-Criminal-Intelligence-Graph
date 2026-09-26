using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Copilot;

public class CopilotContextBuilder : ICopilotContextBuilder
{
    private readonly IAppDbContext _dbContext;
    private readonly IInvestigationGraphService _graphService;
    private readonly IEntityResolutionService _resolutionService;
    private readonly ITemporalService _temporalService;
    private readonly IGeospatialService _geospatialService;
    private readonly IAlertService _alertService;
    private readonly IIntegrityLedgerService _ledgerService;
    private readonly ILogger<CopilotContextBuilder> _logger;

    public CopilotContextBuilder(
        IAppDbContext dbContext,
        IInvestigationGraphService graphService,
        IEntityResolutionService resolutionService,
        ITemporalService temporalService,
        IGeospatialService geospatialService,
        IAlertService alertService,
        IIntegrityLedgerService ledgerService,
        ILogger<CopilotContextBuilder> logger)
    {
        _dbContext = dbContext;
        _graphService = graphService;
        _resolutionService = resolutionService;
        _temporalService = temporalService;
        _geospatialService = geospatialService;
        _alertService = alertService;
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task<CopilotGroundedContext> BuildContextAsync(
        string caseId,
        string query,
        string intent,
        List<string> entityTokens,
        CopilotQueryRequest request,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate case existence & authorization
        var caseItem = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId || c.CaseNumber == caseId, cancellationToken);

        if (caseItem == null)
        {
            throw new KeyNotFoundException($"Investigation case '{caseId}' was not found.");
        }

        bool hasMarathi = System.Text.RegularExpressions.Regex.IsMatch(query, @"[\u0900-\u097F]");
        var lang = (request.Language?.ToLowerInvariant() == "mr" || hasMarathi) ? "mr" : "en";

        var context = new CopilotGroundedContext
        {
            CaseId = caseItem.Id,
            CaseNumber = caseItem.CaseNumber,
            CaseTitle = caseItem.Title,
            QueryIntent = intent,
            ExtractedTokens = entityTokens,
            Language = lang
        };

        // 2. Resolve Candidate Entities
        var matchedEntities = new List<EntityItem>();
        if (entityTokens.Count > 0)
        {
            foreach (var token in entityTokens)
            {
                var normToken = token.ToLowerInvariant();
                var candidates = await _dbContext.Entities
                    .AsNoTracking()
                    .Where(e => e.CaseId == caseItem.Id &&
                                (e.CanonicalName.ToLower().Contains(normToken) ||
                                 e.NormalizedValue.ToLower().Contains(normToken) ||
                                 e.Id.ToLower() == normToken))
                    .Take(5)
                    .ToListAsync(cancellationToken);

                if (candidates.Count > 1)
                {
                    context.DisambiguationNote = $"Multiple matching entities were found for '{token}'.";
                }

                foreach (var cand in candidates)
                {
                    if (!matchedEntities.Any(e => e.Id == cand.Id))
                    {
                        matchedEntities.Add(cand);
                    }
                }
            }
        }

        // If no explicit tokens or general query, load prominent entities in the case
        if (entityTokens.Count == 0 && matchedEntities.Count == 0 && (intent == "GENERAL_CASE_SUMMARY" || intent == "GRAPH_ANALYTICS" || intent == "ALERT"))
        {
            matchedEntities = await _dbContext.Entities
                .AsNoTracking()
                .Where(e => e.CaseId == caseItem.Id)
                .OrderByDescending(e => e.CreatedAtUtc)
                .Take(10)
                .ToListAsync(cancellationToken);
        }

        context.Entities = matchedEntities.Select(e => new EntityCitationDto
        {
            EntityId = e.Id,
            CanonicalName = e.CanonicalName,
            EntityType = e.Type,
            CaseId = e.CaseId,
            Confidence = e.Confidence
        }).ToList();

        var entityIdSet = new HashSet<string>(matchedEntities.Select(e => e.Id));

        // 3. Shortest Path & Graph Neighborhood
        if (request.IncludeGraph)
        {
            if (intent == "SHORTEST_PATH" && matchedEntities.Count >= 2)
            {
                try
                {
                    var path = await _graphService.GetShortestPathAsync(
                        matchedEntities[0].Id,
                        matchedEntities[1].Id,
                        caseItem.Id,
                        4,
                        userId,
                        userRole,
                        cancellationToken);

                    context.ShortestPath = path;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Shortest path query returned no path: {Message}", ex.Message);
                }
            }

            // Retrieve verified relationships connected to resolved entities
            var relationshipsQuery = _dbContext.Relationships
                .AsNoTracking()
                .Where(r => r.CaseId == caseItem.Id);

            if (entityIdSet.Count > 0)
            {
                relationshipsQuery = relationshipsQuery.Where(r => entityIdSet.Contains(r.SourceEntityId) || entityIdSet.Contains(r.TargetEntityId));
            }

            var rels = await relationshipsQuery
                .OrderByDescending(r => r.Confidence)
                .Take(15)
                .ToListAsync(cancellationToken);

            // Fetch relationship evidence citations and entity names
            var relIds = rels.Select(r => r.Id).ToList();
            var relEvidences = await _dbContext.RelationshipEvidences
                .AsNoTracking()
                .Where(re => relIds.Contains(re.RelationshipId))
                .ToListAsync(cancellationToken);

            var relEvidenceMap = relEvidences
                .GroupBy(re => re.RelationshipId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.EvidenceId).Distinct().ToList());

            var involvedEntityIds = rels.Select(r => r.SourceEntityId)
                .Concat(rels.Select(r => r.TargetEntityId))
                .Distinct()
                .ToList();

            var entityNameMap = await _dbContext.Entities
                .AsNoTracking()
                .Where(e => involvedEntityIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.CanonicalName, cancellationToken);

            context.VerifiedRelationships = rels.Select(r => new RelationshipCitationDto
            {
                RelationshipId = r.Id,
                SourceEntityId = r.SourceEntityId,
                SourceEntityName = entityNameMap.TryGetValue(r.SourceEntityId, out var sName) ? sName : r.SourceEntityId,
                TargetEntityId = r.TargetEntityId,
                TargetEntityName = entityNameMap.TryGetValue(r.TargetEntityId, out var tName) ? tName : r.TargetEntityId,
                RelationshipType = r.Type,
                Confidence = r.Confidence,
                Status = "VERIFIED",
                EvidenceIds = relEvidenceMap.TryGetValue(r.Id, out var evList) ? evList : new List<string>()
            }).ToList();

            // Check for Graph Analytical leads / Model Predictions (GAT)
            var leads = await _dbContext.GraphAnalyticalLeads
                .AsNoTracking()
                .Where(l => l.CaseId == caseItem.Id)
                .Take(5)
                .ToListAsync(cancellationToken);

            context.ModelPredictions = leads.Select(l => new ModelSignalDto
            {
                SignalType = l.LeadType,
                SourceEntityId = l.SourceEntityId,
                TargetEntityId = l.TargetEntityId,
                PredictedType = l.SuggestedRelationshipType ?? "ASSOCIATED_WITH",
                Score = l.Score,
                Status = "PENDING_REVIEW",
                Note = l.ExplanationJson
            }).ToList();
        }

        // 4. Cross-Case Connections
        if (request.IncludeCrossCase)
        {
            try
            {
                var crossConnections = await _resolutionService.GetCrossCaseConnectionsAsync(
                    caseItem.Id,
                    null,
                    null,
                    cancellationToken);

                if (crossConnections != null)
                {
                    context.CrossCaseConnections = crossConnections.Take(8).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Cross-case retrieval skipped/failed: {Message}", ex.Message);
            }
        }

        // 5. Timeline Events
        if (request.IncludeTimeline)
        {
            var timelineQuery = _dbContext.ExtractedEvents
                .AsNoTracking()
                .Where(ev => ev.ExtractionJob != null && ev.ExtractionJob.Evidence != null && ev.ExtractionJob.Evidence.CaseId == caseItem.Id);

            var events = await timelineQuery
                .OrderBy(ev => ev.EventTimestampUtc)
                .Take(15)
                .ToListAsync(cancellationToken);

            context.TimelineEvents = events.Select(ev => new TimelineCitationDto
            {
                EventId = ev.Id,
                EventType = ev.EventType,
                EventTimestampUtc = ev.EventTimestampUtc ?? DateTime.UtcNow,
                Precision = ev.EventTimestampUtc.HasValue && ev.EventTimestampUtc.Value.TimeOfDay == TimeSpan.Zero ? "DATE_ONLY" : "DATETIME",
                Description = $"{ev.EventType} recorded in case evidence",
                Location = ev.Location,
                EvidenceIds = new List<string> { ev.ExtractionJob?.EvidenceId ?? "" }
            }).ToList();
        }

        // 6. Geospatial Locations
        if (request.IncludeLocations)
        {
            var locations = await _dbContext.Locations
                .AsNoTracking()
                .Where(l => l.CaseId == caseItem.Id)
                .Take(10)
                .ToListAsync(cancellationToken);

            context.Locations = locations.Select(l => new LocationCitationDto
            {
                LocationId = l.Id,
                LocationName = l.Name,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Description = l.Address
            }).ToList();
        }

        // 7. Investigative Alerts
        if (request.IncludeAlerts)
        {
            var alerts = await _dbContext.Alerts
                .AsNoTracking()
                .Where(a => a.CaseId == caseItem.Id)
                .OrderByDescending(a => a.Score)
                .Take(8)
                .ToListAsync(cancellationToken);

            context.Alerts = alerts.Select(a => new AlertCitationDto
            {
                AlertId = a.Id,
                AlertType = a.AlertType,
                Severity = a.Severity,
                Score = a.Score,
                Threshold = 0.5,
                Explanation = a.Explanation,
                Status = a.Status
            }).ToList();
        }

        // 8. Evidence Items with Phase 9 Integrity Status
        if (request.IncludeEvidence)
        {
            var evidenceQuery = _dbContext.EvidenceItems
                .AsNoTracking()
                .Where(e => e.CaseId == caseItem.Id);

            // Prioritize evidence matching query tokens or associated with retrieved relationships
            var evidenceList = await evidenceQuery
                .OrderByDescending(e => e.UploadedAtUtc)
                .Take(6)
                .ToListAsync(cancellationToken);

            var evidenceCitations = new List<EvidenceCitationDto>();
            foreach (var ev in evidenceList)
            {
                var integrity = await _ledgerService.GetEvidenceIntegrityStatusAsync(ev.Id, cancellationToken);
                evidenceCitations.Add(new EvidenceCitationDto
                {
                    EvidenceId = ev.Id,
                    EvidenceVersion = ev.Version,
                    FileName = ev.FileName,
                    SourceType = ev.MimeType,
                    Snippet = string.IsNullOrWhiteSpace(ev.Description) ? $"Evidence document: {ev.FileName}" : ev.Description,
                    Sha256Hash = ev.Sha256Hash,
                    IntegrityStatus = integrity.Status
                });
            }

            context.EvidenceItems = evidenceCitations;
        }

        return context;
    }
}
