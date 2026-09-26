using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.Unit;

public class Phase5GraphAnalyticsTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<INeo4jService> _mockNeo4j;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly GraphAnalyticsService _analyticsService;

    private readonly Case _case1;
    private readonly Case _case2;

    private class FastFailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        }
    }

    public Phase5GraphAnalyticsTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);
        _mockNeo4j = new Mock<INeo4jService>();
        _mockAudit = new Mock<IAuditService>();
        _httpClient = new HttpClient(new FastFailingHandler());

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"AIService:Url", "http://localhost:8000"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _analyticsService = new GraphAnalyticsService(
            _dbContext,
            _mockNeo4j.Object,
            _mockAudit.Object,
            _httpClient,
            _configuration,
            NullLogger<GraphAnalyticsService>.Instance);

        // Seed Cases
        _case1 = new Case
        {
            Id = "case-pune-001",
            CaseNumber = "CASE-2026-001",
            Title = "Pune Cyber Hawala Operation",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _case2 = new Case
        {
            Id = "case-mumbai-002",
            CaseNumber = "CASE-2026-002",
            Title = "Mumbai Port Interception",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Cases.AddRange(_case1, _case2);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _httpClient.Dispose();
    }

    private void SeedSampleGraph(string caseId)
    {
        // Line Graph: A (ent-A) --- B (ent-B, Bridge) --- C (ent-C) --- D (ent-D)
        // With Phone attached to A and Vehicle attached to D
        var entA = new EntityItem { Id = "ent-A", CaseId = caseId, Type = "PERSON", CanonicalName = "Person A", VerificationStatus = "VERIFIED" };
        var entB = new EntityItem { Id = "ent-B", CaseId = caseId, Type = "PERSON", CanonicalName = "Person B (Bridge)", VerificationStatus = "VERIFIED" };
        var entC = new EntityItem { Id = "ent-C", CaseId = caseId, Type = "PERSON", CanonicalName = "Person C", VerificationStatus = "VERIFIED" };
        var entD = new EntityItem { Id = "ent-D", CaseId = caseId, Type = "PERSON", CanonicalName = "Person D", VerificationStatus = "VERIFIED" };
        var entP = new EntityItem { Id = "ent-P", CaseId = caseId, Type = "PHONE", CanonicalName = "+919000000001", VerificationStatus = "VERIFIED" };
        var entV = new EntityItem { Id = "ent-V", CaseId = caseId, Type = "VEHICLE", CanonicalName = "MH12AB1111", VerificationStatus = "VERIFIED" };

        _dbContext.Entities.AddRange(entA, entB, entC, entD, entP, entV);

        var rel1 = new Relationship { Id = "rel-1", CaseId = caseId, SourceEntityId = "ent-A", TargetEntityId = "ent-B", Type = "COMMUNICATED_WITH", Confidence = 0.9 };
        var rel2 = new Relationship { Id = "rel-2", CaseId = caseId, SourceEntityId = "ent-B", TargetEntityId = "ent-C", Type = "COMMUNICATED_WITH", Confidence = 0.9 };
        var rel3 = new Relationship { Id = "rel-3", CaseId = caseId, SourceEntityId = "ent-C", TargetEntityId = "ent-D", Type = "ASSOCIATE_OF", Confidence = 0.9 };
        var rel4 = new Relationship { Id = "rel-4", CaseId = caseId, SourceEntityId = "ent-A", TargetEntityId = "ent-P", Type = "COMMUNICATED_WITH", Confidence = 0.95 };
        var rel5 = new Relationship { Id = "rel-5", CaseId = caseId, SourceEntityId = "ent-D", TargetEntityId = "ent-V", Type = "OPERATES", Confidence = 0.95 };

        _dbContext.Relationships.AddRange(rel1, rel2, rel3, rel4, rel5);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Test_1_Analytics_Respects_Case_Authorization()
    {
        // Act & Assert
        // Invalid role
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "user-guest", "GUEST"));

        // Non-existent case
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _analyticsService.RunCaseAnalyticsAsync("non-existent-case", new RunAnalyticsRequestDto(), "user-inv", "INVESTIGATOR"));
    }

    [Fact]
    public async Task Test_2_Degree_Centrality_Is_Mathematically_Correct()
    {
        // Triangle graph: A-B, B-C, C-A (each node has degree 2)
        var eA = new EntityItem { Id = "e-A", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "A", VerificationStatus = "VERIFIED" };
        var eB = new EntityItem { Id = "e-B", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "B", VerificationStatus = "VERIFIED" };
        var eC = new EntityItem { Id = "e-C", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "C", VerificationStatus = "VERIFIED" };
        _dbContext.Entities.AddRange(eA, eB, eC);

        _dbContext.Relationships.AddRange(
            new Relationship { Id = "r-1", CaseId = _case1.Id, SourceEntityId = "e-A", TargetEntityId = "e-B", Type = "COMMUNICATED_WITH" },
            new Relationship { Id = "r-2", CaseId = _case1.Id, SourceEntityId = "e-B", TargetEntityId = "e-C", Type = "COMMUNICATED_WITH" },
            new Relationship { Id = "r-3", CaseId = _case1.Id, SourceEntityId = "e-C", TargetEntityId = "e-A", Type = "COMMUNICATED_WITH" }
        );
        await _dbContext.SaveChangesAsync();

        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var centrality = await _analyticsService.GetCentralityMetricsAsync(_case1.Id, "degree", 10, "inv-1", "INVESTIGATOR");

        centrality.Metrics.Should().HaveCount(3);
        foreach (var m in centrality.Metrics)
        {
            m.Degree.Should().Be(2);
            m.NormalizedDegree.Should().Be(1.0); // 2 / (3 - 1) = 1.0
        }
    }

    [Fact]
    public async Task Test_3_Betweenness_Centrality_Is_Correct_For_Known_Graph()
    {
        // Line graph: A --- B --- C
        // Shortest paths between (A, C) must pass through B.
        // Betweenness of B must be greater than betweenness of A and C.
        var eA = new EntityItem { Id = "line-A", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Node A", VerificationStatus = "VERIFIED" };
        var eB = new EntityItem { Id = "line-B", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Node B (Bridge)", VerificationStatus = "VERIFIED" };
        var eC = new EntityItem { Id = "line-C", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Node C", VerificationStatus = "VERIFIED" };
        _dbContext.Entities.AddRange(eA, eB, eC);

        _dbContext.Relationships.AddRange(
            new Relationship { Id = "line-r1", CaseId = _case1.Id, SourceEntityId = "line-A", TargetEntityId = "line-B", Type = "COMMUNICATED_WITH" },
            new Relationship { Id = "line-r2", CaseId = _case1.Id, SourceEntityId = "line-B", TargetEntityId = "line-C", Type = "COMMUNICATED_WITH" }
        );
        await _dbContext.SaveChangesAsync();

        await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var centrality = await _analyticsService.GetCentralityMetricsAsync(_case1.Id, "betweenness", 10, "inv-1", "INVESTIGATOR");

        var metricB = centrality.Metrics.First(m => m.EntityId == "line-B");
        var metricA = centrality.Metrics.First(m => m.EntityId == "line-A");
        var metricC = centrality.Metrics.First(m => m.EntityId == "line-C");

        metricB.BetweennessCentrality.Should().BeGreaterThan(metricA.BetweennessCentrality);
        metricB.BetweennessCentrality.Should().BeGreaterThan(metricC.BetweennessCentrality);
        metricA.BetweennessCentrality.Should().Be(0.0);
        metricC.BetweennessCentrality.Should().Be(0.0);
    }

    [Fact]
    public async Task Test_4_Closeness_Centrality_Is_Correct()
    {
        // Line graph: A --- B --- C
        // B is 1 hop from A and 1 hop from C (total distance 2).
        // A is 1 hop from B and 2 hops from C (total distance 3).
        // B should have higher closeness than A and C.
        var eA = new EntityItem { Id = "cls-A", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Node A", VerificationStatus = "VERIFIED" };
        var eB = new EntityItem { Id = "cls-B", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Node B", VerificationStatus = "VERIFIED" };
        var eC = new EntityItem { Id = "cls-C", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Node C", VerificationStatus = "VERIFIED" };
        _dbContext.Entities.AddRange(eA, eB, eC);

        _dbContext.Relationships.AddRange(
            new Relationship { Id = "cls-r1", CaseId = _case1.Id, SourceEntityId = "cls-A", TargetEntityId = "cls-B", Type = "COMMUNICATED_WITH" },
            new Relationship { Id = "cls-r2", CaseId = _case1.Id, SourceEntityId = "cls-B", TargetEntityId = "cls-C", Type = "COMMUNICATED_WITH" }
        );
        await _dbContext.SaveChangesAsync();

        await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var centrality = await _analyticsService.GetCentralityMetricsAsync(_case1.Id, "closeness", 10, "inv-1", "INVESTIGATOR");

        var metricB = centrality.Metrics.First(m => m.EntityId == "cls-B");
        var metricA = centrality.Metrics.First(m => m.EntityId == "cls-A");

        metricB.ClosenessCentrality.Should().BeGreaterThan(metricA.ClosenessCentrality);
    }

    [Fact]
    public async Task Test_5_Connected_Components_Correctly_Partitions_Disconnected_Graphs()
    {
        // Two disjoint pairs: (comp1-A, comp1-B) and (comp2-X, comp2-Y)
        _dbContext.Entities.AddRange(
            new EntityItem { Id = "comp1-A", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "A", VerificationStatus = "VERIFIED" },
            new EntityItem { Id = "comp1-B", CaseId = _case1.Id, Type = "PHONE", CanonicalName = "P1", VerificationStatus = "VERIFIED" },
            new EntityItem { Id = "comp2-X", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "X", VerificationStatus = "VERIFIED" },
            new EntityItem { Id = "comp2-Y", CaseId = _case1.Id, Type = "VEHICLE", CanonicalName = "V1", VerificationStatus = "VERIFIED" }
        );

        _dbContext.Relationships.AddRange(
            new Relationship { Id = "r-comp1", CaseId = _case1.Id, SourceEntityId = "comp1-A", TargetEntityId = "comp1-B", Type = "COMMUNICATED_WITH" },
            new Relationship { Id = "r-comp2", CaseId = _case1.Id, SourceEntityId = "comp2-X", TargetEntityId = "comp2-Y", Type = "OPERATES" }
        );
        await _dbContext.SaveChangesAsync();

        await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var components = await _analyticsService.GetComponentsAsync(_case1.Id, "inv-1", "INVESTIGATOR");

        components.Should().HaveCount(2);
        components.All(c => c.NodeCount == 2).Should().BeTrue();
    }

    [Fact]
    public async Task Test_6_Community_Detection_Groups_Association_Clusters()
    {
        SeedSampleGraph(_case1.Id);

        await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var communities = await _analyticsService.GetCommunitiesAsync(_case1.Id, "inv-1", "INVESTIGATOR");

        communities.Should().NotBeEmpty();
        communities.Sum(c => c.EntityCount).Should().Be(6);
    }

    [Fact]
    public async Task Test_7_Network_Statistics_Match_Graph_Data()
    {
        // 4 nodes, 3 edges: N=4, M=3. Density = 2*3 / (4 * 3) = 0.50
        _dbContext.Entities.AddRange(
            new EntityItem { Id = "s-1", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "P1", VerificationStatus = "VERIFIED" },
            new EntityItem { Id = "s-2", CaseId = _case1.Id, Type = "PHONE", CanonicalName = "T1", VerificationStatus = "VERIFIED" },
            new EntityItem { Id = "s-3", CaseId = _case1.Id, Type = "VEHICLE", CanonicalName = "V1", VerificationStatus = "VERIFIED" },
            new EntityItem { Id = "s-4", CaseId = _case1.Id, Type = "LOCATION", CanonicalName = "L1", VerificationStatus = "VERIFIED" }
        );

        _dbContext.Relationships.AddRange(
            new Relationship { Id = "sr-1", CaseId = _case1.Id, SourceEntityId = "s-1", TargetEntityId = "s-2", Type = "COMMUNICATED_WITH" },
            new Relationship { Id = "sr-2", CaseId = _case1.Id, SourceEntityId = "s-1", TargetEntityId = "s-3", Type = "OPERATES" },
            new Relationship { Id = "sr-3", CaseId = _case1.Id, SourceEntityId = "s-1", TargetEntityId = "s-4", Type = "LOCATED_AT" }
        );
        await _dbContext.SaveChangesAsync();

        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var stats = await _analyticsService.GetNetworkStatisticsAsync(_case1.Id, "inv-1", "INVESTIGATOR");

        stats.TotalEntities.Should().Be(4);
        stats.TotalRelationships.Should().Be(3);
        stats.AverageDegree.Should().Be(1.5); // 2*3 / 4 = 1.5
        stats.NetworkDensity.Should().Be(0.5); // 3 / (4*3/2) = 0.5
    }

    [Fact]
    public async Task Test_8_Analytics_Excludes_Unverified_Relationships()
    {
        // Unverified entity should not be included
        var verifiedEnt = new EntityItem { Id = "v-ent", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Verified", VerificationStatus = "VERIFIED" };
        var unverifiedEnt = new EntityItem { Id = "u-ent", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "Unverified", VerificationStatus = "UNVERIFIED" };
        _dbContext.Entities.AddRange(verifiedEnt, unverifiedEnt);

        _dbContext.Relationships.Add(new Relationship
        {
            Id = "rel-unverified",
            CaseId = _case1.Id,
            SourceEntityId = "v-ent",
            TargetEntityId = "u-ent",
            Type = "COMMUNICATED_WITH"
        });
        await _dbContext.SaveChangesAsync();

        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        run.NodeCount.Should().Be(1);
        run.EdgeCount.Should().Be(0); // Relationship excluded because u-ent is UNVERIFIED
    }

    [Fact]
    public async Task Test_9_Analytics_Excludes_Rejected_Cross_Case_Connections()
    {
        var e1 = new EntityItem { Id = "e-case1", CaseId = _case1.Id, Type = "PERSON", CanonicalName = "E1", VerificationStatus = "VERIFIED" };
        var e2 = new EntityItem { Id = "e-case2", CaseId = _case2.Id, Type = "PERSON", CanonicalName = "E2", VerificationStatus = "VERIFIED" };
        _dbContext.Entities.AddRange(e1, e2);

        // REJECTED cross-case connection
        _dbContext.CrossCaseConnections.Add(new CrossCaseConnection
        {
            Id = "ccc-rejected",
            SourceCaseId = _case1.Id,
            TargetCaseId = _case2.Id,
            SourceEntityId = "e-case1",
            TargetEntityId = "e-case2",
            ConnectionType = "SHARED_ENTITY",
            Confidence = 0.85,
            Status = "REJECTED"
        });
        await _dbContext.SaveChangesAsync();

        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto
        {
            IncludeCrossCase = true,
            AuthorizedCaseIds = new List<string> { _case1.Id, _case2.Id }
        }, "inv-1", "INVESTIGATOR");

        run.EdgeCount.Should().Be(0); // REJECTED connection must NOT be included as verified edge
    }

    [Fact]
    public async Task Test_10_Analytics_Run_Is_Persisted()
    {
        SeedSampleGraph(_case1.Id);

        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");

        var persisted = await _dbContext.GraphAnalysisRuns.FindAsync(run.Id);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be("COMPLETED");
        persisted.NodeCount.Should().Be(6);
    }

    [Fact]
    public async Task Test_11_Analytics_Run_Is_Idempotent()
    {
        SeedSampleGraph(_case1.Id);

        var run1 = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var run2 = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");

        run1.NodeCount.Should().Be(run2.NodeCount);
        run1.EdgeCount.Should().Be(run2.EdgeCount);
        run1.NetworkDensity.Should().Be(run2.NetworkDensity);
    }

    [Fact]
    public async Task Test_12_Model_Generated_Lead_Is_Persisted_With_Pending_Status()
    {
        SeedSampleGraph(_case1.Id);

        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var leads = await _analyticsService.GetModelLeadsAsync(_case1.Id, null, null, 10, "inv-1", "INVESTIGATOR");

        leads.Should().NotBeEmpty();
        leads.All(l => l.Status == "PENDING").Should().BeTrue();
    }

    [Fact]
    public async Task Test_13_Model_Generated_Lead_Does_Not_Automatically_Create_Verified_Edge()
    {
        SeedSampleGraph(_case1.Id);

        int initialRelCount = await _dbContext.Relationships.CountAsync();
        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        int postRunRelCount = await _dbContext.Relationships.CountAsync();

        // Staged model leads must NOT mutate verified relationship table
        postRunRelCount.Should().Be(initialRelCount);
    }

    [Fact]
    public async Task Test_14_Confirming_Lead_Creates_Documented_Relationship()
    {
        SeedSampleGraph(_case1.Id);

        await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var leads = await _analyticsService.GetModelLeadsAsync(_case1.Id, "PENDING", null, 1, "inv-1", "INVESTIGATOR");
        leads.Should().NotBeEmpty();

        var leadToConfirm = leads.First();
        var reviewed = await _analyticsService.ReviewModelLeadAsync(
            leadToConfirm.Id,
            new LeadReviewRequestDto { Status = "CONFIRMED", ReviewNotes = "Corroborated by call logs", SuggestedRelationshipType = "ASSOCIATE_OF" },
            "dcp-sharma",
            "INVESTIGATOR",
            "127.0.0.1");

        reviewed.Status.Should().Be("CONFIRMED");
        reviewed.ResultingRelationshipId.Should().NotBeNullOrEmpty();

        // Check Relationship table in DB
        var newRel = await _dbContext.Relationships.FindAsync(reviewed.ResultingRelationshipId);
        newRel.Should().NotBeNull();
        newRel!.SourceEvidenceId.Should().Be("MODEL_CONFIRMED");
        newRel.Type.Should().Be("ASSOCIATE_OF");
    }

    [Fact]
    public async Task Test_15_Dismissing_Lead_Creates_No_Graph_Mutation()
    {
        SeedSampleGraph(_case1.Id);

        await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var leads = await _analyticsService.GetModelLeadsAsync(_case1.Id, "PENDING", null, 1, "inv-1", "INVESTIGATOR");
        var leadToDismiss = leads.First();

        int relCountBefore = await _dbContext.Relationships.CountAsync();

        var reviewed = await _analyticsService.ReviewModelLeadAsync(
            leadToDismiss.Id,
            new LeadReviewRequestDto { Status = "DISMISSED", ReviewNotes = "Not relevant to cyber fraud investigation" },
            "dcp-sharma",
            "INVESTIGATOR",
            "127.0.0.1");

        reviewed.Status.Should().Be("DISMISSED");
        reviewed.ResultingRelationshipId.Should().BeNull();

        int relCountAfter = await _dbContext.Relationships.CountAsync();
        relCountAfter.Should().Be(relCountBefore); // No graph mutation
    }

    [Fact]
    public async Task Test_16_Review_Action_Creates_Audit_Log()
    {
        SeedSampleGraph(_case1.Id);

        await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        var leads = await _analyticsService.GetModelLeadsAsync(_case1.Id, "PENDING", null, 1, "inv-1", "INVESTIGATOR");
        var lead = leads.First();

        await _analyticsService.ReviewModelLeadAsync(
            lead.Id,
            new LeadReviewRequestDto { Status = "CONFIRMED", ReviewNotes = "Verified lead" },
            "dcp-sharma",
            "INVESTIGATOR",
            "127.0.0.1");

        _mockAudit.Verify(a => a.LogAsync(
            "dcp-sharma",
            "INVESTIGATOR",
            "MODEL_LEAD_CONFIRMED",
            "GraphAnalyticalLead",
            lead.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            "127.0.0.1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_17_Model_Version_Is_Stored()
    {
        SeedSampleGraph(_case1.Id);

        var run = await _analyticsService.RunCaseAnalyticsAsync(_case1.Id, new RunAnalyticsRequestDto(), "inv-1", "INVESTIGATOR");
        run.ModelVersion.Should().Be("GAT-v1.0.0");

        var leads = await _analyticsService.GetModelLeadsAsync(_case1.Id, null, null, 10, "inv-1", "INVESTIGATOR");
        leads.All(l => l.ModelVersion == "GAT-v1.0.0").Should().BeTrue();
    }

    [Fact]
    public async Task Test_18_Unauthorized_Cross_Case_Analytics_Fails()
    {
        // User attempts to analyze case-1 with an unauthorized case-unknown
        var req = new RunAnalyticsRequestDto
        {
            IncludeCrossCase = true,
            AuthorizedCaseIds = new List<string> { _case1.Id, "unauthorized-case-999" }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _analyticsService.RunCaseAnalyticsAsync(_case1.Id, req, "inv-1", "INVESTIGATOR"));
    }
}
