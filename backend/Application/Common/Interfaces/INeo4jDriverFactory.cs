namespace Application.Common.Interfaces;

public interface INeo4jDriverFactory
{
    Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default);
    Task<double> MeasureLatencyMsAsync(CancellationToken cancellationToken = default);
    string Uri { get; }
}
