using Microsoft.EntityFrameworkCore;
using Wordfeud.Api.Data;
using Wordfeud.Api.Interfaces;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Services;

/// <summary>
/// SQLite-backed implementation of <see cref="IGameRepository"/>.
/// </summary>
public class GameRepository : IGameRepository
{
    private readonly GameDbContext _context;

    public GameRepository(GameDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(GameEntity game)
    {
        await _context.Games.AddAsync(game);
        await _context.SaveChangesAsync();
    }

    public async Task<GameEntity?> GetByIdAsync(string gameId)
    {
        return await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
    }

    public async Task UpdateAsync(GameEntity game)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string gameId)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game != null)
        {
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
        }
    }
}
