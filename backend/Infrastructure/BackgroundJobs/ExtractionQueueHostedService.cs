using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

public class ExtractionQueueHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExtractionQueueHostedService> _logger;

    public ExtractionQueueHostedService(
        IServiceProvider serviceProvider,
        ILogger<ExtractionQueueHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExtractionQueueHostedService started. Monitoring queued evidence extraction tasks.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var extractionService = scope.ServiceProvider.GetRequiredService<IExtractionService>();

                var processedCount = await extractionService.ProcessPendingQueueAsync(stoppingToken);
                if (processedCount > 0)
                {
                    _logger.LogInformation("Processed {Count} queued evidence extraction jobs.", processedCount);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing queued extraction jobs: {Message}", ex.Message);
            }

            // Poll every 3 seconds
            await Task.Delay(3000, stoppingToken);
        }
    }
}
