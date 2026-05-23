using System.Net.Http.Json;
using System.Text.Json;
using Wordfeud.Api.Models;
using Wordfeud.Api.Serialization;

namespace Wordfeud.Api.Tests;

/// <summary>
/// Helper methods for integration tests that need to deserialize Game objects
/// with the TileArrayConverter for 2D board serialization.
/// </summary>
public static class TestHelpers
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new TileArrayConverter() }
    };

    /// <summary>
    /// Deserializes HTTP response content as a Game object with the TileArrayConverter.
    /// </summary>
    public static async Task<Game> ReadAsGameAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Game>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize Game from response.");
    }

    /// <summary>
    /// Deserializes HTTP response content as a Game object with the TileArrayConverter.
    /// </summary>
    public static async Task<Game> ReadAsGameAsync(HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Game>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize Game from content.");
    }
}
