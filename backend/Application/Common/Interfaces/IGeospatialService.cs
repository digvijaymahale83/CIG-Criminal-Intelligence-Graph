using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IGeospatialService
{
    Task<List<LocationDto>> GetCaseLocationsAsync(string caseId, string? search = null, CancellationToken cancellationToken = default);

    Task<CaseMapDto> GetCaseMapDataAsync(
        string caseId,
        string? eventType = null,
        string? entityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool verifiedOnly = false,
        CancellationToken cancellationToken = default);

    Task<LocationDto?> GetLocationDetailsAsync(string locationId, CancellationToken cancellationToken = default);

    Task<LocationActivityDto?> GetLocationActivityAsync(string locationId, string? caseId = null, CancellationToken cancellationToken = default);

    Task<List<LocationDto>> GetEntityLocationsAsync(string entityId, string? caseId = null, CancellationToken cancellationToken = default);

    Task<TravelSequenceDto> GetEntityTravelSequenceAsync(string entityId, string? caseId = null, CancellationToken cancellationToken = default);

    Task<List<SpatialProximityResultDto>> GetSpatialProximityAsync(
        string caseId,
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken cancellationToken = default);

    Task<SpatialAnalysisResultDto?> GetLatestSpatialAnalysisAsync(string caseId, CancellationToken cancellationToken = default);

    Task<SpatialAnalysisResultDto> RunSpatialAnalysisAsync(
        string caseId,
        RunSpatialAnalysisRequestDto request,
        string executedBy,
        CancellationToken cancellationToken = default);

    Task<List<SpatialSignalDto>> GetSpatialSignalsAsync(
        string caseId,
        string? status = null,
        string? signalType = null,
        CancellationToken cancellationToken = default);

    Task<SpatialSignalDto> ReviewSpatialSignalAsync(
        string caseId,
        string signalId,
        ReviewSpatialSignalRequestDto request,
        string reviewer,
        CancellationToken cancellationToken = default);
}
