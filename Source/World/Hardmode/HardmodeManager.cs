using Microsoft.Xna.Framework;
using Terrascent.World.Biomes;
using Terrascent.World.Generation;

namespace Terrascent.World.Hardmode;

/// <summary>
/// Manages the Hardmode state and world transformation.
/// Hardmode is triggered by defeating the Wall of Shadows, transforming the world with:
/// - New ores (Cobalt, Mythril, Adamantite)
/// - The Hallow biome spawning as a stripe across the world
/// - Evil biome expansion/spreading
/// - New enemy spawns and boss availability
/// </summary>
public class HardmodeManager
{
    private readonly ChunkManager _chunkManager;
    private readonly WorldGenerator _worldGenerator;
    private readonly BiomeManager _biomeManager;
    private readonly Random _random;

    private bool _isHardmode;
    private bool _hasTransformed;

    // Transformation progress tracking
    private bool _isTransforming;
    private int _transformationStep;
    private const int TOTAL_TRANSFORMATION_STEPS = 5;

    // Ore generation settings
    public const int COBALT_SMASH_COUNT = 12;      // Altars smashed for cobalt
    public const int MYTHRIL_SMASH_COUNT = 12;    // Additional for mythril
    public const int ADAMANTITE_SMASH_COUNT = 12; // Additional for adamantite

    // Events
    public event Action? OnHardmodeActivated;
    public event Action<string>? OnTransformationProgress;
    public event Action? OnTransformationComplete;

    /// <summary>
    /// True if the world is in Hardmode.
    /// </summary>
    public bool IsHardmode => _isHardmode;

    /// <summary>
    /// True if the hardmode transformation has been performed.
    /// </summary>
    public bool HasTransformed => _hasTransformed;

    /// <summary>
    /// True if transformation is currently in progress.
    /// </summary>
    public bool IsTransforming => _isTransforming;

    /// <summary>
    /// Current transformation progress (0.0 to 1.0).
    /// </summary>
    public float TransformationProgress => _transformationStep / (float)TOTAL_TRANSFORMATION_STEPS;

    public HardmodeManager(ChunkManager chunkManager, WorldGenerator worldGenerator, BiomeManager biomeManager, int seed)
    {
        _chunkManager = chunkManager;
        _worldGenerator = worldGenerator;
        _biomeManager = biomeManager;
        _random = new Random(seed + 9999);
    }

    /// <summary>
    /// Activate Hardmode and begin world transformation.
    /// Called when Wall of Shadows is defeated.
    /// </summary>
    public void ActivateHardmode()
    {
        if (_isHardmode)
        {
            System.Diagnostics.Debug.WriteLine("[HARDMODE] Already in Hardmode!");
            return;
        }

        _isHardmode = true;
        _isTransforming = true;
        _transformationStep = 0;

        System.Diagnostics.Debug.WriteLine("=== HARDMODE ACTIVATED ===");
        System.Diagnostics.Debug.WriteLine("The ancient spirits of light and dark have been released...");

        OnHardmodeActivated?.Invoke();

        // Begin transformation
        PerformWorldTransformation();
    }

    /// <summary>
    /// Perform the world transformation over multiple steps.
    /// In a real implementation, this might be spread across multiple frames.
    /// </summary>
    private void PerformWorldTransformation()
    {
        if (!_worldGenerator.Config.IsHardmode)
        {
            // Update config first
            _worldGenerator.Config.SetHardmode(true);
        }

        // Step 1: Generate initial hardmode ores
        _transformationStep = 1;
        OnTransformationProgress?.Invoke("Generating Cobalt Ore deposits...");
        GenerateHardmodeOres(TileType.CobaltOre, 0.3f, 200);
        System.Diagnostics.Debug.WriteLine("[HARDMODE] Step 1: Cobalt ore generated");

        // Step 2: Generate more ores
        _transformationStep = 2;
        OnTransformationProgress?.Invoke("Generating Mythril Ore deposits...");
        GenerateHardmodeOres(TileType.MythrilOre, 0.4f, 150);
        System.Diagnostics.Debug.WriteLine("[HARDMODE] Step 2: Mythril ore generated");

        // Step 3: Generate rare ores
        _transformationStep = 3;
        OnTransformationProgress?.Invoke("Generating Adamantite Ore deposits...");
        GenerateHardmodeOres(TileType.AdamantiteOre, 0.6f, 100);
        System.Diagnostics.Debug.WriteLine("[HARDMODE] Step 3: Adamantite ore generated");

        // Step 4: Generate Hallow biome
        _transformationStep = 4;
        OnTransformationProgress?.Invoke("The Hallow is spreading...");
        GenerateHallowBiome();
        System.Diagnostics.Debug.WriteLine("[HARDMODE] Step 4: Hallow biome generated");

        // Step 5: Expand evil biome
        _transformationStep = 5;
        OnTransformationProgress?.Invoke("The corruption/crimson grows stronger...");
        ExpandEvilBiome();
        System.Diagnostics.Debug.WriteLine("[HARDMODE] Step 5: Evil biome expanded");

        // Complete transformation
        _hasTransformed = true;
        _isTransforming = false;

        System.Diagnostics.Debug.WriteLine("=== HARDMODE TRANSFORMATION COMPLETE ===");
        OnTransformationComplete?.Invoke();
    }

    /// <summary>
    /// Generate hardmode ores throughout the world.
    /// </summary>
    private void GenerateHardmodeOres(TileType oreType, float minDepthPercent, int nodeCount)
    {
        var config = _worldGenerator.Config;

        int minY = (int)(config.Height * minDepthPercent);
        int maxY = config.UnderworldBoundary - 50;  // Stop before underworld

        var perlin = new PerlinNoise(_random.Next());

        for (int i = 0; i < nodeCount; i++)
        {
            // Random position
            int centerX = _random.Next(100, config.Width - 100);
            int centerY = _random.Next(minY, maxY);

            // Generate ore cluster
            int clusterSize = _random.Next(5, 15);
            GenerateOreCluster(centerX, centerY, oreType, clusterSize, perlin);
        }
    }

    /// <summary>
    /// Generate a cluster of ore tiles at a position.
    /// </summary>
    private void GenerateOreCluster(int centerX, int centerY, TileType oreType, int size, PerlinNoise noise)
    {
        for (int i = 0; i < size; i++)
        {
            // Use noise for organic cluster shape
            float angle = _random.NextSingle() * MathF.PI * 2f;
            float distance = _random.NextSingle() * (size / 2f + 1);

            int x = centerX + (int)(MathF.Cos(angle) * distance);
            int y = centerY + (int)(MathF.Sin(angle) * distance);

            // Get the chunk
            int chunkX = x / Chunk.SIZE;
            int chunkY = y / Chunk.SIZE;

            var chunk = _chunkManager.GetChunk(chunkX, chunkY);
            if (chunk == null) continue;

            int localX = x - chunkX * Chunk.SIZE;
            int localY = y - chunkY * Chunk.SIZE;

            if (localX < 0 || localX >= Chunk.SIZE || localY < 0 || localY >= Chunk.SIZE)
                continue;

            ref var tile = ref chunk.GetTile(localX, localY);

            // Only replace stone/dirt
            if (tile.Type == TileType.Stone || tile.Type == TileType.Dirt)
            {
                tile = new Tile(oreType);
                chunk.MarkDirty();
            }
        }
    }

    /// <summary>
    /// Generate the Hallow biome as a diagonal stripe across the world.
    /// </summary>
    private void GenerateHallowBiome()
    {
        var config = _worldGenerator.Config;

        // Hallow spawns opposite to the evil biome
        bool hallowOnLeft = !config.JungleOnLeft;  // Opposite of jungle side typically

        // Calculate stripe parameters
        int stripeWidth = config.Width / 8;  // ~12.5% of world width
        int startX, endX;

        if (hallowOnLeft)
        {
            startX = config.Width / 4 - stripeWidth / 2;
            endX = startX + stripeWidth;
        }
        else
        {
            startX = config.Width * 3 / 4 - stripeWidth / 2;
            endX = startX + stripeWidth;
        }

        System.Diagnostics.Debug.WriteLine($"[HARDMODE] Generating Hallow from X={startX} to X={endX}");

        // Generate the Hallow stripe
        var perlin = new PerlinNoise(_random.Next());

        for (int worldX = startX; worldX < endX; worldX++)
        {
            // Add some noise to the edges for natural look
            float edgeNoise = perlin.Noise(worldX * 0.02f, 0) * 20;
            int effectiveX = worldX + (int)edgeNoise;

            if (effectiveX < startX || effectiveX >= endX)
                continue;

            // Convert tiles from surface to underworld
            for (int worldY = config.SurfaceLevel - 30; worldY < config.UnderworldBoundary; worldY++)
            {
                // Add vertical variation
                float vNoise = perlin.Noise(worldX * 0.03f, worldY * 0.03f);
                if (Math.Abs(worldX - (startX + endX) / 2) > stripeWidth / 2 - 10 && vNoise < -0.3f)
                    continue;

                HallowifyTile(effectiveX, worldY);
            }
        }
    }

    /// <summary>
    /// Convert a tile to its Hallow variant.
    /// </summary>
    private void HallowifyTile(int worldX, int worldY)
    {
        int chunkX = worldX / Chunk.SIZE;
        int chunkY = worldY / Chunk.SIZE;

        var chunk = _chunkManager.GetChunk(chunkX, chunkY);
        if (chunk == null) return;

        int localX = worldX - chunkX * Chunk.SIZE;
        int localY = worldY - chunkY * Chunk.SIZE;

        if (localX < 0 || localX >= Chunk.SIZE || localY < 0 || localY >= Chunk.SIZE)
            return;

        ref var tile = ref chunk.GetTile(localX, localY);

        TileType? newType = tile.Type switch
        {
            TileType.Stone => TileType.Pearlstone,
            TileType.Grass => TileType.HallowedGrass,
            TileType.Sand => TileType.HallowedSand,
            TileType.Sandstone => TileType.HallowedSandstone,
            TileType.Ice => TileType.HallowedIce,
            TileType.Dirt => TileType.Dirt,  // Dirt can become hallowed grass if on surface
            _ => null
        };

        if (newType.HasValue && newType.Value != tile.Type)
        {
            tile = new Tile(newType.Value);
            chunk.MarkDirty();
        }
    }

    /// <summary>
    /// Expand the evil biome (Corruption/Crimson).
    /// Creates additional V-shaped evil spread.
    /// </summary>
    private void ExpandEvilBiome()
    {
        var config = _worldGenerator.Config;

        // The evil biome expands in a V-shape from spawn
        int centerX = config.Width / 2;

        // Direction opposite to Hallow
        bool expandLeft = config.JungleOnLeft;

        int stripeWidth = config.Width / 10;
        int startX, endX;

        if (expandLeft)
        {
            endX = centerX - config.Width / 8;
            startX = endX - stripeWidth;
        }
        else
        {
            startX = centerX + config.Width / 8;
            endX = startX + stripeWidth;
        }

        // Ensure bounds
        startX = Math.Max(config.OceanWidth + 50, startX);
        endX = Math.Min(config.Width - config.OceanWidth - 50, endX);

        System.Diagnostics.Debug.WriteLine($"[HARDMODE] Expanding evil biome from X={startX} to X={endX}");

        TileType evilStone = config.HasCrimson ? TileType.Crimstone : TileType.Ebonstone;
        TileType evilGrass = config.HasCrimson ? TileType.CrimsonGrass : TileType.CorruptGrass;
        TileType evilSand = config.HasCrimson ? TileType.CrimsonSand : TileType.CorruptSand;
        TileType evilIce = config.HasCrimson ? TileType.CrimsonIce : TileType.CorruptIce;

        var perlin = new PerlinNoise(_random.Next());

        for (int worldX = startX; worldX < endX; worldX++)
        {
            float edgeNoise = perlin.Noise(worldX * 0.02f, 0.5f) * 15;
            int effectiveX = worldX + (int)edgeNoise;

            if (effectiveX < startX || effectiveX >= endX)
                continue;

            for (int worldY = config.SurfaceLevel - 30; worldY < config.UnderworldBoundary; worldY++)
            {
                float vNoise = perlin.Noise(worldX * 0.03f, worldY * 0.03f);
                if (Math.Abs(worldX - (startX + endX) / 2) > stripeWidth / 2 - 10 && vNoise < -0.3f)
                    continue;

                CorruptTile(effectiveX, worldY, evilStone, evilGrass, evilSand, evilIce);
            }
        }
    }

    /// <summary>
    /// Convert a tile to its evil (Corruption/Crimson) variant.
    /// </summary>
    private void CorruptTile(int worldX, int worldY, TileType evilStone, TileType evilGrass,
                            TileType evilSand, TileType evilIce)
    {
        int chunkX = worldX / Chunk.SIZE;
        int chunkY = worldY / Chunk.SIZE;

        var chunk = _chunkManager.GetChunk(chunkX, chunkY);
        if (chunk == null) return;

        int localX = worldX - chunkX * Chunk.SIZE;
        int localY = worldY - chunkY * Chunk.SIZE;

        if (localX < 0 || localX >= Chunk.SIZE || localY < 0 || localY >= Chunk.SIZE)
            return;

        ref var tile = ref chunk.GetTile(localX, localY);

        TileType? newType = tile.Type switch
        {
            TileType.Stone => evilStone,
            TileType.Grass => evilGrass,
            TileType.Sand => evilSand,
            TileType.Ice => evilIce,
            TileType.Dirt => TileType.Dirt,  // Can become evil grass
            _ => null
        };

        // Don't overwrite already hallowed tiles
        if (tile.Type == TileType.Pearlstone || tile.Type == TileType.HallowedGrass ||
            tile.Type == TileType.HallowedSand || tile.Type == TileType.HallowedIce)
        {
            return;
        }

        if (newType.HasValue && newType.Value != tile.Type)
        {
            tile = new Tile(newType.Value);
            chunk.MarkDirty();
        }
    }

    /// <summary>
    /// Update hardmode systems (biome spreading, etc).
    /// Called each game tick.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isHardmode || _isTransforming)
            return;

        // TODO: Implement gradual biome spreading
        // In Terraria, evil/hallow spreads at ~1.5 tiles per game hour
    }

    /// <summary>
    /// Save hardmode state.
    /// </summary>
    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(_isHardmode);
        writer.Write(_hasTransformed);
    }

    /// <summary>
    /// Load hardmode state.
    /// </summary>
    public void LoadFrom(BinaryReader reader)
    {
        _isHardmode = reader.ReadBoolean();
        _hasTransformed = reader.ReadBoolean();

        // Update config
        if (_isHardmode && _worldGenerator.Config != null)
        {
            _worldGenerator.Config.SetHardmode(true);
        }

        System.Diagnostics.Debug.WriteLine($"[HARDMODE] Loaded state: IsHardmode={_isHardmode}, HasTransformed={_hasTransformed}");
    }

    /// <summary>
    /// Force set hardmode state (for debugging/cheats).
    /// </summary>
    public void SetHardmode(bool isHardmode, bool skipTransformation = false)
    {
        _isHardmode = isHardmode;

        if (isHardmode && !_hasTransformed && !skipTransformation)
        {
            PerformWorldTransformation();
        }
        else if (isHardmode)
        {
            _hasTransformed = true;
        }

        _worldGenerator.Config?.SetHardmode(isHardmode);
    }
}
