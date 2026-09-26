using Application.Common.Interfaces;
using Infrastructure.AI;
using Infrastructure.Neo4j;
using Infrastructure.Persistence;
using Infrastructure.Redis;
using Infrastructure.Security;
using Infrastructure.Services;
using Infrastructure.Services.Copilot;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. PostgreSQL EF Core DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=criminal_network_db;Username=postgres;Password=postgres;Include Error Detail=false;";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(10);
            });
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // 2. Neo4j
        services.AddSingleton<INeo4jDriverFactory, Neo4jDriverFactory>();
        services.AddScoped<INeo4jService, Neo4jService>();

        // 3. Redis
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

        // 4. AI Service HttpClient
        services.AddHttpClient<IAIServiceClient, AIServiceClient>();

        // 5. Security Services
        services.AddSingleton<IHashService, Sha256HashService>();
        services.AddSingleton<JwtTokenService>();

        // 6. Storage Service
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // 7. Core Application Services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddHttpClient();
        services.AddScoped<IExtractionService, ExtractionService>();
        services.AddScoped<IEvidenceService, EvidenceService>();
        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IInvestigationGraphService, InvestigationGraphService>();
        services.AddScoped<IEntityResolutionService, EntityResolutionService>();
        services.AddScoped<IGraphAnalyticsService, GraphAnalyticsService>();
        services.AddScoped<ITemporalService, TemporalService>();
        services.AddScoped<IGeospatialService, GeospatialService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IIntegrityLedgerService, IntegrityLedgerService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITranslationService, TranslationService>();

        // Anomaly Detectors
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.NetworkAnomalyDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.TemporalAnomalyDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.GeographicAnomalyDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.RelationshipSurgeDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.ActivitySpikeDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.UnusualTravelDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.DataConsistencyDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.CrossCasePatternDetector>();
        services.AddScoped<IAnomalyDetector, Infrastructure.Services.Detectors.ModelSignalDetector>();

        // Copilot Services (Phase 10)
        services.AddScoped<ICopilotIntentRouter, CopilotIntentRouter>();
        services.AddScoped<ICopilotContextBuilder, CopilotContextBuilder>();
        services.AddScoped<ILLMService, LLMService>();
        services.AddScoped<ICopilotCitationValidator, CopilotCitationValidator>();
        services.AddScoped<ICopilotService, CopilotService>();

        // 7b. Background Extraction Worker
        services.AddHostedService<Infrastructure.BackgroundJobs.ExtractionQueueHostedService>();

        // 8. System Health Service
        services.AddScoped<ISystemHealthService, SystemHealthService>();

        // 9. ASP.NET Core Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgresql", tags: new[] { "ready", "db" });

        return services;
    }
}
