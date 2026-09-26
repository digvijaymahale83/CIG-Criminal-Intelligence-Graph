using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IAnomalyDetector
{
    /// <summary>
    /// Identifier matching the AlertType (e.g., NETWORK_ANOMALY, TEMPORAL_ANOMALY, etc.)
    /// </summary>
    string DetectorType { get; }

    /// <summary>
    /// SemVer rule or model version (e.g. v1.0, v1.2)
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Executes the detector on verified data within the specified case.
    /// </summary>
    Task<List<AnomalySignalDto>> DetectAsync(string caseId, RunAlertDetectionRequestDto request, CancellationToken cancellationToken = default);
}
