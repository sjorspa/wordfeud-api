using Microsoft.EntityFrameworkCore;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Data;

/// <summary>
/// EF Core DbContext for Wordfeud game persistence.
/// </summary>
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<GameEntity> Games => Set<GameEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(36);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.PlayersJson)
                .IsRequired();

            entity.Property(e => e.TileBagJson)
                .IsRequired();

            entity.Property(e => e.BoardJson)
                .IsRequired();

            entity.Property(e => e.FormedWordsJson)
                .IsRequired();

            entity.Property(e => e.MoveHistoryJson)
                .IsRequired();

            entity.Property(e => e.CurrentPlayerId)
                .HasMaxLength(36);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();
        });
    }
}
