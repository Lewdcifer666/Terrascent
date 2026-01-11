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
    public int DirtDepth { get; set; } = 15;  // Deeper pure dirt layer before stone appears
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

        // Horizontal margin for trees that span chunk boundaries
        int checkMargin = 10;
        int startX = chunkWorldX - checkMargin;
        int endX = chunkWorldX + Chunk.SIZE + checkMargin;

        // Max tree height = 12 (trunk) + 6 (canopy) = 18 tiles
        // Vertical margin needs to be at least max tree height to prevent canopy cutoff
        int verticalMarginAbove = 25;  // Trees above chunk whose trunks extend into it
        int verticalMarginBelow = 25;  // Trees below chunk whose canopies extend up into it

        for (int worldX = startX; worldX < endX; worldX++)
        {
            if (!_treeGenerator.ShouldPlaceTree(worldX))
                continue;

            int surfaceY = GetSurfaceHeight(worldX);
            BiomeType biome = GetSurfaceBiome(worldX);

            // Skip trees in desert (unless palm trees at oasis)
            if (biome == BiomeType.Desert)
                continue;

            // Check if tree might have any parts in this chunk
            // Surface above chunk: trunk/canopy might extend down into chunk
            // Surface below chunk: canopy might extend up into chunk
            if (surfaceY < chunkTopY - verticalMarginAbove || surfaceY > chunkBottomY + verticalMarginBelow)
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

        // Get biome blend info for smooth transitions
        if (_biomePlacement != null)
        {
            var blendInfo = _biomePlacement.GetBiomeBlendInfo(worldX);

            if (blendInfo.IsTransition)
            {
                // Use noise to create natural-looking transition with larger patches
                // Lower frequency = larger blobs of each biome tile type
                float transitionNoise = _biomeNoise.Noise01(worldX * 0.08f + 5000, worldY * 0.05f);

                // Add some variation with depth for more organic look
                float depthVariation = MathF.Sin(depth * 0.15f) * 0.12f;
                float threshold = blendInfo.BlendFactor + depthVariation;

                // Clamp threshold to valid range
                threshold = Math.Clamp(threshold, 0.0f, 1.0f);

                // When noise < threshold, pick Biome2 (new), else pick Biome1 (old)
                // This gives gradual transition: low threshold = mostly old, high threshold = mostly new
                BiomeType selectedBiome = transitionNoise < threshold ? blendInfo.Biome2 : blendInfo.Biome1;
                return GetBiomeTile(worldX, worldY, depth, selectedBiome, layer);
            }
        }

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

    /// <summary>
    /// Get the tile type based on biome, depth, and layer.
    /// Implements Terraria-style terrain composition:
    /// - Surface: Grass/biome surface tile
    /// - Shallow (1-15): Pure subsurface material
    /// - Upper Underground (16-40): 50/50 subsurface/stone mix with clay
    /// - Lower Underground (40+): Mostly stone with subsurface patches
    /// - Cavern: Stone with granite/marble mini-biomes
    /// </summary>
    private TileType GetBiomeTile(int worldX, int worldY, int depth, BiomeType biome, WorldLayer layer)
    {
        // === SURFACE TILE (depth 0) ===
        if (depth == 0)
        {
            return GetSurfaceTile(biome);
        }

        // === SHALLOW LAYER (depth 1-15): Pure subsurface ===
        if (depth <= DirtDepth)
        {
            return GetShallowTile(worldX, worldY, depth, biome);
        }

        // Check for ores before terrain (ores can appear in any solid tile)
        TileType oreType = GetOreType(worldX, worldY, depth, layer);
        if (oreType != TileType.Air)
            return oreType;

        // === UPPER UNDERGROUND (depth 16-40): 50/50 mix with clay ===
        int upperUndergroundEnd = DirtDepth + 25;  // ~40 tiles deep
        if (depth <= upperUndergroundEnd && (layer == WorldLayer.Surface || layer == WorldLayer.Underground))
        {
            return GetUpperUndergroundTile(worldX, worldY, depth, biome);
        }

        // === LOWER UNDERGROUND / CAVERN: Mostly stone ===
        if (layer == WorldLayer.Underground)
        {
            return GetLowerUndergroundTile(worldX, worldY, depth, biome);
        }

        // === CAVERN LAYER: Stone with mini-biomes ===
        if (layer == WorldLayer.Cavern)
        {
            return GetCavernTile(worldX, worldY, depth, biome);
        }

        // Fallback
        return GetBiomeStone(biome);
    }

    /// <summary>Get the surface tile for a biome.</summary>
    private TileType GetSurfaceTile(BiomeType biome)
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

    /// <summary>Get shallow subsurface tile (pure dirt/sand/snow layer).</summary>
    private TileType GetShallowTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        // Some biomes have depth-based transitions in shallow layer
        return biome switch
        {
            // Snow: Snow for first few tiles, then transition to Ice
            BiomeType.Snow => depth < 5 ? TileType.Snow :
                             (depth < 10 ? (GetMixNoise(worldX, worldY, 0.15f) > 0.5f ? TileType.Ice : TileType.Snow) : TileType.Ice),

            // Desert/Ocean: Pure sand throughout shallow layer
            BiomeType.Desert or BiomeType.Ocean => TileType.Sand,

            // Jungle: Pure mud
            BiomeType.Jungle => TileType.Mud,

            // Mushroom: Mud substrate
            BiomeType.Mushroom => TileType.Mud,

            // Evil biomes: Dirt (evil stone starts deeper)
            BiomeType.Corruption or BiomeType.Crimson => TileType.Dirt,

            // Hallow: Dirt (pearlstone starts deeper)
            BiomeType.Hallow => TileType.Dirt,

            // Forest (default): Pure dirt
            _ => TileType.Dirt
        };
    }

    /// <summary>Get upper underground tile (~50/50 dirt/stone mix with clay).</summary>
    private TileType GetUpperUndergroundTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        float mixNoise = GetMixNoise(worldX, worldY, 0.08f);
        float clayNoise = _terrainNoise.Noise01(worldX * 0.12f + 500, worldY * 0.12f);

        // Clay pockets in forest/default areas (about 8% of underground)
        if (biome == BiomeType.Forest && clayNoise > 0.92f && mixNoise > 0.3f)
        {
            return TileType.Clay;
        }

        // Check for evil biome ores
        if (biome == BiomeType.Corruption || biome == BiomeType.Crimson)
        {
            TileType evilOre = GetEvilOre(worldX, worldY, biome, WorldLayer.Underground);
            if (evilOre != TileType.Air)
                return evilOre;
        }

        return biome switch
        {
            // Snow: Ice with occasional snow patches
            BiomeType.Snow => mixNoise > 0.3f ? TileType.Ice : TileType.Snow,

            // Desert: Transition from Sand to HardenedSand to Sandstone
            BiomeType.Desert or BiomeType.Ocean =>
                depth < DirtDepth + 10 ? (mixNoise > 0.4f ? TileType.HardenedSand : TileType.Sand) :
                (mixNoise > 0.5f ? TileType.Sandstone : TileType.HardenedSand),

            // Jungle: Mud with stone appearing
            BiomeType.Jungle => mixNoise > 0.6f ? TileType.Stone : TileType.Mud,

            // Mushroom: Mud with stone
            BiomeType.Mushroom => mixNoise > 0.6f ? TileType.Stone : TileType.Mud,

            // Corruption: Dirt transitioning to Ebonstone
            BiomeType.Corruption => mixNoise > 0.5f ? TileType.Ebonstone : TileType.Dirt,

            // Crimson: Dirt transitioning to Crimstone
            BiomeType.Crimson => mixNoise > 0.5f ? TileType.Crimstone : TileType.Dirt,

            // Hallow: Dirt transitioning to Pearlstone
            BiomeType.Hallow => mixNoise > 0.5f ? TileType.Pearlstone : TileType.Dirt,

            // Forest: 50/50 Dirt/Stone mix with clay
            _ => mixNoise > 0.5f ? TileType.Stone : TileType.Dirt
        };
    }

    /// <summary>Get lower underground tile (mostly stone with dirt patches).</summary>
    private TileType GetLowerUndergroundTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        // Check for evil biome ores first
        if (biome == BiomeType.Corruption || biome == BiomeType.Crimson)
        {
            TileType evilOre = GetEvilOre(worldX, worldY, biome, WorldLayer.Underground);
            if (evilOre != TileType.Air)
                return evilOre;
        }

        float mixNoise = GetMixNoise(worldX, worldY, 0.06f);
        float dirtPatchNoise = _terrainNoise.Noise01(worldX * 0.04f + 1000, worldY * 0.04f);

        // Occasional dirt patches (about 20%)
        bool isDirtPatch = dirtPatchNoise > 0.8f;

        return biome switch
        {
            // Snow: Mostly ice
            BiomeType.Snow => isDirtPatch && mixNoise > 0.7f ? TileType.Snow : TileType.Ice,

            // Desert: Mostly sandstone with hardened sand patches
            BiomeType.Desert or BiomeType.Ocean =>
                isDirtPatch ? TileType.HardenedSand : TileType.Sandstone,

            // Jungle: Stone with mud patches
            BiomeType.Jungle => isDirtPatch ? TileType.Mud : TileType.Stone,

            // Mushroom: Stone with mud patches
            BiomeType.Mushroom => isDirtPatch ? TileType.Mud : TileType.Stone,

            // Evil biomes: Mostly evil stone
            BiomeType.Corruption => isDirtPatch && mixNoise > 0.8f ? TileType.Dirt : TileType.Ebonstone,
            BiomeType.Crimson => isDirtPatch && mixNoise > 0.8f ? TileType.Dirt : TileType.Crimstone,

            // Hallow: Mostly pearlstone
            BiomeType.Hallow => isDirtPatch && mixNoise > 0.8f ? TileType.Dirt : TileType.Pearlstone,

            // Forest: Mostly stone with dirt patches
            _ => isDirtPatch ? TileType.Dirt : TileType.Stone
        };
    }

    /// <summary>Get cavern layer tile (stone with mini-biomes).</summary>
    private TileType GetCavernTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        // Check for evil biome ores first (higher chance in cavern)
        if (biome == BiomeType.Corruption || biome == BiomeType.Crimson)
        {
            TileType evilOre = GetEvilOre(worldX, worldY, biome, WorldLayer.Cavern);
            if (evilOre != TileType.Air)
                return evilOre;
        }

        // Check for granite/marble mini-biomes
        float miniBiomeNoise = _biomeNoise.Noise01(worldX * 0.03f + 1000, worldY * 0.03f);

        if (miniBiomeNoise > 0.85f)
            return TileType.GraniteBlock;
        if (miniBiomeNoise < 0.15f)
            return TileType.MarbleBlock;

        // Occasional dirt pockets even in cavern
        float dirtPocketNoise = _terrainNoise.Noise01(worldX * 0.03f + 2000, worldY * 0.03f);
        if (dirtPocketNoise > 0.9f)
        {
            return biome switch
            {
                BiomeType.Jungle => TileType.Mud,
                BiomeType.Snow => TileType.Ice,
                _ => TileType.Dirt
            };
        }

        // Deep biome stone (influence fades with depth)
        return GetDeepBiomeStone(biome, worldX, worldY);
    }

    /// <summary>Helper to get consistent mix noise for terrain blending.</summary>
    private float GetMixNoise(int worldX, int worldY, float frequency)
    {
        return _terrainNoise.Noise01(worldX * frequency, worldY * frequency);
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

    /// <summary>
    /// Get ore type based on depth and layer.
    /// Terraria-style tiered distribution:
    /// - T1 (Copper): Shallow underground, most common
    /// - T2 (Iron): Mid underground, common
    /// - T3 (Silver): Deep underground/upper cavern, uncommon
    /// - T4 (Gold): Cavern layer, rare
    /// - Special: Hellstone (Underworld), Demonite (Corruption), Crimtane (Crimson)
    /// </summary>
    private TileType GetOreType(int worldX, int worldY, int depth, WorldLayer layer)
    {
        float baseNoise = _oreNoise.Noise01(worldX * 0.08f, worldY * 0.08f);

        // === TIER 1: COPPER - starts shallow, most abundant ===
        if (depth >= 5)
        {
            float copperNoise = _oreNoise.Noise01(worldX * 0.15f + 100, worldY * 0.15f);
            // More common in surface/upper underground, rarer deeper
            float copperThreshold = layer == WorldLayer.Surface ? 0.72f : 0.76f;
            if (copperNoise > copperThreshold && baseNoise > 0.58f)
                return TileType.CopperOre;
        }

        // === TIER 2: IRON - underground layer, common ===
        if (depth >= 15 && layer != WorldLayer.Surface)
        {
            float ironNoise = _oreNoise.Noise01(worldX * 0.15f + 200, worldY * 0.15f);
            float ironThreshold = layer == WorldLayer.Underground ? 0.76f : 0.80f;
            if (ironNoise > ironThreshold && baseNoise > 0.62f)
                return TileType.IronOre;
        }

        // === TIER 3: SILVER - deep underground/cavern, uncommon ===
        if (depth >= 40 && (layer == WorldLayer.Underground || layer == WorldLayer.Cavern))
        {
            float silverNoise = _oreNoise.Noise01(worldX * 0.15f + 300, worldY * 0.15f);
            float silverThreshold = layer == WorldLayer.Cavern ? 0.82f : 0.85f;
            if (silverNoise > silverThreshold && baseNoise > 0.68f)
                return TileType.SilverOre;
        }

        // === TIER 4: GOLD - cavern layer only, rare ===
        if (layer == WorldLayer.Cavern)
        {
            float goldNoise = _oreNoise.Noise01(worldX * 0.15f + 400, worldY * 0.15f);
            if (goldNoise > 0.88f && baseNoise > 0.75f)
                return TileType.GoldOre;
        }

        // === SPECIAL: HELLSTONE - Underworld only ===
        if (layer == WorldLayer.Underworld)
        {
            float hellstoneNoise = _oreNoise.Noise01(worldX * 0.12f + 500, worldY * 0.12f);
            if (hellstoneNoise > 0.82f && baseNoise > 0.6f)
                return TileType.Hellstone;
        }

        return TileType.Air;
    }

    /// <summary>
    /// Get evil biome ore (for corruption/crimson terrain generation).
    /// Called separately from GetOreType since evil ores replace stone, not add to it.
    /// </summary>
    private TileType GetEvilOre(int worldX, int worldY, BiomeType biome, WorldLayer layer)
    {
        if (layer != WorldLayer.Underground && layer != WorldLayer.Cavern)
            return TileType.Air;

        float oreNoise = _oreNoise.Noise01(worldX * 0.1f + 600, worldY * 0.1f);
        if (oreNoise > 0.90f)
        {
            return biome switch
            {
                BiomeType.Corruption or BiomeType.UndergroundCorruption => TileType.DemoniteOre,
                BiomeType.Crimson or BiomeType.UndergroundCrimson => TileType.CrimtaneOre,
                _ => TileType.Air
            };
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
/// - Smooth transitions between biomes (20-40 tile blend zones)
/// </summary>
public class BiomePlacement
{
    private readonly WorldConfig _config;
    private readonly Random _random;
    private readonly List<BiomeZone> _zones = new();

    /// <summary>Width of biome transition zones in tiles.</summary>
    public const int TRANSITION_WIDTH = 50;

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

        // === SPAWN SAFE ZONE ===
        // Player always spawns in Forest at world center
        // Keep a buffer zone around spawn point free of other biomes
        int spawnSafeRadius = (int)(width * 0.06f);  // ~6% of world width on each side
        int spawnSafeStart = spawnX - spawnSafeRadius;
        int spawnSafeEnd = spawnX + spawnSafeRadius;

        System.Diagnostics.Debug.WriteLine($"Spawn Safe Zone: {spawnSafeStart} - {spawnSafeEnd} (radius {spawnSafeRadius})");

        // Calculate biome widths based on world size
        int jungleWidth = (int)(width * 0.15f);
        int snowWidth = (int)(width * 0.12f);
        int desertWidth = (int)(width * 0.08f);
        int evilWidth = (int)(width * 0.10f);

        // === JUNGLE (always on one side) ===
        int jungleStart, jungleEnd;
        if (_config.JungleOnLeft)
        {
            jungleStart = oceanWidth + 100;
            jungleEnd = jungleStart + jungleWidth;
            // Ensure jungle doesn't reach spawn zone
            jungleEnd = Math.Min(jungleEnd, spawnSafeStart - TRANSITION_WIDTH);
        }
        else
        {
            jungleEnd = width - oceanWidth - 100;
            jungleStart = jungleEnd - jungleWidth;
            // Ensure jungle doesn't reach spawn zone
            jungleStart = Math.Max(jungleStart, spawnSafeEnd + TRANSITION_WIDTH);
        }
        if (jungleEnd > jungleStart)
        {
            _zones.Add(new BiomeZone(jungleStart, jungleEnd, BiomeType.Jungle));
        }

        // === SNOW (opposite side from Jungle) ===
        int snowStart, snowEnd;
        if (!_config.JungleOnLeft)
        {
            snowStart = oceanWidth + 150;
            snowEnd = snowStart + snowWidth;
            // Ensure snow doesn't reach spawn zone
            snowEnd = Math.Min(snowEnd, spawnSafeStart - TRANSITION_WIDTH);
        }
        else
        {
            snowEnd = width - oceanWidth - 150;
            snowStart = snowEnd - snowWidth;
            // Ensure snow doesn't reach spawn zone
            snowStart = Math.Max(snowStart, spawnSafeEnd + TRANSITION_WIDTH);
        }
        if (snowEnd > snowStart)
        {
            _zones.Add(new BiomeZone(snowStart, snowEnd, BiomeType.Snow));
        }

        // === DESERT (placed away from spawn, on opposite side from evil biome) ===
        int desertStart, desertEnd;

        // Place desert between snow/jungle and spawn zone, or on the outer edge
        if (_config.JungleOnLeft)
        {
            // Jungle on left, so place desert between jungle and spawn zone OR far right
            if (_random.Next(2) == 0 && jungleEnd + 100 + desertWidth < spawnSafeStart - TRANSITION_WIDTH)
            {
                // Between jungle and spawn
                desertStart = jungleEnd + 100 + _random.Next(50, 150);
                desertEnd = desertStart + desertWidth;
                desertEnd = Math.Min(desertEnd, spawnSafeStart - TRANSITION_WIDTH);
            }
            else
            {
                // Far right, between spawn and snow
                desertStart = spawnSafeEnd + TRANSITION_WIDTH + 100 + _random.Next(50, 200);
                desertEnd = desertStart + desertWidth;
                // Don't overlap with snow
                if (desertEnd > snowStart - 50)
                {
                    desertEnd = snowStart - 50;
                }
            }
        }
        else
        {
            // Jungle on right, place desert between snow and spawn zone OR far left
            if (_random.Next(2) == 0 && snowEnd + 100 + desertWidth < spawnSafeStart - TRANSITION_WIDTH)
            {
                // Between snow and spawn
                desertStart = snowEnd + 100 + _random.Next(50, 150);
                desertEnd = desertStart + desertWidth;
                desertEnd = Math.Min(desertEnd, spawnSafeStart - TRANSITION_WIDTH);
            }
            else
            {
                // Far left, between spawn and jungle
                desertStart = spawnSafeEnd + TRANSITION_WIDTH + 100 + _random.Next(50, 200);
                desertEnd = desertStart + desertWidth;
                // Don't overlap with jungle
                if (desertEnd > jungleStart - 50)
                {
                    desertEnd = jungleStart - 50;
                }
            }
        }

        // Clamp desert to valid range
        desertStart = Math.Max(oceanWidth + 50, desertStart);
        desertEnd = Math.Min(width - oceanWidth - 50, desertEnd);

        // Only add desert if it has valid size and doesn't touch spawn zone
        if (desertEnd > desertStart + 100 &&
            (desertEnd < spawnSafeStart - TRANSITION_WIDTH || desertStart > spawnSafeEnd + TRANSITION_WIDTH))
        {
            _zones.Add(new BiomeZone(desertStart, desertEnd, BiomeType.Desert));
            System.Diagnostics.Debug.WriteLine($"Desert placed: {desertStart} - {desertEnd}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Desert skipped (would overlap spawn zone or too small)");
        }

        // === EVIL BIOME (opposite side from jungle, away from spawn) ===
        BiomeType evilType = _config.HasCrimson ? BiomeType.Crimson : BiomeType.Corruption;
        int evilStart, evilEnd;

        if (_config.JungleOnLeft)
        {
            // Jungle on left, evil goes on right side (past spawn zone)
            evilStart = spawnSafeEnd + TRANSITION_WIDTH + 200 + _random.Next(100, 300);
            evilEnd = evilStart + evilWidth;

            // Avoid snow overlap
            if (snowStart > 0 && evilStart < snowEnd + 50)
            {
                evilStart = snowEnd + 100;
                evilEnd = evilStart + evilWidth;
            }
        }
        else
        {
            // Jungle on right, evil goes on left side
            evilEnd = spawnSafeStart - TRANSITION_WIDTH - 200 - _random.Next(100, 300);
            evilStart = evilEnd - evilWidth;

            // Avoid snow overlap
            if (snowEnd > 0 && evilEnd > snowStart - 50)
            {
                evilEnd = snowStart - 100;
                evilStart = evilEnd - evilWidth;
            }
        }

        // Clamp evil biome to valid range
        evilStart = Math.Max(oceanWidth + 50, evilStart);
        evilEnd = Math.Min(width - oceanWidth - 50, evilEnd);

        // Only add evil biome if it has valid size and doesn't touch spawn zone
        if (evilEnd > evilStart + 100 &&
            (evilEnd < spawnSafeStart - TRANSITION_WIDTH || evilStart > spawnSafeEnd + TRANSITION_WIDTH))
        {
            _zones.Add(new BiomeZone(evilStart, evilEnd, evilType));
            System.Diagnostics.Debug.WriteLine($"Evil biome ({evilType}) placed: {evilStart} - {evilEnd}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Evil biome skipped (would overlap spawn zone or too small)");
        }

        // Sort zones by start position
        _zones.Sort((a, b) => a.StartX.CompareTo(b.StartX));

        // Debug output
        System.Diagnostics.Debug.WriteLine("=== Final Biome Zones ===");
        foreach (var zone in _zones)
        {
            System.Diagnostics.Debug.WriteLine($"  {zone.Biome}: {zone.StartX} - {zone.EndX}");
        }
    }

    /// <summary>Get the primary biome at a position (for UI/detection).</summary>
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

    /// <summary>
    /// Get biome blend information for smooth terrain transitions.
    /// Returns the two biomes to blend and the blend factor.
    /// Biome1 = left/from biome, Biome2 = right/to biome
    /// BlendFactor 0.0 = fully Biome1, BlendFactor 1.0 = fully Biome2
    /// </summary>
    public BiomeBlendInfo GetBiomeBlendInfo(int worldX)
    {
        BiomeType primaryBiome = BiomeType.Forest;
        BiomeZone? primaryZone = null;

        // Find the primary biome zone we're inside
        foreach (var zone in _zones)
        {
            if (worldX >= zone.StartX && worldX < zone.EndX)
            {
                primaryBiome = zone.Biome;
                primaryZone = zone;
                break;
            }
        }

        // If not inside any zone, we're in Forest - check if near a zone boundary
        if (primaryZone == null)
        {
            foreach (var zone in _zones)
            {
                // Approaching left edge of a zone (Forest → Zone)
                // worldX is in Forest, zone starts soon
                if (worldX >= zone.StartX - TRANSITION_WIDTH && worldX < zone.StartX)
                {
                    float blend = (float)(worldX - (zone.StartX - TRANSITION_WIDTH)) / TRANSITION_WIDTH;
                    return new BiomeBlendInfo(BiomeType.Forest, zone.Biome, blend);
                }

                // Past right edge of a zone (Zone → Forest)
                // worldX is in Forest, just left the zone
                if (worldX >= zone.EndX && worldX < zone.EndX + TRANSITION_WIDTH)
                {
                    float blend = (float)(worldX - zone.EndX) / TRANSITION_WIDTH;
                    return new BiomeBlendInfo(zone.Biome, BiomeType.Forest, blend);
                }
            }

            // Not near any transition
            return new BiomeBlendInfo(BiomeType.Forest, BiomeType.Forest, 0.5f);
        }

        // We're inside a biome zone - check if near its edges
        int distToStart = worldX - primaryZone.StartX;
        int distToEnd = primaryZone.EndX - worldX - 1;

        // Get adjacent biomes
        BiomeType leftBiome = GetAdjacentBiome(worldX, primaryZone, true);
        BiomeType rightBiome = GetAdjacentBiome(worldX, primaryZone, false);

        // Near left edge - transitioning FROM left biome INTO this one
        // distToStart = 0 means we just entered, should be mostly leftBiome
        // distToStart = TRANSITION_WIDTH means we're past transition, fully primaryBiome
        if (distToStart < TRANSITION_WIDTH)
        {
            float blend = (float)distToStart / TRANSITION_WIDTH;
            return new BiomeBlendInfo(leftBiome, primaryBiome, blend);
        }

        // Near right edge - transitioning FROM this biome INTO right biome
        // distToEnd = TRANSITION_WIDTH means we're just entering transition, mostly primaryBiome
        // distToEnd = 0 means we're about to leave, should be mostly rightBiome
        if (distToEnd < TRANSITION_WIDTH)
        {
            float blend = 1.0f - ((float)distToEnd / TRANSITION_WIDTH);
            return new BiomeBlendInfo(primaryBiome, rightBiome, blend);
        }

        // Not in transition zone - pure biome
        return new BiomeBlendInfo(primaryBiome, primaryBiome, 0.5f);
    }

    /// <summary>Get the biome adjacent to a zone.</summary>
    private BiomeType GetAdjacentBiome(int worldX, BiomeZone zone, bool leftSide)
    {
        int searchX = leftSide ? zone.StartX - 1 : zone.EndX;

        foreach (var otherZone in _zones)
        {
            if (otherZone == zone) continue;

            if (searchX >= otherZone.StartX && searchX < otherZone.EndX)
            {
                return otherZone.Biome;
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

/// <summary>
/// Information about biome blending at a position.
/// Used for smooth terrain transitions between biomes.
/// </summary>
public struct BiomeBlendInfo
{
    /// <summary>The biome being transitioned FROM (or left biome at boundary).</summary>
    public BiomeType Biome1 { get; }

    /// <summary>The biome being transitioned TO (or right biome at boundary).</summary>
    public BiomeType Biome2 { get; }

    /// <summary>Blend factor: 0.0 = fully Biome1, 1.0 = fully Biome2.</summary>
    public float BlendFactor { get; }

    /// <summary>True if this position is in a transition zone between biomes.</summary>
    public bool IsTransition => Biome1 != Biome2;

    public BiomeBlendInfo(BiomeType biome1, BiomeType biome2, float blendFactor)
    {
        Biome1 = biome1;
        Biome2 = biome2;
        BlendFactor = Math.Clamp(blendFactor, 0f, 1f);
    }
}