using System.Text.Json;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class TemporalService : ITemporalService
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<TemporalService> _logger;

    public TemporalService(
        AppDbContext dbContext,
        IAuditService auditService,
        ILogger<TemporalService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<TimelineResponseDto> GetCaseTimelineAsync(
        string caseId,
        TimelineQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var caseItem = await _dbContext.Cases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        if (caseItem == null)
        {
            throw new KeyNotFoundException($"Case '{caseId}' not found.");
        }

        var eventsQuery = _dbContext.ExtractedEvents
            .Include(e => e.Evidence)
            .AsNoTracking()
            .Where(e => e.CaseId == caseId);

        // Filters
        if (query.StartDate.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => (e.StartTimeUtc ?? DateTime.MinValue) >= query.StartDate.Value);
        }
        if (query.EndDate.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => (e.StartTimeUtc ?? DateTime.MaxValue) <= query.EndDate.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.EventType) && query.EventType != "ALL")
        {
            eventsQuery = eventsQuery.Where(e => e.EventType.ToUpper() == query.EventType.ToUpper());
        }
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            eventsQuery = eventsQuery.Where(e => e.Location != null && e.Location.ToLower().Contains(query.Location.ToLower()));
        }
        if (!string.IsNullOrWhiteSpace(query.EvidenceId))
        {
            eventsQuery = eventsQuery.Where(e => e.EvidenceId == query.EvidenceId);
        }
        if (query.MinConfidence.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.Confidence >= query.MinConfidence.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.VerificationStatus) && query.VerificationStatus != "ALL")
        {
            eventsQuery = eventsQuery.Where(e => e.ReviewStatus.ToUpper() == query.VerificationStatus.ToUpper());
        }

        var allMatchingEvents = await eventsQuery
            .OrderBy(e => e.StartTimeUtc ?? DateTime.MinValue)
            .ThenBy(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        // Filter by entity if specified
        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var entity = await _dbContext.Entities.AsNoTracking()
                .FirstOrDefaultAsync(ent => ent.Id == query.EntityId, cancellationToken);
            var entityName = entity?.CanonicalName?.ToLower() ?? query.EntityId.ToLower();
            var targetId = query.EntityId.ToLower();

            allMatchingEvents = allMatchingEvents.Where(e =>
                (e.RelatedEntitiesJson != null && (e.RelatedEntitiesJson.ToLower().Contains(targetId) || e.RelatedEntitiesJson.ToLower().Contains(entityName))) ||
                (e.LocationEntityId != null && e.LocationEntityId.ToLower() == targetId)
            ).ToList();
        }

        var totalEvents = allMatchingEvents.Count;
        var pagedEvents = allMatchingEvents
            .Skip((Math.Max(1, query.Page) - 1) * Math.Max(1, query.PageSize))
            .Take(Math.Max(1, query.PageSize))
            .Select(MapToTimelineEventDto)
            .ToList();

        // Calculate active period
        var datedEvents = allMatchingEvents.Where(e => e.StartTimeUtc.HasValue).ToList();
        DateTime? activeStart = datedEvents.Any() ? datedEvents.Min(e => e.StartTimeUtc) : null;
        DateTime? activeEnd = datedEvents.Any() ? datedEvents.Max(e => e.EndTimeUtc ?? e.StartTimeUtc) : null;

        // Compute clusters
        var clusters = ComputeTemporalClusters(allMatchingEvents);

        // Compute sequence highlights
        var sequenceHighlights = ComputeSequenceHighlights(allMatchingEvents);

        await _auditService.LogAsync(
            "system",
            "Investigator",
            "TIMELINE_VIEWED",
            "Case",
            caseId,
            $"Investigator viewed timeline for case '{caseId}' ({totalEvents} events retrieved).",
            null,
            null,
            cancellationToken);

        return new TimelineResponseDto
        {
            CaseId = caseId,
            TotalEvents = totalEvents,
            Page = query.Page,
            PageSize = query.PageSize,
            ActivePeriodStartUtc = activeStart,
            ActivePeriodEndUtc = activeEnd,
            Events = pagedEvents,
            Clusters = clusters,
            SequenceHighlights = sequenceHighlights
        };
    }

    public async Task<List<TimelineEventDto>> GetCaseTimelineEventsAsync(
        string caseId,
        TimelineQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var response = await GetCaseTimelineAsync(caseId, query, cancellationToken);
        return response.Events;
    }

    public async Task<List<TimelineEventDto>> GetEntityTimelineAsync(
        string entityId,
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Entities.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);

        var query = _dbContext.ExtractedEvents
            .Include(e => e.Evidence)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            query = query.Where(e => e.CaseId == caseId);
        }

        var events = await query
            .OrderBy(e => e.StartTimeUtc ?? DateTime.MinValue)
            .ToListAsync(cancellationToken);

        var entityName = entity?.CanonicalName?.ToLower() ?? entityId.ToLower();
        var targetId = entityId.ToLower();

        return events
            .Where(e =>
                (e.RelatedEntitiesJson != null && (e.RelatedEntitiesJson.ToLower().Contains(targetId) || e.RelatedEntitiesJson.ToLower().Contains(entityName))) ||
                (e.LocationEntityId != null && e.LocationEntityId.ToLower() == targetId))
            .Select(MapToTimelineEventDto)
            .ToList();
    }

    public async Task<List<TimelineEventDto>> GetRelationshipTimelineAsync(
        string relationshipId,
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var rel = await _dbContext.Relationships.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == relationshipId, cancellationToken);

        if (rel == null)
        {
            return new List<TimelineEventDto>();
        }

        var sourceEntity = await _dbContext.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == rel.SourceEntityId, cancellationToken);
        var targetEntity = await _dbContext.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == rel.TargetEntityId, cancellationToken);

        var srcName = sourceEntity?.CanonicalName?.ToLower() ?? rel.SourceEntityId.ToLower();
        var tgtName = targetEntity?.CanonicalName?.ToLower() ?? rel.TargetEntityId.ToLower();

        var query = _dbContext.ExtractedEvents
            .Include(e => e.Evidence)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            query = query.Where(e => e.CaseId == caseId);
        }

        var events = await query
            .OrderBy(e => e.StartTimeUtc ?? DateTime.MinValue)
            .ToListAsync(cancellationToken);

        return events
            .Where(e => e.RelatedEntitiesJson != null &&
                        (e.RelatedEntitiesJson.ToLower().Contains(srcName) || e.RelatedEntitiesJson.ToLower().Contains(rel.SourceEntityId.ToLower())) &&
                        (e.RelatedEntitiesJson.ToLower().Contains(tgtName) || e.RelatedEntitiesJson.ToLower().Contains(rel.TargetEntityId.ToLower())))
            .Select(MapToTimelineEventDto)
            .ToList();
    }

    public async Task<TimelineDateRangeDto> GetTimelineDateRangeAsync(
        string caseId,
        CancellationToken cancellationToken = default)
    {
        var events = await _dbContext.ExtractedEvents.AsNoTracking()
            .Where(e => e.CaseId == caseId && e.StartTimeUtc != null)
            .ToListAsync(cancellationToken);

        if (!events.Any())
        {
            return new TimelineDateRangeDto
            {
                CaseId = caseId,
                TotalEvents = 0
            };
        }

        return new TimelineDateRangeDto
        {
            CaseId = caseId,
            TotalEvents = events.Count,
            EarliestEventUtc = events.Min(e => e.StartTimeUtc),
            LatestEventUtc = events.Max(e => e.EndTimeUtc ?? e.StartTimeUtc)
        };
    }

    public async Task<TemporalAnalysisResultDto> RunTemporalAnalysisAsync(
        string caseId,
        RunTemporalAnalysisRequestDto request,
        string executedBy,
        CancellationToken cancellationToken = default)
    {
        var caseItem = await _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        if (caseItem == null)
        {
            throw new KeyNotFoundException($"Case '{caseId}' not found.");
        }

        await _auditService.LogAsync(
            executedBy,
            executedBy,
            "TEMPORAL_ANALYSIS_STARTED",
            "Case",
            caseId,
            $"Investigator triggered temporal analysis for case '{caseId}'.",
            null,
            null,
            cancellationToken);

        var runId = $"tar-{Guid.NewGuid().ToString("N")[..8]}";
        var run = new TemporalAnalysisRun
        {
            Id = runId,
            CaseId = caseId,
            Status = "RUNNING",
            StartedAtUtc = DateTime.UtcNow,
            ExecutedBy = executedBy
        };
        _dbContext.TemporalAnalysisRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Load case events with timestamps
        var events = await _dbContext.ExtractedEvents
            .Include(e => e.Evidence)
            .Where(e => e.CaseId == caseId && e.StartTimeUtc != null && e.ReviewStatus != "REJECTED")
            .OrderBy(e => e.StartTimeUtc)
            .ToListAsync(cancellationToken);

        var generatedSignals = new List<TemporalSignal>();
        var generatedOverlaps = new List<TemporalOverlapDto>();

        // 1. Intra-Case Temporal Overlap Detection
        for (int i = 0; i < events.Count; i++)
        {
            for (int j = i + 1; j < events.Count; j++)
            {
                var e1 = events[i];
                var e2 = events[j];

                // Check overlap condition
                var start1 = e1.StartTimeUtc!.Value;
                var end1 = e1.EndTimeUtc ?? e1.StartTimeUtc!.Value;
                var start2 = e2.StartTimeUtc!.Value;
                var end2 = e2.EndTimeUtc ?? e2.StartTimeUtc!.Value;

                var overlapStart = start1 > start2 ? start1 : start2;
                var overlapEnd = end1 < end2 ? end1 : end2;

                if (overlapStart < overlapEnd)
                {
                    var durationMinutes = (overlapEnd - overlapStart).TotalMinutes;
                    if (durationMinutes >= request.MinOverlapDurationMinutes)
                    {
                        // Check location or entity correlation
                        var locationMatch = (!string.IsNullOrWhiteSpace(e1.Location) && !string.IsNullOrWhiteSpace(e2.Location) &&
                                             string.Equals(e1.Location, e2.Location, StringComparison.OrdinalIgnoreCase)) ||
                                            (!string.IsNullOrWhiteSpace(e1.LocationEntityId) && string.Equals(e1.LocationEntityId, e2.LocationEntityId, StringComparison.OrdinalIgnoreCase));

                        var entities1 = ParseRelatedEntities(e1.RelatedEntitiesJson);
                        var entities2 = ParseRelatedEntities(e2.RelatedEntitiesJson);
                        var sharedEntities = entities1.Intersect(entities2, StringComparer.OrdinalIgnoreCase).ToList();

                        // Only emit signal if location matches or entities coincide
                        if (locationMatch || sharedEntities.Any())
                        {
                            var sPrec = (e1.TimePrecision == "EXACT" && e2.TimePrecision == "EXACT") ? 1.0 :
                                        (e1.TimePrecision == "MINUTE" || e2.TimePrecision == "MINUTE") ? 0.9 : 0.7;
                            var sLoc = locationMatch ? 1.0 : 0.6;
                            var sVer = (e1.ReviewStatus == "APPROVED" && e2.ReviewStatus == "APPROVED") ? 1.0 : 0.85;
                            var sDur = Math.Min(1.0, durationMinutes / 60.0);

                            var score = Math.Round(0.40 * sDur + 0.25 * sPrec + 0.20 * sLoc + 0.15 * sVer, 2);

                            var entA = entities1.FirstOrDefault() ?? "Entity A";
                            var entB = entities2.FirstOrDefault() ?? "Entity B";
                            var locName = e1.Location ?? e2.Location ?? "Designated Location";

                            var explanation = $"Activity windows coincide at {locName} from {overlapStart:yyyy-MM-dd HH:mm:ss} UTC to {overlapEnd:yyyy-MM-dd HH:mm:ss} UTC ({durationMinutes:0.#} minutes overlap duration). Note: Coincident activity window indicates temporal correlation, not confirmed contact.";

                            var signal = new TemporalSignal
                            {
                                Id = $"ts-{Guid.NewGuid().ToString("N")[..8]}",
                                CaseId = caseId,
                                AnalysisRunId = runId,
                                SignalType = "TEMPORAL_OVERLAP",
                                SourceEntityId = entA,
                                TargetEntityId = entB,
                                LocationName = locName,
                                LocationEntityId = e1.LocationEntityId ?? e2.LocationEntityId,
                                StartTimeUtc = overlapStart,
                                EndTimeUtc = overlapEnd,
                                DurationMinutes = Math.Round(durationMinutes, 1),
                                Score = score,
                                Explanation = explanation,
                                Status = "PENDING",
                                SignalsJson = JsonSerializer.Serialize(new
                                {
                                    overlapStartUtc = overlapStart,
                                    overlapEndUtc = overlapEnd,
                                    durationMinutes = Math.Round(durationMinutes, 1),
                                    event1Id = e1.Id,
                                    event2Id = e2.Id,
                                    evidence1Id = e1.EvidenceId,
                                    evidence2Id = e2.EvidenceId,
                                    evidence1Sha256 = e1.Evidence?.Sha256Hash,
                                    evidence2Sha256 = e2.Evidence?.Sha256Hash,
                                    location = locName,
                                    precision1 = e1.TimePrecision,
                                    precision2 = e2.TimePrecision
                                }),
                                CreatedAtUtc = DateTime.UtcNow
                            };

                            generatedSignals.Add(signal);
                            generatedOverlaps.Add(MapToTemporalOverlapDto(signal, new List<string> { e1.EvidenceId, e2.EvidenceId }, new List<string> { e1.Evidence?.FileName ?? "", e2.Evidence?.FileName ?? "" }));
                        }
                    }
                }
            }
        }

        // 2. Cross-Case Temporal Intelligence (if authorized)
        if (request.IncludeCrossCase && request.AuthorizedCaseIds != null && request.AuthorizedCaseIds.Any())
        {
            var otherAuthorizedCaseIds = request.AuthorizedCaseIds.Where(id => id != caseId).ToList();
            var crossEvents = await _dbContext.ExtractedEvents
                .Include(e => e.Evidence)
                .Where(e => otherAuthorizedCaseIds.Contains(e.CaseId!) && e.StartTimeUtc != null && e.ReviewStatus != "REJECTED")
                .ToListAsync(cancellationToken);

            foreach (var e1 in events)
            {
                foreach (var eCross in crossEvents)
                {
                    var start1 = e1.StartTimeUtc!.Value;
                    var end1 = e1.EndTimeUtc ?? e1.StartTimeUtc!.Value;
                    var start2 = eCross.StartTimeUtc!.Value;
                    var end2 = eCross.EndTimeUtc ?? eCross.StartTimeUtc!.Value;

                    var overlapStart = start1 > start2 ? start1 : start2;
                    var overlapEnd = end1 < end2 ? end1 : end2;

                    if (overlapStart < overlapEnd)
                    {
                        var durationMinutes = (overlapEnd - overlapStart).TotalMinutes;
                        if (durationMinutes >= request.MinOverlapDurationMinutes)
                        {
                            var locationMatch = (!string.IsNullOrWhiteSpace(e1.Location) && !string.IsNullOrWhiteSpace(eCross.Location) &&
                                                 string.Equals(e1.Location, eCross.Location, StringComparison.OrdinalIgnoreCase));

                            if (locationMatch)
                            {
                                var score = Math.Round(0.40 * Math.Min(1.0, durationMinutes / 60.0) + 0.30 + 0.20 + 0.10, 2);
                                var signal = new TemporalSignal
                                {
                                    Id = $"ts-cross-{Guid.NewGuid().ToString("N")[..8]}",
                                    CaseId = caseId,
                                    AnalysisRunId = runId,
                                    SignalType = "CROSS_CASE_TEMPORAL_OVERLAP",
                                    LocationName = e1.Location,
                                    StartTimeUtc = overlapStart,
                                    EndTimeUtc = overlapEnd,
                                    DurationMinutes = Math.Round(durationMinutes, 1),
                                    Score = score,
                                    Explanation = $"Cross-case activity window correlation: Coincident activity detected between Case '{caseId}' and Case '{eCross.CaseId}' at {e1.Location} ({durationMinutes:0.#} minutes duration).",
                                    Status = "PENDING",
                                    SignalsJson = JsonSerializer.Serialize(new
                                    {
                                        primaryCaseId = caseId,
                                        crossCaseId = eCross.CaseId,
                                        primaryEventId = e1.Id,
                                        crossEventId = eCross.Id,
                                        location = e1.Location
                                    }),
                                    CreatedAtUtc = DateTime.UtcNow
                                };

                                generatedSignals.Add(signal);
                                generatedOverlaps.Add(MapToTemporalOverlapDto(signal, new List<string> { e1.EvidenceId, eCross.EvidenceId }, new List<string> { e1.Evidence?.FileName ?? "", eCross.Evidence?.FileName ?? "" }));
                            }
                        }
                    }
                }
            }
        }

        // 3. Activity Clusters
        var clusters = ComputeTemporalClusters(events);

        // Update run entity
        run.Status = "COMPLETED";
        run.CompletedAtUtc = DateTime.UtcNow;
        run.TotalEventsAnalyzed = events.Count;
        run.SignalsGenerated = generatedSignals.Count;
        run.OverlapsFound = generatedSignals.Count(s => s.SignalType == "TEMPORAL_OVERLAP" || s.SignalType == "CROSS_CASE_TEMPORAL_OVERLAP");
        run.ClustersFound = clusters.Count;

        _dbContext.TemporalSignals.AddRange(generatedSignals);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            executedBy,
            executedBy,
            "TEMPORAL_ANALYSIS_COMPLETED",
            "TemporalAnalysisRun",
            runId,
            $"Temporal analysis completed: {events.Count} events analyzed, {generatedSignals.Count} temporal signals produced.",
            JsonSerializer.Serialize(new { totalEvents = events.Count, signalsCount = generatedSignals.Count }),
            null,
            cancellationToken);

        return new TemporalAnalysisResultDto
        {
            RunId = runId,
            CaseId = caseId,
            Status = "COMPLETED",
            StartedAtUtc = run.StartedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            TotalEventsAnalyzed = events.Count,
            SignalsGenerated = generatedSignals.Count,
            OverlapsFound = run.OverlapsFound,
            ClustersFound = clusters.Count,
            Overlaps = generatedOverlaps,
            Clusters = clusters,
            ExecutedBy = executedBy
        };
    }

    public async Task<TemporalAnalysisResultDto?> GetLatestTemporalAnalysisAsync(
        string caseId,
        CancellationToken cancellationToken = default)
    {
        var latestRun = await _dbContext.TemporalAnalysisRuns
            .Include(r => r.Signals)
            .AsNoTracking()
            .Where(r => r.CaseId == caseId)
            .OrderByDescending(r => r.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRun == null)
        {
            return null;
        }

        var events = await _dbContext.ExtractedEvents.AsNoTracking()
            .Where(e => e.CaseId == caseId && e.StartTimeUtc != null)
            .ToListAsync(cancellationToken);

        var clusters = ComputeTemporalClusters(events);

        var overlaps = latestRun.Signals
            .Where(s => s.SignalType == "TEMPORAL_OVERLAP" || s.SignalType == "CROSS_CASE_TEMPORAL_OVERLAP")
            .Select(s => MapToTemporalOverlapDto(s, new List<string>(), new List<string>()))
            .ToList();

        return new TemporalAnalysisResultDto
        {
            RunId = latestRun.Id,
            CaseId = caseId,
            Status = latestRun.Status,
            StartedAtUtc = latestRun.StartedAtUtc,
            CompletedAtUtc = latestRun.CompletedAtUtc,
            TotalEventsAnalyzed = latestRun.TotalEventsAnalyzed,
            SignalsGenerated = latestRun.SignalsGenerated,
            OverlapsFound = latestRun.OverlapsFound,
            ClustersFound = clusters.Count,
            Overlaps = overlaps,
            Clusters = clusters,
            ExecutedBy = latestRun.ExecutedBy
        };
    }

    public async Task<List<TemporalOverlapDto>> GetTemporalOverlapsAsync(
        string caseId,
        string? entityId = null,
        string? location = null,
        double minDurationMinutes = 0,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TemporalSignals.AsNoTracking()
            .Where(s => s.CaseId == caseId && (s.SignalType == "TEMPORAL_OVERLAP" || s.SignalType == "CROSS_CASE_TEMPORAL_OVERLAP"));

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            var target = entityId.ToLower();
            query = query.Where(s => (s.SourceEntityId != null && s.SourceEntityId.ToLower().Contains(target)) ||
                                     (s.TargetEntityId != null && s.TargetEntityId.ToLower().Contains(target)));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.ToLower();
            query = query.Where(s => s.LocationName != null && s.LocationName.ToLower().Contains(loc));
        }

        if (minDurationMinutes > 0)
        {
            query = query.Where(s => s.DurationMinutes >= minDurationMinutes);
        }

        var signals = await query.OrderByDescending(s => s.Score).ToListAsync(cancellationToken);

        return signals.Select(s => MapToTemporalOverlapDto(s, new List<string>(), new List<string>())).ToList();
    }

    public async Task<TemporalSequenceDto> GetEntitySequenceAsync(
        string entityId,
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Entities.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);
        var entityName = entity?.CanonicalName ?? entityId;

        var events = await GetEntityTimelineAsync(entityId, caseId, cancellationToken);
        var datedEvents = events.Where(e => e.StartTimeUtc.HasValue).OrderBy(e => e.StartTimeUtc).ToList();

        var steps = new List<TemporalSequenceItemDto>();
        for (int i = 0; i < datedEvents.Count; i++)
        {
            var current = datedEvents[i];
            var elapsedStr = "Origin";
            double elapsedMin = 0;

            if (i > 0)
            {
                var prev = datedEvents[i - 1];
                var delta = current.StartTimeUtc!.Value - (prev.EndTimeUtc ?? prev.StartTimeUtc!.Value);
                elapsedMin = Math.Max(0, delta.TotalMinutes);
                if (elapsedMin < 60)
                {
                    elapsedStr = $"{elapsedMin:0} mins elapsed";
                }
                else if (elapsedMin < 1440)
                {
                    elapsedStr = $"{elapsedMin / 60.0:0.#} hours elapsed";
                }
                else
                {
                    elapsedStr = $"{elapsedMin / 1440.0:0.#} days elapsed";
                }
            }

            steps.Add(new TemporalSequenceItemDto
            {
                StepIndex = i + 1,
                EventId = current.Id,
                EventType = current.EventType,
                Description = current.Description,
                TimestampUtc = current.StartTimeUtc!.Value,
                Location = current.Location,
                InvolvedEntities = current.RelatedEntityNames,
                ElapsedFromPrevious = elapsedStr,
                ElapsedMinutesFromPrevious = elapsedMin
            });
        }

        return new TemporalSequenceDto
        {
            EntityId = entityId,
            EntityName = entityName,
            TotalSteps = steps.Count,
            SequenceStartUtc = steps.FirstOrDefault()?.TimestampUtc,
            SequenceEndUtc = steps.LastOrDefault()?.TimestampUtc,
            Steps = steps
        };
    }

    public async Task<TemporalSummaryDto> GetTemporalSummaryAsync(
        string caseId,
        CancellationToken cancellationToken = default)
    {
        var events = await _dbContext.ExtractedEvents.AsNoTracking()
            .Where(e => e.CaseId == caseId)
            .ToListAsync(cancellationToken);

        var signals = await _dbContext.TemporalSignals.AsNoTracking()
            .Where(s => s.CaseId == caseId)
            .ToListAsync(cancellationToken);

        var dated = events.Where(e => e.StartTimeUtc.HasValue).ToList();
        var clusters = ComputeTemporalClusters(events);

        return new TemporalSummaryDto
        {
            CaseId = caseId,
            TotalEvents = events.Count,
            ActivePeriodStartUtc = dated.Any() ? dated.Min(e => e.StartTimeUtc) : null,
            ActivePeriodEndUtc = dated.Any() ? dated.Max(e => e.EndTimeUtc ?? e.StartTimeUtc) : null,
            ActivityPeaks = clusters.Count(c => c.Intensity == "HIGH" || c.Intensity == "MEDIUM"),
            TemporalSignals = signals.Count,
            PendingSignals = signals.Count(s => s.Status == "PENDING"),
            ConfirmedSignals = signals.Count(s => s.Status == "CONFIRMED")
        };
    }

    public async Task<TemporalOverlapDto> ReviewTemporalSignalAsync(
        string signalId,
        ReviewTemporalSignalRequestDto review,
        string reviewerName,
        CancellationToken cancellationToken = default)
    {
        var signal = await _dbContext.TemporalSignals.FirstOrDefaultAsync(s => s.Id == signalId, cancellationToken);
        if (signal == null)
        {
            throw new KeyNotFoundException($"Temporal signal '{signalId}' not found.");
        }

        var normalizedStatus = review.Status.ToUpperInvariant() == "CONFIRMED" ? "CONFIRMED" : "DISMISSED";
        signal.Status = normalizedStatus;
        signal.ReviewedAtUtc = DateTime.UtcNow;
        signal.ReviewedBy = reviewerName;
        signal.ReviewNotes = review.ReviewNotes;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var action = normalizedStatus == "CONFIRMED" ? "TEMPORAL_SIGNAL_CONFIRMED" : "TEMPORAL_SIGNAL_DISMISSED";
        await _auditService.LogAsync(
            reviewerName,
            reviewerName,
            action,
            "TemporalSignal",
            signalId,
            $"Investigator {reviewerName} marked temporal signal '{signalId}' as {normalizedStatus}.",
            JsonSerializer.Serialize(new { signalId, status = normalizedStatus, notes = review.ReviewNotes }),
            null,
            cancellationToken);

        return MapToTemporalOverlapDto(signal, new List<string>(), new List<string>());
    }

    // Helper: Map ExtractedEvent to DTO
    private static TimelineEventDto MapToTimelineEventDto(ExtractedEvent ev)
    {
        var entityNames = ParseRelatedEntities(ev.RelatedEntitiesJson);

        return new TimelineEventDto
        {
            Id = ev.Id,
            CaseId = ev.CaseId ?? string.Empty,
            EventType = ev.EventType,
            Description = string.IsNullOrWhiteSpace(ev.Description) ? $"{ev.EventType} recorded in evidence" : ev.Description,
            StartTimeUtc = ev.StartTimeUtc,
            EndTimeUtc = ev.EndTimeUtc ?? ev.StartTimeUtc,
            TimePrecision = ev.TimePrecision,
            Location = ev.Location,
            LocationEntityId = ev.LocationEntityId,
            RelatedEntityNames = entityNames,
            Confidence = ev.Confidence,
            VerificationStatus = ev.ReviewStatus,
            SourceEvidenceId = ev.EvidenceId,
            SourceEvidenceFileName = ev.Evidence?.FileName,
            SourceEvidenceSha256 = ev.Evidence?.Sha256Hash,
            EvidenceIntegrityVerified = !string.IsNullOrWhiteSpace(ev.Evidence?.Sha256Hash),
            SourceLocation = ev.SourceLocation,
            SourcePage = ev.SourcePage,
            CreatedAtUtc = ev.CreatedAtUtc
        };
    }

    // Helper: Map TemporalSignal to DTO
    private static TemporalOverlapDto MapToTemporalOverlapDto(
        TemporalSignal signal,
        List<string> evidenceIds,
        List<string> evidenceFileNames)
    {
        return new TemporalOverlapDto
        {
            Id = signal.Id,
            CaseId = signal.CaseId,
            SourceEntityId = signal.SourceEntityId,
            SourceEntityName = signal.SourceEntityId,
            TargetEntityId = signal.TargetEntityId,
            TargetEntityName = signal.TargetEntityId,
            LocationEntityId = signal.LocationEntityId,
            LocationName = signal.LocationName,
            OverlapStartUtc = signal.StartTimeUtc ?? DateTime.MinValue,
            OverlapEndUtc = signal.EndTimeUtc ?? DateTime.MinValue,
            DurationMinutes = signal.DurationMinutes,
            Score = signal.Score,
            Explanation = signal.Explanation,
            SignalType = signal.SignalType,
            Status = signal.Status,
            SupportingEvidenceIds = evidenceIds,
            SupportingEvidenceFileNames = evidenceFileNames,
            ReviewedBy = signal.ReviewedBy,
            ReviewedAtUtc = signal.ReviewedAtUtc,
            ReviewNotes = signal.ReviewNotes
        };
    }

    // Helper: Parse related entities from JSON
    private static List<string> ParseRelatedEntities(string? json)
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

    // Helper: Activity Clustering (gap < 36h)
    private static List<TemporalClusterDto> ComputeTemporalClusters(List<ExtractedEvent> events)
    {
        var dated = events.Where(e => e.StartTimeUtc.HasValue).OrderBy(e => e.StartTimeUtc).ToList();
        if (!dated.Any()) return new List<TemporalClusterDto>();

        var clusters = new List<TemporalClusterDto>();
        var currentBatch = new List<ExtractedEvent> { dated[0] };

        for (int i = 1; i < dated.Count; i++)
        {
            var prev = dated[i - 1];
            var curr = dated[i];

            var gap = (curr.StartTimeUtc!.Value - (prev.EndTimeUtc ?? prev.StartTimeUtc!.Value)).TotalHours;
            if (gap <= 36.0)
            {
                currentBatch.Add(curr);
            }
            else
            {
                clusters.Add(CreateClusterDto(currentBatch, clusters.Count + 1));
                currentBatch = new List<ExtractedEvent> { curr };
            }
        }

        if (currentBatch.Any())
        {
            clusters.Add(CreateClusterDto(currentBatch, clusters.Count + 1));
        }

        return clusters;
    }

    private static TemporalClusterDto CreateClusterDto(List<ExtractedEvent> batch, int index)
    {
        var start = batch.Min(e => e.StartTimeUtc!.Value);
        var end = batch.Max(e => e.EndTimeUtc ?? e.StartTimeUtc!.Value);

        var entities = batch
            .SelectMany(e => ParseRelatedEntities(e.RelatedEntitiesJson))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var locations = batch
            .Where(e => !string.IsNullOrWhiteSpace(e.Location))
            .Select(e => e.Location!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var intensity = batch.Count >= 4 ? "HIGH" : (batch.Count >= 2 ? "MEDIUM" : "LOW");

        return new TemporalClusterDto
        {
            ClusterId = $"cluster-t-{index}",
            ClusterLabel = $"Activity Period {index} ({start:MMM dd} - {end:MMM dd})",
            StartTimeUtc = start,
            EndTimeUtc = end,
            EventCount = batch.Count,
            DistinctEntitiesCount = entities.Count,
            DistinctLocationsCount = locations.Count,
            Intensity = intensity,
            KeyEntities = entities.Take(5).ToList(),
            KeyLocations = locations.Take(3).ToList(),
            Events = batch.Select(MapToTimelineEventDto).ToList()
        };
    }

    // Helper: Compute sequence highlights
    private static List<TemporalSequenceItemDto> ComputeSequenceHighlights(List<ExtractedEvent> events)
    {
        var dated = events.Where(e => e.StartTimeUtc.HasValue).OrderBy(e => e.StartTimeUtc).Take(10).ToList();
        var highlights = new List<TemporalSequenceItemDto>();

        for (int i = 0; i < dated.Count; i++)
        {
            var curr = dated[i];
            var elapsedStr = i == 0 ? "T-0" : "";
            double elapsedMin = 0;

            if (i > 0)
            {
                var prev = dated[i - 1];
                elapsedMin = (curr.StartTimeUtc!.Value - (prev.EndTimeUtc ?? prev.StartTimeUtc!.Value)).TotalMinutes;
                elapsedStr = elapsedMin < 60 ? $"{elapsedMin:0}m" : $"{elapsedMin / 60.0:0.#}h";
            }

            highlights.Add(new TemporalSequenceItemDto
            {
                StepIndex = i + 1,
                EventId = curr.Id,
                EventType = curr.EventType,
                Description = string.IsNullOrWhiteSpace(curr.Description) ? $"{curr.EventType} in evidence" : curr.Description,
                TimestampUtc = curr.StartTimeUtc!.Value,
                Location = curr.Location,
                InvolvedEntities = ParseRelatedEntities(curr.RelatedEntitiesJson),
                ElapsedFromPrevious = elapsedStr,
                ElapsedMinutesFromPrevious = elapsedMin
            });
        }

        return highlights;
    }
}
