using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IExtractionService
{
    Task<ExtractionResultDto> GetExtractionByEvidenceIdAsync(string evidenceId, CancellationToken cancellationToken = default);

    Task<string> QueueExtractionJobAsync(string evidenceId, string actorId, string actorName, CancellationToken cancellationToken = default);

    Task ProcessJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> ApproveEntityAsync(
        string evidenceId,
        string entityId,
        string? correctedRawValue,
        string? correctedNormalizedValue,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> RejectEntityAsync(
        string evidenceId,
        string entityId,
        string? reason,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> EditEntityAsync(
        string evidenceId,
        string entityId,
        string newRawValue,
        string? newNormalizedValue,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ApproveRelationshipAsync(
        string evidenceId,
        string relationshipId,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> RejectRelationshipAsync(
        string evidenceId,
        string relationshipId,
        string? reason,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<int> ProcessPendingQueueAsync(CancellationToken cancellationToken = default);
}
