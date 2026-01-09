using Microsoft.Xna.Framework;
using Terrascent.Progression;
using Terrascent.World;

namespace Terrascent.Entities.Drops;

/// <summary>
/// Manages all drops in the world (gold, XP, etc.).
/// </summary>
public class DropManager
{
    private readonly List<Drop> _drops = new();
    private const int MAX_DROPS = 500;  // Prevent too many drops

    // XP System reference
    private XPSystem? _xpSystem;

    /// <summary>
    /// Event fired when a drop is collected.
    /// </summary>
    public event Action<DropType, int>? OnDropCollected;

    /// <summary>
    /// Event fired when XP is gained (for UI feedback).
    /// </summary>
    public event Action<int, Vector2>? OnXPCollected;

    /// <summary>
    /// Set the XP system reference for XP gem collection.
    /// </summary>
    public void SetXPSystem(XPSystem xpSystem)
    {
        _xpSystem = xpSystem;
    }

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

        // XP gems - use tier system to determine drop count
        SpawnXPDrops(position, xp);
    }

    /// <summary>
    /// Spawn XP drops with intelligent splitting based on value.
    /// Larger XP values spawn fewer, bigger gems.
    /// </summary>
    private void SpawnXPDrops(Vector2 position, int totalXP)
    {
        if (totalXP <= 0) return;

        // Determine optimal gem distribution
        // Small XP: many tiny gems (satisfying pickup)
        // Large XP: fewer large gems (less clutter)

        if (totalXP <= 10)
        {
            // Single tiny gem
            SpawnDrop(DropType.XPGem, totalXP, position);
        }
        else if (totalXP <= 30)
        {
            // 2-3 small gems
            int count = 2 + (totalXP > 20 ? 1 : 0);
            int perGem = totalXP / count;
            for (int i = 0; i < count; i++)
            {
                SpawnDrop(DropType.XPGem, perGem, position);
            }
        }
        else if (totalXP <= 60)
        {
            // 1 medium + 1-2 small
            int mediumValue = totalXP / 2;
            int remainder = totalXP - mediumValue;
            SpawnDrop(DropType.XPGem, mediumValue, position);

            int smallCount = Math.Clamp(remainder / 10, 1, 2);
            int smallValue = remainder / smallCount;
            for (int i = 0; i < smallCount; i++)
            {
                SpawnDrop(DropType.XPGem, smallValue, position);
            }
        }
        else if (totalXP <= 100)
        {
            // 1 large + 1 small
            int largeValue = (int)(totalXP * 0.7f);
            int smallValue = totalXP - largeValue;
            SpawnDrop(DropType.XPGem, largeValue, position);
            SpawnDrop(DropType.XPGem, smallValue, position);
        }
        else
        {
            // 1 huge gem
            SpawnDrop(DropType.XPGem, totalXP, position);
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
                if (_xpSystem != null)
                {
                    bool leveledUp = _xpSystem.AddXP(drop.Value);
                    OnXPCollected?.Invoke(drop.Value, drop.Center);

                    if (leveledUp)
                    {
                        System.Diagnostics.Debug.WriteLine($"Player reached level {_xpSystem.Level}!");
                    }
                }
                break;

            case DropType.HealthOrb:
                player.Heal(drop.Value);
                break;

            case DropType.ManaOrb:
                // TODO: Restore mana when mana system is implemented
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

    /// <summary>
    /// Get count of drops by type.
    /// </summary>
    public int CountByType(DropType type)
    {
        return _drops.Count(d => d.Type == type);
    }
}