using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _dbContext;
    private readonly IInvestigationGraphService _graphService;
    private readonly IGraphAnalyticsService _analyticsService;
    private readonly IAlertService _alertService;
    private readonly ITemporalService _temporalService;
    private readonly IGeospatialService _geospatialService;
    private readonly IIntegrityLedgerService _ledgerService;
    private readonly IEntityResolutionService _resolutionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IAppDbContext dbContext,
        IInvestigationGraphService graphService,
        IGraphAnalyticsService analyticsService,
        IAlertService alertService,
        ITemporalService temporalService,
        IGeospatialService geospatialService,
        IIntegrityLedgerService ledgerService,
        IEntityResolutionService resolutionService,
        IAuditService auditService,
        ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _graphService = graphService;
        _analyticsService = analyticsService;
        _alertService = alertService;
        _temporalService = temporalService;
        _geospatialService = geospatialService;
        _ledgerService = ledgerService;
        _resolutionService = resolutionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<DashboardDto> GetCaseDashboardAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(caseId))
            throw new ArgumentException("Case ID cannot be empty.", nameof(caseId));

        // 1. Authorization & Role Verification
        var normalizedRole = userRole?.Trim().ToUpperInvariant() ?? string.Empty;
        var authorizedRoles = new[] { "ADMIN", "INVESTIGATOR", "ANALYST", "OFFICER", "SUPERVISOR" };
        if (!authorizedRoles.Contains(normalizedRole))
        {
            throw new UnauthorizedAccessException($"User role '{userRole}' is not authorized to access investigation dashboards.");
        }

        // 2. Case Scoping & Resolution
        var caseRecord = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId || c.CaseNumber == caseId, cancellationToken);

        if (caseRecord == null)
        {
            throw new KeyNotFoundException($"Investigation case '{caseId}' was not found.");
        }

        var resolvedCaseId = caseRecord.Id;

        var dashboard = new DashboardDto
        {
            Case = new CaseDto
            {
                Id = caseRecord.Id,
                CaseNumber = caseRecord.CaseNumber,
                Title = caseRecord.Title,
                Description = caseRecord.Description,
                Status = caseRecord.Status,
                Priority = caseRecord.Priority,
                LeadOfficerName = caseRecord.LeadOfficerName,
                Jurisdiction = caseRecord.Jurisdiction,
                District = caseRecord.District,
                FirNumber = caseRecord.FirNumber,
                CreatedAtUtc = caseRecord.CreatedAtUtc,
                UpdatedAtUtc = caseRecord.UpdatedAtUtc
            },
            GeneratedAtUtc = DateTime.UtcNow
        };

        // 3. Service Retrieval for Efficiency (bounded and case-scoped)
        // A. Direct Database Counts
        var entityCount = await _dbContext.Entities
            .AsNoTracking()
            .CountAsync(e => e.CaseId == resolvedCaseId, cancellationToken);

        var relationshipCount = await _dbContext.Relationships
            .AsNoTracking()
            .CountAsync(r => r.CaseId == resolvedCaseId, cancellationToken);

        var evidenceCount = await _dbContext.EvidenceItems
            .AsNoTracking()
            .CountAsync(e => e.CaseId == resolvedCaseId, cancellationToken);

        var modelLeads = await _dbContext.GraphAnalyticalLeads
            .AsNoTracking()
            .Where(l => l.CaseId == resolvedCaseId)
            .OrderByDescending(l => l.Score)
            .Take(10)
            .ToListAsync(cancellationToken);

        // B. Service Orchestration
        var alertSummary = await _alertService.GetAlertSummaryAsync(resolvedCaseId, cancellationToken);
        var highAlerts = await _alertService.GetCaseAlertsAsync(resolvedCaseId, new AlertQueryDto { Severity = "HIGH", PageSize = 5 }, cancellationToken);
        var timelineEvents = await _temporalService.GetCaseTimelineEventsAsync(resolvedCaseId, new TimelineQueryDto { PageSize = 6 }, cancellationToken);
        var locations = await _geospatialService.GetCaseLocationsAsync(resolvedCaseId, null, cancellationToken);
        var crossCaseConnections = await _resolutionService.GetCrossCaseConnectionsAsync(resolvedCaseId, null, null, cancellationToken);
        var crossCaseCandidates = await _resolutionService.GetCandidatesAsync(resolvedCaseId, "PENDING", null, null, cancellationToken);

        // 4. Evidence Integrity Analysis
        var caseEvidences = await _dbContext.EvidenceItems
            .AsNoTracking()
            .Where(e => e.CaseId == resolvedCaseId)
            .Select(e => new { e.Id, e.FileName, e.Sha256Hash, e.ProcessingStatus })
            .ToListAsync(cancellationToken);

        int integrityVerified = 0;
        int integrityModified = 0;
        int integrityUnreconciled = 0;
        var integrityWarnings = new List<IntegrityWarningItemDto>();

        foreach (var ev in caseEvidences)
        {
            try
            {
                var statusDto = await _ledgerService.GetEvidenceIntegrityStatusAsync(ev.Id, cancellationToken);
                if (statusDto.Status == "VERIFIED")
                {
                    integrityVerified++;
                }
                else if (statusDto.Status == "EVIDENCE_MODIFIED" || statusDto.Status == "HASH_MISMATCH")
                {
                    integrityModified++;
                    integrityWarnings.Add(new IntegrityWarningItemDto
                    {
                        EvidenceId = ev.Id,
                        FileName = ev.FileName,
                        ExpectedSha256 = statusDto.RegisteredSha256,
                        CurrentSha256 = statusDto.ActualFileSha256,
                        LedgerBlockIndex = statusDto.BlockIndex,
                        Status = statusDto.Status,
                        Explanation = statusDto.Explanation
                    });
                }
                else
                {
                    integrityUnreconciled++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Integrity status lookup deferred for evidence {EvidenceId}", ev.Id);
                integrityUnreconciled++;
            }
        }

        dashboard.Integrity = new DashboardIntegrityDto
        {
            VerifiedCount = integrityVerified,
            ModifiedCount = integrityModified,
            UnreconciledCount = integrityUnreconciled,
            TotalEvidenceCount = caseEvidences.Count,
            LedgerHealthStatus = integrityModified > 0 ? "WARNING" : "VALID",
            Warnings = integrityWarnings
        };

        // 5. Cross-Case Summary Breakdown (Confirmed vs Potential vs Model-predicted strictly separated)
        var confirmedConnections = crossCaseConnections.Where(c => c.Status == "APPROVED").ToList();
        var potentialCandidates = crossCaseCandidates.Where(c => c.MatchStatus == "PENDING" && c.MatchMethod != "GAT_EMBEDDING").ToList();
        var modelPredictedCandidates = crossCaseCandidates.Where(c => c.MatchMethod == "GAT_EMBEDDING" || c.MatchScore < 0.85).ToList();

        dashboard.CrossCase = new DashboardCrossCaseDto
        {
            ConfirmedCount = confirmedConnections.Count,
            PotentialCount = potentialCandidates.Count,
            ModelPredictedCount = modelPredictedCandidates.Count,
            Connections = confirmedConnections.Take(4).Select(c => new CrossCaseItemDto
            {
                ConnectionId = c.Id,
                SourceCaseId = c.SourceCaseId,
                TargetCaseId = c.TargetCaseId,
                TargetCaseNumber = c.TargetCaseNumber,
                TargetCaseTitle = c.TargetEntityName,
                Reason = c.Explanation,
                SupportingEvidenceCount = c.SupportingEvidence.Count,
                Status = "CONFIRMED",
                Confidence = c.Confidence,
                ConnectionType = c.ConnectionType
            }).Concat(potentialCandidates.Take(3).Select(p => new CrossCaseItemDto
            {
                ConnectionId = p.Id,
                SourceCaseId = p.SourceCaseId,
                TargetCaseId = p.TargetCaseId,
                TargetCaseNumber = p.TargetCaseNumber ?? p.TargetCaseId,
                TargetCaseTitle = p.TargetEntity?.CanonicalName ?? p.TargetEntityId,
                Reason = $"Potential match on {p.EntityType} via {p.MatchMethod}",
                SupportingEvidenceCount = p.Factors.Count,
                Status = "POTENTIAL",
                Confidence = p.MatchScore,
                ConnectionType = "POTENTIAL_MATCH"
            })).ToList()
        };

        // 6. Summary Counts
        dashboard.Summary = new DashboardSummaryDto
        {
            EntityCount = entityCount,
            RelationshipCount = relationshipCount,
            EvidenceCount = evidenceCount,
            AlertCount = alertSummary.TotalAlerts,
            HighRiskAlerts = alertSummary.CriticalSeverity + alertSummary.HighSeverity,
            CrossCaseTotalCount = confirmedConnections.Count + potentialCandidates.Count + modelPredictedCandidates.Count,
            CrossCaseConfirmedCount = confirmedConnections.Count,
            CrossCasePotentialCount = potentialCandidates.Count,
            CrossCaseModelCount = modelPredictedCandidates.Count,
            ModelSignalCount = modelLeads.Count,
            IntegrityVerifiedCount = integrityVerified,
            IntegrityModifiedCount = integrityModified,
            IntegrityUnreconciledCount = integrityUnreconciled
        };

        // 7. Network Graph Bounded Preview
        try
        {
            var graphDto = await _graphService.GetCaseGraphAsync(
                resolvedCaseId,
                entityType: null,
                relationshipType: null,
                depth: 1,
                search: null,
                limit: 25,
                userId: userId,
                userRole: userRole,
                cancellationToken: cancellationToken);

            var networkStats = await _analyticsService.GetNetworkStatisticsAsync(
                resolvedCaseId,
                userId,
                userRole,
                cancellationToken);

            var topDegrees = await _analyticsService.GetCentralityMetricsAsync(
                resolvedCaseId,
                sortBy: "degree",
                limit: 3,
                userId: userId,
                userRole: userRole,
                cancellationToken: cancellationToken);

            dashboard.Network = new DashboardNetworkDto
            {
                NodeCount = graphDto.Nodes.Count,
                RelationshipCount = graphDto.Edges.Count,
                ComponentCount = networkStats.ConnectedComponents,
                TopConnectedEntities = topDegrees.Metrics.Take(3).Select(s => new TopConnectedEntityDto
                {
                    EntityId = s.EntityId,
                    CanonicalName = s.EntityName,
                    EntityType = s.EntityType,
                    Degree = s.Degree
                }).ToList(),
                Nodes = graphDto.Nodes.Take(20).Select(n => new NetworkNodePreviewDto
                {
                    Id = n.Id,
                    Label = n.Label,
                    Type = n.Type,
                    Status = n.Verified ? "VERIFIED" : "UNVERIFIED",
                    Degree = n.ConnectionsCount
                }).ToList(),
                Edges = graphDto.Edges.Take(30).Select(e => new NetworkEdgePreviewDto
                {
                    Id = e.Id,
                    SourceId = e.Source,
                    TargetId = e.Target,
                    Type = e.Type,
                    Confidence = e.Confidence,
                    Status = e.Confidence >= 0.7 ? "VERIFIED" : "UNVERIFIED"
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bounded graph preview deferred for case {CaseId}: {Message}", resolvedCaseId, ex.Message);
            // Fallback to direct DB entity/relationship preview
            var sampleEntities = await _dbContext.Entities
                .AsNoTracking()
                .Where(e => e.CaseId == resolvedCaseId)
                .Take(15)
                .ToListAsync(cancellationToken);

            dashboard.Network = new DashboardNetworkDto
            {
                NodeCount = entityCount,
                RelationshipCount = relationshipCount,
                ComponentCount = 1,
                Nodes = sampleEntities.Select(e => new NetworkNodePreviewDto
                {
                    Id = e.Id,
                    Label = e.CanonicalName,
                    Type = e.Type,
                    Status = e.VerificationStatus
                }).ToList()
            };
        }

        // 8. Alerts Breakdown
        dashboard.Alerts = new DashboardAlertsDto
        {
            BySeverity = new DashboardAlertSeverityCountsDto
            {
                Critical = alertSummary.CriticalSeverity,
                High = alertSummary.HighSeverity,
                Medium = alertSummary.MediumSeverity,
                Low = alertSummary.LowSeverity
            },
            ByStatus = new DashboardAlertStatusCountsDto
            {
                NewCount = alertSummary.NewAlerts,
                UnderReviewCount = alertSummary.UnderReviewAlerts,
                ResolvedCount = alertSummary.ResolvedAlerts
            },
            HighPriorityAlerts = highAlerts
        };

        // 9. Prioritized Signals (Alerts + Cross-Case + GAT Model Signals)
        var signals = new List<DashboardSignalDto>();

        foreach (var al in highAlerts.Take(3))
        {
            signals.Add(new DashboardSignalDto
            {
                Id = al.Id,
                Title = al.Title,
                Type = al.AlertType,
                Priority = al.Severity,
                WhyItMatters = al.Description,
                EvidenceCount = string.IsNullOrWhiteSpace(al.RelatedEvidenceId) ? 0 : 1,
                Status = al.Status,
                CreatedAtUtc = al.CreatedAtUtc,
                ActionUrl = $"/cases/{resolvedCaseId}/alerts?alertId={al.Id}"
            });
        }

        foreach (var lead in modelLeads.Take(2))
        {
            signals.Add(new DashboardSignalDto
            {
                Id = lead.Id,
                Title = $"Model-generated lead: {lead.SuggestedRelationshipType}",
                Type = "MODEL_SIGNAL",
                Priority = lead.Score >= 0.85 ? "HIGH" : "MEDIUM",
                WhyItMatters = $"GAT structural link hypothesis with confidence score {lead.Score:F2}. Requires human corroboration.",
                EvidenceCount = 0,
                Status = lead.Status,
                ModelScore = lead.Score,
                ModelName = lead.ModelVersion,
                CreatedAtUtc = lead.CreatedAtUtc,
                ActionUrl = $"/analytics?leadId={lead.Id}"
            });
        }

        foreach (var cand in potentialCandidates.Take(2))
        {
            signals.Add(new DashboardSignalDto
            {
                Id = cand.Id,
                Title = $"Potential cross-case connection with {cand.TargetCaseNumber}",
                Type = "CROSS_CASE",
                Priority = "HIGH",
                WhyItMatters = $"Candidate identifier overlap detected between cases. Score: {cand.MatchScore:P0}.",
                EvidenceCount = cand.Factors.Count,
                Status = cand.MatchStatus,
                CreatedAtUtc = cand.CreatedAtUtc,
                ActionUrl = $"/entity-resolution?candidateId={cand.Id}"
            });
        }

        dashboard.Signals = signals.OrderByDescending(s => s.Priority == "CRITICAL" ? 2 : s.Priority == "HIGH" ? 1 : 0)
                                   .ThenByDescending(s => s.CreatedAtUtc)
                                   .Take(6)
                                   .ToList();

        // 10. Timeline Preview with Temporal Precision Preservation
        dashboard.Timeline = timelineEvents.Take(5).Select(t =>
        {
            var timestamp = t.StartTimeUtc ?? DateTime.UtcNow;
            string formattedTime;
            if (t.TimePrecision == "DATE_ONLY" || t.TimePrecision == "DAY")
            {
                formattedTime = timestamp.ToString("yyyy-MM-dd");
            }
            else if (t.TimePrecision == "UNKNOWN" || t.TimePrecision == "TIME_UNAVAILABLE")
            {
                formattedTime = "Time unavailable";
            }
            else
            {
                formattedTime = timestamp.ToString("yyyy-MM-dd HH:mm");
            }

            var evIds = !string.IsNullOrWhiteSpace(t.SourceEvidenceId)
                ? new List<string> { t.SourceEvidenceId }
                : new List<string>();

            return new DashboardTimelineEventDto
            {
                EventId = t.Id,
                EventType = t.EventType,
                EventTimestampUtc = timestamp,
                Precision = t.TimePrecision,
                FormattedTime = formattedTime,
                Description = t.Description,
                Location = t.Location,
                EvidenceIds = evIds
            };
        }).ToList();

        // 11. Locations Preview
        dashboard.Locations = locations.Take(5).Select(l =>
        {
            bool hasCoords = Math.Abs(l.Latitude) > 0.0001 || Math.Abs(l.Longitude) > 0.0001;

            return new DashboardLocationDto
            {
                LocationId = l.Id,
                Name = l.Name,
                Latitude = hasCoords ? l.Latitude : null,
                Longitude = hasCoords ? l.Longitude : null,
                HasCoordinates = hasCoords,
                CoordinateDisplay = hasCoords
                    ? $"{l.Latitude:F4}, {l.Longitude:F4}"
                    : "Location recorded without coordinates",
                ActivityCount = l.EventCount,
                LastActivityUtc = l.CreatedAtUtc
            };
        }).ToList();

        // 12. Investigator Actions Queue (Real Pending Tasks)
        var actions = new List<DashboardActionItemDto>();

        // Pending entity matches
        foreach (var cand in crossCaseCandidates.Where(c => c.MatchStatus == "PENDING").Take(3))
        {
            actions.Add(new DashboardActionItemDto
            {
                Id = $"act-match-{cand.Id}",
                Type = "ENTITY_MATCH",
                Priority = cand.MatchScore >= 0.85 ? "HIGH" : "MEDIUM",
                Description = $"Review entity match candidate ({cand.EntityType}) with {cand.TargetCaseNumber}",
                CaseId = resolvedCaseId,
                CaseNumber = caseRecord.CaseNumber,
                Status = "PENDING",
                ActionLabel = "Review Match",
                ActionUrl = $"/entity-resolution?candidateId={cand.Id}",
                CreatedAtUtc = cand.CreatedAtUtc
            });
        }

        // Unverified / New Alerts
        foreach (var al in highAlerts.Where(a => a.Status == "NEW").Take(3))
        {
            actions.Add(new DashboardActionItemDto
            {
                Id = $"act-alert-{al.Id}",
                Type = "ALERT_VERIFICATION",
                Priority = al.Severity,
                Description = $"Acknowledge and review {al.Severity.ToLower()} priority alert: {al.Title}",
                CaseId = resolvedCaseId,
                CaseNumber = caseRecord.CaseNumber,
                Status = "PENDING",
                ActionLabel = "Verify Alert",
                ActionUrl = $"/cases/{resolvedCaseId}/alerts?alertId={al.Id}",
                CreatedAtUtc = al.CreatedAtUtc
            });
        }

        // Pending GAT Model Signals
        foreach (var ml in modelLeads.Where(l => l.Status == "PENDING").Take(2))
        {
            actions.Add(new DashboardActionItemDto
            {
                Id = $"act-model-{ml.Id}",
                Type = "MODEL_SIGNAL",
                Priority = ml.Score >= 0.85 ? "HIGH" : "MEDIUM",
                Description = $"Review GAT model link prediction ({ml.Score:P0} score) requiring investigator sign-off",
                CaseId = resolvedCaseId,
                CaseNumber = caseRecord.CaseNumber,
                Status = "PENDING",
                ActionLabel = "Review Signal",
                ActionUrl = $"/analytics?leadId={ml.Id}",
                CreatedAtUtc = ml.CreatedAtUtc
            });
        }

        // Unreconciled Evidence Items
        if (integrityUnreconciled > 0)
        {
            actions.Add(new DashboardActionItemDto
            {
                Id = $"act-ledger-{resolvedCaseId}",
                Type = "INTEGRITY_VERIFY",
                Priority = "MEDIUM",
                Description = $"{integrityUnreconciled} evidence record(s) pending registration in cryptographic integrity ledger",
                CaseId = resolvedCaseId,
                CaseNumber = caseRecord.CaseNumber,
                Status = "PENDING",
                ActionLabel = "Reconcile Ledger",
                ActionUrl = "/audit",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        dashboard.Actions = actions.OrderByDescending(a => a.Priority == "CRITICAL" ? 2 : a.Priority == "HIGH" ? 1 : 0)
                                   .Take(6)
                                   .ToList();

        // 13. Recent Activity Stream (Real Audit Records)
        var recentAudits = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.ResourceId == resolvedCaseId || a.ResourceId == caseRecord.CaseNumber || (a.ResourceType == "Case" && a.ResourceId == resolvedCaseId))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(8)
            .ToListAsync(cancellationToken);

        dashboard.RecentActivity = recentAudits.Select(a => new DashboardActivityDto
        {
            Id = a.Id,
            Action = a.Action,
            Details = a.MetadataJson ?? a.Action,
            ActorName = a.ActorName,
            ResourceType = a.ResourceType ?? "System",
            ResourceId = a.ResourceId,
            TimestampUtc = a.CreatedAtUtc
        }).ToList();

        // 14. Data Quality Warnings
        var dataWarnings = new List<DashboardDataQualityWarningDto>();

        var uncoordinatedCount = locations.Count(l => Math.Abs(l.Latitude) < 0.0001 && Math.Abs(l.Longitude) < 0.0001);
        if (uncoordinatedCount > 0)
        {
            dataWarnings.Add(new DashboardDataQualityWarningDto
            {
                Type = "MISSING_COORDINATES",
                Message = $"{uncoordinatedCount} investigation location(s) recorded without geographic coordinates.",
                ActionUrl = "/map"
            });
        }

        var unverifiedRelationshipsCount = await _dbContext.Relationships
            .AsNoTracking()
            .CountAsync(r => r.CaseId == resolvedCaseId && r.Confidence < 0.7, cancellationToken);

        if (unverifiedRelationshipsCount > 0)
        {
            dataWarnings.Add(new DashboardDataQualityWarningDto
            {
                Type = "UNVERIFIED_RELATIONSHIP",
                Message = $"{unverifiedRelationshipsCount} low-confidence relationship connection(s) require primary source evidence corroboration.",
                ActionUrl = "/network"
            });
        }

        dashboard.DataQualityWarnings = dataWarnings;

        return dashboard;
    }
}
