using System.Net;
using System.Net.Http.Json;
using Application.Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLiveness_ShouldReturn200Ok_WithHealthyStatus()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        content.Should().NotBeNull();
        content!["status"].ToString().Should().Be("Healthy");
    }

    [Fact]
    public async Task GetSystemStatus_WithSimulateReady_ShouldReturn200Ok_AndValidSchema()
    {
        var response = await _client.GetAsync("/api/v1/system/status?simulate=ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<SystemStatusDto>();
        dto.Should().NotBeNull();
        dto!.Status.Should().Be("ready");
        dto.IsReady.Should().BeTrue();
        dto.Services.Should().NotBeEmpty();
        dto.CheckedAtUtc.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetSystemStatus_WithSimulateDegraded_ShouldReturn503ServiceUnavailable_WithDegradedPayload()
    {
        var response = await _client.GetAsync("/api/v1/system/status?simulate=degraded");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var dto = await response.Content.ReadFromJsonAsync<SystemStatusDto>();
        dto.Should().NotBeNull();
        dto!.Status.Should().Be("degraded");
        dto.IsReady.Should().BeFalse();
        dto.Services.Should().Contain(s => !s.Healthy);
    }

    [Fact]
    public async Task GetSystemStatus_WithSimulateMalformed_ShouldReturnNonConformingJson()
    {
        var response = await _client.GetAsync("/api/v1/system/status?simulate=malformed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("corruptedField");
        content.Should().NotContain("services");
    }

    [Fact]
    public async Task SetSimulationMode_ShouldUpdateMode()
    {
        var postResponse = await _client.PostAsJsonAsync("/api/v1/system/status/simulate", new SimulationModeRequest
        {
            Mode = "ready"
        });

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
