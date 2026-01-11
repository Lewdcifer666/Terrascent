using Terrascent.World;
using Terrascent.World.Biomes;

namespace Terrascent.World.Generation;

/// <summary>
/// Generates world terrain using multiple passes with Terraria-style biome placement.
/// Biomes follow Terraria's rules: Jungle opposite Snow, Ocean at edges, Evil biome avoids Jungle.
/// </summary>
public class WorldGenerator
{
    private readonly PerlinNoise _terrainNoise;
    private readonly PerlinNoise _caveNoise;
    private readonly PerlinNoise _oreNoise;
    private readonly PerlinNoise _biomeNoise;
    private readonly TreeGenerator _treeGenerator;
    private readonly Random _random;

    public int Seed { get; }

    // World configuration
    public WorldConfig Config { get; private set; }

    // World parameters (derived from WorldConfig)
    public int SurfaceLevel => Config?.SurfaceLevel ?? 100;
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

    // Biome placement
    private BiomePlacement? _biomePlacement;

    public WorldGenerator(int seed, WorldSize worldSize = WorldSize.Medium)
    {
        Seed = seed;
        _random = new Random(seed);

        _terrainNoise = new PerlinNoise(seed);
        _caveNoise = new PerlinNoise(seed + 1000);
        _oreNoise = new PerlinNoise(seed + 2000);
        _biomeNoise = new PerlinNoise(seed + 4000);
        _treeGenerator = new TreeGenerator(seed + 3000);

        // Create world configuration
        Config = WorldConfig.Create(worldSize, seed);

        // Calculate biome placement
        _biomePlacement = new BiomePlacement(Config, seed);

        System.Diagnostics.Debug.WriteLine($"WorldGenerator created: {Config.Size} world ({Config.Width}x{Config.Height})");
        System.Diagnostics.Debug.WriteLine($"  Jungle on {(Config.JungleOnLeft ? "LEFT" : "RIGHT")} side");
        System.Diagnostics.Debug.WriteLine($"  Evil biome: {(Config.HasCrimson ? "CRIMSON" : "CORRUPTION")}");
    }

    public void GenerateChunk(Chunk chunk)
    {
        int chunkWorldX = chunk.Position.X * Chunk.SIZE;
        int chunkWorldY = chunk.Position.Y * Chunk.SIZE;

        // === PASS 1, 2, 3: Terrain, Caves, Ores (with biome awareness) ===
        for (int localX = 0; localX < Chunk.SIZE; localX++)
        {
            int worldX = chunkWorldX + localX;
            int surfaceY = GetSurfaceHeight(worldX);
            BiomeType surfaceBiome = GetSurfaceBiome(worldX);

            for (int localY = 0; localY < Chunk.SIZE; localY++)
            {
                int worldY = chunkWorldY + localY;
                TileType type = GetTileType(worldX, worldY, surfaceY, surfaceBiome);

                ref var tile = ref chunk.GetTile(localX, localY);
                tile = new Tile(type);
            }
        }

        // === PASS 4: Trees (biome-specific) ===
        if (GenerateTrees)
        {
            GenerateTreesForChunk(chunk, chunkWorldX, chunkWorldY);
        }

        chunk.MarkLoaded();
    }

    /// <summary>Determine the surface biome at a given X coordinate using Terraria-style placement.</summary>
    public BiomeType GetSurfaceBiome(int worldX)
    {
        if (_biomePlacement == null)
            return BiomeType.Forest;

        return _biomePlacement.GetBiomeAt(worldX);
    }

    /// <summary>Generate trees for this chunk.</summary>
    private void GenerateTreesForChunk(Chunk chunk, int chunkWorldX, int chunkWorldY)
    {
        int chunkTopY = chunkWorldY;
        int chunkBottomY = chunkWorldY + Chunk.SIZE - 1;

        int checkMargin = 10;
        int startX = chunkWorldX - checkMargin;
        int endX = chunkWorldX + Chunk.SIZE + checkMargin;

        for (int worldX = startX; worldX < endX; worldX++)
        {
            if (!_treeGenerator.ShouldPlaceTree(worldX))
                continue;

            int surfaceY = GetSurfaceHeight(worldX);
            BiomeType biome = GetSurfaceBiome(worldX);

            // Skip trees in desert (unless palm trees at oasis)
            if (biome == BiomeType.Desert)
                continue;

            if (surfaceY < chunkTopY - 20 || surfaceY > chunkBottomY + 5)
                continue;

            int localSurfaceX = worldX - chunkWorldX;
            int localSurfaceY = surfaceY - chunkWorldY;

            if (localSurfaceX >= 0 && localSurfaceX < Chunk.SIZE &&
                localSurfaceY >= 0 && localSurfaceY < Chunk.SIZE)
            {
                ref var surfaceTile = ref chunk.GetTile(localSurfaceX, localSurfaceY);

                bool isValidGrass = surfaceTile.Type == TileType.Grass ||
                                   surfaceTile.Type == TileType.JungleGrass ||
                                   surfaceTile.Type == TileType.MushroomGrass ||
                                   surfaceTile.Type == TileType.Snow;

                // Palm trees on sand near ocean
                if (biome == BiomeType.Ocean && surfaceTile.Type == TileType.Sand)
                {
                    isValidGrass = true;
                }

                if (!isValidGrass)
                    continue;
            }

            TileType woodType = GetBiomeWoodType(biome);
            var tree = _treeGenerator.GenerateTree(worldX, surfaceY);
            PlaceTreeInChunk(chunk, tree, chunkWorldX, chunkWorldY, woodType);
        }
    }

    /// <summary>Get the wood type for a specific biome.</summary>
    private TileType GetBiomeWoodType(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Snow => TileType.BorealWood,
            BiomeType.Jungle => TileType.RichMahogany,
            BiomeType.Corruption => TileType.Ebonwood,
            BiomeType.Crimson => TileType.Shadewood,
            BiomeType.Hallow => TileType.Pearlwood,
            BiomeType.Desert or BiomeType.Ocean => TileType.PalmWood,
            _ => TileType.Wood
        };
    }

    /// <summary>Place tree tiles that fall within this chunk.</summary>
    private void PlaceTreeInChunk(Chunk chunk, TreeData tree, int chunkWorldX, int chunkWorldY, TileType woodType = TileType.Wood)
    {
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
                    tile = new Tile(woodType);
                }
            }
        }

        // Leaf cap
        int capLocalX = tree.TrunkX - chunkWorldX;
        int capLocalY = (trunkTopY - 1) - chunkWorldY;
        if (capLocalX >= 0 && capLocalX < Chunk.SIZE && capLocalY >= 0 && capLocalY < Chunk.SIZE)
        {
            ref var capTile = ref chunk.GetTile(capLocalX, capLocalY);
            if (capTile.IsAir)
            {
                capTile = new Tile(TileType.Leaves);
            }
        }

        // Place canopy
        int canopyBaseY = trunkTopY;
        int canopyTopY = canopyBaseY - tree.CanopyHeight;

        for (int worldY = canopyTopY; worldY <= canopyBaseY + 1; worldY++)
        {
            int localY = worldY - chunkWorldY;
            if (localY < 0 || localY >= Chunk.SIZE)
                continue;

            float canopyProgress = (float)(worldY - canopyTopY) / (canopyBaseY - canopyTopY + 1);

            float radiusMultiplier;
            if (canopyProgress < 0.3f)
                radiusMultiplier = 0.4f + canopyProgress * 2f;
            else if (canopyProgress < 0.7f)
                radiusMultiplier = 1.0f;
            else
                radiusMultiplier = 1.0f - (canopyProgress - 0.7f) * 1.5f;

            int radius = Math.Max(1, (int)(tree.CanopyRadius * radiusMultiplier));

            for (int dx = -radius; dx <= radius; dx++)
            {
                int worldX = tree.TrunkX + dx;
                int localX = worldX - chunkWorldX;

                if (localX < 0 || localX >= Chunk.SIZE)
                    continue;

                if (dx == 0 && worldY > trunkTopY)
                    continue;

                float distX = (float)Math.Abs(dx) / radius;
                if (distX > 1.0f)
                    continue;

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

        BiomeType biome = GetSurfaceBiome(worldX);
        float biomeHeightMod = biome switch
        {
            BiomeType.Desert => 0.3f,
            BiomeType.Snow => 1.2f,
            BiomeType.Jungle => 0.8f,
            BiomeType.Mushroom => 0.5f,
            BiomeType.Ocean => 0.2f,
            _ => 1.0f
        };

        int baseLevel = SurfaceLevel;
        if (biome == BiomeType.Ocean)
        {
            baseLevel += 20;
        }

        int height = (int)(noise * TerrainHeight * biomeHeightMod);
        return baseLevel + height;
    }

    private TileType GetTileType(int worldX, int worldY, int surfaceY, BiomeType surfaceBiome)
    {
        if (worldY < surfaceY)
            return TileType.Air;

        int depth = worldY - surfaceY;

        WorldLayer layer = Config.GetLayerAt(worldY);

        // Underworld generation
        if (layer == WorldLayer.Underworld)
        {
            return GetUnderworldTile(worldX, worldY, depth);
        }

        // Caves (but not too close to surface)
        if (depth > 8 && IsCave(worldX, worldY, depth))
            return TileType.Air;

        return GetBiomeTile(worldX, worldY, depth, surfaceBiome, layer);
    }

    /// <summary>Generate underworld (hell) tiles.</summary>
    private TileType GetUnderworldTile(int worldX, int worldY, int depth)
    {
        float noise = _caveNoise.Noise01(worldX * 0.02f, worldY * 0.02f);

        // Hellstone pockets
        float hellstoneNoise = _oreNoise.Noise01(worldX * 0.1f + 500, worldY * 0.1f);
        if (hellstoneNoise > 0.85f && noise > 0.4f)
            return TileType.Hellstone;

        // Open areas (lava pools)
        if (noise < 0.35f)
            return TileType.Air;

        // Obsidian near lava
        if (noise < 0.45f)
            return TileType.Obsidian;

        return TileType.Ash;
    }

    /// <summary>Get the tile type based on biome, depth, and layer.</summary>
    private TileType GetBiomeTile(int worldX, int worldY, int depth, BiomeType biome, WorldLayer layer)
    {
        // Surface tile
        if (depth == 0)
        {
            return biome switch
            {
                BiomeType.Snow => TileType.Snow,
                BiomeType.Desert => TileType.Sand,
                BiomeType.Jungle => TileType.JungleGrass,
                BiomeType.Mushroom => TileType.MushroomGrass,
                BiomeType.Corruption => TileType.CorruptGrass,
                BiomeType.Crimson => TileType.CrimsonGrass,
                BiomeType.Hallow => TileType.HallowedGrass,
                BiomeType.Ocean => TileType.Sand,
                _ => TileType.Grass
            };
        }

        // Subsurface layer
        if (layer == WorldLayer.Surface || layer == WorldLayer.Underground)
        {
            if (depth < DirtDepth)
            {
                return biome switch
                {
                    BiomeType.Snow => (depth < 3 ? TileType.Snow : TileType.Ice),
                    BiomeType.Desert or BiomeType.Ocean => TileType.Sand,
                    BiomeType.Jungle => TileType.Mud,
                    BiomeType.Mushroom => TileType.Mud,
                    _ => TileType.Dirt
                };
            }

            // Transition zone
            if (depth < DirtDepth * 2)
            {
                float transitionNoise = _terrainNoise.Noise01(worldX * 0.1f, worldY * 0.1f);
                if (transitionNoise > 0.5f)
                {
                    return GetBiomeStone(biome);
                }
                return biome switch
                {
                    BiomeType.Snow => TileType.Ice,
                    BiomeType.Desert or BiomeType.Ocean => TileType.HardenedSand,
                    BiomeType.Jungle => TileType.Mud,
                    _ => TileType.Dirt
                };
            }
        }

        // Check for ores first
        TileType oreType = GetOreType(worldX, worldY, depth, layer);
        if (oreType != TileType.Air)
            return oreType;

        // Cavern layer - mostly stone with biome variants
        if (layer == WorldLayer.Cavern)
        {
            // Mini-biomes
            float graniteMarblenoise = _biomeNoise.Noise01(worldX * 0.03f + 1000, worldY * 0.03f);

            if (graniteMarblenoise > 0.85f)
                return TileType.GraniteBlock;
            if (graniteMarblenoise < 0.15f)
                return TileType.MarbleBlock;

            return GetDeepBiomeStone(biome, worldX, worldY);
        }

        return GetBiomeStone(biome);
    }

    /// <summary>Get the stone variant for a biome (upper layers).</summary>
    private TileType GetBiomeStone(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Snow => TileType.Ice,
            BiomeType.Desert or BiomeType.Ocean => TileType.Sandstone,
            BiomeType.Corruption => TileType.Ebonstone,
            BiomeType.Crimson => TileType.Crimstone,
            BiomeType.Hallow => TileType.Pearlstone,
            _ => TileType.Stone
        };
    }

    /// <summary>Get stone type for deep cavern layer (biome influence fades).</summary>
    private TileType GetDeepBiomeStone(BiomeType biome, int worldX, int worldY)
    {
        float blendNoise = _biomeNoise.Noise01(worldX * 0.05f, worldY * 0.05f);

        if (blendNoise > 0.7f)
        {
            return TileType.Stone;
        }

        return biome switch
        {
            BiomeType.Snow => TileType.Ice,
            BiomeType.Desert or BiomeType.Ocean => (blendNoise > 0.4f ? TileType.Stone : TileType.Sandstone),
            BiomeType.Jungle => TileType.Stone,
            BiomeType.Corruption => TileType.Ebonstone,
            BiomeType.Crimson => TileType.Crimstone,
            BiomeType.Hallow => TileType.Pearlstone,
            _ => TileType.Stone
        };
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

    private TileType GetOreType(int worldX, int worldY, int depth, WorldLayer layer)
    {
        float baseNoise = _oreNoise.Noise01(worldX * 0.08f, worldY * 0.08f);

        // Copper - starts shallow
        if (depth >= 5)
        {
            float copperNoise = _oreNoise.Noise01(worldX * 0.15f + 100, worldY * 0.15f);
            if (copperNoise > 0.75f && baseNoise > 0.6f)
                return TileType.CopperOre;
        }

        // Iron - medium depth
        if (depth >= 15)
        {
            float ironNoise = _oreNoise.Noise01(worldX * 0.15f + 200, worldY * 0.15f);
            if (ironNoise > 0.78f && baseNoise > 0.65f)
                return TileType.IronOre;
        }

        // Silver - deeper
        if (depth >= 30 && (layer == WorldLayer.Underground || layer == WorldLayer.Cavern))
        {
            float silverNoise = _oreNoise.Noise01(worldX * 0.15f + 300, worldY * 0.15f);
            if (silverNoise > 0.82f && baseNoise > 0.7f)
                return TileType.SilverOre;
        }

        // Gold - cavern layer
        if (layer == WorldLayer.Cavern)
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

/// <summary>
/// Handles biome placement following Terraria's rules:
/// - Jungle and Snow are on opposite sides
/// - Dungeon is on Snow side
/// - Evil biome doesn't overlap Jungle
/// - Oceans at world edges
/// </summary>
public class BiomePlacement
{
    private readonly WorldConfig _config;
    private readonly Random _random;
    private readonly List<BiomeZone> _zones = new();

    public BiomePlacement(WorldConfig config, int seed)
    {
        _config = config;
        _random = new Random(seed + 7777);

        CalculateZones();
    }

    private void CalculateZones()
    {
        int width = _config.Width;
        int oceanWidth = _config.OceanWidth;

        // Ocean zones at edges
        _zones.Add(new BiomeZone(0, oceanWidth, BiomeType.Ocean));
        _zones.Add(new BiomeZone(width - oceanWidth, width, BiomeType.Ocean));

        int spawnX = width / 2;

        // Calculate biome widths based on world size
        int jungleWidth = (int)(width * 0.15f);
        int snowWidth = (int)(width * 0.12f);
        int desertWidth = (int)(width * 0.08f);
        int evilWidth = (int)(width * 0.10f);

        // Place Jungle
        int jungleStart, jungleEnd;
        if (_config.JungleOnLeft)
        {
            jungleStart = oceanWidth + 100;
            jungleEnd = jungleStart + jungleWidth;
        }
        else
        {
            jungleEnd = width - oceanWidth - 100;
            jungleStart = jungleEnd - jungleWidth;
        }
        _zones.Add(new BiomeZone(jungleStart, jungleEnd, BiomeType.Jungle));

        // Place Snow (opposite side from Jungle)
        int snowStart, snowEnd;
        if (!_config.JungleOnLeft)
        {
            snowStart = oceanWidth + 150;
            snowEnd = snowStart + snowWidth;
        }
        else
        {
            snowEnd = width - oceanWidth - 150;
            snowStart = snowEnd - snowWidth;
        }
        _zones.Add(new BiomeZone(snowStart, snowEnd, BiomeType.Snow));

        // Place Desert
        int desertStart = spawnX + _random.Next(-300, 100);
        int desertEnd = desertStart + desertWidth;

        // Avoid overlap with jungle/snow
        if (desertStart < jungleEnd + 50 && desertEnd > jungleStart - 50)
        {
            desertStart = jungleEnd + 100;
            desertEnd = desertStart + desertWidth;
        }
        if (desertStart < snowEnd + 50 && desertEnd > snowStart - 50)
        {
            desertStart = snowEnd + 100;
            desertEnd = desertStart + desertWidth;
        }

        desertStart = Math.Max(oceanWidth + 50, desertStart);
        desertEnd = Math.Min(width - oceanWidth - 50, desertEnd);
        if (desertEnd > desertStart)
        {
            _zones.Add(new BiomeZone(desertStart, desertEnd, BiomeType.Desert));
        }

        // Place Evil biome (opposite side from jungle)
        BiomeType evilType = _config.HasCrimson ? BiomeType.Crimson : BiomeType.Corruption;
        int evilStart, evilEnd;

        if (_config.JungleOnLeft)
        {
            evilStart = spawnX + 300;
            evilEnd = evilStart + evilWidth;
        }
        else
        {
            evilEnd = spawnX - 300;
            evilStart = evilEnd - evilWidth;
        }

        // Avoid snow overlap
        if (evilStart < snowEnd && evilEnd > snowStart)
        {
            if (_config.JungleOnLeft)
            {
                evilStart = snowEnd + 50;
                evilEnd = evilStart + evilWidth;
            }
            else
            {
                evilEnd = snowStart - 50;
                evilStart = evilEnd - evilWidth;
            }
        }

        evilStart = Math.Max(oceanWidth, evilStart);
        evilEnd = Math.Min(width - oceanWidth, evilEnd);
        if (evilEnd > evilStart)
        {
            _zones.Add(new BiomeZone(evilStart, evilEnd, evilType));
        }

        // Sort zones
        _zones.Sort((a, b) => a.StartX.CompareTo(b.StartX));

        // Debug output
        System.Diagnostics.Debug.WriteLine("Biome Zones:");
        foreach (var zone in _zones)
        {
            System.Diagnostics.Debug.WriteLine($"  {zone.Biome}: {zone.StartX} - {zone.EndX}");
        }
    }

    public BiomeType GetBiomeAt(int worldX)
    {
        foreach (var zone in _zones)
        {
            if (worldX >= zone.StartX && worldX < zone.EndX)
            {
                return zone.Biome;
            }
        }

        return BiomeType.Forest;
    }

    private class BiomeZone
    {
        public int StartX { get; }
        public int EndX { get; }
        public BiomeType Biome { get; }

        public BiomeZone(int startX, int endX, BiomeType biome)
        {
            StartX = startX;
            EndX = endX;
            Biome = biome;
        }
    }
}