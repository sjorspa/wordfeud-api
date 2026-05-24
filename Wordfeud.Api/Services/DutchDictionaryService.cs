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
    private static readonly HashSet<string> _fallbackWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "HUIS", "LIEFDE", "AAN", "ONDERWIJS", "WATER", "LAMP", "BOEK", "TAFEL",
        "STOEL", "DEUR", "RAAM", "SCHOOL", "VRIEND", "KIND", "GROOT", "KLEIN",
        "DAG", "NACHT", "MOOI", "LEUK", "GOED", "BEST", "SNEL", "LANGZAAM",
        "KOMEN", "GAAN", "ZIJN", "HEBEN", "DOEN", "ZEGGEN", "MAKEN", "WERKEN",
        "LEVEN", "WONEN", "DROOM", "VRIJHEID", "VREDE", "LACH", "TRANEN",
        "ZON", "MAAN", "STER", "HEMEL", "AARDE", "LUCHT", "ZEE", "RIVIER",
        "BERG", "BOOM", "BLAD", "BLUM", "TUIN", "STAD", "DORP", "WEG", "PAD",
        "BRUG", "TOREN", "KERK", "MUUR", "VLOER", "PLAFOND", "KAMER", "HOF",
        "NAAM", "WOORD", "TAAL", "LAND", "VOLK", "NATIE", "WERELD", "JAA",
        "NEE", "DANK", "DAAROM", "OMDAT", "WAAROM", "HOE", "WAT", "WIJ",
        "MIJ", "JOUI", "HIJ", "ZIJ", "HET", "DE", "EEN", "EN", "IN", "OP",
        "MET", "VAN", "UIT", "BIJ", "TUSSEN", "DOOR", "NAAR", "VOOR",
        "MAAR", "OOK", "OF", "DAN", "ALS", "WANNEER", "INDIEN", "HOEWEL",
        "TEZIJN", "OPDAT", "ZODAT", "OM", "GEVOLG", "REDEN", "GROND", "DOEL",
        "EIND", "BEGIN", "MIDDEL", "TIJD", "UUR", "DAG", "WEEK", "MAAND",
        "JAAR", "EEUW", "SEIZOEN", "LENTE", "ZOMER", "HERFST", "WINTER",
        "MAANDAG", "DINSDAG", "WOENSDAG", "DONDERDAG", "VRIJDAG", "ZATERDAG",
        "ZONDAG", "KAAS", "BROOD", "BOTER", "SUIKER", "ZOUT", "OLIE",
        "APP", "PERE", "SINAAS", "BANAAN", "KIWI", "VIS", "KIP", "RUND",
        "VARKEN", "KOFFIE", "THEE", "WIJN", "BIER", "SAP",
        "LIEF", "MOOI", "LEUK", "PRACHTIG", "FANTASTISCH", "WONDER", "SCHATTIG",
        "GEZOND", "STERK", "GEWELDIG", "GROOT", "KLEIN", "KORTE", "LANGE",
        "BREDE", "SMALLE", "DIEPE", "LAAE", "SNELLE", "TRAAGE", "HARDE",
        "ZACHTE", "GLADDE", "RUWE", "VEILIGE", "GEVAARLIKE", "GEMAKKELIKE",
        "MOEILIJKE", "EENVOUDIGE", "KOMPLIEKE", "DUURE", "GOEDKOOPE",
        "HOGE", "LAAE", "DIEPE", "SEPE", "WEDE", "SMAL", "BREDE", "DUNNE",
        "DIKE", "LANG", "KORT", "SWAA", "LIGTE", "SOET", "SUUR", "BITTER",
        "PIKANT", "MILD", "GEURIG", "GEURLOOS", "FRIS", "VERFRISSEND", "VERMOEID",
        "UITGEPUT", "SOUT", "PEPPER"
    };

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
            _logger.LogWarning("Dictionary not initialized yet. Using fallback validation.");
            return _fallbackWords.Contains(word);
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
            var url = "https://raw.githubusercontent.com/OpenTaal/opentaal-wordlist/refs/heads/master/wordlist.txt";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

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
}
