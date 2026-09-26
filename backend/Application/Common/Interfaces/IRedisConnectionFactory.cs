namespace Application.Common.Interfaces;

public interface IRedisConnectionFactory
{
    Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default);
    Task<double> MeasureLatencyMsAsync(CancellationToken cancellationToken = default);
    string Configuration { get; }
}
