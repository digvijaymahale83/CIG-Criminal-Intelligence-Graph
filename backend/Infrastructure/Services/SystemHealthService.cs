using System.Diagnostics;
using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class SystemHealthService : ISystemHealthService
{
    private readonly IAppDbContext _dbContext;
    private readonly INeo4jDriverFactory _neo4jFactory;
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly IAIServiceClient _aiServiceClient;
    private readonly ILogger<SystemHealthService> _logger;

    private static string _currentSimulationMode = "none";

    public SystemHealthService(
        IAppDbContext dbContext,
        INeo4jDriverFactory neo4jFactory,
        IRedisConnectionFactory redisFactory,
        IAIServiceClient aiServiceClient,
        ILogger<SystemHealthService> logger)
    {
        _dbContext = dbContext;
        _neo4jFactory = neo4jFactory;
        _redisFactory = redisFactory;
        _aiServiceClient = aiServiceClient;
        _logger = logger;
    }

    public void SetSimulationMode(string mode)
    {
        _currentSimulationMode = mode.ToLowerInvariant();
        _logger.LogInformation("Simulation mode updated to: {Mode}", _currentSimulationMode);
    }

    public string GetSimulationMode() => _currentSimulationMode;

    public Task<bool> IsLiveAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetSystemStatusAsync(null, cancellationToken);
        return status.IsReady;
    }

    public async Task<SystemHealthSummaryDto> GetHealthSummaryAsync(CancellationToken cancellationToken = default)
    {
        // 1. Check PostgreSQL
        var pgHealthy = false;
        try
        {
            pgHealthy = await _dbContext.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("PostgreSQL health probe error: {Message}", ex.Message);
        }

        // 2. Check Neo4j
        var neo4jHealthy = false;
        try
        {
            neo4jHealthy = await _neo4jFactory.VerifyConnectivityAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Neo4j health probe error: {Message}", ex.Message);
        }

        // 3. Check Python AI Service
        var aiHealthy = false;
        try
        {
            var latency = await _aiServiceClient.MeasureLatencyMsAsync(cancellationToken);
            aiHealthy = latency >= 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AI service health probe error: {Message}", ex.Message);
        }

        var allHealthy = pgHealthy && neo4jHealthy && aiHealthy;

        return new SystemHealthSummaryDto
        {
            Status = allHealthy ? "healthy" : "degraded",
            Services = new Dictionary<string, string>
            {
                ["postgresql"] = pgHealthy ? "healthy" : "degraded",
                ["neo4j"] = neo4jHealthy ? "healthy" : "degraded",
                ["ai_service"] = aiHealthy ? "healthy" : "degraded"
            }
        };
    }

    public async Task<SystemStatusDto> GetSystemStatusAsync(string? simulateMode = null, CancellationToken cancellationToken = default)
    {
        var activeMode = (simulateMode ?? _currentSimulationMode).ToLowerInvariant();
        var now = DateTime.UtcNow.ToString("o");

        if (activeMode == "error")
        {
            throw new InvalidOperationException("Simulated 500 internal server error for investigative health diagnosis.");
        }

        if (activeMode == "ready")
        {
            return new SystemStatusDto
            {
                Status = "ready",
                CheckedAtUtc = now,
                IsReady = true,
                Services = new List<ServiceHealthDto>
                {
                    new() { Name = "PostgreSQL (Authoritative Store)", Healthy = true, LatencyMs = 2.1, Message = "Connected", LastCheckedUtc = now },
                    new() { Name = "Neo4j Graph Database", Healthy = true, LatencyMs = 4.3, Message = "Connected", LastCheckedUtc = now },
                    new() { Name = "Python FastAPI AI Service", Healthy = true, LatencyMs = 12.0, Message = "Operational", LastCheckedUtc = now }
                }
            };
        }

        if (activeMode == "degraded")
        {
            return new SystemStatusDto
            {
                Status = "degraded",
                CheckedAtUtc = now,
                IsReady = false,
                Services = new List<ServiceHealthDto>
                {
                    new() { Name = "PostgreSQL (Authoritative Store)", Healthy = true, LatencyMs = 2.1, Message = "Connected", LastCheckedUtc = now },
                    new() { Name = "Neo4j Graph Database", Healthy = false, LatencyMs = 0, Message = "Connection timeout", LastCheckedUtc = now },
                    new() { Name = "Python FastAPI AI Service", Healthy = true, LatencyMs = 12.0, Message = "Operational", LastCheckedUtc = now }
                }
            };
        }

        // Live probe evaluation - NO fake hardcoded green states
        var services = new List<ServiceHealthDto>();

        // 1. PostgreSQL
        var pgSw = Stopwatch.StartNew();
        var pgHealthy = false;
        try
        {
            pgHealthy = await _dbContext.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Postgres check failed: {Message}", ex.Message);
        }
        pgSw.Stop();

        services.Add(new ServiceHealthDto
        {
            Name = "PostgreSQL (Authoritative Store)",
            Healthy = pgHealthy,
            Message = pgHealthy ? "Primary relational database connected and operational" : "Cannot reach PostgreSQL database server",
            LatencyMs = pgHealthy ? Math.Round(pgSw.Elapsed.TotalMilliseconds, 2) : 0,
            LastCheckedUtc = now
        });

        // 2. Neo4j
        var neo4jLatency = -1.0;
        try
        {
            neo4jLatency = await _neo4jFactory.MeasureLatencyMsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Neo4j check failed: {Message}", ex.Message);
        }
        var neo4jHealthy = neo4jLatency >= 0;

        services.Add(new ServiceHealthDto
        {
            Name = "Neo4j Graph Database",
            Healthy = neo4jHealthy,
            Message = neo4jHealthy ? "Neo4j Bolt driver connected" : "Neo4j instance unreachable",
            LatencyMs = neo4jHealthy ? Math.Round(neo4jLatency, 2) : 0,
            LastCheckedUtc = now
        });

        // 3. Redis
        var redisLatency = -1.0;
        try
        {
            redisLatency = await _redisFactory.MeasureLatencyMsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis check failed: {Message}", ex.Message);
        }
        var redisHealthy = redisLatency >= 0;

        services.Add(new ServiceHealthDto
        {
            Name = "Redis Cache",
            Healthy = redisHealthy,
            Message = redisHealthy ? "Redis multiplexer responsive" : "Redis instance unreachable",
            LatencyMs = redisHealthy ? Math.Round(redisLatency, 2) : 0,
            LastCheckedUtc = now
        });

        // 4. AI Service
        var aiLatency = -1.0;
        try
        {
            aiLatency = await _aiServiceClient.MeasureLatencyMsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AI Service check failed: {Message}", ex.Message);
        }
        var aiHealthy = aiLatency >= 0;

        services.Add(new ServiceHealthDto
        {
            Name = "Python FastAPI AI Service",
            Healthy = aiHealthy,
            Message = aiHealthy ? "AI Service health probe passed" : "AI Service not reachable at configured endpoint",
            LatencyMs = aiHealthy ? Math.Round(aiLatency, 2) : 0,
            LastCheckedUtc = now
        });

        // Evaluation
        var allHealthy = services.All(s => s.Healthy);

        return new SystemStatusDto
        {
            Status = allHealthy ? "ready" : "degraded",
            CheckedAtUtc = now,
            Services = services,
            IsReady = allHealthy
        };
    }
}
