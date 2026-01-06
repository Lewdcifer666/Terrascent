namespace Terrascent.World.Generation;

/// <summary>
/// Generates world terrain using multiple passes.
/// </summary>
public class WorldGenerator
{
    private readonly PerlinNoise _terrainNoise;
    private readonly PerlinNoise _caveNoise;
    private readonly PerlinNoise _oreNoise;
    private readonly TreeGenerator _treeGenerator;
    private readonly Random _random;

    public int Seed { get; }

    // World parameters
    public int SurfaceLevel { get; set; } = 100;
    public int TerrainHeight { get; set; } = 40;
    public int DirtDepth { get; set; } = 8;
    public float CaveThreshold { get; set; } = 0.35f;

    // Noise parameters
    public int TerrainOctaves { get; set; } = 6;
    public float TerrainFrequency { get; set; } = 0.008f;
    public float TerrainPersistence { get; set; } = 0.5f;

    public int CaveOctaves { get; set; } = 4;
    public float CaveFrequency { get; set; } = 0.04f;
    public float CavePersistence { get; set; } = 0.5f;

    public bool GenerateTrees { get; set; } = true;

    public WorldGenerator(int seed)
    {
        Seed = seed;
        _random = new Random(seed);

        _terrainNoise = new PerlinNoise(seed);
        _caveNoise = new PerlinNoise(seed + 1000);
        _oreNoise = new PerlinNoise(seed + 2000);
        _treeGenerator = new TreeGenerator(seed + 3000);

        System.Diagnostics.Debug.WriteLine($"WorldGenerator created with seed: {seed}");
    }

    public void GenerateChunk(Chunk chunk)
    {
        int chunkWorldX = chunk.Position.X * Chunk.SIZE;
        int chunkWorldY = chunk.Position.Y * Chunk.SIZE;

        // === PASS 1, 2, 3: Terrain, Caves, Ores ===
        for (int localX = 0; localX < Chunk.SIZE; localX++)
        {
            int worldX = chunkWorldX + localX;
            int surfaceY = GetSurfaceHeight(worldX);

            for (int localY = 0; localY < Chunk.SIZE; localY++)
            {
                int worldY = chunkWorldY + localY;
                TileType type = GetTileType(worldX, worldY, surfaceY);

                ref var tile = ref chunk.GetTile(localX, localY);
                tile = new Tile(type);
            }
        }

        // === PASS 4: Trees ===
        if (GenerateTrees)
        {
            GenerateTreesForChunk(chunk, chunkWorldX, chunkWorldY);
        }

        chunk.MarkLoaded();
    }

    /// <summary>
    /// Generate trees that could appear in this chunk.
    /// Checks a wider X range to catch trees from neighboring areas.
    /// </summary>
    private void GenerateTreesForChunk(Chunk chunk, int chunkWorldX, int chunkWorldY)
    {
        int chunkTopY = chunkWorldY;
        int chunkBottomY = chunkWorldY + Chunk.SIZE - 1;

        // Check a wider X range to catch trees whose canopy extends into this chunk
        int checkMargin = 10; // Max canopy radius
        int startX = chunkWorldX - checkMargin;
        int endX = chunkWorldX + Chunk.SIZE + checkMargin;

        for (int worldX = startX; worldX < endX; worldX++)
        {
            if (!_treeGenerator.ShouldPlaceTree(worldX))
                continue;

            int surfaceY = GetSurfaceHeight(worldX);

            // Skip if surface is way outside this chunk's Y range
            if (surfaceY < chunkTopY - 20 || surfaceY > chunkBottomY + 5)
                continue;

            // Check if surface tile is grass (verify it's not a cave opening)
            // Only do this check if surface is actually in this chunk
            int localSurfaceX = worldX - chunkWorldX;
            int localSurfaceY = surfaceY - chunkWorldY;

            if (localSurfaceX >= 0 && localSurfaceX < Chunk.SIZE &&
                localSurfaceY >= 0 && localSurfaceY < Chunk.SIZE)
            {
                ref var surfaceTile = ref chunk.GetTile(localSurfaceX, localSurfaceY);
                if (surfaceTile.Type != TileType.Grass)
                    continue;
            }

            // Generate and place tree
            var tree = _treeGenerator.GenerateTree(worldX, surfaceY);
            PlaceTreeInChunk(chunk, tree, chunkWorldX, chunkWorldY);
        }
    }

    /// <summary>
    /// Place tree tiles that fall within this chunk.
    /// </summary>
    private void PlaceTreeInChunk(Chunk chunk, TreeData tree, int chunkWorldX, int chunkWorldY)
    {
        // Place trunk (from base going up)
        int trunkTopY = tree.TrunkBaseY - tree.TrunkHeight + 1;

        for (int i = 0; i < tree.TrunkHeight; i++)
        {
            int worldY = tree.TrunkBaseY - i;
            int localX = tree.TrunkX - chunkWorldX;
            int localY = worldY - chunkWorldY;

            if (localX >= 0 && localX < Chunk.SIZE && localY >= 0 && localY < Chunk.SIZE)
            {
                ref var tile = ref chunk.GetTile(localX, localY);
                if (tile.IsAir)
                {
                    tile = new Tile(TileType.Wood);
                }
            }
        }

        // Place canopy - starts overlapping with trunk top, extends upward
        // Canopy center is above the trunk
        int canopyBaseY = trunkTopY;  // Bottom of canopy at trunk top
        int canopyTopY = canopyBaseY - tree.CanopyHeight;

        for (int worldY = canopyTopY; worldY <= canopyBaseY + 1; worldY++)
        {
            int localY = worldY - chunkWorldY;
            if (localY < 0 || localY >= Chunk.SIZE)
                continue;

            // Calculate how far from top/bottom of canopy (0 = edge, 1 = center)
            float canopyProgress = (float)(worldY - canopyTopY) / (canopyBaseY - canopyTopY + 1);

            // Radius: small at top, wide in middle, tapers at bottom
            float radiusMultiplier;
            if (canopyProgress < 0.3f)
            {
                // Top - small
                radiusMultiplier = 0.4f + canopyProgress * 2f;
            }
            else if (canopyProgress < 0.7f)
            {
                // Middle - full width
                radiusMultiplier = 1.0f;
            }
            else
            {
                // Bottom - taper
                radiusMultiplier = 1.0f - (canopyProgress - 0.7f) * 1.5f;
            }

            int radius = Math.Max(1, (int)(tree.CanopyRadius * radiusMultiplier));

            for (int dx = -radius; dx <= radius; dx++)
            {
                int worldX = tree.TrunkX + dx;
                int localX = worldX - chunkWorldX;

                if (localX < 0 || localX >= Chunk.SIZE)
                    continue;

                // Skip trunk column in lower part of canopy
                if (dx == 0 && worldY >= canopyBaseY - 1)
                    continue;

                // Elliptical shape check
                float distX = (float)Math.Abs(dx) / radius;
                if (distX > 1.0f)
                    continue;

                // Add slight randomness to edges
                int edgeHash = HashPosition(worldX, worldY);
                float edgeRand = (edgeHash % 100) / 100f * 0.2f;

                if (distX <= 0.9f + edgeRand)
                {
                    ref var tile = ref chunk.GetTile(localX, localY);
                    if (tile.IsAir)
                    {
                        tile = new Tile(TileType.Leaves);
                    }
                }
            }
        }
    }

    private static int HashPosition(int x, int y)
    {
        int hash = x * 374761393 + y * 668265263;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        return Math.Abs(hash);
    }

    public int GetSurfaceHeight(int worldX)
    {
        float noise = _terrainNoise.OctaveNoise01(
            worldX, 0,
            TerrainOctaves,
            TerrainPersistence,
            TerrainFrequency
        );

        int height = (int)(noise * TerrainHeight);
        return SurfaceLevel + height;
    }

    private TileType GetTileType(int worldX, int worldY, int surfaceY)
    {
        if (worldY < surfaceY)
            return TileType.Air;

        int depth = worldY - surfaceY;

        if (depth > 8 && IsCave(worldX, worldY, depth))
            return TileType.Air;

        if (depth == 0)
            return TileType.Grass;

        if (depth < DirtDepth)
            return TileType.Dirt;

        TileType oreType = GetOreType(worldX, worldY, depth);
        if (oreType != TileType.Air)
            return oreType;

        return TileType.Stone;
    }

    private bool IsCave(int worldX, int worldY, int depth)
    {
        float noise = _caveNoise.OctaveNoise01(
            worldX, worldY,
            CaveOctaves,
            CavePersistence,
            CaveFrequency
        );

        float depthBonus = MathF.Min(depth / 100f, 0.1f);
        return noise < (CaveThreshold + depthBonus);
    }

    private TileType GetOreType(int worldX, int worldY, int depth)
    {
        float baseNoise = _oreNoise.Noise01(worldX * 0.08f, worldY * 0.08f);

        if (depth >= 5)
        {
            float copperNoise = _oreNoise.Noise01(worldX * 0.15f + 100, worldY * 0.15f);
            if (copperNoise > 0.75f && baseNoise > 0.6f)
                return TileType.CopperOre;
        }

        if (depth >= 15)
        {
            float ironNoise = _oreNoise.Noise01(worldX * 0.15f + 200, worldY * 0.15f);
            if (ironNoise > 0.78f && baseNoise > 0.65f)
                return TileType.IronOre;
        }

        if (depth >= 30)
        {
            float silverNoise = _oreNoise.Noise01(worldX * 0.15f + 300, worldY * 0.15f);
            if (silverNoise > 0.82f && baseNoise > 0.7f)
                return TileType.SilverOre;
        }

        if (depth >= 50)
        {
            float goldNoise = _oreNoise.Noise01(worldX * 0.15f + 400, worldY * 0.15f);
            if (goldNoise > 0.88f && baseNoise > 0.75f)
                return TileType.GoldOre;
        }

        return TileType.Air;
    }

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