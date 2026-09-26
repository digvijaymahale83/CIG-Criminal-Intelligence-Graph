using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Infrastructure.Services.Detectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Unit;

public class Phase8AlertAndAnomalyTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly AlertService _alertService;
    private readonly List<IAnomalyDetector> _detectors;

    private readonly Case _case1;
    private readonly Case _case2;
    private readonly Evidence _evidence1;
    private readonly LocationItem _locPune;
    private readonly LocationItem _locMumbai;

    public Phase8AlertAndAnomalyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);

        // Seed Cases
        _case1 = new Case
        {
            Id = "case-2026-001",
            CaseNumber = "CASE-2026-001",
            Title = "Operation Pune Network",
            Status = "Active"
        };
        _case2 = new Case
        {
            Id = "case-2026-002",
            CaseNumber = "CASE-2026-002",
            Title = "Operation Mumbai Corridor",
            Status = "Active"
        };
        _dbContext.Cases.AddRange(_case1, _case2);

        // Seed Evidence
        _evidence1 = new Evidence
        {
            Id = "ev-001",
            CaseId = _case1.Id,
            FileName = "surveillance_log.txt",
            Sha256Hash = "abc123hash",
            StoragePath = "2026-08/surveillance_log.txt"
        };
        _dbContext.EvidenceItems.Add(_evidence1);

        // Seed Locations
        _locPune = new LocationItem
        {
            Id = "loc-pune",
            CaseId = _case1.Id,
            Name = "Shivajinagar Station",
            NormalizedName = "shivajinagar station",
            Latitude = 18.5314,
            Longitude = 73.8446,
            City = "Pune"
        };
        _locMumbai = new LocationItem
        {
            Id = "loc-mumbai",
            CaseId = _case1.Id,
            Name = "Mumbai Dock Terminal",
            NormalizedName = "mumbai dock terminal",
            Latitude = 18.9220,
            Longitude = 72.8347,
            City = "Mumbai"
        };
        _dbContext.Locations.AddRange(_locPune, _locMumbai);
        _dbContext.SaveChanges();

        // Instantiate detectors
        _detectors = new List<IAnomalyDetector>
        {
            new NetworkAnomalyDetector(_dbContext, NullLogger<NetworkAnomalyDetector>.Instance),
            new TemporalAnomalyDetector(_dbContext, NullLogger<TemporalAnomalyDetector>.Instance),
            new GeographicAnomalyDetector(_dbContext, NullLogger<GeographicAnomalyDetector>.Instance),
            new RelationshipSurgeDetector(_dbContext, NullLogger<RelationshipSurgeDetector>.Instance),
            new ActivitySpikeDetector(_dbContext, NullLogger<ActivitySpikeDetector>.Instance),
            new UnusualTravelDetector(_dbContext, NullLogger<UnusualTravelDetector>.Instance),
            new DataConsistencyDetector(_dbContext, NullLogger<DataConsistencyDetector>.Instance),
            new CrossCasePatternDetector(_dbContext, NullLogger<CrossCasePatternDetector>.Instance),
            new ModelSignalDetector(_dbContext, NullLogger<ModelSignalDetector>.Instance)
        };

        var auditService = new AuditService(_dbContext, NullLogger<AuditService>.Instance);

        _alertService = new AlertService(
            _dbContext,
            _detectors,
            auditService,
            NullLogger<AlertService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Test01_NetworkAnomalyDetector_SurfacesHighDegreeNexusNode()
    {
        // Entity A connects to 5 other entities, while average degree is ~1.6
        var entHub = new EntityItem { Id = "ent-hub", CaseId = _case1.Id, CanonicalName = "Target Hub" };
        var others = Enumerable.Range(1, 5)
            .Select(i => new EntityItem { Id = $"ent-sub-{i}", CaseId = _case1.Id, CanonicalName = $"Sub Entity {i}" })
            .ToList();

        _dbContext.Entities.Add(entHub);
        _dbContext.Entities.AddRange(others);

        foreach (var sub in others)
        {
            _dbContext.Relationships.Add(new Relationship
            {
                Id = $"rel-hub-{sub.Id}",
                CaseId = _case1.Id,
                SourceEntityId = entHub.Id,
                TargetEntityId = sub.Id,
                Type = "COMMUNICATES_WITH"
            });
        }
        await _dbContext.SaveChangesAsync();

        var detector = new NetworkAnomalyDetector(_dbContext, NullLogger<NetworkAnomalyDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto());

        signals.Should().ContainSingle(s => s.SourceEntityId == entHub.Id);
        var sig = signals.First(s => s.SourceEntityId == entHub.Id);
        sig.AlertType.Should().Be("NETWORK_ANOMALY");
        sig.Title.Should().Contain("Target Hub");
        sig.Score.Should().BeGreaterThanOrEqualTo(0.70);
    }

    [Fact]
    public async Task Test02_TemporalAnomalyDetector_DetectsRollingBurst()
    {
        var baseTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        var events = new List<ExtractedEvent>
        {
            new() { Id = "ev-b1", CaseId = _case1.Id, EvidenceId = _evidence1.Id, Description = "Event 1", StartTimeUtc = baseTime.AddHours(1), RelatedEntitiesJson = "[\"Vikram Gaikwad\"]", ReviewStatus = "APPROVED" },
            new() { Id = "ev-b2", CaseId = _case1.Id, EvidenceId = _evidence1.Id, Description = "Event 2", StartTimeUtc = baseTime.AddHours(4), RelatedEntitiesJson = "[\"Vikram Gaikwad\"]", ReviewStatus = "APPROVED" },
            new() { Id = "ev-b3", CaseId = _case1.Id, EvidenceId = _evidence1.Id, Description = "Event 3", StartTimeUtc = baseTime.AddHours(10), RelatedEntitiesJson = "[\"Vikram Gaikwad\"]", ReviewStatus = "APPROVED" },
            new() { Id = "ev-b4", CaseId = _case1.Id, EvidenceId = _evidence1.Id, Description = "Event 4", StartTimeUtc = baseTime.AddHours(20), RelatedEntitiesJson = "[\"Vikram Gaikwad\"]", ReviewStatus = "APPROVED" },
            new() { Id = "ev-b5", CaseId = _case1.Id, EvidenceId = _evidence1.Id, Description = "Event 5", StartTimeUtc = baseTime.AddHours(120), RelatedEntitiesJson = "[\"Vikram Gaikwad\"]", ReviewStatus = "APPROVED" }
        };

        _dbContext.ExtractedEvents.AddRange(events);
        await _dbContext.SaveChangesAsync();

        var detector = new TemporalAnomalyDetector(_dbContext, NullLogger<TemporalAnomalyDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto());

        signals.Should().Contain(s => s.AlertType == "TEMPORAL_ANOMALY" && s.SourceEntityName == "Vikram Gaikwad");
        var burst = signals.First(s => s.SourceEntityName == "Vikram Gaikwad");
        burst.Title.Should().Contain("Temporal Activity Burst");
        burst.Explanation.Should().Contain("Vikram Gaikwad");
    }

    [Fact]
    public async Task Test03_GeographicAnomalyDetector_DetectsCentroidDisparity()
    {
        var baseTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        var ev1 = new ExtractedEvent
        {
            Id = "ev-geo-1",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            LocationEntityId = _locPune.Id,
            StartTimeUtc = baseTime,
            RelatedEntitiesJson = "[\"Arjun Kadam\"]",
            ReviewStatus = "APPROVED"
        };
        // Mumbai is ~120km away from Pune (> 100km threshold)
        var ev2 = new ExtractedEvent
        {
            Id = "ev-geo-2",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            LocationEntityId = _locMumbai.Id,
            StartTimeUtc = baseTime.AddDays(2),
            RelatedEntitiesJson = "[\"Arjun Kadam\"]",
            ReviewStatus = "APPROVED"
        };

        _dbContext.ExtractedEvents.AddRange(ev1, ev2);
        await _dbContext.SaveChangesAsync();

        var detector = new GeographicAnomalyDetector(_dbContext, NullLogger<GeographicAnomalyDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto { GeographicDistanceThresholdKm = 100.0 });

        signals.Should().Contain(s => s.AlertType == "GEOGRAPHIC_ANOMALY" && s.SourceEntityName == "Arjun Kadam");
        var outlier = signals.First(s => s.SourceEntityName == "Arjun Kadam");
        outlier.Title.Should().Contain("Geographic Outlier Activity");
        outlier.LocationId.Should().Be(_locMumbai.Id);
    }

    [Fact]
    public async Task Test04_RelationshipSurgeDetector_DetectsDyadicInteractionSurge()
    {
        var baseTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        var surgeEvents = Enumerable.Range(1, 4).Select(i => new ExtractedEvent
        {
            Id = $"ev-surge-{i}",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Description = $"Joint meeting {i}",
            StartTimeUtc = baseTime.AddHours(i * 3),
            RelatedEntitiesJson = "[\"Alpha\",\"Beta\"]",
            ReviewStatus = "APPROVED"
        }).ToList();

        _dbContext.ExtractedEvents.AddRange(surgeEvents);
        await _dbContext.SaveChangesAsync();

        var detector = new RelationshipSurgeDetector(_dbContext, NullLogger<RelationshipSurgeDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto { RelationshipSurgeThreshold = 4 });

        signals.Should().ContainSingle(s => s.AlertType == "RELATIONSHIP_SURGE");
        var sig = signals.First();
        sig.Title.Should().Contain("Alpha");
        sig.Title.Should().Contain("Beta");
        sig.Explanation.Should().Contain("Surge in co-recorded interactions");
    }

    [Fact]
    public async Task Test05_ActivitySpikeDetector_DetectsZScoreSpike()
    {
        // 5 baseline days with 1 event each, then 1 day with 16 events
        var baseDate = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var events = new List<ExtractedEvent>();
        for (int day = 0; day < 5; day++)
        {
            events.Add(new ExtractedEvent
            {
                Id = $"ev-base-{day}",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                StartTimeUtc = baseDate.AddDays(day),
                ReviewStatus = "APPROVED"
            });
        }
        // Spike on day 6
        for (int i = 0; i < 16; i++)
        {
            events.Add(new ExtractedEvent
            {
                Id = $"ev-spike-{i}",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                StartTimeUtc = baseDate.AddDays(6).AddMinutes(i * 10),
                ReviewStatus = "APPROVED"
            });
        }

        _dbContext.ExtractedEvents.AddRange(events);
        await _dbContext.SaveChangesAsync();

        var detector = new ActivitySpikeDetector(_dbContext, NullLogger<ActivitySpikeDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto());

        signals.Should().ContainSingle(s => s.AlertType == "ACTIVITY_SPIKE");
        var sig = signals.First();
        sig.Severity.Should().BeOneOf("HIGH", "CRITICAL");
        sig.Title.Should().Contain("2026-03-07");
    }

    [Fact]
    public async Task Test06_UnusualTravelDetector_DetectsImplausibleVelocity()
    {
        // Transition between Pune and Mumbai (~120km) in 5 minutes (0.083 hours) -> ~1440 km/h
        var t1 = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddMinutes(5);

        var ev1 = new ExtractedEvent
        {
            Id = "ev-spd-1",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            LocationEntityId = _locPune.Id,
            StartTimeUtc = t1,
            RelatedEntitiesJson = "[\"Rapid Target\"]",
            ReviewStatus = "APPROVED"
        };
        var ev2 = new ExtractedEvent
        {
            Id = "ev-spd-2",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            LocationEntityId = _locMumbai.Id,
            StartTimeUtc = t2,
            RelatedEntitiesJson = "[\"Rapid Target\"]",
            ReviewStatus = "APPROVED"
        };

        _dbContext.ExtractedEvents.AddRange(ev1, ev2);
        await _dbContext.SaveChangesAsync();

        var detector = new UnusualTravelDetector(_dbContext, NullLogger<UnusualTravelDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto());

        signals.Should().ContainSingle(s => s.AlertType == "UNUSUAL_TRAVEL");
        var sig = signals.First();
        sig.Title.Should().Contain("Implausible Travel Velocity");
        sig.Explanation.Should().Contain("km/h");
        sig.Score.Should().BeGreaterThanOrEqualTo(0.85);
    }

    [Fact]
    public async Task Test07_DataConsistencyDetector_DetectsTimestampInversionAndOutOfBoundsCoordinates()
    {
        // 1. Inverted timestamps
        var invertedEv = new ExtractedEvent
        {
            Id = "ev-inv-001",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Description = "Chronologically Inverted Meeting",
            StartTimeUtc = new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc), // End < Start!
            ReviewStatus = "APPROVED"
        };

        // 2. Out of bounds geodetic coordinates
        var badLoc = new LocationItem
        {
            Id = "loc-bad-coords",
            CaseId = _case1.Id,
            Name = "Invalid Geodetic Site",
            Latitude = 115.0, // Exceeds 90.0!
            Longitude = 73.0
        };

        _dbContext.ExtractedEvents.Add(invertedEv);
        _dbContext.Locations.Add(badLoc);
        await _dbContext.SaveChangesAsync();

        var detector = new DataConsistencyDetector(_dbContext, NullLogger<DataConsistencyDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto());

        signals.Should().HaveCount(2);
        signals.Should().Contain(s => s.AlertType == "DATA_CONSISTENCY" && s.Title.Contains("Temporal Inversion Anomaly"));
        signals.Should().Contain(s => s.AlertType == "DATA_CONSISTENCY" && s.Title.Contains("Geodetic Coordinate Out of Bounds"));
    }

    [Fact]
    public async Task Test08_CrossCasePatternDetector_SurfacesAuthorizedCrossCaseConnections()
    {
        var crossEnt = new EntityItem { Id = "ent-cross-1", CaseId = _case1.Id, CanonicalName = "Shared Mule" };
        var crossEnt2 = new EntityItem { Id = "ent-cross-2", CaseId = _case2.Id, CanonicalName = "Target Mule" };
        var conn = new CrossCaseConnection
        {
            Id = "ccc-001",
            SourceCaseId = _case1.Id,
            TargetCaseId = _case2.Id,
            SourceEntityId = crossEnt.Id,
            TargetEntityId = crossEnt2.Id,
            SourceEntity = crossEnt,
            TargetEntity = crossEnt2,
            Confidence = 0.95,
            Status = "APPROVED",
            Explanation = "Matched cellular terminal across Pune and Mumbai port investigations."
        };

        _dbContext.Entities.AddRange(crossEnt, crossEnt2);
        _dbContext.CrossCaseConnections.Add(conn);
        await _dbContext.SaveChangesAsync();

        var detector = new CrossCasePatternDetector(_dbContext, NullLogger<CrossCasePatternDetector>.Instance);

        // When IncludeCrossCase is true
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto { IncludeCrossCase = true });
        signals.Should().Contain(s => s.AlertType == "CROSS_CASE_PATTERN" && s.Title.Contains("Shared Mule"));

        // When IncludeCrossCase is false
        var signalsDisabled = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto { IncludeCrossCase = false });
        signalsDisabled.Should().BeEmpty();
    }

    [Fact]
    public async Task Test09_ModelSignalDetector_SurfacesPendingGATLeads()
    {
        var ent1 = new EntityItem { Id = "ent-gat-1", CaseId = _case1.Id, CanonicalName = "GAT Node Alpha" };
        var ent2 = new EntityItem { Id = "ent-gat-2", CaseId = _case1.Id, CanonicalName = "GAT Node Beta" };
        var gatLead = new GraphAnalyticalLead
        {
            Id = "lead-gat-001",
            CaseId = _case1.Id,
            SourceEntityId = ent1.Id,
            TargetEntityId = ent2.Id,
            Status = "PENDING",
            Score = 0.89,
            SuggestedRelationshipType = "ASSOCIATE_OF",
            ExplanationJson = "Attentional embedding proximity exceeds threshold.",
            ModelVersion = "GAT-v1.0"
        };

        _dbContext.Entities.AddRange(ent1, ent2);
        _dbContext.GraphAnalyticalLeads.Add(gatLead);
        await _dbContext.SaveChangesAsync();

        var detector = new ModelSignalDetector(_dbContext, NullLogger<ModelSignalDetector>.Instance);
        var signals = await detector.DetectAsync(_case1.Id, new RunAlertDetectionRequestDto());

        signals.Should().ContainSingle(s => s.AlertType == "MODEL_SIGNAL");
        var sig = signals.First();
        sig.Title.Should().Contain("GAT Node Alpha");
        sig.Title.Should().Contain("GAT Node Beta");
        sig.Score.Should().Be(0.89);
    }

    [Fact]
    public async Task Test10_AlertService_RunsDetection_PersistsAlertsAndAlertRun()
    {
        // Seed an inverted event to guarantee a detector triggers
        _dbContext.ExtractedEvents.Add(new ExtractedEvent
        {
            Id = "ev-trig-001",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Description = "Trigger Event",
            StartTimeUtc = new DateTime(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc),
            ReviewStatus = "APPROVED"
        });
        await _dbContext.SaveChangesAsync();

        var result = await _alertService.RunAlertDetectionAsync(_case1.Id, new RunAlertDetectionRequestDto(), "DCP Sharma");

        result.Status.Should().Be("COMPLETED");
        result.AlertsCreated.Should().BeGreaterThan(0);
        result.RunId.Should().NotBeNullOrEmpty();

        var runInDb = await _dbContext.AlertRuns.FindAsync(result.RunId);
        runInDb.Should().NotBeNull();
        runInDb!.Status.Should().Be("COMPLETED");
        runInDb.ExecutedBy.Should().Be("DCP Sharma");

        var alertsInDb = await _dbContext.Alerts.Where(a => a.CaseId == _case1.Id).ToListAsync();
        alertsInDb.Should().NotBeEmpty();
        alertsInDb.All(a => a.Status == "NEW").Should().BeTrue();
    }

    [Fact]
    public async Task Test11_AlertService_DeduplicatesAcrossRepeatedRuns()
    {
        // Seed an inverted event
        _dbContext.ExtractedEvents.Add(new ExtractedEvent
        {
            Id = "ev-dup-001",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Description = "Deduplication Test Event",
            StartTimeUtc = new DateTime(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc),
            ReviewStatus = "APPROVED"
        });
        await _dbContext.SaveChangesAsync();

        // Run 1: Should create alert
        var run1 = await _alertService.RunAlertDetectionAsync(_case1.Id, new RunAlertDetectionRequestDto(), "DCP Sharma");
        run1.AlertsCreated.Should().Be(1);
        run1.AlertsDeduplicated.Should().Be(0);

        var initialAlerts = await _dbContext.Alerts.Where(a => a.CaseId == _case1.Id).ToListAsync();
        string alertId = initialAlerts.First().Id;

        // Run 2: Same data -> Should deduplicate, 0 new alerts
        var run2 = await _alertService.RunAlertDetectionAsync(_case1.Id, new RunAlertDetectionRequestDto(), "DCP Sharma");
        run2.AlertsCreated.Should().Be(0);
        run2.AlertsDeduplicated.Should().Be(1);

        var finalAlerts = await _dbContext.Alerts.Where(a => a.CaseId == _case1.Id).ToListAsync();
        finalAlerts.Should().HaveCount(1);
        finalAlerts.First().Id.Should().Be(alertId); // Alert identity preserved
    }

    [Fact]
    public void Test12_AlertService_CalculatesExplainablePriorityFormula_Correctly()
    {
        // Bounded formula: (0.40 * Score) + (0.25 * Evidence) + (0.20 * CrossCase) + 0.15
        
        // Min: Score=0, Ev=False, Cross=False -> 0.15
        AlertService.CalculatePriorityScore(0.0, false, false).Should().BeApproximately(0.15, 0.001);

        // Max: Score=1.0, Ev=True, Cross=True -> 0.40 + 0.25 + 0.20 + 0.15 = 1.0
        AlertService.CalculatePriorityScore(1.0, true, true).Should().BeApproximately(1.0, 0.001);

        // Mid: Score=0.8, Ev=True, Cross=False -> 0.32 + 0.25 + 0 + 0.15 = 0.72
        AlertService.CalculatePriorityScore(0.8, true, false).Should().BeApproximately(0.72, 0.001);
    }

    [Fact]
    public async Task Test13_AlertService_ReviewLifecycle_StateTransitionsAndAudit()
    {
        var alert = new Alert
        {
            Id = "alt-test-lifecycle",
            CaseId = _case1.Id,
            AlertType = "NETWORK_ANOMALY",
            Title = "Lifecycle Test Alert",
            Description = "Test Alert",
            Score = 0.85,
            Status = "NEW",
            DeduplicationFingerprint = "SHA256:TEST-LIFECYCLE"
        };
        _dbContext.Alerts.Add(alert);
        await _dbContext.SaveChangesAsync();

        // 1. Acknowledge
        var ackResult = await _alertService.AcknowledgeAlertAsync(alert.Id, "usr-patil", "Inspector Patil");
        ackResult.Should().NotBeNull();
        ackResult.Status.Should().Be("ACKNOWLEDGED");
        ackResult.ReviewedBy.Should().Be("Inspector Patil");

        // 2. Start Review
        var reviewResult = await _alertService.StartReviewAsync(alert.Id, "usr-patil", "Inspector Patil");
        reviewResult.Should().NotBeNull();
        reviewResult.Status.Should().Be("UNDER_REVIEW");

        // 3. Resolve
        var resolveResult = await _alertService.ResolveAlertAsync(alert.Id, "Confirmed benign routing. Logged operational context.", "usr-patil", "Inspector Patil");
        resolveResult.Should().NotBeNull();
        resolveResult.Status.Should().Be("RESOLVED");
        resolveResult.ReviewNotes.Should().Contain("Confirmed benign routing");

        // Verify audit trail logged in database
        var logs = await _dbContext.AuditLogs.Where(l => l.ResourceId == alert.Id).ToListAsync();
        logs.Should().HaveCount(3);
        logs.Select(l => l.Action).Should().Contain(new[] { "ALERT_ACKNOWLEDGED", "ALERT_REVIEW_STARTED", "ALERT_RESOLVED" });
    }

    [Fact]
    public async Task Test14_AlertService_ZeroKnowledgeGraphMutationOnResolution()
    {
        // Capture baseline entity and relationship counts
        int initialEntityCount = await _dbContext.Entities.CountAsync();
        int initialRelationshipCount = await _dbContext.Relationships.CountAsync();

        var alert = new Alert
        {
            Id = "alt-test-zeromut",
            CaseId = _case1.Id,
            AlertType = "RELATIONSHIP_SURGE",
            Title = "Zero Mutation Test",
            Description = "Surge between A and B",
            Score = 0.90,
            Status = "NEW",
            DeduplicationFingerprint = "SHA256:ZERO-MUT"
        };
        _dbContext.Alerts.Add(alert);
        await _dbContext.SaveChangesAsync();

        // Resolve alert
        await _alertService.ResolveAlertAsync(alert.Id, "Reviewed by investigator; manual decision required.", "usr-sharma", "DCP Sharma");

        // Assert: Graph state MUST BE completely untouched!
        int finalEntityCount = await _dbContext.Entities.CountAsync();
        int finalRelationshipCount = await _dbContext.Relationships.CountAsync();

        finalEntityCount.Should().Be(initialEntityCount);
        finalRelationshipCount.Should().Be(initialRelationshipCount);
    }

    [Fact]
    public async Task Test15_AlertService_CaseIsolationGuaranteed()
    {
        var alertCase1 = new Alert { Id = "alt-c1", CaseId = _case1.Id, Title = "Case 1 Alert", DeduplicationFingerprint = "FP1" };
        var alertCase2 = new Alert { Id = "alt-c2", CaseId = _case2.Id, Title = "Case 2 Alert", DeduplicationFingerprint = "FP2" };

        _dbContext.Alerts.AddRange(alertCase1, alertCase2);
        await _dbContext.SaveChangesAsync();

        var queryCase1 = await _alertService.GetCaseAlertsAsync(_case1.Id, new AlertQueryDto());
        queryCase1.Should().ContainSingle(a => a.Id == "alt-c1");
        queryCase1.Should().NotContain(a => a.Id == "alt-c2");

        var summaryCase1 = await _alertService.GetAlertSummaryAsync(_case1.Id);
        summaryCase1.TotalAlerts.Should().Be(1);
    }

    [Fact]
    public async Task Test16_AlertService_StrictNeutralTerminologyPreserved()
    {
        var alert = new Alert
        {
            Id = "alt-test-terms",
            CaseId = _case1.Id,
            AlertType = "NETWORK_ANOMALY",
            Title = "High Connectivity Nexus Entity: Target X",
            Description = "Entity recorded 6 verified relationships.",
            Explanation = "WHAT: Elevated network degree observed. NOTE: Investigative signal requiring review, not proof of unlawful activity.",
            Score = 0.85,
            Status = "NEW",
            DeduplicationFingerprint = "SHA256:TERMS"
        };
        _dbContext.Alerts.Add(alert);
        await _dbContext.SaveChangesAsync();

        var dto = await _alertService.GetAlertDetailsAsync(_case1.Id, alert.Id);
        dto.Should().NotBeNull();

        string fullText = $"{dto!.Title} {dto.Description} {dto.Explanation}".ToLower();
        fullText.Should().NotContain("criminal syndicate");
        fullText.Should().NotContain("criminal suspect");
        fullText.Should().NotContain("guilty");
        fullText.Should().NotContain("kingpin");
        fullText.Should().NotContain("perpetrator");
    }
}
