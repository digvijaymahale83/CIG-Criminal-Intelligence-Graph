using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.Common.Models;

public class ServiceHealthDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("serviceType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ServiceType? ServiceType { get; set; }

    [JsonPropertyName("healthy")]
    public bool Healthy { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("latencyMs")]
    public double? LatencyMs { get; set; }

    [JsonPropertyName("lastCheckedUtc")]
    public string? LastCheckedUtc { get; set; }
}

public class SystemStatusDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ready";

    [JsonPropertyName("readiness")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SystemReadinessStatus? Readiness { get; set; }

    [JsonPropertyName("checkedAtUtc")]
    public string CheckedAtUtc { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("services")]
    public List<ServiceHealthDto> Services { get; set; } = new();

    [JsonPropertyName("isReady")]
    public bool IsReady { get; set; } = true;
}

public class SimulationModeRequest
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "ready"; // ready | degraded | malformed | error
}

public class SystemHealthSummaryDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("services")]
    public Dictionary<string, string> Services { get; set; } = new();
}

