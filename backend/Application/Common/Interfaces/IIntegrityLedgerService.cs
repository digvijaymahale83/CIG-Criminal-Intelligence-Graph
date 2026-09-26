using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IIntegrityLedgerService
{
    Task EnsureGenesisBlockAsync(CancellationToken cancellationToken = default);

    Task<EvidenceLedgerBlockDto> AppendBlockAsync(
        string? evidenceId,
        int evidenceVersion,
        string evidenceHash,
        string action,
        string actorUserId,
        string actorName,
        string? metadataJson = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<EvidenceIntegrityStatusDto> VerifyEvidenceAsync(
        string evidenceId,
        string? actorUserId = null,
        string? actorName = null,
        CancellationToken cancellationToken = default);

    Task<EvidenceIntegrityStatusDto> GetEvidenceIntegrityStatusAsync(
        string evidenceId,
        CancellationToken cancellationToken = default);

    Task<ChainValidationResultDto> VerifyChainAsync(CancellationToken cancellationToken = default);

    Task<List<EvidenceLedgerBlockDto>> GetEvidenceHistoryAsync(
        string evidenceId,
        CancellationToken cancellationToken = default);

    Task<EvidenceLedgerBlockDto?> GetBlockByIndexAsync(
        long blockIndex,
        CancellationToken cancellationToken = default);

    Task<EvidenceLedgerBlockDto?> GetLatestBlockAsync(
        CancellationToken cancellationToken = default);

    Task<List<EvidenceLedgerBlockDto>> GetPaginatedLedgerAsync(
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<List<ReconciliationItemDto>> GetReconciliationCandidatesAsync(
        CancellationToken cancellationToken = default);

    Task<EvidenceLedgerBlockDto> ReconcileEvidenceAsync(
        string evidenceId,
        string actorUserId,
        string actorName,
        CancellationToken cancellationToken = default);
}
