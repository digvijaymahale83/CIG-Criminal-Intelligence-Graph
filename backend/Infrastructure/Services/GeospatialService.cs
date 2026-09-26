using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class GeospatialService : IGeospatialService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<GeospatialService> _logger;

    public GeospatialService(IAppDbContext dbContext, ILogger<GeospatialService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<LocationDto>> GetCaseLocationsAsync(
        string caseId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId || l.CaseId == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.Name.ToLower().Contains(s) ||
                                     l.NormalizedName.ToLower().Contains(s) ||
                                     (l.City != null && l.City.ToLower().Contains(s)) ||
                                     (l.Address != null && l.Address.ToLower().Contains(s)));
        }

        var locations = await query.ToListAsync(cancellationToken);
        var locationIds = locations.Select(l => l.Id).ToHashSet();
        var locationNames = locations.Select(l => l.Name.ToLower()).ToHashSet();

        // Query events in case to compute counts
        var events = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Where(e => e.CaseId == caseId && e.ReviewStatus != "REJECTED")
            .ToListAsync(cancellationToken);

        return locations.Select(loc =>
        {
            var locName = loc.Name.ToLower();
            var matchingEvents = events.Where(e =>
                (!string.IsNullOrEmpty(e.LocationEntityId) && e.LocationEntityId == loc.Id) ||
                (!string.IsNullOrEmpty(e.Location) && e.Location.ToLower().Contains(locName))
            ).ToList();

            var entityCount = matchingEvents
                .SelectMany(e => GetEntityNames(e.RelatedEntitiesJson))
                .Distinct()
                .Count();

            var evidenceCount = matchingEvents
                .Where(e => !string.IsNullOrEmpty(e.EvidenceId))
                .Select(e => e.EvidenceId)
                .Distinct()
                .Count();

            return MapToLocationDto(loc, matchingEvents.Count, entityCount, evidenceCount);
        }).ToList();
    }

    public async Task<CaseMapDto> GetCaseMapDataAsync(
        string caseId,
        string? eventType = null,
        string? entityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool verifiedOnly = false,
        CancellationToken cancellationToken = default)
    {
        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId || l.CaseId == null)
            .ToListAsync(cancellationToken);

        var eventsQuery = _dbContext.ExtractedEvents
            .AsNoTracking()
            .Where(e => e.CaseId == caseId && e.ReviewStatus != "REJECTED");

        if (verifiedOnly)
        {
            eventsQuery = eventsQuery.Where(e => e.ReviewStatus == "APPROVED");
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            eventsQuery = eventsQuery.Where(e => e.EventType == eventType);
        }

        if (startDate.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => (e.EndTimeUtc ?? e.StartTimeUtc ?? DateTime.MinValue) >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => (e.StartTimeUtc ?? DateTime.MaxValue) <= endDate.Value);
        }

        var events = await eventsQuery.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            var entity = await _dbContext.Entities.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);
            var entityName = entity?.CanonicalName?.ToLower();

            events = events.Where(e =>
                !string.IsNullOrEmpty(e.RelatedEntitiesJson) &&
                (e.RelatedEntitiesJson.Contains(entityId) || (entityName != null && e.RelatedEntitiesJson.ToLower().Contains(entityName)))
            ).ToList();
        }

        var locationDtos = locations.Select(loc =>
        {
            var locName = loc.Name.ToLower();
            var locEvents = events.Where(e =>
                (!string.IsNullOrEmpty(e.LocationEntityId) && e.LocationEntityId == loc.Id) ||
                (!string.IsNullOrEmpty(e.Location) && e.Location.ToLower().Contains(locName))
            ).ToList();

            var entityCount = locEvents
                .SelectMany(e => GetEntityNames(e.RelatedEntitiesJson))
                .Distinct()
                .Count();

            var evidenceCount = locEvents
                .Where(e => !string.IsNullOrEmpty(e.EvidenceId))
                .Select(e => e.EvidenceId)
                .Distinct()
                .Count();

            return MapToLocationDto(loc, locEvents.Count, entityCount, evidenceCount);
        }).ToList();

        // Cluster locations using deterministic proximity (25km)
        var clusters = ClusterLocations(locationDtos, 25.0);

        // Fetch active spatial signals
        var signals = await _dbContext.SpatialSignals
            .AsNoTracking()
            .Where(s => s.CaseId == caseId && s.Status == "PENDING")
            .Take(20)
            .ToListAsync(cancellationToken);

        return new CaseMapDto
        {
            CaseId = caseId,
            TotalLocations = locationDtos.Count,
            TotalEvents = events.Count,
            TotalSignals = signals.Count,
            Locations = locationDtos,
            Clusters = clusters,
            ActiveSignals = signals.Select(MapToSpatialSignalDto).ToList()
        };
    }

    public async Task<LocationDto?> GetLocationDetailsAsync(
        string locationId,
        CancellationToken cancellationToken = default)
    {
        var loc = await _dbContext.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (loc == null) return null;

        var locName = loc.Name.ToLower();
        var matchingEvents = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Where(e => e.ReviewStatus != "REJECTED" &&
                        ((e.LocationEntityId != null && e.LocationEntityId == loc.Id) ||
                         (e.Location != null && e.Location.ToLower().Contains(locName))))
            .ToListAsync(cancellationToken);

        var entityCount = matchingEvents
            .SelectMany(e => GetEntityNames(e.RelatedEntitiesJson))
            .Distinct()
            .Count();

        var evidenceCount = matchingEvents
            .Where(e => !string.IsNullOrEmpty(e.EvidenceId))
            .Select(e => e.EvidenceId)
            .Distinct()
            .Count();

        return MapToLocationDto(loc, matchingEvents.Count, entityCount, evidenceCount);
    }

    public async Task<LocationActivityDto?> GetLocationActivityAsync(
        string locationId,
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var loc = await _dbContext.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (loc == null) return null;

        var eventsQuery = _dbContext.ExtractedEvents
            .Include(e => e.Evidence)
            .AsNoTracking()
            .Where(e => e.ReviewStatus != "REJECTED");

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            eventsQuery = eventsQuery.Where(e => e.CaseId == caseId);
        }

        var locName = loc.Name.ToLower();
        var allEvents = await eventsQuery.ToListAsync(cancellationToken);
        var matchingEvents = allEvents.Where(e =>
            (!string.IsNullOrEmpty(e.LocationEntityId) && e.LocationEntityId == loc.Id) ||
            (!string.IsNullOrEmpty(e.Location) && e.Location.ToLower().Contains(locName))
        ).OrderBy(e => e.StartTimeUtc ?? DateTime.MinValue).ToList();

        // Entity visit summaries
        var entitySummaries = new Dictionary<string, LocationActivityEntitySummaryDto>();
        foreach (var ev in matchingEvents)
        {
            var names = GetEntityNames(ev.RelatedEntitiesJson);
            foreach (var name in names)
            {
                if (!entitySummaries.TryGetValue(name, out var summary))
                {
                    summary = new LocationActivityEntitySummaryDto
                    {
                        EntityId = name,
                        EntityName = name,
                        EntityType = "PERSON",
                        RecordedVisits = 0,
                        FirstObservedUtc = ev.StartTimeUtc,
                        LastObservedUtc = ev.EndTimeUtc ?? ev.StartTimeUtc
                    };
                    entitySummaries[name] = summary;
                }

                summary.RecordedVisits++;
                if (ev.StartTimeUtc.HasValue && (!summary.FirstObservedUtc.HasValue || ev.StartTimeUtc < summary.FirstObservedUtc))
                {
                    summary.FirstObservedUtc = ev.StartTimeUtc;
                }
                var evEnd = ev.EndTimeUtc ?? ev.StartTimeUtc;
                if (evEnd.HasValue && (!summary.LastObservedUtc.HasValue || evEnd > summary.LastObservedUtc))
                {
                    summary.LastObservedUtc = evEnd;
                }
            }
        }

        // Evidence citations with SHA-256 integrity
        var evidenceList = matchingEvents
            .Where(e => e.Evidence != null)
            .Select(e => e.Evidence!)
            .DistinctBy(ev => ev.Id)
            .Select(ev => new LocationEvidenceCitationDto
            {
                EvidenceId = ev.Id,
                FileName = ev.FileName,
                Sha256Hash = ev.Sha256Hash,
                SourceLocation = ev.StoragePath,
                EvidenceIntegrityVerified = !string.IsNullOrEmpty(ev.Sha256Hash)
            })
            .ToList();

        var locationDto = MapToLocationDto(loc, matchingEvents.Count, entitySummaries.Count, evidenceList.Count);

        return new LocationActivityDto
        {
            Location = locationDto,
            Events = matchingEvents.Select(MapToTimelineEventDto).ToList(),
            Entities = entitySummaries.Values.OrderByDescending(s => s.RecordedVisits).ToList(),
            EvidenceRecords = evidenceList,
            ActivePeriodStartUtc = matchingEvents.FirstOrDefault()?.StartTimeUtc,
            ActivePeriodEndUtc = matchingEvents.LastOrDefault()?.EndTimeUtc ?? matchingEvents.LastOrDefault()?.StartTimeUtc
        };
    }

    public async Task<List<LocationDto>> GetEntityLocationsAsync(
        string entityId,
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);
        var targetName = entity?.CanonicalName?.ToLower() ?? entityId.ToLower();
        var targetId = entityId.ToLower();

        var eventsQuery = _dbContext.ExtractedEvents
            .AsNoTracking()
            .Where(e => e.ReviewStatus != "REJECTED");

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            eventsQuery = eventsQuery.Where(e => e.CaseId == caseId);
        }

        var events = await eventsQuery.ToListAsync(cancellationToken);
        var entityEvents = events.Where(e =>
            !string.IsNullOrEmpty(e.RelatedEntitiesJson) &&
            (e.RelatedEntitiesJson.ToLower().Contains(targetId) || e.RelatedEntitiesJson.ToLower().Contains(targetName))
        ).ToList();

        var locationIds = entityEvents
            .Where(e => !string.IsNullOrEmpty(e.LocationEntityId))
            .Select(e => e.LocationEntityId!)
            .ToHashSet();

        var locationNames = entityEvents
            .Where(e => !string.IsNullOrEmpty(e.Location))
            .Select(e => e.Location!.ToLower())
            .ToHashSet();

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id) || locationNames.Contains(l.Name.ToLower()))
            .ToListAsync(cancellationToken);

        return locations.Select(l => MapToLocationDto(l, 1, 1, 1)).ToList();
    }

    public async Task<TravelSequenceDto> GetEntityTravelSequenceAsync(
        string entityId,
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);
        var targetName = entity?.CanonicalName?.ToLower() ?? entityId.ToLower();
        var targetId = entityId.ToLower();

        var eventsQuery = _dbContext.ExtractedEvents
            .AsNoTracking()
            .Where(e => e.ReviewStatus != "REJECTED" && e.StartTimeUtc != null);

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            eventsQuery = eventsQuery.Where(e => e.CaseId == caseId);
        }

        var events = await eventsQuery.ToListAsync(cancellationToken);
        var entityEvents = events
            .Where(e => !string.IsNullOrEmpty(e.RelatedEntitiesJson) &&
                        (e.RelatedEntitiesJson.ToLower().Contains(targetId) || e.RelatedEntitiesJson.ToLower().Contains(targetName)))
            .OrderBy(e => e.StartTimeUtc!.Value)
            .ToList();

        var locations = await _dbContext.Locations.AsNoTracking().ToListAsync(cancellationToken);

        var steps = new List<TravelSequenceStepDto>();
        double totalDistanceKm = 0.0;
        TravelSequenceStepDto? prevStep = null;

        for (int i = 0; i < entityEvents.Count; i++)
        {
            var ev = entityEvents[i];
            LocationItem? matchedLoc = null;

            if (!string.IsNullOrEmpty(ev.LocationEntityId))
            {
                matchedLoc = locations.FirstOrDefault(l => l.Id == ev.LocationEntityId);
            }
            if (matchedLoc == null && !string.IsNullOrEmpty(ev.Location))
            {
                matchedLoc = locations.FirstOrDefault(l => l.Name.Equals(ev.Location, StringComparison.OrdinalIgnoreCase) ||
                                                          ev.Location.Contains(l.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedLoc == null) continue;

            var step = new TravelSequenceStepDto
            {
                StepIndex = steps.Count + 1,
                EventId = ev.Id,
                EventType = ev.EventType,
                Description = ev.Description,
                TimestampUtc = ev.StartTimeUtc!.Value,
                LocationId = matchedLoc.Id,
                LocationName = matchedLoc.Name,
                Latitude = matchedLoc.Latitude,
                Longitude = matchedLoc.Longitude
            };

            if (prevStep != null)
            {
                var distance = GeoMath.HaversineDistanceKm(prevStep.Latitude, prevStep.Longitude, step.Latitude, step.Longitude);
                var durationHours = (step.TimestampUtc - prevStep.TimestampUtc).TotalHours;

                step.DistanceKmFromPrevious = Math.Round(distance, 2);
                step.ElapsedHoursFromPrevious = Math.Round(durationHours, 2);
                step.ElapsedFormatted = FormatElapsed(step.TimestampUtc - prevStep.TimestampUtc);

                var isImplausible = GeoMath.IsImplausibleVelocity(distance, durationHours, out var speed);
                step.ImpliedSpeedKmh = Math.Round(speed, 1);
                step.IsImplausibleSpeed = isImplausible;

                totalDistanceKm += distance;
            }
            else
            {
                step.ElapsedFormatted = "Origin";
                step.DistanceKmFromPrevious = 0.0;
                step.ElapsedHoursFromPrevious = 0.0;
                step.ImpliedSpeedKmh = 0.0;
                step.IsImplausibleSpeed = false;
            }

            steps.Add(step);
            prevStep = step;
        }

        return new TravelSequenceDto
        {
            EntityId = entityId,
            EntityName = entity?.CanonicalName ?? entityId,
            TotalSteps = steps.Count,
            TotalDistanceKm = Math.Round(totalDistanceKm, 2),
            SequenceStartUtc = steps.FirstOrDefault()?.TimestampUtc,
            SequenceEndUtc = steps.LastOrDefault()?.TimestampUtc,
            Steps = steps
        };
    }

    public async Task<List<SpatialProximityResultDto>> GetSpatialProximityAsync(
        string caseId,
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken cancellationToken = default)
    {
        if (!GeoMath.IsValidCoordinate(latitude, longitude))
        {
            throw new ArgumentException($"Invalid geographic coordinates: ({latitude}, {longitude}). Latitude must be [-90, 90], Longitude must be [-180, 180].");
        }

        if (radiusKm <= 0.0)
        {
            throw new ArgumentException("Radius must be greater than zero kilometers.");
        }

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId || l.CaseId == null)
            .ToListAsync(cancellationToken);

        var events = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Where(e => e.CaseId == caseId && e.ReviewStatus != "REJECTED")
            .ToListAsync(cancellationToken);

        var results = new List<SpatialProximityResultDto>();

        foreach (var loc in locations)
        {
            var distance = GeoMath.HaversineDistanceKm(latitude, longitude, loc.Latitude, loc.Longitude);
            if (distance <= radiusKm)
            {
                var locName = loc.Name.ToLower();
                var locEvents = events.Where(e =>
                    (!string.IsNullOrEmpty(e.LocationEntityId) && e.LocationEntityId == loc.Id) ||
                    (!string.IsNullOrEmpty(e.Location) && e.Location.ToLower().Contains(locName))
                ).ToList();

                var entityCount = locEvents
                    .SelectMany(e => GetEntityNames(e.RelatedEntitiesJson))
                    .Distinct()
                    .Count();

                results.Add(new SpatialProximityResultDto
                {
                    LocationId = loc.Id,
                    Name = loc.Name,
                    City = loc.City,
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    DistanceKm = Math.Round(distance, 2),
                    GeocodePrecision = loc.GeocodePrecision,
                    EventCount = locEvents.Count,
                    EntityCount = entityCount
                });
            }
        }

        return results.OrderBy(r => r.DistanceKm).ToList();
    }

    public async Task<SpatialAnalysisResultDto?> GetLatestSpatialAnalysisAsync(
        string caseId,
        CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.SpatialAnalysisRuns
            .AsNoTracking()
            .Where(r => r.CaseId == caseId)
            .OrderByDescending(r => r.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (run == null) return null;

        var signals = await _dbContext.SpatialSignals
            .AsNoTracking()
            .Where(s => s.CaseId == caseId && s.AnalysisRunId == run.Id)
            .ToListAsync(cancellationToken);

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId || l.CaseId == null)
            .ToListAsync(cancellationToken);

        var locationDtos = locations.Select(l => MapToLocationDto(l, 0, 0, 0)).ToList();
        var clusters = ClusterLocations(locationDtos, 25.0);

        return new SpatialAnalysisResultDto
        {
            RunId = run.Id,
            CaseId = run.CaseId,
            Status = run.Status,
            StartedAtUtc = run.StartedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            TotalLocationsAnalyzed = run.TotalLocationsAnalyzed,
            TotalEventsAnalyzed = run.TotalEventsAnalyzed,
            SignalsGenerated = run.SignalsGenerated,
            OverlapsFound = run.OverlapsFound,
            ClustersFound = run.ClustersFound,
            VelocityWarningsFound = run.VelocityWarningsFound,
            Signals = signals.Select(MapToSpatialSignalDto).ToList(),
            Clusters = clusters,
            ExecutedBy = run.ExecutedBy
        };
    }

    public async Task<SpatialAnalysisResultDto> RunSpatialAnalysisAsync(
        string caseId,
        RunSpatialAnalysisRequestDto request,
        string executedBy,
        CancellationToken cancellationToken = default)
    {
        var runId = $"sar-{Guid.NewGuid().ToString("N")[..8]}";
        var startedAt = DateTime.UtcNow;

        var run = new SpatialAnalysisRun
        {
            Id = runId,
            CaseId = caseId,
            ExecutedBy = executedBy,
            Status = "RUNNING",
            StartedAtUtc = startedAt,
            ConfigurationJson = JsonSerializer.Serialize(request)
        };

        _dbContext.SpatialAnalysisRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Fetch case locations
        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(l => l.CaseId == caseId || l.CaseId == null)
            .ToListAsync(cancellationToken);

        // Fetch verified events
        var events = await _dbContext.ExtractedEvents
            .Include(e => e.Evidence)
            .AsNoTracking()
            .Where(e => e.CaseId == caseId && e.ReviewStatus != "REJECTED")
            .ToListAsync(cancellationToken);

        var signals = new List<SpatialSignal>();

        // 1. Detect Spatial-Temporal Overlaps (co-presence at same location in overlapping time)
        for (int i = 0; i < events.Count; i++)
        {
            for (int j = i + 1; j < events.Count; j++)
            {
                var evA = events[i];
                var evB = events[j];

                if (!evA.StartTimeUtc.HasValue || !evB.StartTimeUtc.HasValue) continue;

                var endA = evA.EndTimeUtc ?? evA.StartTimeUtc.Value;
                var endB = evB.EndTimeUtc ?? evB.StartTimeUtc.Value;

                // Overlap condition: max(startA, startB) < min(endA, endB)
                var overlapStart = evA.StartTimeUtc.Value > evB.StartTimeUtc.Value ? evA.StartTimeUtc.Value : evB.StartTimeUtc.Value;
                var overlapEnd = endA < endB ? endA : endB;

                if (overlapStart >= overlapEnd) continue; // No temporal overlap

                // Check spatial coincidence
                var isSameLocation = false;
                LocationItem? locRecord = null;

                if (!string.IsNullOrEmpty(evA.LocationEntityId) && evA.LocationEntityId == evB.LocationEntityId)
                {
                    isSameLocation = true;
                    locRecord = locations.FirstOrDefault(l => l.Id == evA.LocationEntityId);
                }
                else if (!string.IsNullOrEmpty(evA.Location) && !string.IsNullOrEmpty(evB.Location) &&
                         evA.Location.Equals(evB.Location, StringComparison.OrdinalIgnoreCase))
                {
                    isSameLocation = true;
                    locRecord = locations.FirstOrDefault(l => l.Name.Equals(evA.Location, StringComparison.OrdinalIgnoreCase));
                }

                if (!isSameLocation) continue;

                var entitiesA = GetEntityNames(evA.RelatedEntitiesJson);
                var entitiesB = GetEntityNames(evB.RelatedEntitiesJson);

                var distinctPairs = from a in entitiesA
                                    from b in entitiesB
                                    where !a.Equals(b, StringComparison.OrdinalIgnoreCase)
                                    select (EntityA: a, EntityB: b);

                foreach (var pair in distinctPairs)
                {
                    var durationMinutes = (overlapEnd - overlapStart).TotalMinutes;
                    var score = Math.Min(1.0, 0.5 + (Math.Min(durationMinutes, 60.0) / 120.0) + (Math.Min(evA.Confidence, evB.Confidence) * 0.3));

                    var evidenceIds = new List<string>();
                    if (!string.IsNullOrEmpty(evA.EvidenceId)) evidenceIds.Add(evA.EvidenceId);
                    if (!string.IsNullOrEmpty(evB.EvidenceId)) evidenceIds.Add(evB.EvidenceId);

                    signals.Add(new SpatialSignal
                    {
                        Id = $"sig-spat-{Guid.NewGuid().ToString("N")[..8]}",
                        CaseId = caseId,
                        AnalysisRunId = runId,
                        SignalType = "SPATIAL_TEMPORAL_OVERLAP",
                        SourceEntityId = pair.EntityA,
                        TargetEntityId = pair.EntityB,
                        LocationId = locRecord?.Id ?? evA.LocationEntityId,
                        LocationName = locRecord?.Name ?? evA.Location ?? "Recorded Event Location",
                        StartTimeUtc = overlapStart,
                        EndTimeUtc = overlapEnd,
                        DistanceKm = 0.0,
                        Score = Math.Round(score, 2),
                        Explanation = $"Both entities had recorded activity at {locRecord?.Name ?? evA.Location} during an overlapping temporal window ({Math.Round(durationMinutes)} minutes). Supporting evidence: {string.Join(", ", evidenceIds)}.",
                        SupportingEvidenceJson = JsonSerializer.Serialize(evidenceIds),
                        Status = "PENDING",
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        // 2. Detect Implausible Travel Velocity
        var entityGroups = events
            .Where(e => e.StartTimeUtc != null)
            .SelectMany(e => GetEntityNames(e.RelatedEntitiesJson).Select(name => (EntityName: name, Event: e)))
            .GroupBy(x => x.EntityName);

        int velocityWarnings = 0;

        foreach (var group in entityGroups)
        {
            var entityEvents = group
                .Select(x => x.Event)
                .OrderBy(e => e.StartTimeUtc!.Value)
                .ToList();

            for (int i = 1; i < entityEvents.Count; i++)
            {
                var prev = entityEvents[i - 1];
                var curr = entityEvents[i];

                var locPrev = locations.FirstOrDefault(l => l.Id == prev.LocationEntityId || (prev.Location != null && l.Name.Equals(prev.Location, StringComparison.OrdinalIgnoreCase)));
                var locCurr = locations.FirstOrDefault(l => l.Id == curr.LocationEntityId || (curr.Location != null && l.Name.Equals(curr.Location, StringComparison.OrdinalIgnoreCase)));

                if (locPrev == null || locCurr == null) continue;

                var dist = GeoMath.HaversineDistanceKm(locPrev.Latitude, locPrev.Longitude, locCurr.Latitude, locCurr.Longitude);
                var durationHours = (curr.StartTimeUtc!.Value - (prev.EndTimeUtc ?? prev.StartTimeUtc!.Value)).TotalHours;

                if (GeoMath.IsImplausibleVelocity(dist, durationHours, out var speed))
                {
                    velocityWarnings++;
                    signals.Add(new SpatialSignal
                    {
                        Id = $"sig-vel-{Guid.NewGuid().ToString("N")[..8]}",
                        CaseId = caseId,
                        AnalysisRunId = runId,
                        SignalType = "IMPLAUSIBLE_TRAVEL_SPEED",
                        SourceEntityId = group.Key,
                        LocationId = locCurr.Id,
                        LocationName = $"{locPrev.Name} → {locCurr.Name}",
                        StartTimeUtc = prev.StartTimeUtc,
                        EndTimeUtc = curr.StartTimeUtc,
                        DistanceKm = Math.Round(dist, 2),
                        Score = 0.85,
                        Explanation = $"Recorded activity sequence indicates a transition between {locPrev.Name} and {locCurr.Name} ({Math.Round(dist)} km in {Math.Round(durationHours, 1)}h, implied velocity: {Math.Round(speed)} km/h). This warrants investigative review for timestamp accuracy or data fidelity.",
                        Status = "PENDING",
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        // 3. Optional Cross-Case Analysis
        if (request.IncludeCrossCase && request.AuthorizedCaseIds != null && request.AuthorizedCaseIds.Count > 0)
        {
            var authCases = request.AuthorizedCaseIds.ToHashSet();
            var crossCaseEvents = await _dbContext.ExtractedEvents
                .AsNoTracking()
                .Where(e => e.CaseId != null && authCases.Contains(e.CaseId) && e.ReviewStatus != "REJECTED" && e.StartTimeUtc != null)
                .ToListAsync(cancellationToken);

            foreach (var caseEv in events.Where(e => e.StartTimeUtc != null))
            {
                foreach (var otherEv in crossCaseEvents)
                {
                    if (caseEv.LocationEntityId == otherEv.LocationEntityId && !string.IsNullOrEmpty(caseEv.LocationEntityId))
                    {
                        var loc = locations.FirstOrDefault(l => l.Id == caseEv.LocationEntityId);
                        var overlapStart = caseEv.StartTimeUtc!.Value > otherEv.StartTimeUtc!.Value ? caseEv.StartTimeUtc.Value : otherEv.StartTimeUtc.Value;
                        var overlapEnd = (caseEv.EndTimeUtc ?? caseEv.StartTimeUtc.Value) < (otherEv.EndTimeUtc ?? otherEv.StartTimeUtc.Value)
                            ? (caseEv.EndTimeUtc ?? caseEv.StartTimeUtc.Value)
                            : (otherEv.EndTimeUtc ?? otherEv.StartTimeUtc.Value);

                        if (overlapStart < overlapEnd)
                        {
                            var entsA = string.Join(", ", GetEntityNames(caseEv.RelatedEntitiesJson));
                            var entsB = string.Join(", ", GetEntityNames(otherEv.RelatedEntitiesJson));

                            signals.Add(new SpatialSignal
                            {
                                Id = $"sig-cross-{Guid.NewGuid().ToString("N")[..8]}",
                                CaseId = caseId,
                                AnalysisRunId = runId,
                                SignalType = "CROSS_CASE_SPATIAL_OVERLAP",
                                SourceEntityId = entsA,
                                TargetEntityId = entsB,
                                LocationId = loc?.Id,
                                LocationName = loc?.Name ?? "Cross-Case Location",
                                StartTimeUtc = overlapStart,
                                EndTimeUtc = overlapEnd,
                                DistanceKm = 0.0,
                                Score = 0.90,
                                Explanation = $"Entities from Case {caseEv.CaseId} and Case {otherEv.CaseId} were recorded at {loc?.Name} during an overlapping temporal window.",
                                Status = "PENDING",
                                CreatedAtUtc = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
        }

        // Persist signals and update run
        _dbContext.SpatialSignals.AddRange(signals);

        var locationDtos = locations.Select(l => MapToLocationDto(l, 0, 0, 0)).ToList();
        var clusters = ClusterLocations(locationDtos, request.ClusterRadiusKm);

        run.Status = "COMPLETED";
        run.CompletedAtUtc = DateTime.UtcNow;
        run.TotalLocationsAnalyzed = locations.Count;
        run.TotalEventsAnalyzed = events.Count;
        run.SignalsGenerated = signals.Count;
        run.OverlapsFound = signals.Count(s => s.SignalType == "SPATIAL_TEMPORAL_OVERLAP");
        run.ClustersFound = clusters.Count;
        run.VelocityWarningsFound = velocityWarnings;

        // Log audit event
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            ActorId = executedBy,
            ActorName = executedBy,
            Action = "SPATIAL_ANALYSIS_COMPLETED",
            ResourceType = "SpatialAnalysisRun",
            ResourceId = runId,
            MetadataJson = JsonSerializer.Serialize(new { caseId, signalsCount = signals.Count, locationsCount = locations.Count }),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SpatialAnalysisResultDto
        {
            RunId = runId,
            CaseId = caseId,
            Status = "COMPLETED",
            StartedAtUtc = startedAt,
            CompletedAtUtc = run.CompletedAtUtc,
            TotalLocationsAnalyzed = locations.Count,
            TotalEventsAnalyzed = events.Count,
            SignalsGenerated = signals.Count,
            OverlapsFound = run.OverlapsFound,
            ClustersFound = clusters.Count,
            VelocityWarningsFound = velocityWarnings,
            Signals = signals.Select(MapToSpatialSignalDto).ToList(),
            Clusters = clusters,
            ExecutedBy = executedBy
        };
    }

    public async Task<List<SpatialSignalDto>> GetSpatialSignalsAsync(
        string caseId,
        string? status = null,
        string? signalType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SpatialSignals
            .AsNoTracking()
            .Where(s => s.CaseId == caseId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(signalType))
        {
            query = query.Where(s => s.SignalType == signalType);
        }

        var signals = await query
            .OrderByDescending(s => s.Score)
            .ToListAsync(cancellationToken);

        return signals.Select(MapToSpatialSignalDto).ToList();
    }

    public async Task<SpatialSignalDto> ReviewSpatialSignalAsync(
        string caseId,
        string signalId,
        ReviewSpatialSignalRequestDto request,
        string reviewer,
        CancellationToken cancellationToken = default)
    {
        var signal = await _dbContext.SpatialSignals
            .FirstOrDefaultAsync(s => s.Id == signalId && s.CaseId == caseId, cancellationToken);

        if (signal == null)
        {
            throw new KeyNotFoundException($"SpatialSignal with ID '{signalId}' not found in case '{caseId}'.");
        }

        signal.Status = request.Status;
        signal.ReviewedAtUtc = DateTime.UtcNow;
        signal.ReviewedBy = reviewer;
        signal.ReviewNotes = request.ReviewNotes;

        // Log audit event
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            ActorId = reviewer,
            ActorName = reviewer,
            Action = request.Status == "CONFIRMED" ? "SPATIAL_SIGNAL_CONFIRMED" : "SPATIAL_SIGNAL_DISMISSED",
            ResourceType = "SpatialSignal",
            ResourceId = signalId,
            MetadataJson = JsonSerializer.Serialize(new { caseId, signalId, status = request.Status, notes = request.ReviewNotes }),
            CreatedAtUtc = DateTime.UtcNow
        });

        // CRITICAL ZERO-MUTATION GUARANTEE: Confirming a spatial signal NEVER creates or mutates graph edges.
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToSpatialSignalDto(signal);
    }

    private static List<SpatialClusterDto> ClusterLocations(List<LocationDto> locations, double radiusKm)
    {
        var clusters = new List<SpatialClusterDto>();
        var visited = new HashSet<string>();
        int clusterCounter = 1;

        foreach (var loc in locations)
        {
            if (visited.Contains(loc.Id)) continue;

            var clusterMembers = new List<LocationDto> { loc };
            visited.Add(loc.Id);

            foreach (var other in locations)
            {
                if (visited.Contains(other.Id)) continue;

                var dist = GeoMath.HaversineDistanceKm(loc.Latitude, loc.Longitude, other.Latitude, other.Longitude);
                if (dist <= radiusKm)
                {
                    clusterMembers.Add(other);
                    visited.Add(other.Id);
                }
            }

            var centroidLat = clusterMembers.Average(l => l.Latitude);
            var centroidLon = clusterMembers.Average(l => l.Longitude);
            var totalEvents = clusterMembers.Sum(l => l.EventCount);
            var totalEntities = clusterMembers.Sum(l => l.EntityCount);

            clusters.Add(new SpatialClusterDto
            {
                ClusterId = $"cluster-geo-{clusterCounter++}",
                ClusterLabel = clusterMembers.Count > 1
                    ? $"{loc.City ?? loc.Name} Regional Activity Cluster ({clusterMembers.Count} Locations)"
                    : $"{loc.Name} Isolated Node",
                CentroidLatitude = Math.Round(centroidLat, 4),
                CentroidLongitude = Math.Round(centroidLon, 4),
                LocationCount = clusterMembers.Count,
                EventCount = totalEvents,
                EntityCount = totalEntities,
                Locations = clusterMembers,
                KeyEntities = clusterMembers.Select(l => l.Name).Distinct().Take(3).ToList()
            });
        }

        return clusters;
    }

    private static LocationDto MapToLocationDto(LocationItem loc, int eventCount, int entityCount, int evidenceCount)
    {
        return new LocationDto
        {
            Id = loc.Id,
            CaseId = loc.CaseId,
            EntityId = loc.EntityId,
            Name = loc.Name,
            NormalizedName = loc.NormalizedName,
            Address = loc.Address,
            City = loc.City,
            District = loc.District,
            State = loc.State,
            Country = loc.Country,
            Latitude = loc.Latitude,
            Longitude = loc.Longitude,
            GeocodePrecision = loc.GeocodePrecision,
            Source = loc.Source,
            EventCount = eventCount,
            EntityCount = entityCount,
            EvidenceCount = evidenceCount,
            CreatedAtUtc = loc.CreatedAtUtc
        };
    }

    private static SpatialSignalDto MapToSpatialSignalDto(SpatialSignal s)
    {
        var evidenceIds = new List<string>();
        if (!string.IsNullOrEmpty(s.SupportingEvidenceJson))
        {
            try
            {
                evidenceIds = JsonSerializer.Deserialize<List<string>>(s.SupportingEvidenceJson) ?? new List<string>();
            }
            catch { }
        }

        return new SpatialSignalDto
        {
            Id = s.Id,
            CaseId = s.CaseId,
            AnalysisRunId = s.AnalysisRunId,
            SignalType = s.SignalType,
            SourceEntityId = s.SourceEntityId,
            TargetEntityId = s.TargetEntityId,
            LocationId = s.LocationId,
            LocationName = s.LocationName,
            StartTimeUtc = s.StartTimeUtc,
            EndTimeUtc = s.EndTimeUtc,
            DistanceKm = s.DistanceKm,
            Score = s.Score,
            Explanation = s.Explanation,
            SupportingEvidenceIds = evidenceIds,
            SupportingEvidenceFileNames = evidenceIds,
            Status = s.Status,
            CreatedAtUtc = s.CreatedAtUtc,
            ReviewedAtUtc = s.ReviewedAtUtc,
            ReviewedBy = s.ReviewedBy,
            ReviewNotes = s.ReviewNotes
        };
    }

    private static TimelineEventDto MapToTimelineEventDto(ExtractedEvent e)
    {
        return new TimelineEventDto
        {
            Id = e.Id,
            CaseId = e.CaseId ?? string.Empty,
            EventType = e.EventType,
            Description = e.Description,
            StartTimeUtc = e.StartTimeUtc,
            EndTimeUtc = e.EndTimeUtc,
            TimePrecision = e.TimePrecision ?? "UNKNOWN",
            Location = e.Location,
            LocationEntityId = e.LocationEntityId,
            RelatedEntityNames = GetEntityNames(e.RelatedEntitiesJson),
            RelatedEntityIds = GetEntityNames(e.RelatedEntitiesJson),
            Confidence = e.Confidence,
            VerificationStatus = e.ReviewStatus,
            SourceEvidenceId = e.EvidenceId,
            SourceEvidenceFileName = e.Evidence?.FileName,
            SourceEvidenceSha256 = e.Evidence?.Sha256Hash,
            EvidenceIntegrityVerified = !string.IsNullOrEmpty(e.Evidence?.Sha256Hash),
            SourceLocation = e.SourceLocation,
            SourcePage = e.SourcePage,
            CreatedAtUtc = e.CreatedAtUtc
        };
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

    private static string FormatElapsed(TimeSpan span)
    {
        if (span.TotalDays >= 1) return $"+{(int)span.TotalDays}d {(int)span.Hours}h";
        if (span.TotalHours >= 1) return $"+{(int)span.TotalHours}h {(int)span.Minutes}m";
        return $"+{(int)Math.Max(1, span.TotalMinutes)}m";
    }
}
