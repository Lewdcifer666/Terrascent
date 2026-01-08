using Microsoft.Xna.Framework;
using Terrascent.Economy;
using Terrascent.Entities.Drops;
using Terrascent.World;

namespace Terrascent.Entities.Enemies;

/// <summary>
/// Manages all enemies including spawning, updating, and cleanup.
/// </summary>
public class EnemyManager
{
    private readonly List<Enemy> _enemies = new();
    private readonly DifficultyManager _difficulty;
    private readonly DropManager _dropManager;
    private readonly Random _random;

    // Spawn settings
    private const int MAX_ENEMIES = 50;
    private const float SPAWN_CHECK_INTERVAL = 2f;  // Check spawn every 2 seconds
    private const float MIN_SPAWN_DISTANCE = 400f;  // Minimum distance from player
    private const float MAX_SPAWN_DISTANCE = 800f;  // Maximum distance from player

    private float _spawnTimer;

    /// <summary>
    /// Event fired when an enemy is killed.
    /// </summary>
    public event Action<Enemy>? OnEnemyKilled;

    /// <summary>
    /// Event fired when player takes damage from an enemy.
    /// </summary>
    public event Action<Enemy, int>? OnPlayerDamaged;

    public int ActiveEnemyCount => _enemies.Count;

    public EnemyManager(DifficultyManager difficulty, DropManager dropManager, int seed)
    {
        _difficulty = difficulty;
        _dropManager = dropManager;
        _random = new Random(seed);
    }

    /// <summary>
    /// Update all enemies.
    /// </summary>
    public void Update(float deltaTime, Player player, ChunkManager chunks)
    {
        // Update spawn timer
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0)
        {
            _spawnTimer = SPAWN_CHECK_INTERVAL;
            TrySpawnEnemy(player, chunks);
        }

        // Update all enemies
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];

            // Remove fully dead enemies (after death animation)
            if (enemy.IsFullyDead)
            {
                _enemies.RemoveAt(i);
                continue;
            }

            // Remove enemies that should despawn (Terraria-style)
            if (enemy.ShouldDespawn && !enemy.IsDead)
            {
                _enemies.RemoveAt(i);
                continue;
            }

            // Update enemy
            enemy.SetTarget(player);
            enemy.Update(deltaTime);
            enemy.ApplyMovement(deltaTime, chunks);

            // Check if enemy attacks hit player (with line of sight check)
            if (!enemy.IsDead && enemy.CanDamageTarget(player.Hitbox))
            {
                // Check line of sight before dealing damage
                if (HasLineOfSight(enemy.Center, player.Center, chunks))
                {
                    DamagePlayer(enemy, player);
                }
            }
        }
    }

    /// <summary>
    /// Check if there's a clear line of sight between two points (no solid tiles blocking).
    /// </summary>
    public bool HasLineOfSight(Vector2 from, Vector2 to, ChunkManager chunks)
    {
        Vector2 direction = to - from;
        float distance = direction.Length();

        if (distance < 1f) return true;

        direction /= distance;  // Normalize

        // Step through tiles along the line
        float stepSize = 8f;  // Check every half tile
        int steps = (int)(distance / stepSize);

        for (int i = 1; i < steps; i++)
        {
            Vector2 checkPoint = from + direction * (i * stepSize);
            Point tilePos = WorldCoordinates.WorldToTile(checkPoint);

            if (chunks.IsSolidAt(tilePos.X, tilePos.Y))
            {
                return false;  // Blocked by solid tile
            }
        }

        return true;
    }

    /// <summary>
    /// Try to spawn a new enemy near the player.
    /// </summary>
    private void TrySpawnEnemy(Player player, ChunkManager chunks)
    {
        if (_enemies.Count >= MAX_ENEMIES) return;

        // Calculate spawn chance based on difficulty
        float baseChance = 0.3f;
        float difficultyBonus = (_difficulty.Coefficient - 1f) * 0.1f;
        float spawnChance = baseChance + difficultyBonus;

        if (_random.NextSingle() > spawnChance) return;

        // Find a valid spawn position
        Vector2? spawnPos = FindSpawnPosition(player, chunks);
        if (!spawnPos.HasValue) return;

        // Determine spawn depth (Y position in tiles)
        int depth = (int)(spawnPos.Value.Y / WorldCoordinates.TILE_SIZE);
        bool isSurface = depth < 50;  // Rough surface threshold

        // Get valid enemies for this location
        var validEnemies = EnemyRegistry.GetSpawnableAt(depth, isSurface).ToList();
        if (validEnemies.Count == 0) return;

        // Weighted random selection
        float totalWeight = validEnemies.Sum(e => e.SpawnWeight);
        float roll = _random.NextSingle() * totalWeight;
        float cumulative = 0f;

        EnemyData? selected = null;
        foreach (var data in validEnemies)
        {
            cumulative += data.SpawnWeight;
            if (roll <= cumulative)
            {
                selected = data;
                break;
            }
        }

        if (selected == null) return;

        // Spawn the enemy
        var enemy = new Enemy(selected.Type, spawnPos.Value, _difficulty, _random.Next());
        _enemies.Add(enemy);
    }

    /// <summary>
    /// Find a valid spawn position for an enemy.
    /// </summary>
    private Vector2? FindSpawnPosition(Player player, ChunkManager chunks)
    {
        // Try multiple times to find a valid position
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Random angle from player
            float angle = _random.NextSingle() * MathF.PI * 2f;
            float distance = MIN_SPAWN_DISTANCE + _random.NextSingle() * (MAX_SPAWN_DISTANCE - MIN_SPAWN_DISTANCE);

            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            Vector2 spawnWorld = player.Center + offset;

            // Convert to tile position
            Point spawnTile = WorldCoordinates.WorldToTile(spawnWorld);

            // Check if position is valid (not in solid tile, has ground below for walkers)
            if (chunks.IsSolidAt(spawnTile.X, spawnTile.Y)) continue;
            if (chunks.IsSolidAt(spawnTile.X, spawnTile.Y - 1)) continue;  // Head space

            // For ground enemies, check for floor
            bool hasFloor = chunks.IsSolidAt(spawnTile.X, spawnTile.Y + 1) ||
                           chunks.IsSolidAt(spawnTile.X, spawnTile.Y + 2);

            // Allow spawn
            return WorldCoordinates.TileToWorld(spawnTile);
        }

        return null;
    }

    /// <summary>
    /// Apply damage to an enemy from a player attack.
    /// Includes line of sight check.
    /// </summary>
    public void DamageEnemy(Rectangle attackBox, int damage, float knockback, Vector2 attackerPosition, ChunkManager chunks)
    {
        foreach (var enemy in _enemies)
        {
            if (enemy.IsDead) continue;
            if (!attackBox.Intersects(enemy.Hitbox)) continue;

            // Check line of sight from attacker to enemy
            if (!HasLineOfSight(attackerPosition, enemy.Center, chunks))
            {
                continue;  // Blocked by wall
            }

            // Calculate knockback direction
            Vector2 knockDir = enemy.Center - attackerPosition;
            if (knockDir.Length() > 0.1f)
                knockDir.Normalize();
            else
                knockDir = Vector2.UnitX;

            bool killed = enemy.TakeDamage(damage, knockDir, knockback);

            if (killed)
            {
                // Spawn drops
                _dropManager.SpawnEnemyDrops(enemy.Center, enemy.GoldReward, enemy.XPReward);
                OnEnemyKilled?.Invoke(enemy);
            }
        }
    }

    /// <summary>
    /// Overload for backwards compatibility - without chunks parameter.
    /// </summary>
    public void DamageEnemy(Rectangle attackBox, int damage, float knockback, Vector2 attackerPosition)
    {
        // This version doesn't check line of sight - use the overload with chunks for that
        foreach (var enemy in _enemies)
        {
            if (enemy.IsDead) continue;
            if (!attackBox.Intersects(enemy.Hitbox)) continue;

            Vector2 knockDir = enemy.Center - attackerPosition;
            if (knockDir.Length() > 0.1f)
                knockDir.Normalize();
            else
                knockDir = Vector2.UnitX;

            bool killed = enemy.TakeDamage(damage, knockDir, knockback);

            if (killed)
            {
                _dropManager.SpawnEnemyDrops(enemy.Center, enemy.GoldReward, enemy.XPReward);
                OnEnemyKilled?.Invoke(enemy);
            }
        }
    }

    /// <summary>
    /// Damage the player from an enemy attack.
    /// </summary>
    private void DamagePlayer(Enemy enemy, Player player)
    {
        // Apply damage to player with knockback
        bool died = player.TakeDamage(enemy.Damage, enemy.Center);

        if (died)
        {
            System.Diagnostics.Debug.WriteLine($"Player killed by {enemy.Data.Name}!");
        }

        OnPlayerDamaged?.Invoke(enemy, enemy.Damage);
    }

    /// <summary>
    /// Manually spawn an enemy (for testing or events).
    /// </summary>
    public Enemy SpawnEnemy(EnemyType type, Vector2 position)
    {
        var enemy = new Enemy(type, position, _difficulty, _random.Next());
        _enemies.Add(enemy);
        return enemy;
    }

    /// <summary>
    /// Get all enemies for rendering.
    /// </summary>
    public IEnumerable<Enemy> GetEnemies() => _enemies;

    /// <summary>
    /// Get enemies within a specific area.
    /// </summary>
    public IEnumerable<Enemy> GetEnemiesInArea(Rectangle area)
    {
        return _enemies.Where(e => area.Intersects(e.Hitbox));
    }

    /// <summary>
    /// Clear all enemies (for world reset).
    /// </summary>
    public void Clear()
    {
        _enemies.Clear();
    }

    /// <summary>
    /// Get enemy count by type.
    /// </summary>
    public int CountByType(EnemyType type)
    {
        return _enemies.Count(e => e.EnemyType == type && !e.IsDead);
    }
}