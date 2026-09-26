using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Infrastructure.Services.Copilot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.Unit;

public class Phase11DashboardTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IInvestigationGraphService> _mockGraphService;
    private readonly Mock<IGraphAnalyticsService> _mockAnalyticsService;
    private readonly Mock<IAlertService> _mockAlertService;
    private readonly Mock<ITemporalService> _mockTemporalService;
    private readonly Mock<IGeospatialService> _mockGeospatialService;
    private readonly Mock<IIntegrityLedgerService> _mockLedgerService;
    private readonly Mock<IEntityResolutionService> _mockResolutionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly DashboardService _dashboardService;
    private readonly TranslationService _translationService;

    private readonly Case _caseA;
    private readonly Case _caseB;
    private readonly EntityItem _entityA1;
    private readonly EntityItem _entityA2;
    private readonly EntityItem _entityB1;
    private readonly Relationship _relA;
    private readonly Evidence _evidenceA1;
    private readonly Evidence _evidenceA2;
    private readonly GraphAnalyticalLead _modelLeadA;

    public Phase11DashboardTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);

        // Seed Cases
        _caseA = new Case
        {
            Id = "case-p11-001",
            CaseNumber = "CASE-2026-001",
            Title = "Hawala Banking Network",
            Status = "Active",
            Priority = "High",
            LeadOfficerName = "DCP R. Sharma",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };

        _caseB = new Case
        {
            Id = "case-p11-002",
            CaseNumber = "CASE-2026-002",
            Title = "Narcotics Cargo Transit",
            Status = "Active",
            Priority = "Critical",
            LeadOfficerName = "ACP V. Malhotra",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };

        _dbContext.Cases.AddRange(_caseA, _caseB);

        // Seed Entities for Case A
        _entityA1 = new EntityItem
        {
            Id = "ent-p11-001",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Rajesh Kumar",
            NormalizedValue = "rajesh kumar",
            VerificationStatus = "VERIFIED"
        };
        _entityA2 = new EntityItem
        {
            Id = "ent-p11-002",
            CaseId = _caseA.Id,
            Type = "PHONE",
            CanonicalName = "+919876543210",
            NormalizedValue = "919876543210",
            VerificationStatus = "VERIFIED"
        };

        // Seed Entity for Case B
        _entityB1 = new EntityItem
        {
            Id = "ent-p11-b01",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "Vikram Singhania",
            NormalizedValue = "vikram singhania",
            VerificationStatus = "VERIFIED"
        };

        _dbContext.Entities.AddRange(_entityA1, _entityA2, _entityB1);

        // Seed Relationship for Case A
        _relA = new Relationship
        {
            Id = "rel-p11-001",
            CaseId = _caseA.Id,
            SourceEntityId = _entityA1.Id,
            TargetEntityId = _entityA2.Id,
            Type = "COMMUNICATES_WITH",
            Confidence = 0.95
        };
        _dbContext.Relationships.Add(_relA);

        // Seed Evidence for Case A
        _evidenceA1 = new Evidence
        {
            Id = "evd-p11-001",
            CaseId = _caseA.Id,
            FileName = "cdr_records.csv",
            Sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ProcessingStatus = "APPROVED"
        };
        _evidenceA2 = new Evidence
        {
            Id = "evd-p11-002",
            CaseId = _caseA.Id,
            FileName = "bank_statement.pdf",
            Sha256Hash = "872983ac1f23b72a9348ecfa0293481239847120349812739812739812739123",
            ProcessingStatus = "APPROVED"
        };
        _dbContext.EvidenceItems.AddRange(_evidenceA1, _evidenceA2);

        // Seed GAT Model Signal for Case A
        _modelLeadA = new GraphAnalyticalLead
        {
            Id = "lead-p11-001",
            CaseId = _caseA.Id,
            SourceEntityId = _entityA1.Id,
            TargetEntityId = _entityA2.Id,
            LeadType = "POTENTIAL_RELATIONSHIP",
            SuggestedRelationshipType = "FINANCIAL_TRANSFER",
            Score = 0.91,
            Status = "PENDING",
            ModelVersion = "GAT-v1.0.0",
            ExplanationJson = "{\"reason\":\"Neighborhood structural similarity in latent graph space\"}",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.GraphAnalyticalLeads.Add(_modelLeadA);

        _dbContext.SaveChanges();

        // Mocks setup
        _mockGraphService = new Mock<IInvestigationGraphService>();
        _mockGraphService.Setup(g => g.GetCaseGraphAsync(_caseA.Id, null, null, 1, null, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaseGraphResponseDto
            {
                CaseId = _caseA.Id,
                Nodes = new List<GraphNodeDto>
                {
                    new() { Id = _entityA1.Id, Label = _entityA1.CanonicalName, Type = _entityA1.Type, Verified = true, ConnectionsCount = 1 },
                    new() { Id = _entityA2.Id, Label = _entityA2.CanonicalName, Type = _entityA2.Type, Verified = true, ConnectionsCount = 1 }
                },
                Edges = new List<GraphEdgeDto>
                {
                    new() { Id = _relA.Id, Source = _entityA1.Id, Target = _entityA2.Id, Type = _relA.Type, Confidence = 0.95 }
                }
            });

        _mockAnalyticsService = new Mock<IGraphAnalyticsService>();
        _mockAnalyticsService.Setup(a => a.GetNetworkStatisticsAsync(_caseA.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NetworkStatisticsDto
            {
                CaseId = _caseA.Id,
                TotalEntities = 2,
                TotalRelationships = 1,
                ConnectedComponents = 1,
                NetworkDensity = 0.5
            });
        _mockAnalyticsService.Setup(a => a.GetCentralityMetricsAsync(_caseA.Id, "degree", 3, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CentralityResultsDto
            {
                CaseId = _caseA.Id,
                SortedBy = "degree",
                Metrics = new List<EntityCentralityMetricDto>
                {
                    new() { EntityId = _entityA1.Id, EntityName = _entityA1.CanonicalName, EntityType = "PERSON", Degree = 1 }
                }
            });

        _mockAlertService = new Mock<IAlertService>();
        _mockAlertService.Setup(s => s.GetAlertSummaryAsync(_caseA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertSummaryDto
            {
                TotalAlerts = 2,
                CriticalSeverity = 1,
                HighSeverity = 1,
                MediumSeverity = 0,
                LowSeverity = 0,
                NewAlerts = 2,
                UnderReviewAlerts = 0,
                ResolvedAlerts = 0
            });
        _mockAlertService.Setup(s => s.GetCaseAlertsAsync(_caseA.Id, It.IsAny<AlertQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlertDto>
            {
                new()
                {
                    Id = "alt-p11-001",
                    CaseId = _caseA.Id,
                    Title = "Suspicious financial routing pattern detected",
                    Severity = "CRITICAL",
                    Status = "NEW",
                    AlertType = "NETWORK_ANOMALY",
                    Description = "Multi-hop routing pattern exceeding statistical baseline",
                    CreatedAtUtc = DateTime.UtcNow
                }
            });

        _mockTemporalService = new Mock<ITemporalService>();
        _mockTemporalService.Setup(t => t.GetCaseTimelineEventsAsync(_caseA.Id, It.IsAny<TimelineQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimelineEventDto>
            {
                new()
                {
                    Id = "evt-p11-001",
                    EventType = "PHONE_CALL",
                    StartTimeUtc = new DateTime(2026, 8, 14, 9, 12, 0, DateTimeKind.Utc),
                    TimePrecision = "DATETIME",
                    Description = "Call between Rajesh and Unknown caller"
                },
                new()
                {
                    Id = "evt-p11-002",
                    EventType = "VEHICLE_SIGHTING",
                    StartTimeUtc = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc),
                    TimePrecision = "DATE_ONLY",
                    Description = "Vehicle MH12AB1234 recorded in Sector 4"
                }
            });

        _mockGeospatialService = new Mock<IGeospatialService>();
        _mockGeospatialService.Setup(g => g.GetCaseLocationsAsync(_caseA.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LocationDto>
            {
                new()
                {
                    Id = "loc-p11-001",
                    Name = "Bandra Courier Terminal",
                    Latitude = 19.0596,
                    Longitude = 72.8295,
                    EventCount = 4
                },
                new()
                {
                    Id = "loc-p11-002",
                    Name = "Warehouse B-12",
                    Latitude = 0.0,
                    Longitude = 0.0,
                    EventCount = 1
                }
            });

        _mockLedgerService = new Mock<IIntegrityLedgerService>();
        _mockLedgerService.Setup(l => l.GetEvidenceIntegrityStatusAsync(_evidenceA1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceIntegrityStatusDto
            {
                EvidenceId = _evidenceA1.Id,
                Status = "VERIFIED",
                RegisteredSha256 = _evidenceA1.Sha256Hash,
                ActualFileSha256 = _evidenceA1.Sha256Hash,
                BlockIndex = 12,
                Explanation = "Byte-level hash matches append-only ledger block."
            });
        _mockLedgerService.Setup(l => l.GetEvidenceIntegrityStatusAsync(_evidenceA2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceIntegrityStatusDto
            {
                EvidenceId = _evidenceA2.Id,
                Status = "EVIDENCE_MODIFIED",
                RegisteredSha256 = _evidenceA2.Sha256Hash,
                ActualFileSha256 = "compromised_hash_00000000000000000000000000000000000000000000000000",
                BlockIndex = 14,
                Explanation = "Physical file hash does not match registered ledger hash."
            });

        _mockResolutionService = new Mock<IEntityResolutionService>();
        _mockResolutionService.Setup(r => r.GetCrossCaseConnectionsAsync(_caseA.Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrossCaseConnectionDto>
            {
                new()
                {
                    Id = "ccc-p11-001",
                    SourceCaseId = _caseA.Id,
                    TargetCaseId = _caseB.Id,
                    TargetCaseNumber = _caseB.CaseNumber,
                    TargetEntityName = "Vikram Singhania",
                    ConnectionType = "SHARED_PHONE",
                    Confidence = 0.98,
                    Status = "APPROVED",
                    Explanation = "Shared phone number +919876543210 linked across both cases",
                    SupportingEvidence = new List<SupportingEvidenceCitationDto> { new() { EvidenceId = _evidenceA1.Id } }
                }
            });
        _mockResolutionService.Setup(r => r.GetCandidatesAsync(_caseA.Id, "PENDING", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityMatchCandidateDto>
            {
                new()
                {
                    Id = "cand-p11-001",
                    SourceCaseId = _caseA.Id,
                    TargetCaseId = _caseB.Id,
                    TargetCaseNumber = _caseB.CaseNumber,
                    EntityType = "PERSON",
                    MatchStatus = "PENDING",
                    MatchScore = 0.88,
                    MatchMethod = "MULTI_SIGNAL",
                    TargetEntity = new EntitySummaryDto { CanonicalName = "R Sharma" },
                    Factors = new List<MatchFactorDto> { new() { Type = "NAME_SIMILARITY", Weight = 0.88 } }
                }
            });

        _mockAuditService = new Mock<IAuditService>();

        _dashboardService = new DashboardService(
            _dbContext,
            _mockGraphService.Object,
            _mockAnalyticsService.Object,
            _mockAlertService.Object,
            _mockTemporalService.Object,
            _mockGeospatialService.Object,
            _mockLedgerService.Object,
            _mockResolutionService.Object,
            _mockAuditService.Object,
            NullLogger<DashboardService>.Instance);

        var configMock = new Mock<IConfiguration>();
        _translationService = new TranslationService(
            configMock.Object,
            new System.Net.Http.HttpClient(),
            NullLogger<TranslationService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetCaseDashboard_RequiresValidCase_ThrowsKeyNotFoundForUnknownCase()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _dashboardService.GetCaseDashboardAsync("case-unknown-999", "usr-1", "INVESTIGATOR"));
    }

    [Fact]
    public async Task GetCaseDashboard_RequiresCaseAuthorization_ThrowsUnauthorizedForInvalidRole()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-unauth", "EXTERNAL_GUEST"));
    }

    [Fact]
    public async Task GetCaseDashboard_CaseScoped_SummaryCountsComeFromPersistedData()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.Case.CaseNumber.Should().Be("CASE-2026-001");
        dashboard.Summary.EntityCount.Should().Be(2); // Only Case A entities
        dashboard.Summary.RelationshipCount.Should().Be(1);
        dashboard.Summary.EvidenceCount.Should().Be(2);
        dashboard.Summary.ModelSignalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCaseDashboard_CaseA_DoesNotIncludeCaseBEntitiesInGraphPreview()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        dashboard.Network.Nodes.Should().NotContain(n => n.Id == _entityB1.Id);
        dashboard.Network.Nodes.Should().Contain(n => n.Id == _entityA1.Id);
    }

    [Fact]
    public async Task GetCaseDashboard_AlertSummary_ReflectsRealCounts()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        dashboard.Alerts.BySeverity.Critical.Should().Be(1);
        dashboard.Alerts.BySeverity.High.Should().Be(1);
        dashboard.Alerts.HighPriorityAlerts.Should().NotBeEmpty();
        dashboard.Alerts.HighPriorityAlerts[0].Title.Should().Contain("financial routing");
    }

    [Fact]
    public async Task GetCaseDashboard_CrossCaseIntelligence_StrictlySeparatesConfirmedAndPotential()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        dashboard.CrossCase.ConfirmedCount.Should().Be(1);
        dashboard.CrossCase.PotentialCount.Should().Be(1);
        dashboard.Summary.CrossCaseConfirmedCount.Should().Be(1);
        dashboard.Summary.CrossCasePotentialCount.Should().Be(1);
        dashboard.CrossCase.Connections.Should().Contain(c => c.Status == "CONFIRMED" && c.ConnectionType == "SHARED_PHONE");
        dashboard.CrossCase.Connections.Should().Contain(c => c.Status == "POTENTIAL");
    }

    [Fact]
    public async Task GetCaseDashboard_EvidenceIntegrity_SurfacesIntegrityWarningForModifiedFile()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        dashboard.Integrity.VerifiedCount.Should().Be(1);
        dashboard.Integrity.ModifiedCount.Should().Be(1);
        dashboard.Integrity.LedgerHealthStatus.Should().Be("WARNING");
        dashboard.Integrity.Warnings.Should().HaveCount(1);
        dashboard.Integrity.Warnings[0].EvidenceId.Should().Be(_evidenceA2.Id);
        dashboard.Integrity.Warnings[0].Status.Should().Be("EVIDENCE_MODIFIED");
    }

    [Fact]
    public async Task GetCaseDashboard_ModelSignals_ClearlyMarkedAsModelGeneratedWithoutGuiltClaims()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        var modelSignals = dashboard.Signals.Where(s => s.Type == "MODEL_SIGNAL").ToList();
        modelSignals.Should().NotBeEmpty();
        var sig = modelSignals[0];
        sig.ModelScore.Should().Be(0.91);
        sig.ModelName.Should().Be("GAT-v1.0.0");
        sig.WhyItMatters.Should().Contain("GAT structural link hypothesis");
        sig.WhyItMatters.Should().NotContain("guilt");
        sig.WhyItMatters.Should().NotContain("criminal");
        dashboard.ResponsibleAiNotice.Should().Contain("human verification");
    }

    [Fact]
    public async Task GetCaseDashboard_TimelinePreview_PreservesTemporalPrecision()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        var dateOnlyEvent = dashboard.Timeline.FirstOrDefault(t => t.Precision == "DATE_ONLY");
        dateOnlyEvent.Should().NotBeNull();
        dateOnlyEvent!.FormattedTime.Should().Be("2026-08-14"); // No invented hours/minutes!
    }

    [Fact]
    public async Task GetCaseDashboard_GeospatialPreview_DoesNotFabricateMissingCoordinates()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        var uncoord = dashboard.Locations.FirstOrDefault(l => l.LocationId == "loc-p11-002");
        uncoord.Should().NotBeNull();
        uncoord!.HasCoordinates.Should().BeFalse();
        uncoord.CoordinateDisplay.Should().Be("Location recorded without coordinates");
        uncoord.Latitude.Should().BeNull();
        uncoord.Longitude.Should().BeNull();
    }

    [Fact]
    public async Task GetCaseDashboard_InvestigatorActionQueue_ContainsRealPendingTasks()
    {
        // Act
        var dashboard = await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert
        dashboard.Actions.Should().NotBeEmpty();
        dashboard.Actions.Should().Contain(a => a.Type == "ALERT_VERIFICATION" && a.ActionLabel == "Verify Alert");
        dashboard.Actions.Should().Contain(a => a.Type == "MODEL_SIGNAL" && a.ActionLabel == "Review Signal");
        dashboard.Actions.Should().Contain(a => a.Type == "ENTITY_MATCH" && a.ActionLabel == "Review Match");
    }

    [Fact]
    public async Task GetCaseDashboard_DoesNotMutateCaseOrEvidenceState()
    {
        // Act
        await _dashboardService.GetCaseDashboardAsync(_caseA.Id, "usr-1", "INVESTIGATOR");

        // Assert: Database state unchanged
        var lead = await _dbContext.GraphAnalyticalLeads.FindAsync(_modelLeadA.Id);
        lead!.Status.Should().Be("PENDING"); // Not auto-promoted!

        var rel = await _dbContext.Relationships.FindAsync(_relA.Id);
        rel!.Confidence.Should().Be(0.95);
    }

    [Fact]
    public async Task TranslationService_PreservesEvidenceProvenanceAndIdentifiers()
    {
        // Arrange
        var req = new TranslationRequestDto
        {
            Text = "Investigation report for vehicle and phone call record",
            TargetLanguage = "mr"
        };

        // Act
        var res = await _translationService.TranslateEvidenceTextAsync(req);

        // Assert
        res.IsMachineTranslation.Should().BeTrue();
        res.Disclaimer.Should().Contain("not authoritative evidence");
        res.TranslatedText.Should().Contain("तपास");
        res.TranslatedText.Should().Contain("वाहन");
    }

    [Fact]
    public async Task CopilotIntentRouter_RecognizesMarathiQueryIntent()
    {
        // Arrange
        var router = new CopilotIntentRouter(_dbContext, NullLogger<CopilotIntentRouter>.Instance);

        // Act
        var (intent, _) = await router.RouteIntentAsync("या प्रकरणातील प्रमुख संबंध कोणते आहेत?", _caseA.Id);

        // Assert
        intent.Should().Be("ENTITY_RELATIONSHIPS");
    }

    [Fact]
    public async Task MultilingualCopilot_AnswersMarathiQueryWithGroundedCitations()
    {
        // Arrange
        var mockRouter = new Mock<ICopilotIntentRouter>();
        mockRouter.Setup(r => r.RouteIntentAsync(It.IsAny<string>(), _caseA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("ENTITY_RELATIONSHIPS", new List<string>()));

        var mockContextBuilder = new Mock<ICopilotContextBuilder>();
        mockContextBuilder.Setup(b => b.BuildContextAsync(_caseA.Id, It.IsAny<string>(), "ENTITY_RELATIONSHIPS", It.IsAny<List<string>>(), It.IsAny<CopilotQueryRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CopilotGroundedContext
            {
                CaseId = _caseA.Id,
                CaseNumber = _caseA.CaseNumber,
                CaseTitle = _caseA.Title,
                QueryIntent = "ENTITY_RELATIONSHIPS",
                Language = "mr",
                VerifiedRelationships = new List<RelationshipCitationDto>
                {
                    new()
                    {
                        RelationshipId = _relA.Id,
                        SourceEntityId = _entityA1.Id,
                        SourceEntityName = _entityA1.CanonicalName,
                        TargetEntityId = _entityA2.Id,
                        TargetEntityName = _entityA2.CanonicalName,
                        RelationshipType = _relA.Type,
                        Confidence = 0.95,
                        EvidenceIds = new List<string> { _evidenceA1.Id }
                    }
                }
            });

        var configMock = new Mock<IConfiguration>();
        var llmService = new LLMService(configMock.Object, NullLogger<LLMService>.Instance);
        var validator = new CopilotCitationValidator(NullLogger<CopilotCitationValidator>.Instance);

        var copilot = new CopilotService(
            _dbContext,
            mockRouter.Object,
            mockContextBuilder.Object,
            llmService,
            validator,
            _mockAuditService.Object,
            NullLogger<CopilotService>.Instance);

        var request = new CopilotQueryRequest
        {
            CaseId = _caseA.Id,
            Query = "या प्रकरणातील प्रमुख संबंध कोणते आहेत?",
            Language = "mr"
        };

        // Act
        var response = await copilot.AskCopilotAsync(request, "usr-1", "INVESTIGATOR", "Officer");

        // Assert
        response.Should().NotBeNull();
        response.Answer.Should().Contain("पडताळलेले संबंध");
        response.Answer.Should().Contain("Rajesh Kumar"); // Preserves canonical entity name!
        response.RelationshipCitations.Should().HaveCount(1);
        response.RelationshipCitations[0].RelationshipId.Should().Be(_relA.Id);
    }
}
