using System.Diagnostics;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI;

public class AIServiceClient : IAIServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIServiceClient> _logger;
    private readonly string _baseUrl;

    public string BaseUrl => _baseUrl;

    public AIServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<AIServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(3);
    }

    public async Task<AIServiceHealthResponse?> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AIServiceHealthResponse>("/health", cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("AI Service health check failed: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<double> MeasureLatencyMsAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            sw.Stop();
            return response.IsSuccessStatusCode ? sw.Elapsed.TotalMilliseconds : -1;
        }
        catch
        {
            return -1;
        }
    }
}
