using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IEntityResolutionService
{
    /// <summary>
    /// Executes a controlled, indexed batch analysis to discover cross-case candidate matches.
    /// </summary>
    Task<BatchResolutionResultDto> RunBatchResolutionAsync(
        string? caseId,
        List<string>? entityTypes,
        double minimumScore,
        string actorId,
        string actorName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves candidate entity matches with optional filtering.
    /// </summary>
    Task<List<EntityMatchCandidateDto>> GetCandidatesAsync(
        string? caseId,
        string? status,
        string? entityType,
        double? minScore,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves full candidate detail by ID.
    /// </summary>
    Task<EntityMatchCandidateDto?> GetCandidateByIdAsync(string candidateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves side-by-side comparison payload between source and target entities.
    /// </summary>
    Task<CandidateComparisonDto?> GetCandidateComparisonAsync(string candidateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits human investigator decision (APPROVE or REJECT) with audit recording and graph synchronization.
    /// </summary>
    Task<bool> ReviewCandidateAsync(
        string candidateId,
        CandidateReviewRequestDto review,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all potential cross-case candidate matches for a specific entity.
    /// </summary>
    Task<List<EntityMatchCandidateDto>> GetCrossCaseMatchesForEntityAsync(string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves approved cross-case connections for a case.
    /// </summary>
    Task<List<CrossCaseConnectionDto>> GetCrossCaseConnectionsAsync(
        string caseId,
        string? connectionType,
        double? minConfidence,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a cross-case subgraph suitable for visualization (case nodes + connected entities + cross-case edges).
    /// </summary>
    Task<CrossCaseNetworkDto> GetCrossCaseNetworkAsync(
        string caseId,
        string? connectionType,
        double? minConfidence,
        CancellationToken cancellationToken = default);
}
