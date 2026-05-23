using Wordfeud.Api.Interfaces;

namespace Wordfeud.Api.Services;

/// <summary>
/// Service that loads and validates words against the OpenTaal Dutch dictionary.
/// </summary>
public class DutchDictionaryService : IDutchDictionaryService
{
    private readonly HashSet<string> _words = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<DutchDictionaryService> _logger;
    private bool _isInitialized;

    /// <summary>
    /// Creates a new DutchDictionaryService.
    /// </summary>
    public DutchDictionaryService(ILogger<DutchDictionaryService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public int WordCount => _words.Count;

    /// <inheritdoc />
    public bool Contains(string word)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("Dictionary not initialized yet. Performing basic validation only.");
            return BasicValidation(word);
        }

        return _words.Contains(word);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Try to load from embedded resource first
            var embeddedWords = LoadFromEmbeddedResource();
            if (embeddedWords.Any())
            {
                _words.UnionWith(embeddedWords);
                _isInitialized = true;
                _logger.LogInformation("Loaded {Count} words from embedded resource", _words.Count);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load embedded dictionary, attempting HTTP download");
        }

        // Fallback: try to download from OpenTaal
        await InitializeFromOpenTaalAsync();
    }

    private IEnumerable<string> LoadFromEmbeddedResource()
    {
        var assembly = typeof(DutchDictionaryService).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            if (resourceName.EndsWith("dutch-words.txt", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    return content.Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            }
        }

        return Enumerable.Empty<string>();
    }

    private async Task InitializeFromOpenTaalAsync()
    {
        try
        {
            // OpenTaal provides Dutch word lists
            var url = "https://open_ta.al/api/v1/dutch/text/overall/10000.txt";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            var content = await httpClient.GetStringAsync(url);
            var words = content
                .Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(w => w.Length >= 2 && w.All(char.IsAsciiLetter))
                .ToArray();

            _words.UnionWith(words);
            _isInitialized = true;

            _logger.LogInformation("Loaded {Count} words from OpenTaal API", _words.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Dutch dictionary from OpenTaal. Using basic validation.");
            // Basic validation: only check length and characters
            _isInitialized = true; // Mark as initialized to avoid repeated failures
        }
    }

    /// <summary>
    /// Basic validation when dictionary is not available.
    /// </summary>
    private static bool BasicValidation(string word)
    {
        if (string.IsNullOrEmpty(word))
            return false;

        if (word.Length < 2)
            return false;

        if (!word.All(char.IsAsciiLetter))
            return false;

        return true;
    }
}
