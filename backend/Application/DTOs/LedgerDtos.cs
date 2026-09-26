namespace Application.DTOs;

public class EvidenceLedgerBlockDto
{
    public string Id { get; set; } = string.Empty;
    public long BlockIndex { get; set; }
    public string? EvidenceItemId { get; set; }
    public int EvidenceVersion { get; set; } = 1;
    public string EvidenceHash { get; set; } = string.Empty;
    public string PreviousBlockHash { get; set; } = string.Empty;
    public string BlockHash { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class EvidenceIntegrityStatusDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string ActualFileSha256 { get; set; } = string.Empty;
    public string RegisteredSha256 { get; set; } = string.Empty;
    public string LedgerSha256 { get; set; } = string.Empty;
    public string Status { get; set; } = "VERIFIED";
    // Possible Status values: VERIFIED, EVIDENCE_MODIFIED, LEDGER_MISMATCH, CHAIN_INVALID, MISSING_FILE, MISSING_LEDGER_RECORD, HASH_MISMATCH
    public string ChainStatus { get; set; } = "VALID";
    // ChainStatus: VALID, BROKEN, UNKNOWN
    public long? BlockIndex { get; set; }
    public string? Action { get; set; }
    public string? ActorName { get; set; }
    public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
    public string Explanation { get; set; } = string.Empty;
}

public class ChainValidationResultDto
{
    public bool IsValid { get; set; }
    public long TotalBlocks { get; set; }
    public long CheckedBlocks { get; set; }
    public long? FirstInvalidBlockIndex { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ValidatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ReconciliationItemDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Sha256Hash { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING_LEDGER_REGISTRATION";
}

public class AppendBlockRequestDto
{
    public string Action { get; set; } = "INTEGRITY_CHECK";
    public string? MetadataJson { get; set; }
}
