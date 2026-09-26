using System.Globalization;
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
using Xunit;

namespace Tests.Unit;

public class Phase9IntegrityLedgerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Sha256HashService _hashService;
    private readonly LocalFileStorageService _storageService;
    private readonly AuditService _auditService;
    private readonly IntegrityLedgerService _ledgerService;
    private readonly string _testTempDir;

    public Phase9IntegrityLedgerTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);

        _testTempDir = Path.Combine(Path.GetTempPath(), "ledger_unit_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:LocalPath"] = _testTempDir
            })
            .Build();

        _storageService = new LocalFileStorageService(config, NullLogger<LocalFileStorageService>.Instance);
        _hashService = new Sha256HashService();
        _auditService = new AuditService(_dbContext, NullLogger<AuditService>.Instance);
        _ledgerService = new IntegrityLedgerService(
            _dbContext,
            _storageService,
            _hashService,
            _auditService,
            NullLogger<IntegrityLedgerService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        try
        {
            if (Directory.Exists(_testTempDir))
            {
                Directory.Delete(_testTempDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task Test01_GenesisBlock_CreatedDeterministically()
    {
        // Act
        await _ledgerService.EnsureGenesisBlockAsync();

        // Assert
        var genesis = await _dbContext.EvidenceLedgerBlocks.FirstOrDefaultAsync(b => b.BlockIndex == 0);
        genesis.Should().NotBeNull();
        genesis!.BlockIndex.Should().Be(0);
        genesis.Action.Should().Be(IntegrityLedgerService.GenesisAction);
        genesis.EvidenceHash.Should().Be(IntegrityLedgerService.GenesisEvidenceHash.ToLowerInvariant());
        genesis.PreviousBlockHash.Should().Be(IntegrityLedgerService.GenesisPreviousBlockHash.ToLowerInvariant());
        genesis.ActorUserId.Should().Be("SYSTEM");

        // Compute expected hash independently
        var expectedHash = IntegrityLedgerService.ComputeCanonicalBlockHash(
            0,
            null,
            0,
            IntegrityLedgerService.GenesisEvidenceHash,
            IntegrityLedgerService.GenesisPreviousBlockHash,
            IntegrityLedgerService.GenesisAction,
            IntegrityLedgerService.GenesisActorId,
            IntegrityLedgerService.GenesisTimestampUtc,
            IntegrityLedgerService.GenesisMetadata);

        genesis.BlockHash.Should().Be(expectedHash);

        // Call EnsureGenesisBlockAsync again - should be idempotent
        await _ledgerService.EnsureGenesisBlockAsync();
        var count = await _dbContext.EvidenceLedgerBlocks.CountAsync(b => b.BlockIndex == 0);
        count.Should().Be(1);
    }

    [Fact]
    public void Test02_CanonicalHashCalculation_DeterministicAcrossOrder()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var canonicalString = IntegrityLedgerService.BuildCanonicalString(
            1,
            "ev-101",
            1,
            "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            "0000001234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            "UPLOAD",
            "usr-sharma",
            testTime,
            "{\"note\":\"synthetic test\"}");

        // Act
        var hash1 = IntegrityLedgerService.ComputeCanonicalBlockHash(
            1,
            "ev-101",
            1,
            "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            "0000001234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            "UPLOAD",
            "usr-sharma",
            testTime,
            "{\"note\":\"synthetic test\"}");

        var hash2 = IntegrityLedgerService.ComputeCanonicalBlockHash(
            1,
            "ev-101",
            1,
            "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            "0000001234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            "UPLOAD",
            "usr-sharma",
            testTime,
            "{\"note\":\"synthetic test\"}");

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
        hash1.Should().Be(hash1.ToLowerInvariant());
        canonicalString.Should().Contain("1|ev-101|1|abcdef");
    }

    [Fact]
    public async Task Test03_BlockAppend_SequentialChaining()
    {
        // Arrange & Act
        var block1 = await _ledgerService.AppendBlockAsync(
            "ev-001",
            1,
            "1111111111111111111111111111111111111111111111111111111111111111",
            "UPLOAD",
            "usr-001",
            "Officer 1");

        var block2 = await _ledgerService.AppendBlockAsync(
            "ev-002",
            1,
            "2222222222222222222222222222222222222222222222222222222222222222",
            "UPLOAD",
            "usr-002",
            "Officer 2");

        var block3 = await _ledgerService.AppendBlockAsync(
            "ev-001",
            2,
            "3333333333333333333333333333333333333333333333333333333333333333",
            "VERSION_CREATED",
            "usr-001",
            "Officer 1");

        // Assert
        block1.BlockIndex.Should().Be(1);
        block2.BlockIndex.Should().Be(2);
        block3.BlockIndex.Should().Be(3);

        // Genesis should have been automatically created at index 0
        var genesis = await _ledgerService.GetBlockByIndexAsync(0);
        genesis.Should().NotBeNull();

        block1.PreviousBlockHash.Should().Be(genesis!.BlockHash);
        block2.PreviousBlockHash.Should().Be(block1.BlockHash);
        block3.PreviousBlockHash.Should().Be(block2.BlockHash);
    }

    [Fact]
    public async Task Test04_ChainValidation_SucceedsOnValidChain()
    {
        // Arrange
        await _ledgerService.AppendBlockAsync(
            "ev-valid-1",
            1,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "UPLOAD",
            "usr-001",
            "Officer A");

        await _ledgerService.AppendBlockAsync(
            "ev-valid-2",
            1,
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            "UPLOAD",
            "usr-002",
            "Officer B");

        // Act
        var validation = await _ledgerService.VerifyChainAsync();

        // Assert
        validation.IsValid.Should().BeTrue();
        validation.TotalBlocks.Should().Be(3); // Genesis (0) + Block 1 + Block 2
        validation.CheckedBlocks.Should().Be(3);
        validation.FirstInvalidBlockIndex.Should().BeNull();
        validation.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Test05_TamperDetection_ModifiedStoredBlockHash_FailsChainValidation()
    {
        // Arrange
        await _ledgerService.AppendBlockAsync(
            "ev-block-1",
            1,
            "1234567890123456789012345678901234567890123456789012345678901234",
            "UPLOAD",
            "usr-001",
            "Officer 1");

        await _ledgerService.AppendBlockAsync(
            "ev-block-2",
            1,
            "abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd",
            "UPLOAD",
            "usr-001",
            "Officer 1");

        // Direct database tamper: corrupt the stored BlockHash of Block #1
        var block1 = await _dbContext.EvidenceLedgerBlocks.FirstAsync(b => b.BlockIndex == 1);
        block1.BlockHash = "badhash000000000000000000000000000000000000000000000000000000000";
        await _dbContext.SaveChangesAsync();

        // Act
        var validation = await _ledgerService.VerifyChainAsync();

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.FirstInvalidBlockIndex.Should().Be(1);
        validation.FailureReason.Should().Contain("BlockHash mismatch at Block #1");
    }

    [Fact]
    public async Task Test06_TamperDetection_ModifiedPreviousHash_FailsChainValidation()
    {
        // Arrange
        await _ledgerService.AppendBlockAsync(
            "ev-a",
            1,
            "1111111111111111111111111111111111111111111111111111111111111111",
            "UPLOAD",
            "usr-001",
            "Officer 1");

        await _ledgerService.AppendBlockAsync(
            "ev-b",
            1,
            "2222222222222222222222222222222222222222222222222222222222222222",
            "UPLOAD",
            "usr-001",
            "Officer 1");

        // Direct database tamper: corrupt PreviousBlockHash of Block #2
        var block2 = await _dbContext.EvidenceLedgerBlocks.FirstAsync(b => b.BlockIndex == 2);
        block2.PreviousBlockHash = "corruptedprevioushash00000000000000000000000000000000000000000000";
        await _dbContext.SaveChangesAsync();

        // Act
        var validation = await _ledgerService.VerifyChainAsync();

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.FirstInvalidBlockIndex.Should().Be(2);
        validation.FailureReason.Should().Contain("PreviousBlockHash mismatch at Block #2");
    }

    [Fact]
    public async Task Test07_EvidenceVerification_AuthenticFile_ReturnsVerified()
    {
        // Arrange: Create synthetic authentic evidence file on disk
        var fileContent = "MH-POLICE-REPORT-EVIDENCE-AUTHENTIC-2026";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        var sha256 = _hashService.ComputeSha256Hex(fileBytes);

        using var stream = new MemoryStream(fileBytes);
        var storagePath = await _storageService.SaveFileAsync(stream, "authentic_report.txt");

        var evidence = new Evidence
        {
            Id = "ev-auth-001",
            FileName = "authentic_report.txt",
            MimeType = "text/plain",
            FileSize = fileBytes.Length,
            StoragePath = storagePath,
            Sha256Hash = sha256,
            UploadedById = "usr-sharma",
            UploadedByName = "DCP Sharma",
            Version = 1
        };
        _dbContext.EvidenceItems.Add(evidence);
        await _dbContext.SaveChangesAsync();

        // Register in blockchain ledger
        await _ledgerService.AppendBlockAsync(
            evidence.Id,
            evidence.Version,
            evidence.Sha256Hash,
            "UPLOAD",
            evidence.UploadedById,
            evidence.UploadedByName);

        // Act: Run live byte-level verification
        var result = await _ledgerService.VerifyEvidenceAsync(evidence.Id);

        // Assert
        result.Status.Should().Be("VERIFIED");
        result.ChainStatus.Should().Be("VALID");
        result.ActualFileSha256.Should().Be(sha256);
        result.RegisteredSha256.Should().Be(sha256);
        result.LedgerSha256.Should().Be(sha256);
        result.BlockIndex.Should().BeGreaterThan(0);
        result.Explanation.Should().Contain("Cryptographic integrity confirmed");
    }

    [Fact]
    public async Task Test08_TamperDetection_ModifiedFileBytes_DetectedAndLedgerIntact()
    {
        // Step 1: Create synthetic evidence bytes and compute actual SHA-256
        var originalContent = "Original authentic police surveillance log: suspect vehicle MH12AB4321 spotted.";
        var originalBytes = Encoding.UTF8.GetBytes(originalContent);
        var originalSha256 = _hashService.ComputeSha256Hex(originalBytes);

        // Step 2 & 3: Save file and register evidence
        using var originalStream = new MemoryStream(originalBytes);
        var storagePath = await _storageService.SaveFileAsync(originalStream, "surveillance_log_pune.txt");

        var evidence = new Evidence
        {
            Id = "ev-tamper-001",
            FileName = "surveillance_log_pune.txt",
            MimeType = "text/plain",
            FileSize = originalBytes.Length,
            StoragePath = storagePath,
            Sha256Hash = originalSha256,
            UploadedById = "usr-sharma",
            UploadedByName = "DCP Sharma",
            Version = 1
        };
        _dbContext.EvidenceItems.Add(evidence);
        await _dbContext.SaveChangesAsync();

        // Step 4: Append ledger block
        var ledgerBlock = await _ledgerService.AppendBlockAsync(
            evidence.Id,
            evidence.Version,
            evidence.Sha256Hash,
            "UPLOAD",
            evidence.UploadedById,
            evidence.UploadedByName);

        // Step 5 & 6: Verify evidence -> Assert VERIFIED
        var initialVerification = await _ledgerService.VerifyEvidenceAsync(evidence.Id);
        initialVerification.Status.Should().Be("VERIFIED");

        // Step 7: Modify underlying physical evidence bytes on disk
        var tamperedContent = "MODIFIED: suspect vehicle changed to MH01XX0000 by unauthorized actor.";
        var tamperedBytes = Encoding.UTF8.GetBytes(tamperedContent);
        var fullPhysicalPath = _storageService.GetAbsolutePath(storagePath);
        await File.WriteAllBytesAsync(fullPhysicalPath, tamperedBytes);

        // Step 8 & 9: Run verification again -> Assert EVIDENCE_MODIFIED
        var tamperedVerification = await _ledgerService.VerifyEvidenceAsync(evidence.Id);
        tamperedVerification.Status.Should().Be("EVIDENCE_MODIFIED");
        tamperedVerification.ActualFileSha256.Should().NotBe(originalSha256);
        tamperedVerification.RegisteredSha256.Should().Be(originalSha256);
        tamperedVerification.LedgerSha256.Should().Be(originalSha256);
        tamperedVerification.Explanation.Should().Contain("Evidence content does not match registered hash");

        // Step 10: Assert the ledger itself remains strictly unchanged
        var currentBlock = await _ledgerService.GetBlockByIndexAsync(ledgerBlock.BlockIndex);
        currentBlock.Should().NotBeNull();
        currentBlock!.EvidenceHash.Should().Be(originalSha256);
        currentBlock.BlockHash.Should().Be(ledgerBlock.BlockHash);

        // Step 11 & 12: Restore original bytes
        await File.WriteAllBytesAsync(fullPhysicalPath, originalBytes);

        // Step 13: Verify again -> Assert VERIFIED
        var restoredVerification = await _ledgerService.VerifyEvidenceAsync(evidence.Id);
        restoredVerification.Status.Should().Be("VERIFIED");
        restoredVerification.ActualFileSha256.Should().Be(originalSha256);
    }

    [Fact]
    public async Task Test09_EvidenceVerification_MissingFile_ReturnsMissingFile()
    {
        // Arrange: Register evidence whose physical file doesn't exist
        var evidence = new Evidence
        {
            Id = "ev-missing-001",
            FileName = "missing_file.pdf",
            StoragePath = "nonexistent/missing_file.pdf",
            Sha256Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Version = 1
        };
        _dbContext.EvidenceItems.Add(evidence);
        await _dbContext.SaveChangesAsync();

        await _ledgerService.AppendBlockAsync(
            evidence.Id,
            1,
            evidence.Sha256Hash,
            "UPLOAD",
            "usr-test",
            "Officer Test");

        // Act
        var result = await _ledgerService.VerifyEvidenceAsync(evidence.Id);

        // Assert
        result.Status.Should().Be("MISSING_FILE");
        result.Explanation.Should().Contain("Underlying physical file is missing");
    }

    [Fact]
    public async Task Test10_EvidenceVerification_MissingLedgerBlock_ReturnsMissingLedgerRecord()
    {
        // Arrange: Create evidence record in DB without appending a ledger block
        var fileBytes = Encoding.UTF8.GetBytes("unledgered content");
        using var stream = new MemoryStream(fileBytes);
        var storagePath = await _storageService.SaveFileAsync(stream, "unregistered.txt");

        var evidence = new Evidence
        {
            Id = "ev-unledgered-001",
            FileName = "unregistered.txt",
            StoragePath = storagePath,
            Sha256Hash = _hashService.ComputeSha256Hex(fileBytes),
            Version = 1
        };
        _dbContext.EvidenceItems.Add(evidence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _ledgerService.VerifyEvidenceAsync(evidence.Id);

        // Assert
        result.Status.Should().Be("MISSING_LEDGER_RECORD");
        result.Explanation.Should().Contain("No cryptographic ledger block found");
    }

    [Fact]
    public async Task Test11_VersionAwareIntegrity_MultipleVersionsHaveDistinctBlocks()
    {
        // Arrange
        var v1Bytes = Encoding.UTF8.GetBytes("Version 1 content");
        var v1Hash = _hashService.ComputeSha256Hex(v1Bytes);

        var v2Bytes = Encoding.UTF8.GetBytes("Version 2 updated content");
        var v2Hash = _hashService.ComputeSha256Hex(v2Bytes);

        // Append v1
        var block1 = await _ledgerService.AppendBlockAsync(
            "ev-multi-001",
            1,
            v1Hash,
            "UPLOAD",
            "usr-001",
            "Officer 1");

        // Append v2
        var block2 = await _ledgerService.AppendBlockAsync(
            "ev-multi-001",
            2,
            v2Hash,
            "VERSION_CREATED",
            "usr-002",
            "Officer 2");

        // Act: Get history for evidence
        var history = await _ledgerService.GetEvidenceHistoryAsync("ev-multi-001");

        // Assert
        history.Should().HaveCount(2);
        history[0].EvidenceVersion.Should().Be(1);
        history[0].EvidenceHash.Should().Be(v1Hash);
        history[0].Action.Should().Be("UPLOAD");

        history[1].EvidenceVersion.Should().Be(2);
        history[1].EvidenceHash.Should().Be(v2Hash);
        history[1].Action.Should().Be("VERSION_CREATED");

        // V1 hash is never overwritten
        block1.EvidenceHash.Should().Be(v1Hash);
        block2.EvidenceHash.Should().Be(v2Hash);
    }

    [Fact]
    public async Task Test12_Reconciliation_IdentifiesAndRegistersMissingEvidence()
    {
        // Arrange: Create evidence with actual file on disk but no ledger block
        var bytes = Encoding.UTF8.GetBytes("Reconciliation target file bytes");
        var hash = _hashService.ComputeSha256Hex(bytes);

        using var stream = new MemoryStream(bytes);
        var storagePath = await _storageService.SaveFileAsync(stream, "reconcile_target.txt");

        var evidence = new Evidence
        {
            Id = "ev-reconcile-001",
            CaseId = "case-2026-001",
            FileName = "reconcile_target.txt",
            StoragePath = storagePath,
            Sha256Hash = hash,
            UploadedById = "usr-001",
            UploadedByName = "Officer A",
            Version = 1
        };
        _dbContext.EvidenceItems.Add(evidence);
        await _dbContext.SaveChangesAsync();

        // Act 1: Get reconciliation candidates
        var candidates = await _ledgerService.GetReconciliationCandidatesAsync();
        candidates.Should().Contain(c => c.EvidenceId == evidence.Id);

        // Act 2: Perform reconciliation
        var reconciledBlock = await _ledgerService.ReconcileEvidenceAsync(
            evidence.Id,
            "usr-admin",
            "Admin User");

        // Assert
        reconciledBlock.Action.Should().Be("RECONCILIATION");
        reconciledBlock.EvidenceHash.Should().Be(hash);
        reconciledBlock.EvidenceItemId.Should().Be(evidence.Id);

        // Candidates should no longer contain this evidence
        var postCandidates = await _ledgerService.GetReconciliationCandidatesAsync();
        postCandidates.Should().NotContain(c => c.EvidenceId == evidence.Id);
    }

    [Fact]
    public async Task Test13_AppendConcurrency_20ConcurrentRequests_ProducesValidLinearChain()
    {
        // Arrange: 20 concurrent threads appending blocks simultaneously
        var tasks = new List<Task<EvidenceLedgerBlockDto>>();
        for (int i = 0; i < 20; i++)
        {
            var idx = i;
            var hash = _hashService.ComputeSha256Hex(Encoding.UTF8.GetBytes($"thread-{idx}"));
            tasks.Add(Task.Run(async () =>
            {
                return await _ledgerService.AppendBlockAsync(
                    $"ev-concurrent-{idx}",
                    1,
                    hash,
                    "INTEGRITY_CHECK",
                    $"usr-{idx}",
                    $"Officer {idx}");
            }));
        }

        // Act: Await all 20 concurrent appends
        var results = await Task.WhenAll(tasks);

        // Assert: 20 blocks created
        results.Should().HaveCount(20);

        // Total blocks in DB: Genesis (0) + 20 blocks = 21 blocks
        var allBlocks = await _dbContext.EvidenceLedgerBlocks.OrderBy(b => b.BlockIndex).ToListAsync();
        allBlocks.Should().HaveCount(21);

        // All indices must be strictly sequential 0..20 with no duplicates
        var indices = allBlocks.Select(b => b.BlockIndex).ToList();
        indices.Should().BeEquivalentTo(Enumerable.Range(0, 21).Select(x => (long)x));

        // Every block must correctly link to previous block hash
        for (int i = 1; i < allBlocks.Count; i++)
        {
            allBlocks[i].PreviousBlockHash.Should().Be(allBlocks[i - 1].BlockHash);
        }

        // Full chain validation must pass
        var chainResult = await _ledgerService.VerifyChainAsync();
        chainResult.IsValid.Should().BeTrue();
        chainResult.CheckedBlocks.Should().Be(21);
    }

    [Fact]
    public async Task Test14_AuditLogging_LogsVerificationAndTamperAlerts()
    {
        // Arrange: Create authentic evidence
        var bytes = Encoding.UTF8.GetBytes("Audit test file");
        var hash = _hashService.ComputeSha256Hex(bytes);

        using var stream = new MemoryStream(bytes);
        var storagePath = await _storageService.SaveFileAsync(stream, "audit_test.txt");

        var evidence = new Evidence
        {
            Id = "ev-audit-001",
            FileName = "audit_test.txt",
            StoragePath = storagePath,
            Sha256Hash = hash,
            Version = 1
        };
        _dbContext.EvidenceItems.Add(evidence);
        await _dbContext.SaveChangesAsync();

        await _ledgerService.AppendBlockAsync(evidence.Id, 1, hash, "UPLOAD", "usr-test", "Officer Test");

        // Act 1: Successful verification
        await _ledgerService.VerifyEvidenceAsync(evidence.Id, "usr-test", "Officer Test");

        // Act 2: Tamper file and run verification
        var fullPath = _storageService.GetAbsolutePath(storagePath);
        await File.WriteAllBytesAsync(fullPath, Encoding.UTF8.GetBytes("TAMPERED BYTES"));
        await _ledgerService.VerifyEvidenceAsync(evidence.Id, "usr-test", "Officer Test");

        // Assert: Audit log entries created
        var auditLogs = await _dbContext.AuditLogs.ToListAsync();
        auditLogs.Should().Contain(l => l.Action == "LEDGER_BLOCK_APPENDED");
        auditLogs.Should().Contain(l => l.Action == "INTEGRITY_VERIFICATION_SUCCESS");
        auditLogs.Should().Contain(l => l.Action == "INTEGRITY_VERIFICATION_FAILED");
    }
}
