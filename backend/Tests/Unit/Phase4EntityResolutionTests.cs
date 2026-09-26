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

public class Phase4EntityResolutionTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<INeo4jService> _mockNeo4j;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly EntityResolutionService _resolutionService;

    private readonly Case _caseA;
    private readonly Case _caseB;
    private readonly Case _caseC;

    private readonly Evidence _evidenceA;
    private readonly Evidence _evidenceB;

    public Phase4EntityResolutionTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);
        _mockNeo4j = new Mock<INeo4jService>();
        _mockAudit = new Mock<IAuditService>();

        _resolutionService = new EntityResolutionService(
            _dbContext,
            _mockNeo4j.Object,
            _mockAudit.Object,
            NullLogger<EntityResolutionService>.Instance);

        // Seed Cases
        _caseA = new Case
        {
            Id = "case-alpha",
            CaseNumber = "CASE-2026-001",
            Title = "Pune Hawala Syndicate",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _caseB = new Case
        {
            Id = "case-beta",
            CaseNumber = "CASE-2026-002",
            Title = "Mumbai Port Interception",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _caseC = new Case
        {
            Id = "case-gamma",
            CaseNumber = "CASE-2026-003",
            Title = "Nashik Highway Corridor",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Cases.AddRange(_caseA, _caseB, _caseC);

        // Seed Evidence
        _evidenceA = new Evidence
        {
            Id = "ev-a-01",
            CaseId = _caseA.Id,
            FileName = "call_records_pune.csv",
            StoragePath = "uploads/evidence/call_records_pune.csv",
            Sha256Hash = "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
            ProcessingStatus = "APPROVED",
            UploadedAtUtc = DateTime.UtcNow
        };

        _evidenceB = new Evidence
        {
            Id = "ev-b-01",
            CaseId = _caseB.Id,
            FileName = "mumbai_manifest.csv",
            StoragePath = "uploads/evidence/mumbai_manifest.csv",
            Sha256Hash = "7c2c73dfdc485cbf788c2ed0f27e6be2a0859a9024ea3723aaf3cc18f6369168",
            ProcessingStatus = "APPROVED",
            UploadedAtUtc = DateTime.UtcNow
        };

        _dbContext.EvidenceItems.AddRange(_evidenceA, _evidenceB);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task ExactPhoneMatch_CreatesCandidate_WithHighConfidence()
    {
        // Arrange
        var personA = new EntityItem
        {
            Id = "ent-pune-person",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Vikram Deshmukh",
            NormalizedValue = "VIKRAM DESHMUKH",
            PhoneNumber = "+91 98201 99888"
        };

        var personB = new EntityItem
        {
            Id = "ent-mum-person",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "V. Deshmukh",
            NormalizedValue = "V DESHMUKH",
            PhoneNumber = "9820199888"
        };

        _dbContext.Entities.AddRange(personA, personB);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _resolutionService.RunBatchResolutionAsync(
            _caseA.Id,
            null,
            0.6,
            "test-user",
            "Inspector Sharma");

        // Assert
        result.CandidatesCreated.Should().BeGreaterOrEqualTo(1);

        var candidate = await _dbContext.EntityMatchCandidates
            .FirstOrDefaultAsync(c => c.SourceEntityId == personA.Id && c.TargetEntityId == personB.Id);
        candidate.Should().NotBeNull();
        candidate!.MatchScore.Should().BeGreaterOrEqualTo(0.85);
        candidate.MatchStatus.Should().Be("PENDING");
        candidate.MatchExplanationJson.Should().Contain("SHARED_PHONE");
    }

    [Fact]
    public async Task ExactVehicleMatch_CreatesCandidate_WithHighConfidence()
    {
        // Arrange
        var personA = new EntityItem
        {
            Id = "ent-pune-veh-owner",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Suresh Patil",
            NormalizedValue = "SURESH PATIL",
            VehicleNumber = "MH 12 AB 4321"
        };

        var personB = new EntityItem
        {
            Id = "ent-mum-veh-owner",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "S. Patil",
            NormalizedValue = "S PATIL",
            VehicleNumber = "MH12AB4321"
        };

        _dbContext.Entities.AddRange(personA, personB);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _resolutionService.RunBatchResolutionAsync(
            _caseA.Id,
            null,
            0.6,
            "test-user",
            "Inspector Sharma");

        // Assert
        result.CandidatesCreated.Should().BeGreaterOrEqualTo(1);
        var candidate = await _dbContext.EntityMatchCandidates
            .FirstOrDefaultAsync(c => c.SourceEntityId == personA.Id && c.TargetEntityId == personB.Id);
        candidate.Should().NotBeNull();
        candidate!.MatchScore.Should().BeGreaterOrEqualTo(0.85);
        candidate.MatchExplanationJson.Should().Contain("SHARED_VEHICLE");
    }

    [Fact]
    public async Task ExactAccountMatch_CreatesCandidate_WithHighConfidence()
    {
        // Arrange
        var personA = new EntityItem
        {
            Id = "ent-acc-a",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Rohan Shinde",
            NormalizedValue = "ROHAN SHINDE",
            AccountNumber = "998877665544",
            BankName = "State Bank"
        };

        var personB = new EntityItem
        {
            Id = "ent-acc-b",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "R. Shinde",
            NormalizedValue = "R SHINDE",
            AccountNumber = "998877665544",
            BankName = "State Bank"
        };

        _dbContext.Entities.AddRange(personA, personB);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _resolutionService.RunBatchResolutionAsync(
            _caseA.Id,
            null,
            0.6,
            "test-user",
            "Inspector Sharma");

        // Assert
        result.CandidatesCreated.Should().BeGreaterOrEqualTo(1);
        var candidate = await _dbContext.EntityMatchCandidates
            .FirstOrDefaultAsync(c => c.SourceEntityId == personA.Id && c.TargetEntityId == personB.Id);
        candidate.Should().NotBeNull();
        candidate!.MatchScore.Should().BeGreaterOrEqualTo(0.85);
        candidate.MatchExplanationJson.Should().Contain("SHARED_ACCOUNT");
    }

    [Fact]
    public async Task NameSimilarity_AboveThreshold_CreatesCandidate()
    {
        // Arrange
        var orgA = new EntityItem
        {
            Id = "ent-org-a",
            CaseId = _caseA.Id,
            Type = "ORGANIZATION",
            CanonicalName = "Apex Maritime Logistics Private Limited",
            NormalizedValue = "APEX MARITIME LOGISTICS PRIVATE LIMITED"
        };

        var orgB = new EntityItem
        {
            Id = "ent-org-b",
            CaseId = _caseB.Id,
            Type = "ORGANIZATION",
            CanonicalName = "Apex Maritime Logistics Pvt Ltd",
            NormalizedValue = "APEX MARITIME LOGISTICS PVT LTD"
        };

        _dbContext.Entities.AddRange(orgA, orgB);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _resolutionService.RunBatchResolutionAsync(
            _caseA.Id,
            null,
            0.6,
            "test-user",
            "Inspector Sharma");

        // Assert
        result.CandidatesCreated.Should().BeGreaterOrEqualTo(1);
        var candidate = await _dbContext.EntityMatchCandidates
            .FirstOrDefaultAsync(c => c.SourceEntityId == orgA.Id && c.TargetEntityId == orgB.Id);
        candidate.Should().NotBeNull();
        candidate!.MatchScore.Should().BeGreaterOrEqualTo(0.70);
    }

    [Fact]
    public async Task SameNameAlone_DoesNotExceedSafeguardCap_RemainsPending()
    {
        // False-Positive Safeguard Test:
        // Two PERSON entities with common name "Rahul Sharma" across cases,
        // but NO shared phone, vehicle, account, or location context.
        var person1 = new EntityItem
        {
            Id = "ent-rahul-1",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Rahul Sharma",
            NormalizedValue = "RAHUL SHARMA"
        };

        var person2 = new EntityItem
        {
            Id = "ent-rahul-2",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "Rahul Sharma",
            NormalizedValue = "RAHUL SHARMA"
        };

        _dbContext.Entities.AddRange(person1, person2);
        await _dbContext.SaveChangesAsync();

        // Act - run resolution with low threshold to capture it
        var result = await _resolutionService.RunBatchResolutionAsync(
            _caseA.Id,
            null,
            0.50,
            "test-user",
            "Inspector Sharma");

        // Assert
        var candidate = await _dbContext.EntityMatchCandidates
            .FirstOrDefaultAsync(c => c.SourceEntityId == person1.Id && c.TargetEntityId == person2.Id);

        candidate.Should().NotBeNull();
        // Mandatory requirement: Same name alone without corroborating signal must be capped strictly at < 0.60
        candidate!.MatchScore.Should().BeLessThanOrEqualTo(0.55);
        candidate.MatchStatus.Should().Be("PENDING");
        candidate.MatchExplanationJson.Should().Contain("Name Similarity (Uncorroborated - Common Name Safeguard Applied)");
    }

    [Fact]
    public async Task DifferentEntityTypes_AreBlockedFromComparison()
    {
        // Arrange: A PERSON and a VEHICLE with similar strings should never be matched
        var person = new EntityItem
        {
            Id = "ent-person-swift",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Swift Logistics",
            NormalizedValue = "SWIFT LOGISTICS"
        };

        var vehicle = new EntityItem
        {
            Id = "ent-veh-swift",
            CaseId = _caseB.Id,
            Type = "VEHICLE",
            CanonicalName = "Swift Logistics",
            NormalizedValue = "SWIFT LOGISTICS"
        };

        _dbContext.Entities.AddRange(person, vehicle);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _resolutionService.RunBatchResolutionAsync(
            _caseA.Id,
            null,
            0.50,
            "test-user",
            "Inspector Sharma");

        // Assert
        var candidate = await _dbContext.EntityMatchCandidates
            .FirstOrDefaultAsync(c =>
                (c.SourceEntityId == person.Id && c.TargetEntityId == vehicle.Id) ||
                (c.SourceEntityId == vehicle.Id && c.TargetEntityId == person.Id));

        candidate.Should().BeNull(); // Blocked by entity type mismatch
    }

    [Fact]
    public void Normalization_CalculatesAccurateSimilarityMetrics()
    {
        var sim1 = StringSimilarity.ComparePersonNames("Rahul Suresh Sharma", "Rahul S. Sharma");
        sim1.Should().BeGreaterOrEqualTo(0.80);

        var jaro1 = StringSimilarity.JaroWinklerSimilarity("VIKRAM DESHMUKH", "V DESHMUKH");
        jaro1.Should().BeGreaterOrEqualTo(0.70);

        var lev1 = StringSimilarity.LevenshteinRatio("MAHARASHTRA", "MAHARASHTA");
        lev1.Should().BeGreaterOrEqualTo(0.90);
    }

    [Fact]
    public async Task AliasMatching_MatchesEntitiesOnKnownAliases()
    {
        // Arrange
        var personA = new EntityItem
        {
            Id = "ent-alias-a",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "R. K. Verma",
            NormalizedValue = "R K VERMA",
            AliasesJson = "[\"Bhaijaan\", \"RK Seth\", \"Rakesh Verma\"]"
        };

        var personB = new EntityItem
        {
            Id = "ent-alias-b",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "Rakesh Verma",
            NormalizedValue = "RAKESH VERMA",
            PhoneNumber = "+919811122233"
        };

        _dbContext.Entities.AddRange(personA, personB);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _resolutionService.RunBatchResolutionAsync(
            _caseA.Id,
            null,
            0.60,
            "test-user",
            "Inspector Sharma");

        // Assert
        var candidate = await _dbContext.EntityMatchCandidates
            .FirstOrDefaultAsync(c => c.SourceEntityId == personA.Id && c.TargetEntityId == personB.Id);

        candidate.Should().NotBeNull();
        candidate!.MatchExplanationJson.Should().Contain("ALIAS_MATCH");
    }

    [Fact]
    public async Task MultipleSignals_AccumulateHigherConfidence()
    {
        // Single signal: Phone match only
        var personA1 = new EntityItem
        {
            Id = "ent-sig-a1",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "John Doe",
            NormalizedValue = "JOHN DOE",
            PhoneNumber = "+919999988888"
        };
        var personB1 = new EntityItem
        {
            Id = "ent-sig-b1",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "Different Name",
            NormalizedValue = "DIFFERENT NAME",
            PhoneNumber = "+919999988888"
        };

        // Dual signal: Phone match AND Vehicle match AND Name similarity
        var personA2 = new EntityItem
        {
            Id = "ent-sig-a2",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Ajay Rathod",
            NormalizedValue = "AJAY RATHOD",
            PhoneNumber = "+918888877777",
            VehicleNumber = "MH01CD1234"
        };
        var personB2 = new EntityItem
        {
            Id = "ent-sig-b2",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "A. Rathod",
            NormalizedValue = "A RATHOD",
            PhoneNumber = "+918888877777",
            VehicleNumber = "MH01CD1234"
        };

        _dbContext.Entities.AddRange(personA1, personB1, personA2, personB2);
        await _dbContext.SaveChangesAsync();

        // Act
        await _resolutionService.RunBatchResolutionAsync(_caseA.Id, null, 0.6, "test-user", "Inspector Sharma");

        // Assert
        var candidateSingle = await _dbContext.EntityMatchCandidates.FirstOrDefaultAsync(c => c.SourceEntityId == personA1.Id);
        var candidateMulti = await _dbContext.EntityMatchCandidates.FirstOrDefaultAsync(c => c.SourceEntityId == personA2.Id);

        candidateSingle.Should().NotBeNull();
        candidateMulti.Should().NotBeNull();

        candidateMulti!.MatchScore.Should().BeGreaterThan(candidateSingle!.MatchScore);
        candidateMulti.MatchScore.Should().BeGreaterOrEqualTo(0.95);
    }

    [Fact]
    public async Task CandidateCreation_IsIdempotent()
    {
        // Arrange
        var personA = new EntityItem
        {
            Id = "ent-idem-a",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Kunal Kapoor",
            NormalizedValue = "KUNAL KAPOOR",
            PhoneNumber = "+919876543210"
        };

        var personB = new EntityItem
        {
            Id = "ent-idem-b",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "Kunal Kapoor",
            NormalizedValue = "KUNAL KAPOOR",
            PhoneNumber = "+919876543210"
        };

        _dbContext.Entities.AddRange(personA, personB);
        await _dbContext.SaveChangesAsync();

        // Act - Run 1
        var run1 = await _resolutionService.RunBatchResolutionAsync(_caseA.Id, null, 0.6, "test-user", "Inspector Sharma");
        run1.CandidatesCreated.Should().Be(1);

        // Act - Run 2 (repeated)
        var run2 = await _resolutionService.RunBatchResolutionAsync(_caseA.Id, null, 0.6, "test-user", "Inspector Sharma");

        // Assert: no duplicate candidates created
        run2.CandidatesCreated.Should().Be(0);
        var totalCandidates = await _dbContext.EntityMatchCandidates
            .CountAsync(c => c.SourceEntityId == personA.Id && c.TargetEntityId == personB.Id);
        totalCandidates.Should().Be(1);
    }

    [Fact]
    public async Task CandidateApproval_UpdatesStatusToApproved_AndSetsAuditAndReviewMetadata()
    {
        // Arrange
        var entA = new EntityItem
        {
            Id = "ent-src-01",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Vikram Deshmukh",
            NormalizedValue = "VIKRAM DESHMUKH"
        };
        var entB = new EntityItem
        {
            Id = "ent-tgt-01",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "V. Deshmukh",
            NormalizedValue = "V DESHMUKH"
        };
        _dbContext.Entities.AddRange(entA, entB);

        var candidate = new EntityMatchCandidate
        {
            Id = "cand-app-01",
            SourceCaseId = _caseA.Id,
            TargetCaseId = _caseB.Id,
            SourceEntityId = entA.Id,
            TargetEntityId = entB.Id,
            EntityType = "PERSON",
            MatchScore = 0.92,
            MatchMethod = "MULTI_SIGNAL_DETERMINISTIC",
            MatchStatus = "PENDING",
            MatchExplanationJson = "{\"factors\":[{\"type\":\"DIRECT_PHONE\",\"score\":0.95}]}",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.EntityMatchCandidates.Add(candidate);
        await _dbContext.SaveChangesAsync();

        // Act
        var success = await _resolutionService.ReviewCandidateAsync(
            candidate.Id,
            new CandidateReviewRequestDto
            {
                Status = "APPROVED",
                ReviewNotes = "Confirmed via CDR cross-verification."
            },
            "user-123",
            "Investigator Desai",
            "127.0.0.1");

        // Assert
        success.Should().BeTrue();

        var inDb = await _dbContext.EntityMatchCandidates.FindAsync(candidate.Id);
        inDb.Should().NotBeNull();
        inDb!.MatchStatus.Should().Be("APPROVED");
        inDb.ReviewedBy.Should().Be("Investigator Desai");
        inDb.ReviewedAtUtc.Should().NotBeNull();
        inDb.ReviewNotes.Should().Be("Confirmed via CDR cross-verification.");

        // Check Audit event logged
        _mockAudit.Verify(a => a.LogAsync(
            "user-123",
            "Investigator Desai",
            "MATCH_APPROVED",
            "EntityMatchCandidate",
            candidate.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            "127.0.0.1",
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Check Neo4j CrossCaseLink called
        _mockNeo4j.Verify(n => n.CreateCrossCaseLinkAsync(
            It.IsAny<string>(),
            candidate.SourceEntityId,
            candidate.TargetEntityId,
            It.IsAny<string>(),
            candidate.MatchScore,
            candidate.SourceCaseId,
            candidate.TargetCaseId,
            "Investigator Desai",
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Check CrossCaseConnection record created
        var connection = await _dbContext.CrossCaseConnections
            .FirstOrDefaultAsync(c => c.CandidateId == candidate.Id);
        connection.Should().NotBeNull();
        connection!.Status.Should().Be("APPROVED");
        connection.Confidence.Should().Be(candidate.MatchScore);
    }

    [Fact]
    public async Task CandidateRejection_UpdatesStatusToRejected_AndDoesNotCreateCrossCaseLink()
    {
        // Arrange
        var entA = new EntityItem
        {
            Id = "ent-src-02",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Sunil Gaikwad",
            NormalizedValue = "SUNIL GAIKWAD"
        };
        var entB = new EntityItem
        {
            Id = "ent-tgt-02",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "S. Gaikwad",
            NormalizedValue = "S GAIKWAD"
        };
        _dbContext.Entities.AddRange(entA, entB);

        var candidate = new EntityMatchCandidate
        {
            Id = "cand-rej-01",
            SourceCaseId = _caseA.Id,
            TargetCaseId = _caseB.Id,
            SourceEntityId = entA.Id,
            TargetEntityId = entB.Id,
            EntityType = "PERSON",
            MatchScore = 0.65,
            MatchMethod = "MULTI_SIGNAL_DETERMINISTIC",
            MatchStatus = "PENDING",
            MatchExplanationJson = "{\"factors\":[{\"type\":\"NAME_SIMILARITY\",\"score\":0.65}]}",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.EntityMatchCandidates.Add(candidate);
        await _dbContext.SaveChangesAsync();

        // Act
        var success = await _resolutionService.ReviewCandidateAsync(
            candidate.Id,
            new CandidateReviewRequestDto
            {
                Status = "REJECTED",
                ReviewNotes = "Distinct individuals confirmed with differing dates of birth."
            },
            "user-456",
            "Senior Inspector Kulkarni",
            "127.0.0.1");

        // Assert
        success.Should().BeTrue();

        var inDb = await _dbContext.EntityMatchCandidates.FindAsync(candidate.Id);
        inDb.Should().NotBeNull();
        inDb!.MatchStatus.Should().Be("REJECTED");
        inDb.ReviewedBy.Should().Be("Senior Inspector Kulkarni");

        // Audit log verified
        _mockAudit.Verify(a => a.LogAsync(
            "user-456",
            "Senior Inspector Kulkarni",
            "MATCH_REJECTED",
            "EntityMatchCandidate",
            candidate.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            "127.0.0.1",
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Neo4j cross-case link MUST NOT be called
        _mockNeo4j.Verify(n => n.CreateCrossCaseLinkAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // CrossCaseConnection MUST NOT be created
        var connection = await _dbContext.CrossCaseConnections
            .FirstOrDefaultAsync(c => c.CandidateId == candidate.Id);
        connection.Should().BeNull();
    }

    [Fact]
    public async Task PendingMatch_DoesNotAppearInVerifiedCrossCaseConnections()
    {
        // Arrange
        var candidate = new EntityMatchCandidate
        {
            Id = "cand-pending-01",
            SourceCaseId = _caseA.Id,
            TargetCaseId = _caseB.Id,
            SourceEntityId = "ent-p-01",
            TargetEntityId = "ent-p-02",
            EntityType = "PERSON",
            MatchScore = 0.90,
            MatchMethod = "MULTI_SIGNAL_DETERMINISTIC",
            MatchStatus = "PENDING",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.EntityMatchCandidates.Add(candidate);
        await _dbContext.SaveChangesAsync();

        // Act
        var connections = await _resolutionService.GetCrossCaseConnectionsAsync(_caseA.Id, null, null);

        // Assert: Pending candidate must not appear as a verified cross-case connection
        connections.Should().BeEmpty();
    }

    [Fact]
    public async Task CrossCaseComparison_ReturnsDetailedMultiSignalBreakdownAndCitations()
    {
        // Arrange
        var personA = new EntityItem
        {
            Id = "ent-comp-a",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Sameer Khan",
            NormalizedValue = "SAMEER KHAN",
            PhoneNumber = "+919833322211"
        };

        var personB = new EntityItem
        {
            Id = "ent-comp-b",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "S. Khan",
            NormalizedValue = "S KHAN",
            PhoneNumber = "+919833322211"
        };

        _dbContext.Entities.AddRange(personA, personB);
        await _dbContext.SaveChangesAsync();

        await _resolutionService.RunBatchResolutionAsync(_caseA.Id, null, 0.6, "test-user", "Inspector Sharma");

        var candidate = await _dbContext.EntityMatchCandidates
            .FirstAsync(c => c.SourceEntityId == personA.Id && c.TargetEntityId == personB.Id);

        // Act
        var comparison = await _resolutionService.GetCandidateComparisonAsync(candidate.Id);

        // Assert
        comparison.Should().NotBeNull();
        comparison!.SideA.CanonicalName.Should().Be("Sameer Khan");
        comparison.SideB.CanonicalName.Should().Be("S. Khan");
        comparison.Factors.Should().NotBeEmpty();
        comparison.Factors.Should().Contain(f => f.Type.Contains("PHONE"));
    }

    [Fact]
    public async Task EvidenceProvenance_PreservedAcrossCases()
    {
        // Arrange
        var entA = new EntityItem
        {
            Id = "ent-src-prov",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Tarun Mehta",
            NormalizedValue = "TARUN MEHTA"
        };

        var entB = new EntityItem
        {
            Id = "ent-tgt-prov",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "Tarun Mehta",
            NormalizedValue = "TARUN MEHTA"
        };

        var candidate = new EntityMatchCandidate
        {
            Id = "cand-prov-01",
            SourceCaseId = _caseA.Id,
            TargetCaseId = _caseB.Id,
            SourceEntityId = entA.Id,
            TargetEntityId = entB.Id,
            EntityType = "PERSON",
            MatchScore = 0.95,
            MatchMethod = "MULTI_SIGNAL_DETERMINISTIC",
            MatchStatus = "PENDING",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Entities.AddRange(entA, entB);
        _dbContext.EntityMatchCandidates.Add(candidate);
        await _dbContext.SaveChangesAsync();

        // Act - approve
        var success = await _resolutionService.ReviewCandidateAsync(
            candidate.Id,
            new CandidateReviewRequestDto
            {
                Status = "APPROVED",
                ReviewNotes = "Provenance cross-checked"
            },
            "user-789",
            "Lead Inspector Roy",
            "127.0.0.1");

        // Assert - verify CrossCaseConnection has supporting evidence linking both cases
        success.Should().BeTrue();
        var connection = await _dbContext.CrossCaseConnections.FirstOrDefaultAsync(c => c.CandidateId == candidate.Id);
        connection.Should().NotBeNull();
        connection!.SupportingEvidenceJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CrossCaseNetwork_ReturnsCorrectNodesAndEdges()
    {
        // Arrange
        var entA = new EntityItem
        {
            Id = "ent-net-a",
            CaseId = _caseA.Id,
            Type = "PERSON",
            CanonicalName = "Deepak Joshi",
            NormalizedValue = "DEEPAK JOSHI"
        };

        var entB = new EntityItem
        {
            Id = "ent-net-b",
            CaseId = _caseB.Id,
            Type = "PERSON",
            CanonicalName = "D. Joshi",
            NormalizedValue = "D JOSHI"
        };

        var connection = new CrossCaseConnection
        {
            Id = "conn-net-01",
            CandidateId = "cand-net-01",
            SourceCaseId = _caseA.Id,
            TargetCaseId = _caseB.Id,
            SourceEntityId = entA.Id,
            TargetEntityId = entB.Id,
            ConnectionType = "POTENTIAL_SAME_ENTITY",
            Confidence = 0.94,
            Status = "APPROVED",
            Explanation = "Shared Phone Number Match",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Entities.AddRange(entA, entB);
        _dbContext.CrossCaseConnections.Add(connection);
        await _dbContext.SaveChangesAsync();

        // Act
        var network = await _resolutionService.GetCrossCaseNetworkAsync(_caseA.Id, null, null);

        // Assert
        network.Should().NotBeNull();
        network.Nodes.Should().HaveCount(2);
        network.Edges.Should().HaveCount(1);
        network.ConnectedCasesCount.Should().BeGreaterOrEqualTo(1);
        network.Edges[0].Type.Should().Be("POTENTIAL_SAME_ENTITY");
        network.Edges[0].Confidence.Should().Be(0.94);
    }
}
