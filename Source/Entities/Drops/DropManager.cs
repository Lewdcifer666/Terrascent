using Microsoft.Xna.Framework;
using Terrascent.World;

namespace Terrascent.Entities.Drops;

/// <summary>
/// Manages all drops in the world (gold, XP, etc.).
/// </summary>
public class DropManager
{
    private readonly List<Drop> _drops = new();
    private const int MAX_DROPS = 500;  // Prevent too many drops

    /// <summary>
    /// Event fired when a drop is collected.
    /// </summary>
    public event Action<DropType, int>? OnDropCollected;

    /// <summary>
    /// Spawn drops from an enemy death.
    /// </summary>
    public void SpawnEnemyDrops(Vector2 position, int gold, int xp)
    {
        // Gold drops (split into multiple if large amount)
        int goldDrops = Math.Clamp(gold / 10 + 1, 1, 5);
        int goldPerDrop = gold / goldDrops;
        for (int i = 0; i < goldDrops; i++)
        {
            SpawnDrop(DropType.Gold, goldPerDrop, position);
        }

        // XP gems (split based on value)
        int xpDrops = Math.Clamp(xp / 15 + 1, 1, 4);
        int xpPerDrop = xp / xpDrops;
        for (int i = 0; i < xpDrops; i++)
        {
            SpawnDrop(DropType.XPGem, xpPerDrop, position);
        }
    }

    /// <summary>
    /// Spawn a single drop.
    /// </summary>
    public void SpawnDrop(DropType type, int value, Vector2 position)
    {
        if (_drops.Count >= MAX_DROPS)
        {
            // Remove oldest drop
            _drops.RemoveAt(0);
        }

        // Add slight position variance
        Vector2 spawnPos = position + new Vector2(
            (Random.Shared.NextSingle() - 0.5f) * 16f,
            -8f
        );

        _drops.Add(new Drop(type, value, spawnPos));
    }

    /// <summary>
    /// Update all drops and check for player collection.
    /// </summary>
    public void Update(float deltaTime, Player player, ChunkManager chunks)
    {
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            var drop = _drops[i];

            // Update with magnet effect
            drop.UpdateWithMagnet(deltaTime, player.Center);
            drop.ApplyMovement(deltaTime, chunks);

            // Check for pickup
            if (drop.IntersectsWith(player.Hitbox))
            {
                CollectDrop(drop, player);
                _drops.RemoveAt(i);
                continue;
            }

            // Remove expired drops
            if (drop.IsExpired)
            {
                _drops.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Collect a drop.
    /// </summary>
    private void CollectDrop(Drop drop, Player player)
    {
        switch (drop.Type)
        {
            case DropType.Gold:
                player.Currency.AddGold(drop.Value);
                break;

            case DropType.XPGem:
                // XP will be handled in Phase 3
                // For now, just fire the event
                break;

            case DropType.HealthOrb:
                // TODO: Heal player
                break;

            case DropType.ManaOrb:
                // TODO: Restore mana
                break;
        }

        OnDropCollected?.Invoke(drop.Type, drop.Value);
        System.Diagnostics.Debug.WriteLine($"Collected {drop.Type}: {drop.Value}");
    }

    /// <summary>
    /// Get all drops for rendering.
    /// </summary>
    public IEnumerable<Drop> GetDrops() => _drops;

    /// <summary>
    /// Clear all drops (for world reset).
    /// </summary>
    public void Clear()
    {
        _drops.Clear();
    }
}