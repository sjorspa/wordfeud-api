using System.Text;
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
        "KOMEN", "GAAN", "ZIJN", "HEBBEN", "DOEN", "ZEGGEN", "MAKEN", "WERKEN",
        "LEVEN", "WONEN", "DROOM", "VRIJHEID", "VREDE", "LACH", "TRANEN",
        "ZON", "MAAN", "STER", "HEMEL", "AARDE", "LUCHT", "ZEE", "RIVIER",
        "BERG", "BOOM", "BLAD", "BLOEM", "TUIN", "STAD", "DORP", "WEG", "PAD",
        "BRUG", "TOREN", "KERK", "MUUR", "VLOER", "PLAFOND", "KAMER", "HOF",
        "NAAM", "WOORD", "TAAL", "LAND", "VOLK", "NATIE", "WERELD", "JA",
        "NEE", "DANK", "DAAROM", "OMDAT", "WAAROM", "HOE", "WAT", "WIJ",
        "MIJ", "JOULLIE", "HIJ", "ZIJ", "HET", "DE", "EEN", "EN", "IN", "OP",
        "MET", "VAN", "UIT", "BIJ", "TUSSEN", "DOOR", "NAAR", "VOOR",
        "MAAR", "OOK", "OF", "DAN", "ALS", "WANNEER", "INDIEN", "HOEWEL",
        "TEZIJN", "OPDAT", "ZODAT", "OM", "GEVOLG", "REDEN", "GROND", "DOEL",
        "EIND", "BEGIN", "MIDDEL", "TIJD", "UUR", "WEEK", "MAAND",
        "JAAR", "EEUW", "SEIZOEN", "LENTE", "ZOMER", "HERFST", "WINTER",
        "MAANDAG", "DINSDAG", "WOENSDAG", "DONDERDAG", "VRIJDAG", "ZATERDAG",
        "ZONDAG", "KAAS", "BROOD", "BOTER", "SUIKER", "ZOUT", "OLIE",
        "APP", "PEER", "SINAASAPPEL", "BANAAN", "KIWI", "VIS", "KIP", "RUND",
        "VARKEN", "KOFFIE", "THEE", "WIJN", "BIER", "SAP",
        "LIEF", "MOOI", "LEUK", "PRACHTIG", "FANTASTISCH", "WONDER", "SCHATTIG",
        "GEZOND", "STERK", "GEWELDIG", "GROOT", "KLEIN", "KORTE", "LANGE",
        "BREDE", "SMALLE", "DIEPE", "LANGE", "SNELLE", "TRAAGE", "HARDE",
        "ZACHTE", "GLADDE", "RUWE", "VEILIGE", "GEVAARLIJKE", "GEMAKKELIJKE",
        "MOEILIJKE", "KOMPLIEKE", "DUURE", "GOEDKOOPE",
        "HOGE", "LANGE", "DIEPE", "SEPE", "WEDE", "SMAL", "BREDE", "DUNNE",
        "DIKE", "LANG", "KORT", "SWAA", "LIGTE", "ZOET", "ZUUR", "BITTER",
        "PIKANT", "MILD", "GEURIG", "GEURLOOS", "FRIS", "VERFRISSEND", "VERMOEID",
        "UITGEPUT", "ZOUT", "PEPER"
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
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc />
    public bool Contains(string? word)
    {
        if (string.IsNullOrEmpty(word))
            return false;

        if (!_isInitialized)
        {
            _logger.LogWarning("Dictionary not initialized yet. Using fallback validation.");
            return _fallbackWords.Contains(word);
        }

        var normalizedWord = NormalizeDiacritics(word);
        return _words.Contains(normalizedWord);
    }

    /// <summary>
    /// Normalizes a word by removing diacritics and special characters.
    /// Converts characters like ë→e, é→e, à→a, ô→o, etc.
    /// </summary>
    /// <param name="word">The word to normalize.</param>
    /// <returns>A normalized string with diacritics removed.</returns>
    public static string NormalizeDiacritics(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        var normalized = new StringBuilder();
        foreach (var c in word)
        {
            var normalizedChar = c switch
            {
                'é' or 'è' or 'ê' or 'ë' or 'È' or 'É' or 'Ê' or 'Ë' => 'e',
                'à' or 'á' or 'â' or 'ã' or 'ä' or 'å' or 'À' or 'Á' or 'Â' or 'Ã' or 'Ä' or 'Å' => 'a',
                'î' or 'ï' or 'Í' or 'Ì' or 'Î' or 'Ï' => 'i',
                'ó' or 'ò' or 'ô' or 'õ' or 'ö' or 'Ó' or 'Ò' or 'Ô' or 'Õ' or 'Ö' => 'o',
                'ú' or 'ù' or 'û' or 'ü' or 'Ú' or 'Ù' or 'Û' or 'Ü' => 'u',
                'ç' or 'Ç' => 'c',
                'ñ' or 'Ñ' => 'n',
                'ý' or 'ÿ' or 'Ý' => 'y',
                _ => c
            };
            normalized.Append(normalizedChar);
        }

        return normalized.ToString();
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        if (_isInitialized)
            return Task.CompletedTask;

        var embeddedWords = LoadFromEmbeddedResource();
        if (embeddedWords.Any())
        {
            foreach (var word in embeddedWords)
            {
                _words.Add(NormalizeDiacritics(word));
            }
            _isInitialized = true;
            _logger.LogInformation("Loaded {Count} words from embedded resource", _words.Count);
            return Task.CompletedTask;
        }

        _logger.LogError("Failed to load embedded Dutch dictionary. Falling back to limited word validation.");
        _isInitialized = true;
        return Task.CompletedTask;
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
}
