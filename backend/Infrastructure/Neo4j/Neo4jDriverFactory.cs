using System.Diagnostics;
using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Infrastructure.Neo4j;

public class Neo4jDriverFactory : INeo4jDriverFactory, IAsyncDisposable, IDisposable
{
    private readonly ILogger<Neo4jDriverFactory> _logger;
    private readonly Lazy<IDriver?> _driverLazy;
    private readonly string _uri;
    private readonly string _user;
    private readonly string _password;

    public string Uri => _uri;

    public Neo4jDriverFactory(IConfiguration configuration, ILogger<Neo4jDriverFactory> logger)
    {
        _logger = logger;
        _uri = configuration["Neo4j:Uri"] ?? "bolt://localhost:7687";
        _user = configuration["Neo4j:User"] ?? "neo4j";
        _password = configuration["Neo4j:Password"] ?? "CriminalNetwork#2026!";

        _driverLazy = new Lazy<IDriver?>(() =>
        {
            try
            {
                var authToken = AuthTokens.Basic(_user, _password);
                return GraphDatabase.Driver(_uri, authToken, o =>
                {
                    o.WithConnectionTimeout(TimeSpan.FromSeconds(3));
                    o.WithMaxConnectionPoolSize(50);
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to initialize Neo4j driver: {Message}", ex.Message);
                return null;
            }
        });
    }

    public async Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var driver = _driverLazy.Value;
            if (driver == null) return false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            await driver.VerifyConnectivityAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Neo4j connectivity check failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<double> MeasureLatencyMsAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var driver = _driverLazy.Value;
            if (driver == null) return -1;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            await driver.VerifyConnectivityAsync();
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }
        catch
        {
            return -1;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_driverLazy.IsValueCreated && _driverLazy.Value != null)
        {
            try
            {
                await _driverLazy.Value.DisposeAsync();
            }
            catch
            {
                // ignore shutdown cleanup
            }
        }
    }

    public void Dispose()
    {
        if (_driverLazy.IsValueCreated && _driverLazy.Value != null)
        {
            try
            {
                _driverLazy.Value.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}
