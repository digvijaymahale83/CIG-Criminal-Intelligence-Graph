using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface ISystemHealthService
{
    Task<SystemStatusDto> GetSystemStatusAsync(string? simulateMode = null, CancellationToken cancellationToken = default);
    Task<SystemHealthSummaryDto> GetHealthSummaryAsync(CancellationToken cancellationToken = default);
    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
    Task<bool> IsLiveAsync(CancellationToken cancellationToken = default);
    void SetSimulationMode(string mode);
    string GetSimulationMode();
}
