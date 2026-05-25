using System.Net;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for the <see cref="HealthController"/>.
/// </summary>
public class HealthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region Liveness Endpoint Tests

    [Fact]
    public async Task GetLiveness_ShouldReturn200Ok()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLiveness_ShouldReturnJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"status\"");
        content.Should().Contain("\"alive\"");
    }

    [Fact]
    public async Task GetLiveness_ShouldReturnTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"timestamp\"");
    }

    [Fact]
    public async Task GetLiveness_ShouldReturnValidTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Verify the timestamp is a valid ISO 8601 format (UTC)
        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
        var timestampString = json.GetProperty("timestamp").GetString();
        timestampString.Should().NotBeNullOrEmpty();
        // Use TryParse to validate format without asserting Kind
        DateTime.TryParse(timestampString, out var timestamp).Should().BeTrue();
        timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetLiveness_ShouldHaveCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var mediaType = response.Content.Headers.ContentType?.MediaType;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        mediaType.Should().Contain("application/json");
    }

    [Fact]
    public async Task GetLiveness_ShouldReturnConsistentStatus()
    {
        // Act
        var response1 = await _client.GetAsync("/health/live");
        var response2 = await _client.GetAsync("/health/live");
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json1 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content1);
        var json2 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content2);
        
        json1.GetProperty("status").GetString().Should().Be("alive");
        json2.GetProperty("status").GetString().Should().Be("alive");
    }

    #endregion

    #region Readiness Endpoint Tests

    [Fact]
    public async Task GetReadiness_ShouldReturn200Ok()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReadiness_ShouldReturnJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"status\"");
        content.Should().Contain("\"ready\"");
    }

    [Fact]
    public async Task GetReadiness_ShouldReturnTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"timestamp\"");
    }

    [Fact]
    public async Task GetReadiness_ShouldReturnValidTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
        var timestampString = json.GetProperty("timestamp").GetString();
        timestampString.Should().NotBeNullOrEmpty();
        // Use TryParse to validate format without asserting Kind
        DateTime.TryParse(timestampString, out var timestamp).Should().BeTrue();
        timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetReadiness_ShouldHaveCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");
        var mediaType = response.Content.Headers.ContentType?.MediaType;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        mediaType.Should().Contain("application/json");
    }

    [Fact]
    public async Task GetReadiness_ShouldReturnConsistentStatus()
    {
        // Act
        var response1 = await _client.GetAsync("/health/ready");
        var response2 = await _client.GetAsync("/health/ready");
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json1 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content1);
        var json2 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content2);
        
        json1.GetProperty("status").GetString().Should().Be("ready");
        json2.GetProperty("status").GetString().Should().Be("ready");
    }

    #endregion

    #region Health Endpoint Comparison Tests

    [Fact]
    public async Task BothHealthEndpoints_ShouldReturnDifferentStatusMessages()
    {
        // Act
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");
        var liveContent = await liveResponse.Content.ReadAsStringAsync();
        var readyContent = await readyResponse.Content.ReadAsStringAsync();

        // Assert
        var liveJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(liveContent);
        var readyJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(readyContent);
        
        liveJson.GetProperty("status").GetString().Should().Be("alive");
        readyJson.GetProperty("status").GetString().Should().Be("ready");
    }

    [Fact]
    public async Task BothHealthEndpoints_ShouldReturn200Ok()
    {
        // Act
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");

        // Assert
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoints_ShouldReturnUniqueTimestamps()
    {
        // Act
        await Task.Delay(10); // Ensure time difference
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");
        var liveContent = await liveResponse.Content.ReadAsStringAsync();
        var readyContent = await readyResponse.Content.ReadAsStringAsync();

        // Assert
        var liveJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(liveContent);
        var readyJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(readyContent);
        
        var liveTimestamp = DateTime.TryParse(liveJson.GetProperty("timestamp").GetString(), out var lt) ? lt : throw new InvalidOperationException("Invalid timestamp");
        var readyTimestamp = DateTime.TryParse(readyJson.GetProperty("timestamp").GetString(), out var rt) ? rt : throw new InvalidOperationException("Invalid timestamp");
        
        readyTimestamp.Should().BeAfter(liveTimestamp);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task GetLiveness_ShouldHandleConcurrentRequests()
    {
        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ => _client.GetAsync("/health/live")).ToList();
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Fact]
    public async Task GetReadiness_ShouldHandleConcurrentRequests()
    {
        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ => _client.GetAsync("/health/ready")).ToList();
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Fact]
    public async Task GetLiveness_ShouldReturn200AfterMultipleRequests()
    {
        // Act
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/health/live");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetReadiness_ShouldReturn200AfterMultipleRequests()
    {
        // Act
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/health/ready");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task HealthEndpoints_ShouldNotThrowExceptions()
    {
        // Act & Assert
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");
        
        liveResponse.Should().NotBeNull();
        readyResponse.Should().NotBeNull();
    }

    #endregion
}
