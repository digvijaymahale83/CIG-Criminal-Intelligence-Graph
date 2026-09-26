using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class IntegrityLedgerService : IIntegrityLedgerService
{
    private readonly IAppDbContext _dbContext;
    private readonly IFileStorageService _storageService;
    private readonly IHashService _hashService;
    private readonly IAuditService _auditService;
    private readonly ILogger<IntegrityLedgerService> _logger;

    // Concurrency semaphore to serialize block creation across concurrent threads
    private static readonly SemaphoreSlim _ledgerLock = new(1, 1);

    public const string GenesisEvidenceHash = "GENESIS";
    public const string GenesisPreviousBlockHash = "GENESIS";
    public const string GenesisAction = "GENESIS";
    public const string GenesisActorId = "SYSTEM";
    public const string GenesisActorName = "System Genesis Authority";
    public static readonly DateTime GenesisTimestampUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public const string GenesisMetadata = "{\"genesis\":\"Maharashtra Police Evidence Integrity Ledger Genesis Block\",\"network\":\"SIH-2026-RESEARCH\"}";

    public IntegrityLedgerService(
        IAppDbContext dbContext,
        IFileStorageService storageService,
        IHashService hashService,
        IAuditService auditService,
        ILogger<IntegrityLedgerService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _hashService = hashService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Deterministic canonical block string serialization:
    /// BLOCK_INDEX|EVIDENCE_ID|VERSION_ID|EVIDENCE_HASH|PREVIOUS_HASH|ACTION|ACTOR_USER_ID|TIMESTAMP_ISO8601|METADATA
    /// </summary>
    public static string BuildCanonicalString(
        long blockIndex,
        string? evidenceItemId,
        int evidenceVersion,
        string evidenceHash,
        string previousBlockHash,
        string action,
        string actorUserId,
        DateTime timestampUtc,
        string? metadataJson)
    {
        var timestampIso = timestampUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
        return $"{blockIndex}|{evidenceItemId ?? ""}|{evidenceVersion}|{evidenceHash.ToLowerInvariant()}|{previousBlockHash.ToLowerInvariant()}|{action}|{actorUserId}|{timestampIso}|{metadataJson ?? ""}";
    }

    /// <summary>
    /// Compute SHA-256 of the canonical block string representation.
    /// </summary>
    public static string ComputeCanonicalBlockHash(
        long blockIndex,
        string? evidenceItemId,
        int evidenceVersion,
        string evidenceHash,
        string previousBlockHash,
        string action,
        string actorUserId,
        DateTime timestampUtc,
        string? metadataJson)
    {
        var canonical = BuildCanonicalString(
            blockIndex,
            evidenceItemId,
            evidenceVersion,
            evidenceHash,
            previousBlockHash,
            action,
            actorUserId,
            timestampUtc,
            metadataJson);

        var bytes = Encoding.UTF8.GetBytes(canonical);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Calculates the canonical BlockHash for an existing EvidenceLedgerBlock instance.
    /// </summary>
    public static string ComputeBlockHash(EvidenceLedgerBlock block)
    {
        return ComputeCanonicalBlockHash(
            block.BlockIndex,
            block.EvidenceItemId,
            block.EvidenceVersion,
            block.EvidenceHash,
            block.PreviousBlockHash,
            block.Action,
            block.ActorUserId,
            block.TimestampUtc,
            block.MetadataJson);
    }

    /// <summary>
    /// Ensures that the deterministic Genesis block (Block #0) exists in the database.
    /// </summary>
    public async Task EnsureGenesisBlockAsync(CancellationToken cancellationToken = default)
    {
        await _ledgerLock.WaitAsync(cancellationToken);
        try
        {
            var exists = await _dbContext.EvidenceLedgerBlocks
                .AnyAsync(b => b.BlockIndex == 0, cancellationToken);

            if (!exists)
            {
                var genesisHash = ComputeCanonicalBlockHash(
                    0,
                    null,
                    0,
                    GenesisEvidenceHash,
                    GenesisPreviousBlockHash,
                    GenesisAction,
                    GenesisActorId,
                    GenesisTimestampUtc,
                    GenesisMetadata);

                var genesisBlock = new EvidenceLedgerBlock
                {
                    Id = "block-genesis-000000",
                    BlockIndex = 0,
                    EvidenceItemId = null,
                    EvidenceVersion = 0,
                    EvidenceHash = GenesisEvidenceHash.ToLowerInvariant(),
                    PreviousBlockHash = GenesisPreviousBlockHash.ToLowerInvariant(),
                    BlockHash = genesisHash,
                    Action = GenesisAction,
                    ActorUserId = GenesisActorId,
                    ActorName = GenesisActorName,
                    TimestampUtc = GenesisTimestampUtc,
                    MetadataJson = GenesisMetadata,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _dbContext.EvidenceLedgerBlocks.Add(genesisBlock);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deterministic Genesis Block #0 created with hash: {GenesisHash}", genesisHash);
            }
        }
        finally
        {
            _ledgerLock.Release();
        }
    }

    public async Task<EvidenceLedgerBlockDto> AppendBlockAsync(
        string? evidenceId,
        int evidenceVersion,
        string evidenceHash,
        string action,
        string actorUserId,
        string actorName,
        string? metadataJson = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(evidenceHash))
            throw new ArgumentException("EvidenceHash is required to append a block.", nameof(evidenceHash));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required to append a block.", nameof(action));

        await _ledgerLock.WaitAsync(cancellationToken);
        try
        {
            // Ensure Genesis block exists
            var hasGenesis = await _dbContext.EvidenceLedgerBlocks
                .AnyAsync(b => b.BlockIndex == 0, cancellationToken);

            if (!hasGenesis)
            {
                var genesisHash = ComputeCanonicalBlockHash(
                    0,
                    null,
                    0,
                    GenesisEvidenceHash,
                    GenesisPreviousBlockHash,
                    GenesisAction,
                    GenesisActorId,
                    GenesisTimestampUtc,
                    GenesisMetadata);

                var genesisBlock = new EvidenceLedgerBlock
                {
                    Id = "block-genesis-000000",
                    BlockIndex = 0,
                    EvidenceItemId = null,
                    EvidenceVersion = 0,
                    EvidenceHash = GenesisEvidenceHash.ToLowerInvariant(),
                    PreviousBlockHash = GenesisPreviousBlockHash.ToLowerInvariant(),
                    BlockHash = genesisHash,
                    Action = GenesisAction,
                    ActorUserId = GenesisActorId,
                    ActorName = GenesisActorName,
                    TimestampUtc = GenesisTimestampUtc,
                    MetadataJson = GenesisMetadata,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _dbContext.EvidenceLedgerBlocks.Add(genesisBlock);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Get latest block to compute previous hash and next index
            var latestBlock = await _dbContext.EvidenceLedgerBlocks
                .OrderByDescending(b => b.BlockIndex)
                .FirstAsync(cancellationToken);

            var nextIndex = latestBlock.BlockIndex + 1;
            var prevHash = latestBlock.BlockHash;
            var timestampUtc = DateTime.UtcNow;

            var newBlockHash = ComputeCanonicalBlockHash(
                nextIndex,
                evidenceId,
                evidenceVersion,
                evidenceHash,
                prevHash,
                action,
                actorUserId,
                timestampUtc,
                metadataJson);

            var newBlock = new EvidenceLedgerBlock
            {
                Id = $"block-{nextIndex:D6}-{Guid.NewGuid().ToString("N")[..8]}",
                BlockIndex = nextIndex,
                EvidenceItemId = evidenceId,
                EvidenceVersion = evidenceVersion,
                EvidenceHash = evidenceHash.ToLowerInvariant(),
                PreviousBlockHash = prevHash.ToLowerInvariant(),
                BlockHash = newBlockHash,
                Action = action.ToUpperInvariant(),
                ActorUserId = actorUserId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "Investigator" : actorName,
                TimestampUtc = timestampUtc,
                MetadataJson = metadataJson,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.EvidenceLedgerBlocks.Add(newBlock);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Appended EvidenceLedgerBlock #{Index} for Evidence {EvidenceId} (Action: {Action}, Hash: {BlockHash})",
                nextIndex, evidenceId, action, newBlockHash);

            // Audit log the append
            await _auditService.LogAsync(
                actorUserId,
                actorName,
                "LEDGER_BLOCK_APPENDED",
                "EvidenceLedgerBlock",
                newBlock.Id,
                $"Appended Block #{nextIndex} for evidence {evidenceId} (action: {action})",
                $"{{\"blockIndex\":{nextIndex},\"evidenceId\":\"{evidenceId}\",\"evidenceHash\":\"{evidenceHash}\",\"blockHash\":\"{newBlockHash}\"}}",
                ipAddress,
                cancellationToken);

            return MapToDto(newBlock);
        }
        finally
        {
            _ledgerLock.Release();
        }
    }

    public async Task<EvidenceIntegrityStatusDto> VerifyEvidenceAsync(
        string evidenceId,
        string? actorUserId = null,
        string? actorName = null,
        CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.EvidenceItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);

        if (evidence == null)
        {
            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                Status = "MISSING_FILE",
                ChainStatus = "UNKNOWN",
                Explanation = $"Evidence record '{evidenceId}' does not exist in the database."
            };
        }

        // 1. Locate the latest ledger block for this evidence item
        var ledgerBlock = await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .Where(b => b.EvidenceItemId == evidenceId)
            .OrderByDescending(b => b.BlockIndex)
            .FirstOrDefaultAsync(cancellationToken);

        if (ledgerBlock == null)
        {
            await _auditService.LogAsync(
                actorUserId,
                actorName ?? "Investigator",
                "INTEGRITY_VERIFICATION_FAILED",
                "Evidence",
                evidenceId,
                $"Integrity verification failed for {evidence.FileName}: Missing ledger record",
                $"{{\"evidenceId\":\"{evidenceId}\",\"status\":\"MISSING_LEDGER_RECORD\"}}",
                null,
                cancellationToken);

            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                FileName = evidence.FileName,
                Version = evidence.Version,
                RegisteredSha256 = evidence.Sha256Hash,
                LedgerSha256 = string.Empty,
                ActualFileSha256 = string.Empty,
                Status = "MISSING_LEDGER_RECORD",
                ChainStatus = "UNKNOWN",
                Explanation = "No cryptographic ledger block found for this evidence item. Unregistered custody state."
            };
        }

        // 2. Open physical file stream from storage
        using var stream = await _storageService.GetFileAsync(evidence.StoragePath, cancellationToken);
        if (stream == null)
        {
            await _auditService.LogAsync(
                actorUserId,
                actorName ?? "Investigator",
                "INTEGRITY_VERIFICATION_FAILED",
                "Evidence",
                evidenceId,
                $"Integrity verification failed for {evidence.FileName}: Physical file missing from storage ({evidence.StoragePath})",
                $"{{\"evidenceId\":\"{evidenceId}\",\"storagePath\":\"{evidence.StoragePath}\",\"status\":\"MISSING_FILE\"}}",
                null,
                cancellationToken);

            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                FileName = evidence.FileName,
                Version = evidence.Version,
                RegisteredSha256 = evidence.Sha256Hash,
                LedgerSha256 = ledgerBlock.EvidenceHash,
                BlockIndex = ledgerBlock.BlockIndex,
                Action = ledgerBlock.Action,
                ActorName = ledgerBlock.ActorName,
                Status = "MISSING_FILE",
                ChainStatus = "UNKNOWN",
                Explanation = $"Underlying physical file is missing from storage path: {evidence.StoragePath}"
            };
        }

        // 3. Compute live SHA-256 directly from physical file bytes
        var actualSha256 = _hashService.ComputeSha256Hex(stream);

        // 4. Validate full chain health
        var chainResult = await VerifyChainAsync(cancellationToken);
        var chainStatus = chainResult.IsValid ? "VALID" : "BROKEN";

        // 5. Compare physical file bytes vs stored evidence metadata
        if (!string.Equals(actualSha256, evidence.Sha256Hash, StringComparison.OrdinalIgnoreCase))
        {
            await _auditService.LogAsync(
                actorUserId,
                actorName ?? "Investigator",
                "INTEGRITY_VERIFICATION_FAILED",
                "Evidence",
                evidenceId,
                $"Tamper detected for {evidence.FileName}: File hash {actualSha256} does not match registered hash {evidence.Sha256Hash}",
                $"{{\"actualSha256\":\"{actualSha256}\",\"registeredSha256\":\"{evidence.Sha256Hash}\",\"ledgerSha256\":\"{ledgerBlock.EvidenceHash}\"}}",
                null,
                cancellationToken);

            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                FileName = evidence.FileName,
                Version = evidence.Version,
                ActualFileSha256 = actualSha256,
                RegisteredSha256 = evidence.Sha256Hash,
                LedgerSha256 = ledgerBlock.EvidenceHash,
                BlockIndex = ledgerBlock.BlockIndex,
                Action = ledgerBlock.Action,
                ActorName = ledgerBlock.ActorName,
                Status = "EVIDENCE_MODIFIED",
                ChainStatus = chainStatus,
                Explanation = "Evidence content does not match registered hash. Physical file modifications detected."
            };
        }

        // 6. Compare physical file bytes vs registered ledger block
        if (!string.Equals(actualSha256, ledgerBlock.EvidenceHash, StringComparison.OrdinalIgnoreCase))
        {
            await _auditService.LogAsync(
                actorUserId,
                actorName ?? "Investigator",
                "INTEGRITY_VERIFICATION_FAILED",
                "Evidence",
                evidenceId,
                $"Ledger mismatch for {evidence.FileName}: File hash {actualSha256} does not match ledger block hash {ledgerBlock.EvidenceHash}",
                $"{{\"actualSha256\":\"{actualSha256}\",\"ledgerSha256\":\"{ledgerBlock.EvidenceHash}\"}}",
                null,
                cancellationToken);

            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                FileName = evidence.FileName,
                Version = evidence.Version,
                ActualFileSha256 = actualSha256,
                RegisteredSha256 = evidence.Sha256Hash,
                LedgerSha256 = ledgerBlock.EvidenceHash,
                BlockIndex = ledgerBlock.BlockIndex,
                Action = ledgerBlock.Action,
                ActorName = ledgerBlock.ActorName,
                Status = "LEDGER_MISMATCH",
                ChainStatus = chainStatus,
                Explanation = "Evidence file bytes do not match the cryptographic ledger block hash."
            };
        }

        // 7. Check if chain itself is corrupted
        if (!chainResult.IsValid)
        {
            await _auditService.LogAsync(
                actorUserId,
                actorName ?? "Investigator",
                "INTEGRITY_VERIFICATION_FAILED",
                "Evidence",
                evidenceId,
                $"Chain invalid during verification for {evidence.FileName}: {chainResult.FailureReason}",
                $"{{\"firstInvalidBlockIndex\":{chainResult.FirstInvalidBlockIndex},\"reason\":\"{chainResult.FailureReason}\"}}",
                null,
                cancellationToken);

            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                FileName = evidence.FileName,
                Version = evidence.Version,
                ActualFileSha256 = actualSha256,
                RegisteredSha256 = evidence.Sha256Hash,
                LedgerSha256 = ledgerBlock.EvidenceHash,
                BlockIndex = ledgerBlock.BlockIndex,
                Action = ledgerBlock.Action,
                ActorName = ledgerBlock.ActorName,
                Status = "CHAIN_INVALID",
                ChainStatus = "BROKEN",
                Explanation = $"Underlying ledger blockchain has integrity failure: {chainResult.FailureReason}"
            };
        }

        // 8. All checks passed: genuine cryptographic verification
        await _auditService.LogAsync(
            actorUserId,
            actorName ?? "Investigator",
            "INTEGRITY_VERIFICATION_SUCCESS",
            "Evidence",
            evidenceId,
            $"Verified cryptographic integrity for {evidence.FileName} (Block #{ledgerBlock.BlockIndex})",
            $"{{\"sha256\":\"{actualSha256}\",\"blockIndex\":{ledgerBlock.BlockIndex}}}",
            null,
            cancellationToken);

        return new EvidenceIntegrityStatusDto
        {
            EvidenceId = evidenceId,
            FileName = evidence.FileName,
            Version = evidence.Version,
            ActualFileSha256 = actualSha256,
            RegisteredSha256 = evidence.Sha256Hash,
            LedgerSha256 = ledgerBlock.EvidenceHash,
            BlockIndex = ledgerBlock.BlockIndex,
            Action = ledgerBlock.Action,
            ActorName = ledgerBlock.ActorName,
            Status = "VERIFIED",
            ChainStatus = "VALID",
            Explanation = "Evidence file bytes match registered SHA-256 and immutable blockchain ledger record. Cryptographic integrity confirmed."
        };
    }

    public async Task<EvidenceIntegrityStatusDto> GetEvidenceIntegrityStatusAsync(
        string evidenceId,
        CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.EvidenceItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);

        if (evidence == null)
        {
            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                Status = "MISSING_FILE",
                ChainStatus = "UNKNOWN",
                Explanation = "Evidence record not found."
            };
        }

        var ledgerBlock = await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .Where(b => b.EvidenceItemId == evidenceId)
            .OrderByDescending(b => b.BlockIndex)
            .FirstOrDefaultAsync(cancellationToken);

        if (ledgerBlock == null)
        {
            return new EvidenceIntegrityStatusDto
            {
                EvidenceId = evidenceId,
                FileName = evidence.FileName,
                Version = evidence.Version,
                RegisteredSha256 = evidence.Sha256Hash,
                LedgerSha256 = string.Empty,
                ActualFileSha256 = evidence.Sha256Hash,
                Status = "MISSING_LEDGER_RECORD",
                ChainStatus = "UNKNOWN",
                Explanation = "Evidence item is pending ledger registration."
            };
        }

        var isMatch = string.Equals(evidence.Sha256Hash, ledgerBlock.EvidenceHash, StringComparison.OrdinalIgnoreCase);

        return new EvidenceIntegrityStatusDto
        {
            EvidenceId = evidenceId,
            FileName = evidence.FileName,
            Version = evidence.Version,
            ActualFileSha256 = evidence.Sha256Hash,
            RegisteredSha256 = evidence.Sha256Hash,
            LedgerSha256 = ledgerBlock.EvidenceHash,
            BlockIndex = ledgerBlock.BlockIndex,
            Action = ledgerBlock.Action,
            ActorName = ledgerBlock.ActorName,
            Status = isMatch ? "VERIFIED" : "LEDGER_MISMATCH",
            ChainStatus = "VALID",
            Explanation = isMatch
                ? "Registered evidence hash matches ledger record."
                : "Stored evidence hash does not match ledger record."
        };
    }

    public async Task<ChainValidationResultDto> VerifyChainAsync(CancellationToken cancellationToken = default)
    {
        var blocks = await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .OrderBy(b => b.BlockIndex)
            .ToListAsync(cancellationToken);

        if (blocks.Count == 0)
        {
            return new ChainValidationResultDto
            {
                IsValid = false,
                TotalBlocks = 0,
                CheckedBlocks = 0,
                FirstInvalidBlockIndex = 0,
                FailureReason = "Ledger is empty: no genesis block found."
            };
        }

        // 1. Verify Genesis Block (Block #0)
        var genesis = blocks[0];
        if (genesis.BlockIndex != 0)
        {
            return new ChainValidationResultDto
            {
                IsValid = false,
                TotalBlocks = blocks.Count,
                CheckedBlocks = 0,
                FirstInvalidBlockIndex = genesis.BlockIndex,
                FailureReason = $"First block index is {genesis.BlockIndex}, expected 0 (Genesis)."
            };
        }

        if (!string.Equals(genesis.PreviousBlockHash, GenesisPreviousBlockHash, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(genesis.EvidenceHash, GenesisEvidenceHash, StringComparison.OrdinalIgnoreCase))
        {
            return new ChainValidationResultDto
            {
                IsValid = false,
                TotalBlocks = blocks.Count,
                CheckedBlocks = 1,
                FirstInvalidBlockIndex = 0,
                FailureReason = "Genesis block parameters are corrupted."
            };
        }

        var expectedGenesisHash = ComputeBlockHash(genesis);
        if (!string.Equals(expectedGenesisHash, genesis.BlockHash, StringComparison.OrdinalIgnoreCase))
        {
            return new ChainValidationResultDto
            {
                IsValid = false,
                TotalBlocks = blocks.Count,
                CheckedBlocks = 1,
                FirstInvalidBlockIndex = 0,
                FailureReason = $"Genesis block hash mismatch: stored {genesis.BlockHash}, computed {expectedGenesisHash}."
            };
        }

        // 2. Sequentially verify each subsequent block
        for (int i = 1; i < blocks.Count; i++)
        {
            var prev = blocks[i - 1];
            var curr = blocks[i];

            // A. Check sequential indexing
            if (curr.BlockIndex != prev.BlockIndex + 1)
            {
                return new ChainValidationResultDto
                {
                    IsValid = false,
                    TotalBlocks = blocks.Count,
                    CheckedBlocks = i,
                    FirstInvalidBlockIndex = curr.BlockIndex,
                    FailureReason = $"Block index sequence broken at #{curr.BlockIndex}. Preceding index was #{prev.BlockIndex}."
                };
            }

            // B. Check previous hash linkage
            if (!string.Equals(curr.PreviousBlockHash, prev.BlockHash, StringComparison.OrdinalIgnoreCase))
            {
                return new ChainValidationResultDto
                {
                    IsValid = false,
                    TotalBlocks = blocks.Count,
                    CheckedBlocks = i,
                    FirstInvalidBlockIndex = curr.BlockIndex,
                    FailureReason = $"PreviousBlockHash mismatch at Block #{curr.BlockIndex}. Points to {curr.PreviousBlockHash}, but Block #{prev.BlockIndex} hash is {prev.BlockHash}."
                };
            }

            // C. Recompute canonical block hash and verify against stored BlockHash
            var computedHash = ComputeBlockHash(curr);
            if (!string.Equals(computedHash, curr.BlockHash, StringComparison.OrdinalIgnoreCase))
            {
                return new ChainValidationResultDto
                {
                    IsValid = false,
                    TotalBlocks = blocks.Count,
                    CheckedBlocks = i,
                    FirstInvalidBlockIndex = curr.BlockIndex,
                    FailureReason = $"BlockHash mismatch at Block #{curr.BlockIndex}. Stored hash {curr.BlockHash} does not match computed canonical hash {computedHash}."
                };
            }

            // D. Structural check on EvidenceHash (must be 64-char hex)
            if (string.IsNullOrWhiteSpace(curr.EvidenceHash) || curr.EvidenceHash.Length != 64)
            {
                return new ChainValidationResultDto
                {
                    IsValid = false,
                    TotalBlocks = blocks.Count,
                    CheckedBlocks = i,
                    FirstInvalidBlockIndex = curr.BlockIndex,
                    FailureReason = $"Invalid EvidenceHash format at Block #{curr.BlockIndex}. Must be a 64-character SHA-256 hexadecimal string."
                };
            }
        }

        return new ChainValidationResultDto
        {
            IsValid = true,
            TotalBlocks = blocks.Count,
            CheckedBlocks = blocks.Count,
            FirstInvalidBlockIndex = null,
            FailureReason = null
        };
    }

    public async Task<List<EvidenceLedgerBlockDto>> GetEvidenceHistoryAsync(
        string evidenceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .Where(b => b.EvidenceItemId == evidenceId)
            .OrderBy(b => b.BlockIndex)
            .Select(b => MapToDto(b))
            .ToListAsync(cancellationToken);
    }

    public async Task<EvidenceLedgerBlockDto?> GetBlockByIndexAsync(
        long blockIndex,
        CancellationToken cancellationToken = default)
    {
        var block = await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BlockIndex == blockIndex, cancellationToken);

        return block == null ? null : MapToDto(block);
    }

    public async Task<EvidenceLedgerBlockDto?> GetLatestBlockAsync(CancellationToken cancellationToken = default)
    {
        var block = await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .OrderByDescending(b => b.BlockIndex)
            .FirstOrDefaultAsync(cancellationToken);

        return block == null ? null : MapToDto(block);
    }

    public async Task<List<EvidenceLedgerBlockDto>> GetPaginatedLedgerAsync(
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 50;
        if (limit > 200) limit = 200;
        if (offset < 0) offset = 0;

        return await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .OrderByDescending(b => b.BlockIndex)
            .Skip(offset)
            .Take(limit)
            .Select(b => MapToDto(b))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReconciliationItemDto>> GetReconciliationCandidatesAsync(
        CancellationToken cancellationToken = default)
    {
        // Find evidence items that do not have a ledger block
        var ledgerEvidenceIds = await _dbContext.EvidenceLedgerBlocks
            .AsNoTracking()
            .Where(b => b.EvidenceItemId != null)
            .Select(b => b.EvidenceItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var ledgerSet = new HashSet<string>(ledgerEvidenceIds);

        var allEvidence = await _dbContext.EvidenceItems
            .AsNoTracking()
            .OrderByDescending(e => e.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return allEvidence
            .Where(e => !ledgerSet.Contains(e.Id))
            .Select(e => new ReconciliationItemDto
            {
                EvidenceId = e.Id,
                CaseId = e.CaseId,
                FileName = e.FileName,
                Version = e.Version,
                Sha256Hash = e.Sha256Hash,
                UploadedAtUtc = e.UploadedAtUtc,
                UploadedByName = e.UploadedByName,
                Status = "PENDING_LEDGER_REGISTRATION"
            })
            .ToList();
    }

    public async Task<EvidenceLedgerBlockDto> ReconcileEvidenceAsync(
        string evidenceId,
        string actorUserId,
        string actorName,
        CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.EvidenceItems
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);

        if (evidence == null)
            throw new KeyNotFoundException($"Evidence item '{evidenceId}' not found.");

        // Check if block already exists
        var existingBlock = await _dbContext.EvidenceLedgerBlocks
            .FirstOrDefaultAsync(b => b.EvidenceItemId == evidenceId, cancellationToken);

        if (existingBlock != null)
        {
            return MapToDto(existingBlock);
        }

        // Verify physical file before registering
        using var stream = await _storageService.GetFileAsync(evidence.StoragePath, cancellationToken);
        if (stream == null)
            throw new InvalidOperationException($"Cannot reconcile evidence '{evidenceId}': physical file is missing from storage.");

        var actualSha256 = _hashService.ComputeSha256Hex(stream);
        if (!string.Equals(actualSha256, evidence.Sha256Hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot reconcile evidence '{evidenceId}': physical file hash differs from recorded metadata.");

        var meta = $"{{\"reconciled\":true,\"fileName\":\"{evidence.FileName}\",\"originalUpload\":{evidence.UploadedAtUtc:O}}}";

        var block = await AppendBlockAsync(
            evidence.Id,
            evidence.Version,
            evidence.Sha256Hash,
            "RECONCILIATION",
            actorUserId,
            actorName,
            meta,
            null,
            cancellationToken);

        await _auditService.LogAsync(
            actorUserId,
            actorName,
            "EVIDENCE_RECONCILED",
            "Evidence",
            evidenceId,
            $"Reconciled evidence {evidence.FileName} (Block #{block.BlockIndex})",
            meta,
            null,
            cancellationToken);

        return block;
    }

    private static EvidenceLedgerBlockDto MapToDto(EvidenceLedgerBlock b)
    {
        return new EvidenceLedgerBlockDto
        {
            Id = b.Id,
            BlockIndex = b.BlockIndex,
            EvidenceItemId = b.EvidenceItemId,
            EvidenceVersion = b.EvidenceVersion,
            EvidenceHash = b.EvidenceHash,
            PreviousBlockHash = b.PreviousBlockHash,
            BlockHash = b.BlockHash,
            Action = b.Action,
            ActorUserId = b.ActorUserId,
            ActorName = b.ActorName,
            TimestampUtc = b.TimestampUtc,
            MetadataJson = b.MetadataJson,
            CreatedAtUtc = b.CreatedAtUtc
        };
    }
}
