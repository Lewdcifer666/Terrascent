using Microsoft.Xna.Framework;

namespace Terrascent.World.Biomes;

/// <summary>
/// Manages biome detection, spreading, and environmental effects.
/// </summary>
public class BiomeManager
{
    private readonly ChunkManager _chunks;
    private readonly Random _random;

    // Spreading settings
    private const float SPREAD_CHECK_INTERVAL = 1.0f;  // Check every 1 second
    private const int SPREAD_ATTEMPTS_PER_CHUNK = 5;   // Spread attempts per loaded chunk
    private float _spreadTimer;

    // Hardmode state
    private bool _isHardmode;

    // Detection cache (for performance)
    private BiomeType _cachedBiome = BiomeType.Forest;
    private Point _cachedPosition;
    private bool _cacheValid = false;  // Use flag instead of MinValue sentinel
    private const int CACHE_RADIUS = 5;  // Recalculate if player moves this many tiles

    /// <summary>
    /// Event fired when the local biome changes.
    /// </summary>
    public event Action<BiomeType, BiomeType>? OnBiomeChanged;

    /// <summary>
    /// Event fired when a tile is converted by spreading.
    /// </summary>
    public event Action<Point, TileType, TileType>? OnTileConverted;

    public BiomeManager(ChunkManager chunks, int seed)
    {
        _chunks = chunks;
        _random = new Random(seed + 5000);
        _spreadTimer = SPREAD_CHECK_INTERVAL;
    }

    /// <summary>
    /// Update biome spreading and effects.
    /// </summary>
    public void Update(float deltaTime, Point playerTilePos)
    {
        // Update spreading timer
        _spreadTimer -= deltaTime;
        if (_spreadTimer <= 0)
        {
            _spreadTimer = SPREAD_CHECK_INTERVAL;
            ProcessSpreading(playerTilePos);
        }
    }

    /// <summary>
    /// Enable hardmode, allowing Hallow biome to spread.
    /// </summary>
    public void EnableHardmode()
    {
        _isHardmode = true;
        System.Diagnostics.Debug.WriteLine("HARDMODE ENABLED - Hallow biome can now spread!");
    }

    /// <summary>
    /// Detect the current biome at a position.
    /// </summary>
    public BiomeType DetectBiome(Point tilePosition)
    {
        // Check cache first
        if (_cacheValid)
        {
            int dx = Math.Abs(tilePosition.X - _cachedPosition.X);
            int dy = Math.Abs(tilePosition.Y - _cachedPosition.Y);
            if (dx < CACHE_RADIUS && dy < CACHE_RADIUS)
            {
                return _cachedBiome;
            }
        }

        BiomeType newBiome = CalculateBiome(tilePosition);

        // Fire event if biome changed
        if (newBiome != _cachedBiome || !_cacheValid)
        {
            var oldBiome = _cachedBiome;
            _cachedBiome = newBiome;
            _cachedPosition = tilePosition;
            _cacheValid = true;

            if (oldBiome != newBiome)
            {
                OnBiomeChanged?.Invoke(oldBiome, newBiome);
            }
        }
        else
        {
            _cachedPosition = tilePosition;
        }

        return newBiome;
    }

    /// <summary>
    /// Calculate the biome at a position by counting nearby tiles.
    /// </summary>
    private BiomeType CalculateBiome(Point center)
    {
        int depth = center.Y;

        // Check special depth-based biomes first
        if (depth < -100)
        {
            return BiomeType.Space;
        }

        if (depth > 400)
        {
            // Check for underworld tiles
            int ashCount = CountTilesInRadius(center, 42, TileType.Ash, TileType.Hellstone);
            if (ashCount >= 50)
                return BiomeType.Underworld;
        }

        // Count tiles for each biome
        Dictionary<BiomeType, int> biomeCounts = new();

        foreach (var biomeData in BiomeRegistry.GetByPriority())
        {
            // Skip hardmode biomes if not in hardmode
            if (biomeData.RequiresHardmode && !_isHardmode)
                continue;

            // Check depth requirements
            if (depth < biomeData.MinDepth || depth > biomeData.MaxDepth)
                continue;

            // Count associated tiles
            int count = CountTilesInRadius(center, biomeData.DetectionRadius, biomeData.AssociatedTiles);

            if (count >= biomeData.MinTileCount)
            {
                biomeCounts[biomeData.Type] = count;
            }
        }

        // Return highest priority biome that meets threshold
        if (biomeCounts.Count > 0)
        {
            // Get highest priority biome (already sorted by priority)
            foreach (var biomeData in BiomeRegistry.GetByPriority())
            {
                if (biomeCounts.ContainsKey(biomeData.Type))
                    return biomeData.Type;
            }
        }

        // Default biomes based on depth
        if (depth >= 50)
            return BiomeType.Underground;

        return BiomeType.Forest;
    }

    /// <summary>
    /// Count specific tile types within a radius.
    /// </summary>
    private int CountTilesInRadius(Point center, int radius, params TileType[] tileTypes)
    {
        if (tileTypes == null || tileTypes.Length == 0)
            return 0;

        HashSet<TileType> targetTypes = new(tileTypes);
        int count = 0;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int x = center.X + dx;
                int y = center.Y + dy;

                var tile = _chunks.GetTileAt(x, y);
                if (targetTypes.Contains(tile.Type))
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Get the biome at a specific tile position (uncached, for spawning).
    /// </summary>
    public BiomeType GetBiomeAt(Point tilePosition)
    {
        return CalculateBiome(tilePosition);
    }

    /// <summary>
    /// Process biome spreading for loaded chunks.
    /// </summary>
    private void ProcessSpreading(Point playerTilePos)
    {
        // Get loaded chunks near player
        var loadedChunks = _chunks.GetLoadedChunks();
        if (loadedChunks == null || !loadedChunks.Any())
            return;

        foreach (var chunk in loadedChunks)
        {
            ProcessChunkSpreading(chunk);
        }
    }

    /// <summary>
    /// Process spreading for a single chunk.
    /// </summary>
    private void ProcessChunkSpreading(Chunk chunk)
    {
        int chunkWorldX = chunk.Position.X * Chunk.SIZE;
        int chunkWorldY = chunk.Position.Y * Chunk.SIZE;

        for (int attempt = 0; attempt < SPREAD_ATTEMPTS_PER_CHUNK; attempt++)
        {
            // Pick random tile in chunk
            int localX = _random.Next(Chunk.SIZE);
            int localY = _random.Next(Chunk.SIZE);
            int worldX = chunkWorldX + localX;
            int worldY = chunkWorldY + localY;

            var tile = _chunks.GetTileAt(worldX, worldY);
            if (tile.Type == TileType.Air)
                continue;

            // Check if this tile belongs to a spreading biome
            TrySpreadFrom(worldX, worldY, tile.Type);
        }
    }

    /// <summary>
    /// Attempt to spread from a source tile.
    /// </summary>
    private void TrySpreadFrom(int sourceX, int sourceY, TileType sourceTile)
    {
        // Find which spreading biome this tile belongs to
        BiomeData? sourceBiome = null;
        foreach (var biome in BiomeRegistry.GetSpreadingBiomes())
        {
            if (biome.RequiresHardmode && !_isHardmode)
                continue;

            if (biome.AssociatedTiles.Contains(sourceTile))
            {
                sourceBiome = biome;
                break;
            }
        }

        if (sourceBiome == null)
            return;

        // Random chance check
        if (_random.NextSingle() > sourceBiome.SpreadChance)
            return;

        // Pick random adjacent tile within spread radius
        int radius = sourceBiome.SpreadRadius;
        int dx = _random.Next(-radius, radius + 1);
        int dy = _random.Next(-radius, radius + 1);
        if (dx == 0 && dy == 0) return;

        int targetX = sourceX + dx;
        int targetY = sourceY + dy;

        var targetTile = _chunks.GetTileAt(targetX, targetY);
        if (targetTile.Type == TileType.Air)
            return;

        // Check if we can convert this tile
        if (sourceBiome.SpreadConversions.TryGetValue(targetTile.Type, out var newType))
        {
            // Don't convert to the same type
            if (newType == targetTile.Type)
                return;

            // Convert the tile
            _chunks.SetTileAt(targetX, targetY, new Tile(newType));
            OnTileConverted?.Invoke(new Point(targetX, targetY), targetTile.Type, newType);
        }
    }

    /// <summary>
    /// Get spawn rate multiplier for current biome.
    /// </summary>
    public float GetSpawnRateMultiplier(BiomeType biome)
    {
        return BiomeRegistry.Get(biome).SpawnRateMultiplier;
    }

    /// <summary>
    /// Get gold drop multiplier for current biome.
    /// </summary>
    public float GetGoldMultiplier(BiomeType biome)
    {
        return BiomeRegistry.Get(biome).GoldMultiplier;
    }

    /// <summary>
    /// Get XP multiplier for current biome.
    /// </summary>
    public float GetXPMultiplier(BiomeType biome)
    {
        return BiomeRegistry.Get(biome).XPMultiplier;
    }

    /// <summary>
    /// Get rare drop multiplier for current biome.
    /// </summary>
    public float GetRareDropMultiplier(BiomeType biome)
    {
        return BiomeRegistry.Get(biome).RareDropMultiplier;
    }

    /// <summary>
    /// Get danger level for current biome.
    /// </summary>
    public int GetDangerLevel(BiomeType biome)
    {
        return BiomeRegistry.Get(biome).DangerLevel;
    }

    /// <summary>
    /// Get tile counts for debugging.
    /// </summary>
    public Dictionary<TileType, int> GetBiomeTileCounts(Point center, int radius = 42)
    {
        Dictionary<TileType, int> counts = new();

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int x = center.X + dx;
                int y = center.Y + dy;

                var tile = _chunks.GetTileAt(x, y);
                if (tile.Type == TileType.Air) continue;

                if (!counts.ContainsKey(tile.Type))
                    counts[tile.Type] = 0;
                counts[tile.Type]++;
            }
        }

        return counts;
    }

    /// <summary>
    /// Purify an area (convert evil tiles back to normal).
    /// </summary>
    public int PurifyArea(Point center, int radius)
    {
        int converted = 0;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int x = center.X + dx;
                int y = center.Y + dy;

                var tile = _chunks.GetTileAt(x, y);
                TileType newType = tile.Type switch
                {
                    TileType.CorruptGrass or TileType.CrimsonGrass => TileType.Grass,
                    TileType.Ebonstone or TileType.Crimstone => TileType.Stone,
                    TileType.CorruptSand or TileType.CrimsonSand => TileType.Sand,
                    TileType.CorruptSandstone or TileType.CrimsonSandstone => TileType.Sandstone,
                    TileType.CorruptIce or TileType.CrimsonIce => TileType.Ice,
                    TileType.HallowedGrass => TileType.Grass,
                    TileType.Pearlstone => TileType.Stone,
                    TileType.HallowedSand => TileType.Sand,
                    TileType.HallowedSandstone => TileType.Sandstone,
                    TileType.HallowedIce => TileType.Ice,
                    _ => tile.Type
                };

                if (newType != tile.Type)
                {
                    _chunks.SetTileAt(x, y, new Tile(newType));
                    converted++;
                }
            }
        }

        return converted;
    }

    /// <summary>
    /// Corrupt an area (spread corruption).
    /// </summary>
    public int CorruptArea(Point center, int radius)
    {
        int converted = 0;
        var corruptionData = BiomeRegistry.Get(BiomeType.Corruption);

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int x = center.X + dx;
                int y = center.Y + dy;

                var tile = _chunks.GetTileAt(x, y);
                if (corruptionData.SpreadConversions.TryGetValue(tile.Type, out var newType))
                {
                    if (newType != tile.Type)
                    {
                        _chunks.SetTileAt(x, y, new Tile(newType));
                        converted++;
                    }
                }
            }
        }

        return converted;
    }

    /// <summary>
    /// Hallow an area (spread hallow).
    /// </summary>
    public int HallowArea(Point center, int radius)
    {
        if (!_isHardmode)
        {
            System.Diagnostics.Debug.WriteLine("Cannot spread Hallow - Hardmode not enabled!");
            return 0;
        }

        int converted = 0;
        var hallowData = BiomeRegistry.Get(BiomeType.Hallow);

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int x = center.X + dx;
                int y = center.Y + dy;

                var tile = _chunks.GetTileAt(x, y);
                if (hallowData.SpreadConversions.TryGetValue(tile.Type, out var newType))
                {
                    if (newType != tile.Type)
                    {
                        _chunks.SetTileAt(x, y, new Tile(newType));
                        converted++;
                    }
                }
            }
        }

        return converted;
    }

    /// <summary>
    /// Check if hardmode is enabled.
    /// </summary>
    public bool IsHardmode => _isHardmode;

    /// <summary>
    /// Get the current cached biome.
    /// </summary>
    public BiomeType CurrentBiome => _cachedBiome;
}