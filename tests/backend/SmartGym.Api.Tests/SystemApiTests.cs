using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SmartGym.Api.Tests;

public class SystemApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SystemApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task SystemInfoEndpoint_ReturnsValidApplicationMetadata()
    {
        // Act
        var response = await _client.GetAsync("/api/system/info");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("SmartGym Backend API", root.GetProperty("application").GetString());
        Assert.Equal("1.0.0", root.GetProperty("version").GetString());
        Assert.Equal("Online", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("roles").GetArrayLength() == 3);
        Assert.True(root.GetProperty("components").GetArrayLength() == 4);
    }

    [Fact]
    public async Task SwaggerJson_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SmartGym API", content);
    }
}
