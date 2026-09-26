namespace Application.Common.Interfaces;

public class AIServiceHealthResponse
{
    public string Status { get; set; } = "healthy";
    public string Version { get; set; } = "1.0.0";
    public string Service { get; set; } = "Criminal Network AI Service";
    public bool Ready { get; set; } = true;
}

public interface IAIServiceClient
{
    Task<AIServiceHealthResponse?> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<double> MeasureLatencyMsAsync(CancellationToken cancellationToken = default);
    string BaseUrl { get; }
}
