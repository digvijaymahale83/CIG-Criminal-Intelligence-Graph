using Application.DTOs;

namespace Application.Common.Interfaces;

public interface ITemporalService
{
    Task<TimelineResponseDto> GetCaseTimelineAsync(string caseId, TimelineQueryDto query, CancellationToken cancellationToken = default);
    Task<List<TimelineEventDto>> GetCaseTimelineEventsAsync(string caseId, TimelineQueryDto query, CancellationToken cancellationToken = default);
    Task<List<TimelineEventDto>> GetEntityTimelineAsync(string entityId, string? caseId = null, CancellationToken cancellationToken = default);
    Task<List<TimelineEventDto>> GetRelationshipTimelineAsync(string relationshipId, string? caseId = null, CancellationToken cancellationToken = default);
    Task<TimelineDateRangeDto> GetTimelineDateRangeAsync(string caseId, CancellationToken cancellationToken = default);
    Task<TemporalAnalysisResultDto> RunTemporalAnalysisAsync(string caseId, RunTemporalAnalysisRequestDto request, string executedBy, CancellationToken cancellationToken = default);
    Task<TemporalAnalysisResultDto?> GetLatestTemporalAnalysisAsync(string caseId, CancellationToken cancellationToken = default);
    Task<List<TemporalOverlapDto>> GetTemporalOverlapsAsync(string caseId, string? entityId = null, string? location = null, double minDurationMinutes = 0, CancellationToken cancellationToken = default);
    Task<TemporalSequenceDto> GetEntitySequenceAsync(string entityId, string? caseId = null, CancellationToken cancellationToken = default);
    Task<TemporalSummaryDto> GetTemporalSummaryAsync(string caseId, CancellationToken cancellationToken = default);
    Task<TemporalOverlapDto> ReviewTemporalSignalAsync(string signalId, ReviewTemporalSignalRequestDto review, string reviewerName, CancellationToken cancellationToken = default);
}
