namespace Terrascent.World.Generation;

/// <summary>
/// Generates world terrain using multiple passes.
/// Pass 1: Base terrain (height)
/// Pass 2: Caves
/// Pass 3: Ores
/// Pass 4: Surface decorations (trees)
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

    // Tree generation enabled
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

    /// <summary>
    /// Generate terrain for a specific chunk.
    /// </summary>
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
        // Trees need to be placed after base terrain because they span multiple tiles
        if (GenerateTrees)
        {
            GenerateTreesInChunk(chunk, chunkWorldX, chunkWorldY);
        }

        chunk.MarkLoaded();
    }

    /// <summary>
    /// Generate trees within a chunk.
    /// </summary>
    private void GenerateTreesInChunk(Chunk chunk, int chunkWorldX, int chunkWorldY)
    {
        // Only process chunks that might contain the surface
        int minSurfaceY = SurfaceLevel;
        int maxSurfaceY = SurfaceLevel + TerrainHeight + 10;

        int chunkTopY = chunkWorldY;
        int chunkBottomY = chunkWorldY + Chunk.SIZE;

        // Skip if chunk is entirely above or below surface range
        if (chunkBottomY < minSurfaceY - 15 || chunkTopY > maxSurfaceY)
            return;

        // Check each X position in the chunk for tree placement
        for (int localX = 0; localX < Chunk.SIZE; localX++)
        {
            int worldX = chunkWorldX + localX;

            // Check if tree should spawn here
            if (!_treeGenerator.ShouldPlaceTree(worldX))
                continue;

            int surfaceY = GetSurfaceHeight(worldX);

            // Check if surface is in this chunk
            int localSurfaceY = surfaceY - chunkWorldY;
            if (localSurfaceY < 0 || localSurfaceY >= Chunk.SIZE)
                continue;

            // Verify the surface tile is grass (not in a cave entrance)
            ref var surfaceTile = ref chunk.GetTile(localX, localSurfaceY);
            if (surfaceTile.Type != TileType.Grass)
                continue;

            // Check tile above is air
            if (localSurfaceY > 0)
            {
                ref var aboveTile = ref chunk.GetTile(localX, localSurfaceY - 1);
                if (!aboveTile.IsAir)
                    continue;
            }

            // Generate tree data
            var tree = _treeGenerator.GenerateTree(worldX, surfaceY);

            // Place tree tiles directly in chunk (only what fits)
            PlaceTreeInChunk(chunk, tree, chunkWorldX, chunkWorldY);
        }
    }

    /// <summary>
    /// Place tree tiles that fall within this chunk.
    /// </summary>
    private void PlaceTreeInChunk(Chunk chunk, TreeData tree, int chunkWorldX, int chunkWorldY)
    {
        // Place trunk (from base going up)
        for (int y = 0; y < tree.TrunkHeight; y++)
        {
            int worldY = tree.TrunkBaseY - y;
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

        // Place canopy (leaves) - more natural oval shape
        int trunkTopY = tree.TrunkBaseY - tree.TrunkHeight + 1;
        int canopyCenterY = trunkTopY - (tree.CanopyHeight / 2);

        for (int dy = -tree.CanopyHeight; dy <= 1; dy++)
        {
            int worldY = canopyCenterY + dy;

            // Calculate radius at this height - widest in the middle, tapers at top and bottom
            float normalizedHeight = (float)(dy + tree.CanopyHeight) / (tree.CanopyHeight + 1);
            float radiusMultiplier;

            if (normalizedHeight < 0.5f)
            {
                // Bottom half - expand from 0.3 to 1.0
                radiusMultiplier = 0.3f + normalizedHeight * 1.4f;
            }
            else
            {
                // Top half - shrink from 1.0 to 0.4
                radiusMultiplier = 1.0f - (normalizedHeight - 0.5f) * 1.2f;
            }

            int radiusAtHeight = Math.Max(1, (int)(tree.CanopyRadius * radiusMultiplier));

            for (int dx = -radiusAtHeight; dx <= radiusAtHeight; dx++)
            {
                int worldX = tree.TrunkX + dx;
                int localX = worldX - chunkWorldX;
                int localY = worldY - chunkWorldY;

                // Skip if outside chunk bounds
                if (localX < 0 || localX >= Chunk.SIZE || localY < 0 || localY >= Chunk.SIZE)
                    continue;

                // Don't overwrite trunk (center column in lower portion)
                if (dx == 0 && worldY >= trunkTopY - 1)
                    continue;

                // Use elliptical distance for more natural shape
                float distX = (float)dx / radiusAtHeight;
                float distY = (float)(dy + tree.CanopyHeight / 2) / (tree.CanopyHeight / 2 + 1);
                float dist = MathF.Sqrt(distX * distX + distY * distY * 0.5f);

                // Add some variation to edges
                int edgeHash = HashPosition(worldX, worldY);
                float edgeVariation = (edgeHash % 100) / 100f * 0.3f;

                if (dist <= 1.0f + edgeVariation)
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

        // Check for caves (but not too close to surface)
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
        float noise = _caveNoise.OctaveNoise01(
            worldX, worldY,
            CaveOctaves,
            CavePersistence,
            CaveFrequency
        );

        float depthBonus = MathF.Min(depth / 100f, 0.1f);
        return noise < (CaveThreshold + depthBonus);
    }

    /// <summary>
    /// Determine if an ore should spawn at this position.
    /// </summary>
    private TileType GetOreType(int worldX, int worldY, int depth)
    {
        float baseNoise = _oreNoise.Noise01(worldX * 0.08f, worldY * 0.08f);

        // Copper: depth 5+
        if (depth >= 5)
        {
            float copperNoise = _oreNoise.Noise01(worldX * 0.15f + 100, worldY * 0.15f);
            if (copperNoise > 0.75f && baseNoise > 0.6f)
                return TileType.CopperOre;
        }

        // Iron: depth 15+
        if (depth >= 15)
        {
            float ironNoise = _oreNoise.Noise01(worldX * 0.15f + 200, worldY * 0.15f);
            if (ironNoise > 0.78f && baseNoise > 0.65f)
                return TileType.IronOre;
        }

        // Silver: depth 30+
        if (depth >= 30)
        {
            float silverNoise = _oreNoise.Noise01(worldX * 0.15f + 300, worldY * 0.15f);
            if (silverNoise > 0.82f && baseNoise > 0.7f)
                return TileType.SilverOre;
        }

        // Gold: depth 50+
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