using Microsoft.Xna.Framework;
using Terrascent.World;

namespace Terrascent.World.Biomes;

/// <summary>
/// Manages biome detection, spreading, and environmental effects.
/// Uses Terraria-accurate detection with 169x124 tile rectangle (84 left/right, 62 above, 61 below).
/// </summary>
public class BiomeManager
{
    private readonly ChunkManager _chunks;
    private readonly Random _random;
    private WorldConfig? _worldConfig;

    // Spreading settings
    private const float SPREAD_CHECK_INTERVAL = 1.0f;
    private const int SPREAD_ATTEMPTS_PER_CHUNK = 5;
    private float _spreadTimer;

    // Hardmode state
    private bool _isHardmode;
    private bool _postPlantera;  // Spread rate reduced by 50% after Plantera

    // Detection cache
    private BiomeType _cachedBiome = BiomeType.Forest;
    private Point _cachedPosition;
    private bool _cacheValid = false;
    private const int CACHE_RADIUS = 5;

    /// <summary>Event fired when the local biome changes.</summary>
    public event Action<BiomeType, BiomeType>? OnBiomeChanged;

    /// <summary>Event fired when a tile is converted by spreading.</summary>
    public event Action<Point, TileType, TileType>? OnTileConverted;

    public BiomeManager(ChunkManager chunks, int seed)
    {
        _chunks = chunks;
        _random = new Random(seed + 5000);
        _spreadTimer = SPREAD_CHECK_INTERVAL;
    }

    /// <summary>Set the world configuration for layer-based detection.</summary>
    public void SetWorldConfig(WorldConfig config)
    {
        _worldConfig = config;
    }

    /// <summary>Update biome spreading and effects.</summary>
    public void Update(float deltaTime, Point playerTilePos)
    {
        _spreadTimer -= deltaTime;
        if (_spreadTimer <= 0)
        {
            _spreadTimer = SPREAD_CHECK_INTERVAL;
            ProcessSpreading(playerTilePos);
        }
    }

    /// <summary>Enable hardmode, doubling spread rate.</summary>
    public void EnableHardmode()
    {
        _isHardmode = true;
        System.Diagnostics.Debug.WriteLine("HARDMODE ENABLED - Hallow biome can now spread! Evil biomes spread faster.");
    }

    /// <summary>Mark Plantera as defeated, reducing spread rate by 50%.</summary>
    public void SetPostPlantera()
    {
        _postPlantera = true;
        System.Diagnostics.Debug.WriteLine("POST-PLANTERA - Biome spread rate reduced by 50%.");
    }

    /// <summary>Detect the current biome at a position.</summary>
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
    /// Calculate the biome at a position using Terraria-accurate detection.
    /// Uses 169x124 tile rectangle (84 left/right, 62 above, 61 below).
    /// </summary>
    private BiomeType CalculateBiome(Point center)
    {
        // Get current layer
        WorldLayer currentLayer = WorldLayer.Surface;
        if (_worldConfig != null)
        {
            currentLayer = _worldConfig.GetLayerAt(center.Y);
        }
        else
        {
            currentLayer = GetFallbackLayer(center.Y);
        }

        // Check position-based biomes first

        // Space biome (position-based)
        if (currentLayer == WorldLayer.Space)
        {
            return BiomeType.Space;
        }

        // Underworld biome (position-based + ash/hellstone tiles)
        if (currentLayer == WorldLayer.Underworld)
        {
            int underworldTiles = CountTilesInDetectionArea(center, TileType.Ash, TileType.Hellstone, TileType.Obsidian);
            if (underworldTiles >= BiomeTileThresholds.Underworld)
                return BiomeType.Underworld;
        }

        // Ocean biome (position-based)
        if (_worldConfig != null && _worldConfig.IsOceanArea(center.X) &&
            (currentLayer == WorldLayer.Surface || currentLayer == WorldLayer.Underground))
        {
            return BiomeType.Ocean;
        }

        // Count tiles for tile-based biomes
        // Priority order: Meteorite > Dungeon > Evil > Hallow > Mushroom > Snow > Jungle > Desert > Forest

        Dictionary<BiomeType, int> biomeCounts = new();

        foreach (var biomeData in BiomeRegistry.GetByPriority())
        {
            if (biomeData.RequiresHardmode && !_isHardmode)
                continue;

            if (biomeData.IsPositionBased)
                continue;

            if (!biomeData.ValidLayers.Contains(currentLayer))
                continue;

            int count = 0;
            if (biomeData.AssociatedTiles.Length > 0)
            {
                count = CountTilesInDetectionArea(center, biomeData.AssociatedTiles);
            }

            if (count >= biomeData.MinTileCount)
            {
                biomeCounts[biomeData.Type] = count;
            }
        }

        // Handle Hallow vs Evil biome priority (they counter each other)
        if (biomeCounts.ContainsKey(BiomeType.Hallow) &&
            (biomeCounts.ContainsKey(BiomeType.Corruption) || biomeCounts.ContainsKey(BiomeType.Crimson)))
        {
            int hallowCount = biomeCounts.GetValueOrDefault(BiomeType.Hallow, 0);
            int corruptCount = biomeCounts.GetValueOrDefault(BiomeType.Corruption, 0);
            int crimsonCount = biomeCounts.GetValueOrDefault(BiomeType.Crimson, 0);
            int evilCount = Math.Max(corruptCount, crimsonCount);

            if (hallowCount > evilCount)
            {
                biomeCounts.Remove(BiomeType.Corruption);
                biomeCounts.Remove(BiomeType.Crimson);
            }
            else
            {
                biomeCounts.Remove(BiomeType.Hallow);
            }
        }

        // Return highest priority biome that meets threshold
        if (biomeCounts.Count > 0)
        {
            foreach (var biomeData in BiomeRegistry.GetByPriority())
            {
                if (biomeCounts.ContainsKey(biomeData.Type))
                {
                    // Return underground variant if in underground/cavern layer
                    if (currentLayer == WorldLayer.Underground || currentLayer == WorldLayer.Cavern)
                    {
                        return BiomeRegistry.GetUndergroundVariant(biomeData.Type);
                    }
                    return biomeData.Type;
                }
            }
        }

        // Default biomes based on layer
        return currentLayer switch
        {
            WorldLayer.Space => BiomeType.Space,
            WorldLayer.Surface => BiomeType.Forest,
            WorldLayer.Underground => BiomeType.Underground,
            WorldLayer.Cavern => BiomeType.Underground,
            WorldLayer.Underworld => BiomeType.Underworld,
            _ => BiomeType.Forest
        };
    }

    /// <summary>
    /// Count tiles in Terraria's detection area (169x124: 84 left/right, 62 above, 61 below).
    /// </summary>
    private int CountTilesInDetectionArea(Point center, params TileType[] tileTypes)
    {
        if (tileTypes == null || tileTypes.Length == 0)
            return 0;

        HashSet<TileType> targetTypes = new(tileTypes);
        int count = 0;

        // Terraria detection area: 84 tiles left, 84 tiles right (169 wide)
        //                          62 tiles above, 61 tiles below (124 tall)
        for (int dy = -WorldConfig.BIOME_DETECT_ABOVE; dy <= WorldConfig.BIOME_DETECT_BELOW; dy++)
        {
            for (int dx = -WorldConfig.BIOME_DETECT_RADIUS_X; dx <= WorldConfig.BIOME_DETECT_RADIUS_X; dx++)
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

    /// <summary>Fallback layer detection when WorldConfig is not set.</summary>
    private WorldLayer GetFallbackLayer(int worldY)
    {
        if (worldY < 100)
            return WorldLayer.Space;
        if (worldY < 150)
            return WorldLayer.Surface;
        if (worldY < 300)
            return WorldLayer.Underground;
        if (worldY < 500)
            return WorldLayer.Cavern;
        return WorldLayer.Underworld;
    }

    /// <summary>Get the biome at a specific tile position (uncached, for spawning).</summary>
    public BiomeType GetBiomeAt(Point tilePosition)
    {
        return CalculateBiome(tilePosition);
    }

    /// <summary>Get the current world layer at a position.</summary>
    public WorldLayer GetLayerAt(int worldY)
    {
        if (_worldConfig != null)
            return _worldConfig.GetLayerAt(worldY);
        return GetFallbackLayer(worldY);
    }

    /// <summary>Process biome spreading for loaded chunks.</summary>
    private void ProcessSpreading(Point playerTilePos)
    {
        var loadedChunks = _chunks.GetLoadedChunks();
        if (loadedChunks == null || !loadedChunks.Any())
            return;

        foreach (var chunk in loadedChunks)
        {
            ProcessChunkSpreading(chunk);
        }
    }

    /// <summary>Process spreading for a single chunk.</summary>
    private void ProcessChunkSpreading(Chunk chunk)
    {
        int chunkWorldX = chunk.Position.X * Chunk.SIZE;
        int chunkWorldY = chunk.Position.Y * Chunk.SIZE;

        // Spread rate multiplier based on game state
        float spreadMultiplier = 1.0f;
        if (_isHardmode) spreadMultiplier = 2.0f;
        if (_postPlantera) spreadMultiplier *= 0.5f;

        int attempts = (int)(SPREAD_ATTEMPTS_PER_CHUNK * spreadMultiplier);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int localX = _random.Next(Chunk.SIZE);
            int localY = _random.Next(Chunk.SIZE);
            int worldX = chunkWorldX + localX;
            int worldY = chunkWorldY + localY;

            var tile = _chunks.GetTileAt(worldX, worldY);
            if (tile.Type == TileType.Air)
                continue;

            TrySpreadFrom(worldX, worldY, tile.Type);
        }
    }

    /// <summary>Attempt to spread from a source tile.</summary>
    private void TrySpreadFrom(int sourceX, int sourceY, TileType sourceTile)
    {
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

        // Random chance check (affected by spread rate)
        float effectiveChance = sourceBiome.SpreadChance;
        if (_isHardmode) effectiveChance *= 2.0f;
        if (_postPlantera) effectiveChance *= 0.5f;

        if (_random.NextSingle() > effectiveChance)
            return;

        // Pick random adjacent tile within spread radius (Terraria uses 3 tiles)
        int radius = sourceBiome.SpreadRadius;
        int dx = _random.Next(-radius, radius + 1);
        int dy = _random.Next(-radius, radius + 1);
        if (dx == 0 && dy == 0) return;

        int targetX = sourceX + dx;
        int targetY = sourceY + dy;

        var targetTile = _chunks.GetTileAt(targetX, targetY);
        if (targetTile.Type == TileType.Air)
            return;

        if (sourceBiome.SpreadConversions.TryGetValue(targetTile.Type, out var newType))
        {
            if (newType == targetTile.Type)
                return;

            _chunks.SetTileAt(targetX, targetY, new Tile(newType));
            OnTileConverted?.Invoke(new Point(targetX, targetY), targetTile.Type, newType);
        }
    }

    // === Biome Properties ===

    public float GetSpawnRateMultiplier(BiomeType biome) => BiomeRegistry.Get(biome).SpawnRateMultiplier;
    public float GetGoldMultiplier(BiomeType biome) => BiomeRegistry.Get(biome).GoldMultiplier;
    public float GetXPMultiplier(BiomeType biome) => BiomeRegistry.Get(biome).XPMultiplier;
    public float GetRareDropMultiplier(BiomeType biome) => BiomeRegistry.Get(biome).RareDropMultiplier;
    public int GetDangerLevel(BiomeType biome) => BiomeRegistry.Get(biome).DangerLevel;

    /// <summary>Get tile counts for debugging.</summary>
    public Dictionary<TileType, int> GetBiomeTileCounts(Point center)
    {
        Dictionary<TileType, int> counts = new();

        for (int dy = -WorldConfig.BIOME_DETECT_ABOVE; dy <= WorldConfig.BIOME_DETECT_BELOW; dy++)
        {
            for (int dx = -WorldConfig.BIOME_DETECT_RADIUS_X; dx <= WorldConfig.BIOME_DETECT_RADIUS_X; dx++)
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

    /// <summary>Purify an area (convert evil/hallow tiles back to normal).</summary>
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

    /// <summary>Corrupt an area (spread corruption).</summary>
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

    /// <summary>Hallow an area (spread hallow).</summary>
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

    // === State Properties ===

    public bool IsHardmode => _isHardmode;
    public bool IsPostPlantera => _postPlantera;
    public BiomeType CurrentBiome => _cachedBiome;
    public WorldConfig? WorldConfig => _worldConfig;
}