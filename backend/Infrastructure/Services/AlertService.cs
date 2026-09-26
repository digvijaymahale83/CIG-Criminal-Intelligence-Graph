using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly IAppDbContext _dbContext;
    private readonly IEnumerable<IAnomalyDetector> _detectors;
    private readonly IAuditService _auditService;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IAppDbContext dbContext,
        IEnumerable<IAnomalyDetector> detectors,
        IAuditService auditService,
        ILogger<AlertService> logger)
    {
        _dbContext = dbContext;
        _detectors = detectors;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<AlertDto>> GetCaseAlertsAsync(string caseId, AlertQueryDto query, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Alerts
            .AsNoTracking()
            .Where(a => a.CaseId == caseId);

        if (!string.IsNullOrWhiteSpace(query.Status) && query.Status != "ALL")
        {
            q = q.Where(a => a.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Severity) && query.Severity != "ALL")
        {
            q = q.Where(a => a.Severity == query.Severity);
        }

        if (!string.IsNullOrWhiteSpace(query.AlertType) && query.AlertType != "ALL")
        {
            q = q.Where(a => a.AlertType == query.AlertType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            q = q.Where(a => a.SourceEntityId == query.EntityId ||
                             a.TargetEntityId == query.EntityId ||
                             (a.SourceEntityName != null && a.SourceEntityName.Contains(query.EntityId)) ||
                             (a.TargetEntityName != null && a.TargetEntityName.Contains(query.EntityId)));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(a => a.Title.ToLower().Contains(s) ||
                             a.Description.ToLower().Contains(s) ||
                             (a.SourceEntityName != null && a.SourceEntityName.ToLower().Contains(s)) ||
                             (a.TargetEntityName != null && a.TargetEntityName.ToLower().Contains(s)) ||
                             (a.LocationName != null && a.LocationName.ToLower().Contains(s)));
        }

        if (query.StartDate.HasValue)
        {
            q = q.Where(a => a.CreatedAtUtc >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            q = q.Where(a => a.CreatedAtUtc <= query.EndDate.Value);
        }

        int skip = Math.Max(0, (query.Page - 1) * query.PageSize);
        int take = Math.Clamp(query.PageSize, 1, 200);

        var list = await q
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<AlertDto?> GetAlertDetailsAsync(string caseId, string alertId, CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.Alerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alertId && a.CaseId == caseId, cancellationToken);

        return alert != null ? MapToDto(alert) : null;
    }

    public async Task<AlertSummaryDto> GetAlertSummaryAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var alerts = await _dbContext.Alerts
            .AsNoTracking()
            .Where(a => a.CaseId == caseId)
            .ToListAsync(cancellationToken);

        return new AlertSummaryDto
        {
            CaseId = caseId,
            TotalAlerts = alerts.Count,
            NewAlerts = alerts.Count(a => a.Status == "NEW"),
            AcknowledgedAlerts = alerts.Count(a => a.Status == "ACKNOWLEDGED"),
            UnderReviewAlerts = alerts.Count(a => a.Status == "UNDER_REVIEW"),
            ResolvedAlerts = alerts.Count(a => a.Status == "RESOLVED"),
            DismissedAlerts = alerts.Count(a => a.Status == "DISMISSED"),

            CriticalSeverity = alerts.Count(a => a.Severity == "CRITICAL"),
            HighSeverity = alerts.Count(a => a.Severity == "HIGH"),
            MediumSeverity = alerts.Count(a => a.Severity == "MEDIUM"),
            LowSeverity = alerts.Count(a => a.Severity == "LOW"),

            NetworkAnomalies = alerts.Count(a => a.AlertType == "NETWORK_ANOMALY"),
            TemporalAnomalies = alerts.Count(a => a.AlertType == "TEMPORAL_ANOMALY"),
            GeographicAnomalies = alerts.Count(a => a.AlertType == "GEOGRAPHIC_ANOMALY"),
            RelationshipSurges = alerts.Count(a => a.AlertType == "RELATIONSHIP_SURGE"),
            ActivitySpikes = alerts.Count(a => a.AlertType == "ACTIVITY_SPIKE"),
            UnusualTravel = alerts.Count(a => a.AlertType == "UNUSUAL_TRAVEL"),
            DataConsistency = alerts.Count(a => a.AlertType == "DATA_CONSISTENCY"),
            CrossCasePatterns = alerts.Count(a => a.AlertType == "CROSS_CASE_PATTERN"),
            ModelSignals = alerts.Count(a => a.AlertType == "MODEL_SIGNAL")
        };
    }

    public async Task<AlertRunResultDto> RunAlertDetectionAsync(
        string caseId,
        RunAlertDetectionRequestDto request,
        string executedBy,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (caseEntity == null)
        {
            throw new KeyNotFoundException($"Case with ID '{caseId}' was not found.");
        }

        var run = new AlertRun
        {
            Id = Guid.NewGuid().ToString(),
            CaseId = caseId,
            Status = "RUNNING",
            StartedAtUtc = DateTime.UtcNow,
            ExecutedBy = executedBy
        };

        _dbContext.AlertRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            executedBy,
            executedBy,
            "ALERT_DETECTION_STARTED",
            "ALERT_RUN",
            run.Id,
            $"Anomaly detection initiated for case {caseEntity.CaseNumber}",
            null,
            ipAddress,
            cancellationToken);

        var activeDetectors = _detectors
            .Where(d => request.EnabledDetectors == null || request.EnabledDetectors.Contains(d.DetectorType))
            .ToList();

        var generatedSignals = new List<AnomalySignalDto>();

        foreach (var detector in activeDetectors)
        {
            try
            {
                var detectorSignals = await detector.DetectAsync(caseId, request, cancellationToken);
                generatedSignals.AddRange(detectorSignals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detector {DetectorType} failed during alert run on case {CaseId}", detector.DetectorType, caseId);
            }
        }

        // Deduplication and Persistence
        int alertsCreated = 0;
        int alertsDeduplicated = 0;
        var createdAlerts = new List<Alert>();

        var existingActiveAlerts = await _dbContext.Alerts
            .Where(a => a.CaseId == caseId && (a.Status == "NEW" || a.Status == "ACKNOWLEDGED" || a.Status == "UNDER_REVIEW"))
            .ToListAsync(cancellationToken);

        var existingFingerprintMap = existingActiveAlerts.ToDictionary(a => a.DeduplicationFingerprint);

        foreach (var signal in generatedSignals)
        {
            string fingerprint = ComputeFingerprint(caseId, signal);

            if (existingFingerprintMap.TryGetValue(fingerprint, out var existingAlert))
            {
                // Refresh existing alert without duplicating
                existingAlert.Score = Math.Max(existingAlert.Score, signal.Score);
                existingAlert.UpdatedAtUtc = DateTime.UtcNow;
                alertsDeduplicated++;
            }
            else
            {
                // Compute transparent priority severity
                double priorityScore = CalculatePriorityScore(
                    signal.Score,
                    signal.RelatedEvidenceSha256 != null,
                    signal.AlertType == "CROSS_CASE_PATTERN");

                string severity = priorityScore >= 0.80 ? "CRITICAL" :
                                  priorityScore >= 0.65 ? "HIGH" :
                                  priorityScore >= 0.45 ? "MEDIUM" : "LOW";

                // Ensure signal severity doesn't get downgraded if detector specified higher
                if (signal.Severity == "CRITICAL" || signal.Severity == "HIGH")
                {
                    severity = signal.Severity;
                }

                var newAlert = new Alert
                {
                    Id = Guid.NewGuid().ToString(),
                    CaseId = caseId,
                    AlertRunId = run.Id,
                    AlertType = signal.AlertType,
                    Severity = severity,
                    Status = "NEW",
                    Title = signal.Title,
                    Description = signal.Description,
                    SourceEntityId = signal.SourceEntityId,
                    SourceEntityName = signal.SourceEntityName,
                    TargetEntityId = signal.TargetEntityId,
                    TargetEntityName = signal.TargetEntityName,
                    LocationId = signal.LocationId,
                    LocationName = signal.LocationName,
                    RelatedEventId = signal.RelatedEventId,
                    RelatedEvidenceId = signal.RelatedEvidenceId,
                    RelatedEvidenceFileName = signal.RelatedEvidenceFileName,
                    RelatedEvidenceSha256 = signal.RelatedEvidenceSha256,
                    Score = signal.Score,
                    DetectionMethod = signal.DetectionMethod,
                    DetectionVersion = signal.DetectionVersion,
                    Explanation = signal.Explanation,
                    DeduplicationFingerprint = fingerprint,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _dbContext.Alerts.Add(newAlert);
                createdAlerts.Add(newAlert);
                existingFingerprintMap[fingerprint] = newAlert;
                alertsCreated++;
            }
        }

        stopwatch.Stop();

        run.Status = "COMPLETED";
        run.CompletedAtUtc = DateTime.UtcNow;
        run.DetectorsExecuted = activeDetectors.Count;
        run.SignalsGenerated = generatedSignals.Count;
        run.AlertsCreated = alertsCreated;
        run.AlertsDeduplicated = alertsDeduplicated;
        run.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            executedBy,
            executedBy,
            "ALERT_DETECTION_COMPLETED",
            "ALERT_RUN",
            run.Id,
            $"Generated {generatedSignals.Count} signals, created {alertsCreated} alerts, deduplicated {alertsDeduplicated} in {stopwatch.ElapsedMilliseconds}ms",
            null,
            ipAddress,
            cancellationToken);

        return new AlertRunResultDto
        {
            RunId = run.Id,
            CaseId = caseId,
            Status = run.Status,
            StartedAtUtc = run.StartedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            DetectorsExecuted = run.DetectorsExecuted,
            SignalsGenerated = run.SignalsGenerated,
            AlertsCreated = alertsCreated,
            AlertsDeduplicated = alertsDeduplicated,
            ExecutionDurationMs = run.ExecutionDurationMs,
            CreatedAlerts = createdAlerts.Select(MapToDto).ToList()
        };
    }

    public async Task<AlertDto> AcknowledgeAlertAsync(
        string alertId,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert == null) throw new KeyNotFoundException($"Alert with ID '{alertId}' not found.");

        alert.Status = "ACKNOWLEDGED";
        alert.ReviewedBy = actorName;
        alert.ReviewedAtUtc = DateTime.UtcNow;
        alert.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "ALERT_ACKNOWLEDGED",
            "ALERT",
            alert.Id,
            $"Alert {alert.Title} acknowledged by {actorName}",
            null,
            ipAddress,
            cancellationToken);

        return MapToDto(alert);
    }

    public async Task<AlertDto> StartReviewAsync(
        string alertId,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert == null) throw new KeyNotFoundException($"Alert with ID '{alertId}' not found.");

        alert.Status = "UNDER_REVIEW";
        alert.ReviewedBy = actorName;
        alert.ReviewedAtUtc = DateTime.UtcNow;
        alert.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "ALERT_REVIEW_STARTED",
            "ALERT",
            alert.Id,
            $"Alert review initiated for {alert.Title}",
            null,
            ipAddress,
            cancellationToken);

        return MapToDto(alert);
    }

    public async Task<AlertDto> ResolveAlertAsync(
        string alertId,
        string notes,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert == null) throw new KeyNotFoundException($"Alert with ID '{alertId}' not found.");

        alert.Status = "RESOLVED";
        alert.ReviewNotes = notes;
        alert.ReviewedBy = actorName;
        alert.ReviewedAtUtc = DateTime.UtcNow;
        alert.UpdatedAtUtc = DateTime.UtcNow;

        // CRITICAL GUARANTEE: Never mutates or adds knowledge graph edges!
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "ALERT_RESOLVED",
            "ALERT",
            alert.Id,
            $"Alert resolved with notes: {notes}",
            null,
            ipAddress,
            cancellationToken);

        return MapToDto(alert);
    }

    public async Task<AlertDto> DismissAlertAsync(
        string alertId,
        string notes,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert == null) throw new KeyNotFoundException($"Alert with ID '{alertId}' not found.");

        alert.Status = "DISMISSED";
        alert.ReviewNotes = notes;
        alert.ReviewedBy = actorName;
        alert.ReviewedAtUtc = DateTime.UtcNow;
        alert.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "ALERT_DISMISSED",
            "ALERT",
            alert.Id,
            $"Alert dismissed with rationale: {notes}",
            null,
            ipAddress,
            cancellationToken);

        return MapToDto(alert);
    }

    private static string ComputeFingerprint(string caseId, AnomalySignalDto signal)
    {
        string raw = $"{caseId}:{signal.AlertType}:{signal.SourceEntityId ?? signal.SourceEntityName ?? ""}:{signal.TargetEntityId ?? signal.TargetEntityName ?? ""}:{signal.LocationId ?? signal.LocationName ?? ""}:{signal.TimeWindowKey ?? ""}";
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static double CalculatePriorityScore(double score, bool hasEvidence, bool isCrossCase)
    {
        double evidenceScore = hasEvidence ? 1.0 : 0.0;
        double crossCaseScore = isCrossCase ? 1.0 : 0.0;
        return Math.Round((0.40 * Math.Clamp(score, 0.0, 1.0)) + (0.25 * evidenceScore) + (0.20 * crossCaseScore) + 0.15, 4);
    }

    private static AlertDto MapToDto(Alert a) => new()
    {
        Id = a.Id,
        CaseId = a.CaseId,
        AlertRunId = a.AlertRunId,
        AlertType = a.AlertType,
        Severity = a.Severity,
        Status = a.Status,
        Title = a.Title,
        Description = a.Description,
        SourceEntityId = a.SourceEntityId,
        SourceEntityName = a.SourceEntityName,
        TargetEntityId = a.TargetEntityId,
        TargetEntityName = a.TargetEntityName,
        LocationId = a.LocationId,
        LocationName = a.LocationName,
        RelatedEventId = a.RelatedEventId,
        RelatedEvidenceId = a.RelatedEvidenceId,
        RelatedEvidenceFileName = a.RelatedEvidenceFileName,
        RelatedEvidenceSha256 = a.RelatedEvidenceSha256,
        Score = a.Score,
        DetectionMethod = a.DetectionMethod,
        DetectionVersion = a.DetectionVersion,
        Explanation = a.Explanation,
        DeduplicationFingerprint = a.DeduplicationFingerprint,
        CreatedAtUtc = a.CreatedAtUtc,
        UpdatedAtUtc = a.UpdatedAtUtc,
        ReviewedAtUtc = a.ReviewedAtUtc,
        ReviewedBy = a.ReviewedBy,
        ReviewNotes = a.ReviewNotes
    };
}
