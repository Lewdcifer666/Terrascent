using Microsoft.Xna.Framework;

namespace Terrascent.World.Generation;

/// <summary>
/// Generates world terrain using Perlin noise.
/// </summary>
public class WorldGenerator
{
    private readonly PerlinNoise _terrainNoise;
    private readonly PerlinNoise _caveNoise;
    private readonly PerlinNoise _oreNoise;
    private readonly Random _random;

    /// <summary>
    /// The world seed.
    /// </summary>
    public int Seed { get; }

    // World parameters
    public int SurfaceLevel { get; set; } = 100;        // Base surface Y level (in tiles)
    public int TerrainHeight { get; set; } = 40;        // Max terrain variation
    public int DirtDepth { get; set; } = 8;             // Dirt layer thickness
    public float CaveThreshold { get; set; } = 0.35f;   // Cave density (lower = more caves)

    // Noise parameters
    public int TerrainOctaves { get; set; } = 6;
    public float TerrainFrequency { get; set; } = 0.008f;
    public float TerrainPersistence { get; set; } = 0.5f;

    public int CaveOctaves { get; set; } = 4;
    public float CaveFrequency { get; set; } = 0.04f;
    public float CavePersistence { get; set; } = 0.5f;

    public WorldGenerator(int seed)
    {
        Seed = seed;
        _random = new Random(seed);

        // Create noise generators with different seeds
        _terrainNoise = new PerlinNoise(seed);
        _caveNoise = new PerlinNoise(seed + 1000);  // More separation
        _oreNoise = new PerlinNoise(seed + 2000);
    }

    /// <summary>
    /// Generate terrain for a specific chunk.
    /// </summary>
    public void GenerateChunk(Chunk chunk)
    {
        int chunkWorldX = chunk.Position.X * Chunk.SIZE;
        int chunkWorldY = chunk.Position.Y * Chunk.SIZE;

        for (int localX = 0; localX < Chunk.SIZE; localX++)
        {
            int worldX = chunkWorldX + localX;

            // Calculate surface height at this X position
            int surfaceY = GetSurfaceHeight(worldX);

            for (int localY = 0; localY < Chunk.SIZE; localY++)
            {
                int worldY = chunkWorldY + localY;

                // Get tile type for this position
                TileType type = GetTileType(worldX, worldY, surfaceY);

                // Set the tile
                ref var tile = ref chunk.GetTile(localX, localY);
                tile = new Tile(type);
            }
        }

        chunk.MarkLoaded();
    }

    /// <summary>
    /// Calculate surface height at a given X coordinate.
    /// </summary>
    public int GetSurfaceHeight(int worldX)
    {
        float noise = _terrainNoise.OctaveNoise01(
            worldX, 0,
            TerrainOctaves,
            TerrainPersistence,
            TerrainFrequency
        );

        // Map noise to terrain height
        int height = (int)(noise * TerrainHeight);

        return SurfaceLevel + height;
    }

    /// <summary>
    /// Determine what tile type should be at a given world position.
    /// </summary>
    private TileType GetTileType(int worldX, int worldY, int surfaceY)
    {
        // Above surface = air
        if (worldY < surfaceY)
        {
            return TileType.Air;
        }

        int depth = worldY - surfaceY;

        // Check for caves (but not too close to surface, and not too deep initially)
        if (depth > 8 && IsCave(worldX, worldY, depth))
        {
            return TileType.Air;
        }

        // Surface layer = grass
        if (depth == 0)
        {
            return TileType.Grass;
        }

        // Dirt layer
        if (depth < DirtDepth)
        {
            return TileType.Dirt;
        }

        // Stone layer - check for ores
        TileType oreType = GetOreType(worldX, worldY, depth);
        if (oreType != TileType.Air)
        {
            return oreType;
        }

        return TileType.Stone;
    }

    /// <summary>
    /// Check if a position should be a cave.
    /// </summary>
    private bool IsCave(int worldX, int worldY, int depth)
    {
        // Use 2D noise for cave generation
        float noise = _caveNoise.OctaveNoise01(
            worldX, worldY,
            CaveOctaves,
            CavePersistence,
            CaveFrequency
        );

        // Caves are slightly more common deeper, but cap it
        float depthBonus = MathF.Min(depth / 100f, 0.1f);

        // Cave if noise is below threshold
        // Lower threshold = fewer caves
        return noise < (CaveThreshold + depthBonus);
    }

    /// <summary>
    /// Determine if an ore should spawn at this position.
    /// </summary>
    private TileType GetOreType(int worldX, int worldY, int depth)
    {
        // Sample ore noise at different offsets for each ore type
        float baseNoise = _oreNoise.Noise01(worldX * 0.08f, worldY * 0.08f);

        // Copper: Spawns at depth 5+, common
        if (depth >= 5)
        {
            float copperNoise = _oreNoise.Noise01(worldX * 0.15f + 100, worldY * 0.15f);
            if (copperNoise > 0.75f && baseNoise > 0.6f)
                return TileType.CopperOre;
        }

        // Iron: Spawns at depth 15+, less common
        if (depth >= 15)
        {
            float ironNoise = _oreNoise.Noise01(worldX * 0.15f + 200, worldY * 0.15f);
            if (ironNoise > 0.78f && baseNoise > 0.65f)
                return TileType.IronOre;
        }

        // Silver: Spawns at depth 30+, rare
        if (depth >= 30)
        {
            float silverNoise = _oreNoise.Noise01(worldX * 0.15f + 300, worldY * 0.15f);
            if (silverNoise > 0.82f && baseNoise > 0.7f)
                return TileType.SilverOre;
        }

        // Gold: Spawns at depth 50+, very rare
        if (depth >= 50)
        {
            float goldNoise = _oreNoise.Noise01(worldX * 0.15f + 400, worldY * 0.15f);
            if (goldNoise > 0.88f && baseNoise > 0.75f)
                return TileType.GoldOre;
        }

        return TileType.Air; // No ore
    }

    /// <summary>
    /// Generate a preview heightmap for visualization.
    /// </summary>
    public int[] GenerateHeightmap(int startX, int width)
    {
        int[] heights = new int[width];

        for (int i = 0; i < width; i++)
        {
            heights[i] = GetSurfaceHeight(startX + i);
        }

        return heights;
    }
}