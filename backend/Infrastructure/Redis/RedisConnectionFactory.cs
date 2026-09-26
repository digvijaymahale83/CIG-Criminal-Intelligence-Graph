using System.Diagnostics;
using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Redis;

public class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private readonly ILogger<RedisConnectionFactory> _logger;
    private readonly string _configuration;
    private readonly Lazy<ConnectionMultiplexer?> _multiplexerLazy;

    public string Configuration => _configuration;

    public RedisConnectionFactory(IConfiguration configuration, ILogger<RedisConnectionFactory> logger)
    {
        _logger = logger;
        _configuration = configuration["Redis:Configuration"] ?? "localhost:6379,abortConnect=false,connectTimeout=3000";

        _multiplexerLazy = new Lazy<ConnectionMultiplexer?>(() =>
        {
            try
            {
                var options = ConfigurationOptions.Parse(_configuration);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 3000;
                options.SyncTimeout = 3000;
                return ConnectionMultiplexer.Connect(options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to initialize Redis connection: {Message}", ex.Message);
                return null;
            }
        });
    }

    public async Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var multiplexer = _multiplexerLazy.Value;
            if (multiplexer == null || !multiplexer.IsConnected) return false;

            var db = multiplexer.GetDatabase();
            var sw = Stopwatch.StartNew();
            var ping = await db.PingAsync();
            return ping.TotalMilliseconds >= 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Redis connectivity check failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<double> MeasureLatencyMsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var multiplexer = _multiplexerLazy.Value;
            if (multiplexer == null || !multiplexer.IsConnected) return -1;

            var db = multiplexer.GetDatabase();
            var ping = await db.PingAsync();
            return ping.TotalMilliseconds;
        }
        catch
        {
            return -1;
        }
    }

    public void Dispose()
    {
        if (_multiplexerLazy.IsValueCreated && _multiplexerLazy.Value != null)
        {
            try
            {
                _multiplexerLazy.Value.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}
