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
    public float CaveThreshold { get; set; } = 0.45f;   // Cave density (lower = more caves)

    // Noise parameters
    public int TerrainOctaves { get; set; } = 6;
    public float TerrainFrequency { get; set; } = 0.008f;
    public float TerrainPersistence { get; set; } = 0.5f;

    public int CaveOctaves { get; set; } = 4;
    public float CaveFrequency { get; set; } = 0.05f;
    public float CavePersistence { get; set; } = 0.5f;

    public WorldGenerator(int seed)
    {
        Seed = seed;
        _random = new Random(seed);

        // Create noise generators with different seeds
        _terrainNoise = new PerlinNoise(seed);
        _caveNoise = new PerlinNoise(seed + 1);
        _oreNoise = new PerlinNoise(seed + 2);
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
        // Use octave noise for natural-looking terrain
        float noise = _terrainNoise.OctaveNoise01(worldX, 0, TerrainOctaves,
                                                   TerrainPersistence, TerrainFrequency);

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

        // Check for caves first (but not too close to surface)
        if (depth > 5 && IsCave(worldX, worldY))
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
    private bool IsCave(int worldX, int worldY)
    {
        float noise = _caveNoise.OctaveNoise01(worldX, worldY, CaveOctaves,
                                                CavePersistence, CaveFrequency);

        // Caves are more likely deeper underground
        int depth = worldY - SurfaceLevel;
        float depthFactor = MathF.Min(1f, depth / 50f);

        // Adjust threshold based on depth (more caves deeper)
        float threshold = CaveThreshold + (1f - depthFactor) * 0.1f;

        return noise < threshold;
    }

    /// <summary>
    /// Determine if an ore should spawn at this position.
    /// </summary>
    private TileType GetOreType(int worldX, int worldY, int depth)
    {
        // Use noise for ore distribution
        float oreNoise = _oreNoise.Noise01(worldX * 0.1f, worldY * 0.1f);

        // Copper: Common, spawns at all depths
        if (depth >= 10 && oreNoise > 0.75f)
        {
            float copperNoise = _oreNoise.Noise01(worldX * 0.2f + 100, worldY * 0.2f);
            if (copperNoise > 0.7f)
                return TileType.CopperOre;
        }

        // Iron: Less common, spawns below depth 20
        if (depth >= 20 && oreNoise > 0.8f)
        {
            float ironNoise = _oreNoise.Noise01(worldX * 0.2f + 200, worldY * 0.2f);
            if (ironNoise > 0.75f)
                return TileType.IronOre;
        }

        // Silver: Rare, spawns below depth 35
        if (depth >= 35 && oreNoise > 0.85f)
        {
            float silverNoise = _oreNoise.Noise01(worldX * 0.2f + 300, worldY * 0.2f);
            if (silverNoise > 0.8f)
                return TileType.SilverOre;
        }

        // Gold: Very rare, spawns below depth 50
        if (depth >= 50 && oreNoise > 0.9f)
        {
            float goldNoise = _oreNoise.Noise01(worldX * 0.2f + 400, worldY * 0.2f);
            if (goldNoise > 0.85f)
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