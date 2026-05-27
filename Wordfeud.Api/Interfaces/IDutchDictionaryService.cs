namespace Wordfeud.Api.Interfaces;

/// <summary>
/// Service for validating Dutch words.
/// </summary>
public interface IDutchDictionaryService
{
    /// <summary>
    /// Checks whether a word exists in the Dutch dictionary.
    /// </summary>
    /// <param name="word">The word to check (case-insensitive).</param>
    /// <returns>True if the word is valid Dutch; otherwise, false.</returns>
    bool Contains(string word);

    /// <summary>
    /// Initializes the dictionary from the embedded resource.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets the number of words currently loaded.
    /// </summary>
    int WordCount { get; }

    /// <summary>
    /// Gets whether the dictionary has been fully initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets a paginated list of words from the dictionary.
    /// </summary>
    /// <param name="skip">Number of words to skip.</param>
    /// <param name="take">Number of words to take.</param>
    /// <returns>A list of words.</returns>
    IEnumerable<string> GetWords(int skip, int take);
}
