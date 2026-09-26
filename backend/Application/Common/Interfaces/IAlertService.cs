using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IAlertService
{
    Task<List<AlertDto>> GetCaseAlertsAsync(string caseId, AlertQueryDto query, CancellationToken cancellationToken = default);

    Task<AlertDto?> GetAlertDetailsAsync(string caseId, string alertId, CancellationToken cancellationToken = default);

    Task<AlertSummaryDto> GetAlertSummaryAsync(string caseId, CancellationToken cancellationToken = default);

    Task<AlertRunResultDto> RunAlertDetectionAsync(
        string caseId,
        RunAlertDetectionRequestDto request,
        string executedBy,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<AlertDto> AcknowledgeAlertAsync(
        string alertId,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<AlertDto> StartReviewAsync(
        string alertId,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<AlertDto> ResolveAlertAsync(
        string alertId,
        string notes,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<AlertDto> DismissAlertAsync(
        string alertId,
        string notes,
        string actorId,
        string actorName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
