using Wordfeud.Api.Models;

namespace Wordfeud.Api.Interfaces;

/// <summary>
/// Repository interface for Wordfeud game persistence.
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Creates a new game in the database.
    /// </summary>
    Task CreateAsync(GameEntity game);

    /// <summary>
    /// Gets a game by its ID.
    /// </summary>
    Task<GameEntity?> GetByIdAsync(string gameId);

    /// <summary>
    /// Updates an existing game in the database.
    /// </summary>
    Task UpdateAsync(GameEntity game);

    /// <summary>
    /// Deletes a game from the database.
    /// </summary>
    Task DeleteAsync(string gameId);
}
