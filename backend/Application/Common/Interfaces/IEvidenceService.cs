using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IEvidenceService
{
    Task<List<EvidenceDto>> GetEvidenceAsync(string? caseId, CancellationToken cancellationToken = default);
    Task<EvidenceDto?> GetEvidenceByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<EvidenceUploadResultDto> UploadEvidenceAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSize,
        string? caseId,
        string description,
        string clearance,
        string uploaderId,
        string uploaderName,
        string? ipAddress,
        string? parentEvidenceId = null,
        CancellationToken cancellationToken = default);

    Task<IntegrityCheckResultDto> VerifyIntegrityAsync(
        string evidenceId,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> RetryProcessingAsync(
        string evidenceId,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
