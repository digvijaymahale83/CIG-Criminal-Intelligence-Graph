using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.Unit;

public class Phase3InvestigationGraphTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<INeo4jService> _mockNeo4j;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly InvestigationGraphService _graphService;

    private readonly Case _case1;
    private readonly Case _case2;
    private readonly Evidence _evidence1;
    private readonly Evidence _evidence2;
    private readonly EntityItem _personA;
    private readonly EntityItem _phoneA;
    private readonly EntityItem _vehicleA;
    private readonly EntityItem _locationA;
    private readonly EntityItem _orgA;
    private readonly Relationship _rel1;
    private readonly Relationship _rel2;
    private readonly Relationship _rel3;
    private readonly Relationship _rel4;

    public Phase3InvestigationGraphTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);
        _mockNeo4j = new Mock<INeo4jService>();
        _mockAudit = new Mock<IAuditService>();

        _graphService = new InvestigationGraphService(
            _dbContext,
            _mockNeo4j.Object,
            _mockAudit.Object,
            NullLogger<InvestigationGraphService>.Instance);

        // Seed Cases
        _case1 = new Case
        {
            Id = "inv-test-001",
            CaseNumber = "CASE-2026-001",
            Title = "Pune Hawala Syndicate",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _case2 = new Case
        {
            Id = "inv-test-002",
            CaseNumber = "CASE-2026-002",
            Title = "Mumbai Port Interception",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Cases.AddRange(_case1, _case2);

        // Seed Evidence
        _evidence1 = new Evidence
        {
            Id = "ev-test-001",
            CaseId = _case1.Id,
            FileName = "call_records_pune.csv",
            StoragePath = "uploads/evidence/call_records_pune.csv",
            Sha256Hash = "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
            ProcessingStatus = "APPROVED",
            UploadedAtUtc = DateTime.UtcNow
        };

        _evidence2 = new Evidence
        {
            Id = "ev-test-002",
            CaseId = _case1.Id,
            FileName = "surveillance_report_pune.txt",
            StoragePath = "uploads/evidence/surveillance_report_pune.txt",
            Sha256Hash = "7c2c73dfdc485cbf788c2ed0f27e6be2a0859a9024ea3723aaf3cc18f6369168",
            ProcessingStatus = "APPROVED",
            UploadedAtUtc = DateTime.UtcNow
        };
        _dbContext.EvidenceItems.AddRange(_evidence1, _evidence2);

        // Seed Entities
        _personA = new EntityItem
        {
            Id = "ent-rahul-01",
            CaseId = _case1.Id,
            Type = "PERSON",
            CanonicalName = "Rahul Mehta",
            NormalizedValue = "Rahul Mehta",
            VerificationStatus = "VERIFIED",
            Confidence = 0.98,
            RiskLevel = "HIGH",
            CreatedAtUtc = DateTime.UtcNow
        };

        _phoneA = new EntityItem
        {
            Id = "ent-phone-01",
            CaseId = _case1.Id,
            Type = "PHONE",
            CanonicalName = "+91-9000001001",
            NormalizedValue = "+919000001001",
            PhoneNumber = "+91-9000001001",
            VerificationStatus = "VERIFIED",
            Confidence = 0.99,
            RiskLevel = "CRITICAL",
            CreatedAtUtc = DateTime.UtcNow
        };

        _vehicleA = new EntityItem
        {
            Id = "ent-veh-01",
            CaseId = _case1.Id,
            Type = "VEHICLE",
            CanonicalName = "MH12AB4321",
            NormalizedValue = "MH12AB4321",
            VehicleNumber = "MH12AB4321",
            VerificationStatus = "VERIFIED",
            Confidence = 0.95,
            RiskLevel = "HIGH",
            CreatedAtUtc = DateTime.UtcNow
        };

        _locationA = new EntityItem
        {
            Id = "ent-loc-01",
            CaseId = _case1.Id,
            Type = "LOCATION",
            CanonicalName = "Pune Central",
            NormalizedValue = "Pune Central",
            VerificationStatus = "VERIFIED",
            Confidence = 0.92,
            RiskLevel = "MEDIUM",
            CreatedAtUtc = DateTime.UtcNow
        };

        _orgA = new EntityItem
        {
            Id = "ent-org-01",
            CaseId = _case1.Id,
            Type = "ORGANIZATION",
            CanonicalName = "Apex Trading Syndicate",
            NormalizedValue = "APEX TRADING SYNDICATE",
            VerificationStatus = "VERIFIED",
            Confidence = 0.90,
            RiskLevel = "CRITICAL",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Entities.AddRange(_personA, _phoneA, _vehicleA, _locationA, _orgA);

        // Seed Relationships
        _rel1 = new Relationship
        {
            Id = "rel-001",
            CaseId = _case1.Id,
            SourceEntityId = _personA.Id,
            TargetEntityId = _phoneA.Id,
            Type = "COMMUNICATED_WITH",
            Confidence = 0.98,
            SourceEvidenceId = _evidence1.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        _rel2 = new Relationship
        {
            Id = "rel-002",
            CaseId = _case1.Id,
            SourceEntityId = _personA.Id,
            TargetEntityId = _vehicleA.Id,
            Type = "OPERATES",
            Confidence = 0.95,
            SourceEvidenceId = _evidence2.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        _rel3 = new Relationship
        {
            Id = "rel-003",
            CaseId = _case1.Id,
            SourceEntityId = _personA.Id,
            TargetEntityId = _locationA.Id,
            Type = "LOCATED_AT",
            Confidence = 0.92,
            SourceEvidenceId = _evidence2.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        _rel4 = new Relationship
        {
            Id = "rel-004",
            CaseId = _case1.Id,
            SourceEntityId = _phoneA.Id,
            TargetEntityId = _orgA.Id,
            Type = "ASSOCIATE_OF",
            Confidence = 0.89,
            SourceEvidenceId = _evidence1.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Relationships.AddRange(_rel1, _rel2, _rel3, _rel4);

        // Seed Multiple Supporting Evidence records on rel1
        var relEv1 = new RelationshipEvidence
        {
            Id = "relev-test-01",
            RelationshipId = _rel1.Id,
            EvidenceId = _evidence1.Id,
            CaseId = _case1.Id,
            Confidence = 0.98,
            SourceLocation = "Line 10: CDR Record",
            VerifiedBy = "DCP Rajesh Sharma",
            VerifiedAtUtc = DateTime.UtcNow
        };

        var relEv2 = new RelationshipEvidence
        {
            Id = "relev-test-02",
            RelationshipId = _rel1.Id,
            EvidenceId = _evidence2.Id,
            CaseId = _case1.Id,
            Confidence = 0.99,
            SourceLocation = "Line 3: Surveillance observation",
            VerifiedBy = "DCP Rajesh Sharma",
            VerifiedAtUtc = DateTime.UtcNow
        };
        _dbContext.RelationshipEvidences.AddRange(relEv1, relEv2);

        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    // 1. Case graph retrieval
    [Fact]
    public async Task CaseGraphRetrieval_ShouldReturnCaseScopedNodesAndEdges()
    {
        var result = await _graphService.GetCaseGraphAsync(
            _case1.Id, null, null, 1, null, 100, "usr-01", "INVESTIGATOR");

        result.Should().NotBeNull();
        result.CaseId.Should().Be(_case1.Id);
        result.Nodes.Should().HaveCount(5);
        result.Edges.Should().HaveCount(4);
        result.Nodes.Should().Contain(n => n.Name == "Rahul Mehta");
    }

    // 2. Empty graph
    [Fact]
    public async Task EmptyGraph_ShouldReturnEmptyDtoWhenCaseHasNoApprovedEntities()
    {
        var emptyCase = new Case
        {
            Id = "inv-empty-001",
            CaseNumber = "CASE-EMPTY",
            Title = "Empty Case",
            Status = "Active"
        };
        _dbContext.Cases.Add(emptyCase);
        await _dbContext.SaveChangesAsync();

        var result = await _graphService.GetCaseGraphAsync(
            emptyCase.Id, null, null, 1, null, 100, "usr-01", "INVESTIGATOR");

        result.Should().NotBeNull();
        result.Nodes.Should().BeEmpty();
        result.Edges.Should().BeEmpty();
    }

    // 3. Graph node mapping
    [Fact]
    public async Task GraphNodeMapping_ShouldMapPropertiesAndTypesCorrectly()
    {
        var result = await _graphService.GetCaseGraphAsync(
            _case1.Id, null, null, 1, null, 100, "usr-01", "INVESTIGATOR");

        var node = result.Nodes.First(n => n.Id == _personA.Id);
        node.Name.Should().Be("Rahul Mehta");
        node.Type.Should().Be("PERSON");
        node.Verified.Should().BeTrue();
        node.CaseId.Should().Be(_case1.Id);
        node.Risk.Should().Be("HIGH");
    }

    // 4. Relationship mapping
    [Fact]
    public async Task RelationshipMapping_ShouldIncludeProvenanceAndConfidence()
    {
        var result = await _graphService.GetCaseGraphAsync(
            _case1.Id, null, null, 1, null, 100, "usr-01", "INVESTIGATOR");

        var edge = result.Edges.First(e => e.Id == _rel1.Id);
        edge.Source.Should().Be(_personA.Id);
        edge.Target.Should().Be(_phoneA.Id);
        edge.Type.Should().Be("COMMUNICATED_WITH");
        edge.Confidence.Should().Be(0.98);
        edge.SourceEvidenceId.Should().Be(_evidence1.Id);
    }

    // 5. Case authorization
    [Fact]
    public async Task CaseAuthorization_ShouldDenyAccessToUnauthorizedRole()
    {
        Func<Task> act = async () =>
        {
            await _graphService.GetCaseGraphAsync(
                _case1.Id, null, null, 1, null, 100, "usr-unauth", "GUEST");
        };

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // 6. Cross-case access denial / isolation
    [Fact]
    public async Task CrossCaseAccessDenial_ShouldNotLeakNodesFromOtherCases()
    {
        // Seed an entity in Case 2
        var case2Ent = new EntityItem
        {
            Id = "ent-case2-secret",
            CaseId = _case2.Id,
            Type = "PERSON",
            CanonicalName = "Secret Agent",
            VerificationStatus = "VERIFIED"
        };
        _dbContext.Entities.Add(case2Ent);
        await _dbContext.SaveChangesAsync();

        var result = await _graphService.GetCaseGraphAsync(
            _case1.Id, null, null, 1, null, 100, "usr-01", "INVESTIGATOR");

        result.Nodes.Should().NotContain(n => n.Id == case2Ent.Id);
        result.Nodes.Should().OnlyContain(n => n.CaseId == _case1.Id);
    }

    // 7. Neighborhood depth 1
    [Fact]
    public async Task Neighborhood_Depth1_ShouldReturnDirectNeighbors()
    {
        var result = await _graphService.GetEntityNeighborhoodAsync(
            _personA.Id, 1, _case1.Id, "usr-01", "INVESTIGATOR");

        result.Should().NotBeNull();
        result.CenterEntityId.Should().Be(_personA.Id);
        // Direct neighbors of Rahul Mehta are: Phone, Vehicle, Location
        result.Nodes.Should().Contain(n => n.Id == _phoneA.Id);
        result.Nodes.Should().Contain(n => n.Id == _vehicleA.Id);
        result.Nodes.Should().Contain(n => n.Id == _locationA.Id);
        // OrgA is 2 hops away (Rahul -> Phone -> OrgA), so it shouldn't be in depth 1
        result.Nodes.Should().NotContain(n => n.Id == _orgA.Id);
    }

    // 8. Neighborhood depth 2
    [Fact]
    public async Task Neighborhood_Depth2_ShouldReturnTwoHopNeighbors()
    {
        var result = await _graphService.GetEntityNeighborhoodAsync(
            _personA.Id, 2, _case1.Id, "usr-01", "INVESTIGATOR");

        result.Should().NotBeNull();
        result.Nodes.Should().Contain(n => n.Id == _orgA.Id); // 2 hops away
        result.Edges.Should().Contain(e => e.Id == _rel4.Id);
    }

    // 9. Maximum depth enforcement
    [Fact]
    public async Task MaxDepthEnforcement_ShouldClampDepthExceeding3()
    {
        var result = await _graphService.GetEntityNeighborhoodAsync(
            _personA.Id, 10, _case1.Id, "usr-01", "INVESTIGATOR");

        result.RequestedDepth.Should().Be(3);
    }

    // 10. Graph search
    [Fact]
    public async Task GraphSearch_ShouldFindEntitiesByNamePhoneOrVehicle()
    {
        var searchByName = await _graphService.SearchGraphAsync("Rahul", _case1.Id, "usr-01", "INVESTIGATOR");
        searchByName.Should().ContainSingle(r => r.Name == "Rahul Mehta");

        var searchByPhone = await _graphService.SearchGraphAsync("9000001001", _case1.Id, "usr-01", "INVESTIGATOR");
        searchByPhone.Should().ContainSingle(r => r.Name == "+91-9000001001");

        var searchByVehicle = await _graphService.SearchGraphAsync("MH12AB4321", _case1.Id, "usr-01", "INVESTIGATOR");
        searchByVehicle.Should().ContainSingle(r => r.Name == "MH12AB4321");
    }

    // 11. Shortest path
    [Fact]
    public async Task ShortestPath_ShouldFindConnectingPathBetweenEntities()
    {
        // Rahul Mehta -> Phone (+91-9000001001) -> Apex Trading Syndicate
        var path = await _graphService.GetShortestPathAsync(
            _personA.Id, _orgA.Id, _case1.Id, 4, "usr-01", "INVESTIGATOR");

        path.Should().NotBeNull();
        path.Found.Should().BeTrue();
        path.HopsCount.Should().Be(2);
        path.Nodes.Select(n => n.Id).Should().Equal(new[] { _personA.Id, _phoneA.Id, _orgA.Id });
        path.Edges.Should().HaveCount(2);
    }

    // 12. Graph statistics
    [Fact]
    public async Task GraphStatistics_ShouldCalculateAccurateMetricsAndComponents()
    {
        var stats = await _graphService.GetGraphStatisticsAsync(
            _case1.Id, "usr-01", "INVESTIGATOR");

        stats.Should().NotBeNull();
        stats.TotalNodes.Should().Be(5);
        stats.TotalEdges.Should().Be(4);
        stats.EntityTypeDistribution.Should().ContainKey("PERSON");
        stats.EntityTypeDistribution["PERSON"].Should().Be(1);
        stats.ConnectedComponents.Should().NotBeEmpty();
        stats.ConnectedComponents[0].ClusterLabel.Should().StartWith("Network Cluster");
    }

    // 13. Centrality
    [Fact]
    public async Task Centrality_ShouldCalculateDegreeCentralityWithNeutralTerminology()
    {
        var stats = await _graphService.GetGraphStatisticsAsync(
            _case1.Id, "usr-01", "INVESTIGATOR");

        stats.CentralityRankings.Should().NotBeEmpty();
        var top = stats.CentralityRankings[0];
        top.EntityId.Should().Be(_personA.Id);
        top.Degree.Should().Be(3); // 3 connections: Phone, Vehicle, Location
        top.AnalyticalIndicator.Should().Be("High Connectivity Lead"); // Neutral investigative term
    }

    // 14. Relationship provenance
    [Fact]
    public async Task RelationshipProvenance_ShouldContainSourceEvidenceAndVerifiedBy()
    {
        var details = await _graphService.GetRelationshipDetailsAsync(
            _rel1.Id, _case1.Id, "usr-01", "INVESTIGATOR");

        details.Should().NotBeNull();
        details!.Id.Should().Be(_rel1.Id);
        details.Type.Should().Be("COMMUNICATED_WITH");
        details.SupportingEvidence.Should().NotBeEmpty();
        details.SupportingEvidence.Should().Contain(se => se.EvidenceId == _evidence1.Id);
    }

    // 15. Evidence navigation ID
    [Fact]
    public async Task EvidenceNavigation_ShouldProvideValidEvidenceIdForRouting()
    {
        var details = await _graphService.GetRelationshipDetailsAsync(
            _rel1.Id, _case1.Id, "usr-01", "INVESTIGATOR");

        details.Should().NotBeNull();
        var primaryId = details!.SupportingEvidence.First().EvidenceId;
        var evidence = await _dbContext.EvidenceItems.FindAsync(primaryId);
        evidence.Should().NotBeNull();
        evidence!.FileName.Should().Be("call_records_pune.csv");
        evidence.Sha256Hash.Should().NotBeNullOrWhiteSpace();
    }

    // 16. Multiple evidence support
    [Fact]
    public async Task MultipleEvidenceSupport_ShouldAccumulateMultipleEvidenceRecordsOnSameRelationship()
    {
        var details = await _graphService.GetRelationshipDetailsAsync(
            _rel1.Id, _case1.Id, "usr-01", "INVESTIGATOR");

        details.Should().NotBeNull();
        // rel1 has both evidence1 (CDR) and evidence2 (Surveillance) attached
        details!.SupportingEvidence.Should().HaveCount(2);
        details.SupportingEvidence.Select(s => s.EvidenceId)
            .Should().Contain(new[] { _evidence1.Id, _evidence2.Id });
    }

    // 17. No mock graph dependency
    [Fact]
    public async Task NoMockGraphDependency_ShouldRelySolelyOnDatabaseState()
    {
        // Add a new dynamic entity to database
        var dynamicEntity = new EntityItem
        {
            Id = "ent-dynamic-test",
            CaseId = _case1.Id,
            Type = "ACCOUNT",
            CanonicalName = "ACC-999-SPECIAL",
            VerificationStatus = "VERIFIED"
        };
        _dbContext.Entities.Add(dynamicEntity);
        await _dbContext.SaveChangesAsync();

        var graph = await _graphService.GetCaseGraphAsync(
            _case1.Id, null, null, 1, null, 100, "usr-01", "INVESTIGATOR");

        // The newly inserted entity is returned directly from the database without any static mocks
        graph.Nodes.Should().Contain(n => n.Id == "ent-dynamic-test");
    }
}
