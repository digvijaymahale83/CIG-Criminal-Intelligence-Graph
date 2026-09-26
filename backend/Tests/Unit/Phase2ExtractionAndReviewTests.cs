using System.Net;
using System.Text;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Services;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Tests.Unit;

public class Phase2ExtractionAndReviewTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly AuditService _auditService;
    private readonly Sha256HashService _hashService;
    private readonly LocalFileStorageService _storageService;
    private readonly Mock<INeo4jService> _mockNeo4j;
    private readonly ExtractionService _extractionService;
    private readonly EvidenceService _evidenceService;
    private readonly string _testTempDir;

    public Phase2ExtractionAndReviewTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);
        _auditService = new AuditService(_dbContext, NullLogger<AuditService>.Instance);
        _hashService = new Sha256HashService();

        _testTempDir = Path.Combine(Path.GetTempPath(), "cortex_p2_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:LocalPath"] = _testTempDir,
                ["AiService:BaseUrl"] = "http://localhost:8000"
            })
            .Build();

        _storageService = new LocalFileStorageService(config, NullLogger<LocalFileStorageService>.Instance);
        _mockNeo4j = new Mock<INeo4jService>();

        // Mock HTTP client for AI service
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var aiJsonResponse = @"{
            ""status"": ""REVIEW_REQUIRED"",
            ""text_snippet"": ""Subject Rahul Mehta used vehicle MH12AB4321 near Pune Central. Contacted on +919000001001."",
            ""entities"": [
                {
                    ""id"": ""ENT-001"",
                    ""type"": ""PERSON"",
                    ""raw_value"": ""Rahul Mehta"",
                    ""normalized_value"": ""Rahul Mehta"",
                    ""confidence"": 0.96,
                    ""source"": { ""page"": 1, ""location_label"": ""Line 1"" }
                },
                {
                    ""id"": ""ENT-002"",
                    ""type"": ""VEHICLE"",
                    ""raw_value"": ""MH12AB4321"",
                    ""normalized_value"": ""MH12AB4321"",
                    ""confidence"": 0.94,
                    ""source"": { ""page"": 1, ""location_label"": ""Line 1"" }
                },
                {
                    ""id"": ""ENT-003"",
                    ""type"": ""PHONE"",
                    ""raw_value"": ""+919000001001"",
                    ""normalized_value"": ""+919000001001"",
                    ""confidence"": 0.98,
                    ""source"": { ""page"": 1, ""location_label"": ""Line 1"" }
                }
            ],
            ""relationships"": [
                {
                    ""source"": ""ENT-001"",
                    ""relationship"": ""USED"",
                    ""target"": ""ENT-002"",
                    ""confidence"": 0.94,
                    ""source_location"": ""Line 1: Rahul Mehta used vehicle MH12AB4321""
                }
            ],
            ""events"": [
                {
                    ""event_type"": ""USED"",
                    ""location"": ""Line 1"",
                    ""related_entities"": [""Rahul Mehta"", ""MH12AB4321""],
                    ""confidence"": 0.94
                }
            ],
            ""metadata"": { ""file_type"": ""TEXT"" }
        }";

        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(aiJsonResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpHandler.Object);

        _extractionService = new ExtractionService(
            _dbContext,
            _mockNeo4j.Object,
            _storageService,
            _auditService,
            httpClient,
            config,
            NullLogger<ExtractionService>.Instance);

        var ledgerService = new IntegrityLedgerService(_dbContext, _storageService, _hashService, _auditService, NullLogger<IntegrityLedgerService>.Instance);

        _evidenceService = new EvidenceService(
            _dbContext,
            _storageService,
            _hashService,
            _auditService,
            _extractionService,
            ledgerService,
            NullLogger<EvidenceService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        if (Directory.Exists(_testTempDir))
        {
            try { Directory.Delete(_testTempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task VerifyIntegrity_ShouldReturnValid_WhenBytesMatchStoredSha256()
    {
        var content = "Authentic Investigation Transcript: Confidential Statement of Operative.";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "statement.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Operative statement",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        var checkResult = await _evidenceService.VerifyIntegrityAsync(upload.Id, "OFFICER-1", "Insp. Sharma", "127.0.0.1");

        checkResult.Status.Should().Be("VALID");
        checkResult.HashesMatch.Should().BeTrue();
        checkResult.StoredHash.Should().Be(upload.Sha256Hash);
        checkResult.ComputedHash.Should().Be(upload.Sha256Hash);

        // Verify audit log
        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().Contain(l => l.Action == "EVIDENCE_INTEGRITY_CHECKED" && l.ResourceId == upload.Id);
    }

    [Fact]
    public async Task VerifyIntegrity_ShouldReturnTampered_WhenFileBytesAreAltered()
    {
        var content = "Original Evidence Stream";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "tamper_test.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Test file",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        // Manually tamper with the file on disk
        var evidenceEntity = await _dbContext.EvidenceItems.FindAsync(upload.Id);
        var diskPath = _storageService.GetAbsolutePath(evidenceEntity!.StoragePath);
        await File.WriteAllTextAsync(diskPath, "CORRUPTED AND TAMPERED EVIDENCE CONTENT BY MALICIOUS ACTOR");

        var checkResult = await _evidenceService.VerifyIntegrityAsync(upload.Id, "OFFICER-1", "Insp. Sharma", "127.0.0.1");

        checkResult.Status.Should().Be("TAMPERED");
        checkResult.HashesMatch.Should().BeFalse();
        checkResult.ComputedHash.Should().NotBe(evidenceEntity.Sha256Hash);
    }

    [Fact]
    public async Task UploadEvidence_ShouldIncrementVersion_OnReupload()
    {
        var bytes1 = Encoding.UTF8.GetBytes("Version 1 content");
        using var stream1 = new MemoryStream(bytes1);

        var v1 = await _evidenceService.UploadEvidenceAsync(
            stream1,
            "ledger.csv",
            "text/csv",
            bytes1.Length,
            "CASE-001",
            "Initial upload",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        v1.Version.Should().Be(1);

        var bytes2 = Encoding.UTF8.GetBytes("Version 2 updated audit content");
        using var stream2 = new MemoryStream(bytes2);

        var v2 = await _evidenceService.UploadEvidenceAsync(
            stream2,
            "ledger.csv",
            "text/csv",
            bytes2.Length,
            "CASE-001",
            "Updated version",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1",
            parentEvidenceId: v1.Id);

        v2.Version.Should().Be(2);
        v2.ParentEvidenceId.Should().Be(v1.Id);
        v2.Sha256Hash.Should().NotBe(v1.Sha256Hash);
    }

    [Fact]
    public async Task ProcessJobAsync_ShouldStageEntitiesAndSetReviewRequired()
    {
        var bytes = Encoding.UTF8.GetBytes("Rahul Mehta used vehicle MH12AB4321");
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "report.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Surveillance report",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        var queuedJob = await _dbContext.ExtractionJobs.FirstOrDefaultAsync(j => j.EvidenceId == upload.Id);
        queuedJob.Should().NotBeNull();
        queuedJob!.Status.Should().Be("QUEUED");

        // Execute processing
        await _extractionService.ProcessJobAsync(queuedJob.Id);

        // Verify status transition
        var updatedEvidence = await _dbContext.EvidenceItems.FindAsync(upload.Id);
        updatedEvidence!.ProcessingStatus.Should().Be("REVIEW_REQUIRED");

        var stagedEntities = await _dbContext.ExtractedEntities.Where(e => e.EvidenceId == upload.Id).ToListAsync();
        stagedEntities.Should().HaveCount(3);
        stagedEntities.Should().OnlyContain(e => e.ReviewStatus == "PENDING");

        var stagedRels = await _dbContext.ExtractedRelationships.Where(r => r.EvidenceId == upload.Id).ToListAsync();
        stagedRels.Should().HaveCount(1);
        stagedRels[0].RelationshipType.Should().Be("USED");
        stagedRels[0].ReviewStatus.Should().Be("PENDING");

        // Verify graph was NOT yet called (Rule 15: No unreviewed output in Neo4j)
        _mockNeo4j.Verify(n => n.CreateNodeAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockNeo4j.Verify(n => n.CreateRelationshipAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveEntity_ShouldPromoteToPostgresAndNeo4jWithProvenance()
    {
        var bytes = Encoding.UTF8.GetBytes("Rahul Mehta used vehicle MH12AB4321");
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "surv.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Surveillance",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        var job = await _dbContext.ExtractionJobs.FirstAsync(j => j.EvidenceId == upload.Id);
        await _extractionService.ProcessJobAsync(job.Id);

        var ent = await _dbContext.ExtractedEntities.FirstAsync(e => e.EvidenceId == upload.Id && e.EntityType == "PERSON");

        // Approve entity
        var success = await _extractionService.ApproveEntityAsync(
            upload.Id,
            ent.Id,
            null,
            null,
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        success.Should().BeTrue();

        var approvedEnt = await _dbContext.ExtractedEntities.FindAsync(ent.Id);
        approvedEnt!.ReviewStatus.Should().Be("APPROVED");
        approvedEnt.PromotedEntityId.Should().NotBeNullOrWhiteSpace();

        // Verify canonical entity in Postgres
        var canonical = await _dbContext.Entities.FindAsync(approvedEnt.PromotedEntityId);
        canonical.Should().NotBeNull();
        canonical!.CanonicalName.Should().Be("Rahul Mehta");
        canonical.VerificationStatus.Should().Be("VERIFIED");

        // Verify Neo4j promotion occurred
        _mockNeo4j.Verify(n => n.CreateNodeAsync("PERSON", It.Is<Dictionary<string, object>>(p =>
            p.ContainsKey("name") && (string)p["name"] == "Rahul Mehta" &&
            p.ContainsKey("source_evidence_id") && (string)p["source_evidence_id"] == upload.Id &&
            p.ContainsKey("verified_by") && (string)p["verified_by"] == "Insp. Sharma"
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify audit log
        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().Contain(l => l.Action == "ENTITY_APPROVED" && l.ResourceId == ent.Id);
    }

    [Fact]
    public async Task EditEntity_ShouldCorrectValuesAndLogAudit()
    {
        var bytes = Encoding.UTF8.GetBytes("Typo Name Rahul Mehata used vehicle MH12AB4321");
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "typo.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Doc",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        var job = await _dbContext.ExtractionJobs.FirstAsync(j => j.EvidenceId == upload.Id);
        await _extractionService.ProcessJobAsync(job.Id);

        var ent = await _dbContext.ExtractedEntities.FirstAsync(e => e.EvidenceId == upload.Id);

        var edited = await _extractionService.EditEntityAsync(
            upload.Id,
            ent.Id,
            "Rahul Mehta",
            "Rahul Mehta",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        edited.Should().BeTrue();

        var updatedEnt = await _dbContext.ExtractedEntities.FindAsync(ent.Id);
        updatedEnt!.RawValue.Should().Be("Rahul Mehta");
        updatedEnt.NormalizedValue.Should().Be("Rahul Mehta");

        // Verify audit log recorded correction
        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().Contain(l => l.Action == "ENTITY_VALUE_CORRECTED" && l.ResourceId == ent.Id);
    }

    [Fact]
    public async Task RejectEntity_ShouldMarkRejectedAndNotPromoteToGraph()
    {
        var bytes = Encoding.UTF8.GetBytes("False positive line");
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "false_pos.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Doc",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        var job = await _dbContext.ExtractionJobs.FirstAsync(j => j.EvidenceId == upload.Id);
        await _extractionService.ProcessJobAsync(job.Id);

        var ent = await _dbContext.ExtractedEntities.FirstAsync(e => e.EvidenceId == upload.Id);

        var rejected = await _extractionService.RejectEntityAsync(
            upload.Id,
            ent.Id,
            "Spurious match",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        rejected.Should().BeTrue();

        var rejectedEnt = await _dbContext.ExtractedEntities.FindAsync(ent.Id);
        rejectedEnt!.ReviewStatus.Should().Be("REJECTED");
        rejectedEnt.PromotedEntityId.Should().BeNull();

        // Verify Neo4j was NOT called
        _mockNeo4j.Verify(n => n.CreateNodeAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveRelationship_ShouldPromoteToGraphWithProvenance()
    {
        var bytes = Encoding.UTF8.GetBytes("Rahul Mehta used vehicle MH12AB4321");
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "rel_test.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Doc",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        var job = await _dbContext.ExtractionJobs.FirstAsync(j => j.EvidenceId == upload.Id);
        await _extractionService.ProcessJobAsync(job.Id);

        var rel = await _dbContext.ExtractedRelationships.FirstAsync(r => r.EvidenceId == upload.Id);

        var approved = await _extractionService.ApproveRelationshipAsync(
            upload.Id,
            rel.Id,
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        approved.Should().BeTrue();

        var updatedRel = await _dbContext.ExtractedRelationships.FindAsync(rel.Id);
        updatedRel!.ReviewStatus.Should().Be("APPROVED");
        updatedRel.PromotedRelationshipId.Should().NotBeNullOrWhiteSpace();

        // Verify Neo4j edge promotion with provenance
        _mockNeo4j.Verify(n => n.CreateRelationshipAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "USED",
            It.Is<Dictionary<string, object>>(p =>
                p.ContainsKey("evidence_id") && (string)p["evidence_id"] == upload.Id &&
                p.ContainsKey("confidence") && (double)p["confidence"] == 0.94 &&
                p.ContainsKey("verified_by") && (string)p["verified_by"] == "Insp. Sharma"
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        // Verify audit log
        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().Contain(l => l.Action == "RELATIONSHIP_APPROVED" && l.ResourceId == rel.Id);
    }

    [Fact]
    public async Task UploadEvidence_ShouldRejectUnsupportedFileExtension()
    {
        var bytes = Encoding.UTF8.GetBytes("Executable malware payload");
        using var stream = new MemoryStream(bytes);

        var act = async () => await _evidenceService.UploadEvidenceAsync(
            stream,
            "payload.exe",
            "application/octet-stream",
            bytes.Length,
            "CASE-001",
            "Dangerous file",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not permitted*");
    }

    [Fact]
    public async Task RetryProcessing_ShouldResetStatusAndRequeueJob()
    {
        var bytes = Encoding.UTF8.GetBytes("Retry test content");
        using var stream = new MemoryStream(bytes);

        var upload = await _evidenceService.UploadEvidenceAsync(
            stream,
            "retry.txt",
            "text/plain",
            bytes.Length,
            "CASE-001",
            "Retry doc",
            "RESTRICTED",
            "OFFICER-1",
            "Insp. Sharma",
            "127.0.0.1");

        var retryResult = await _evidenceService.RetryProcessingAsync(upload.Id, "OFFICER-1", "Insp. Sharma", "127.0.0.1");

        retryResult.Should().BeTrue();

        var evidence = await _dbContext.EvidenceItems.FindAsync(upload.Id);
        evidence!.ProcessingStatus.Should().Be("QUEUED");

        var jobs = await _dbContext.ExtractionJobs.Where(j => j.EvidenceId == upload.Id).ToListAsync();
        jobs.Should().HaveCount(2); // Initial upload job + retry job

        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().Contain(l => l.Action == "EVIDENCE_PROCESSING_RETRIED" && l.ResourceId == upload.Id);
    }
}
