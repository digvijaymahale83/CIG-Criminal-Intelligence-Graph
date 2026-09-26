namespace Worker;

public class InvestigationBackgroundWorker : BackgroundService
{
    private readonly ILogger<InvestigationBackgroundWorker> _logger;

    public InvestigationBackgroundWorker(ILogger<InvestigationBackgroundWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Investigation Background Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
