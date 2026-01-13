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

        // Get base biome height modifier
        BiomeType biome = GetSurfaceBiome(worldX);
        float biomeHeightMod = GetBiomeHeightMod(biome);
        int baseLevel = GetBiomeBaseLevel(biome);

        // Check for transition zone and interpolate height
        if (_biomePlacement != null)
        {
            var blendInfo = _biomePlacement.GetBiomeBlendInfo(worldX);
            if (blendInfo.IsTransition)
            {
                // Get height mods for both biomes
                float heightMod1 = GetBiomeHeightMod(blendInfo.Biome1);
                float heightMod2 = GetBiomeHeightMod(blendInfo.Biome2);
                int baseLevel1 = GetBiomeBaseLevel(blendInfo.Biome1);
                int baseLevel2 = GetBiomeBaseLevel(blendInfo.Biome2);

                // Smoothly interpolate between biome heights using smoothstep
                float t = blendInfo.BlendFactor;
                float smoothT = t * t * (3f - 2f * t);  // Smoothstep for gradual transition

                biomeHeightMod = heightMod1 + (heightMod2 - heightMod1) * smoothT;
                baseLevel = (int)(baseLevel1 + (baseLevel2 - baseLevel1) * smoothT);
            }
        }

        int height = (int)(noise * TerrainHeight * biomeHeightMod);
        return baseLevel + height;
    }

    /// <summary>Get the height modifier for a biome.</summary>
    private float GetBiomeHeightMod(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Desert => 0.3f,
            BiomeType.Snow => 1.2f,
            BiomeType.Jungle => 0.8f,
            BiomeType.Mushroom => 0.5f,
            BiomeType.Ocean => 0.2f,
            _ => 1.0f
        };
    }

    /// <summary>Get the base surface level for a biome.</summary>
    private int GetBiomeBaseLevel(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Ocean => SurfaceLevel + 20,
            _ => SurfaceLevel
        };
    }

    private TileType GetTileType(int worldX, int worldY, int surfaceY, BiomeType surfaceBiome)
    {
        if (worldY < surfaceY)
            return TileType.Air;

        int depth = worldY - surfaceY;

        WorldLayer layer = Config.GetLayerAt(worldY);

        // === CAVERN to UNDERWORLD TRANSITION ===
        // Create a gradual blend zone at the boundary
        int transitionStart = Config.UnderworldBoundary - 80;  // Start blending 80 tiles before underworld
        int transitionEnd = Config.UnderworldBoundary + 30;    // End blending 30 tiles into underworld

        if (worldY >= transitionStart && worldY <= transitionEnd)
        {
            // Calculate blend factor (0 = cavern, 1 = underworld)
            float blendFactor = (worldY - transitionStart) / (float)(transitionEnd - transitionStart);

            // Use noise to create organic transition boundary
            float transitionNoise = _biomeNoise.Noise01(worldX * 0.03f + 8000, worldY * 0.02f);

            // Adjust blend factor with noise for jagged natural edge
            float adjustedBlend = blendFactor + (transitionNoise - 0.5f) * 0.4f;

            if (adjustedBlend > 0.5f)
            {
                // Underworld tile
                return GetUnderworldTile(worldX, worldY, depth);
            }
            // Otherwise continue to cavern generation below
        }
        else if (layer == WorldLayer.Underworld)
        {
            // Fully in underworld
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
                // SURFACE TILES (depth 0-2): Respect zone boundaries strictly
                // Only switch biomes when ENTERING a new zone, not when approaching edge from inside
                if (depth <= 2)
                {
                    // Get the actual biome at this position (respects zone boundaries)
                    BiomeType actualBiome = _biomePlacement.GetBiomeAt(worldX);

                    // Only blend in the Forest (no-zone) areas approaching a biome zone
                    // If we're inside a defined zone, use that zone's biome
                    if (actualBiome != BiomeType.Forest)
                    {
                        // We're inside a biome zone - use it, no blending
                        return GetBiomeTile(worldX, worldY, depth, actualBiome, layer);
                    }

                    // We're in Forest approaching another biome - use blend
                    BiomeType surfaceBiomeChoice = blendInfo.BlendFactor >= 0.5f ? blendInfo.Biome2 : blendInfo.Biome1;
                    return GetBiomeTile(worldX, worldY, depth, surfaceBiomeChoice, layer);
                }

                // UNDERGROUND TILES (depth 3+): Use noise for organic blending
                // Lower frequency noise = larger patches of each biome
                float transitionNoise = _biomeNoise.Noise01(worldX * 0.05f + 5000, worldY * 0.03f);

                // Bias the noise based on blend factor - the further into new biome, the more new tiles
                float threshold = blendInfo.BlendFactor;

                // When noise < threshold, pick Biome2 (new), else pick Biome1 (old)
                BiomeType undergroundBiomeChoice = transitionNoise < threshold ? blendInfo.Biome2 : blendInfo.Biome1;
                return GetBiomeTile(worldX, worldY, depth, undergroundBiomeChoice, layer);
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
    /// Implements Terraria-style terrain composition with GRADUAL transitions:
    /// - Surface (depth 0): Grass/biome surface tile
    /// - Very Shallow (1-5): Pure dirt/subsurface, no stone
    /// - Shallow (6-20): Mostly dirt, rare small stone veins (5-15%)
    /// - Upper Underground (21-50): Dirt with increasing stone (20-50%)
    /// - Lower Underground (51-100): Mostly stone with dirt patches (60-80% stone)
    /// - Deep/Cavern (100+): Almost all stone (85%+ stone)
    /// </summary>
    private TileType GetBiomeTile(int worldX, int worldY, int depth, BiomeType biome, WorldLayer layer)
    {
        // === SURFACE TILE (depth 0) ===
        if (depth == 0)
        {
            return GetSurfaceTile(biome);
        }

        // === VERY SHALLOW (depth 1-5): Pure subsurface, NO stone at all ===
        if (depth <= 5)
        {
            return GetVeryShallowTile(biome);
        }

        // === SHALLOW (depth 6-20): Mostly dirt, rare stone/ore veins ===
        if (depth <= 20)
        {
            return GetShallowTile(worldX, worldY, depth, biome);
        }

        // Check for ores (they appear below shallow layer)
        TileType oreType = GetOreType(worldX, worldY, depth, layer);
        if (oreType != TileType.Air)
            return oreType;

        // === UPPER UNDERGROUND (depth 21-50): Increasing stone mix ===
        if (depth <= 50)
        {
            return GetUpperUndergroundTile(worldX, worldY, depth, biome);
        }

        // === LOWER UNDERGROUND (depth 51-100): Mostly stone ===
        if (depth <= 100 || layer == WorldLayer.Underground)
        {
            return GetLowerUndergroundTile(worldX, worldY, depth, biome);
        }

        // === CAVERN LAYER (depth 100+): Stone with mini-biomes ===
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

    /// <summary>Get very shallow tile (depth 1-5): Pure subsurface, no stone.</summary>
    private TileType GetVeryShallowTile(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Snow => TileType.Snow,
            BiomeType.Desert or BiomeType.Ocean => TileType.Sand,
            BiomeType.Jungle or BiomeType.Mushroom => TileType.Mud,
            _ => TileType.Dirt
        };
    }

    /// <summary>Get shallow tile (depth 6-20): Mostly dirt, rare stone veins (5-15%).</summary>
    private TileType GetShallowTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        // Calculate stone probability: 5% at depth 6, up to 15% at depth 20
        float stoneChance = 0.05f + (depth - 6) * 0.007f;  // ~5% to ~15%

        float noise = GetMixNoise(worldX, worldY, 0.12f);

        // Very rare stone veins
        if (noise > (1.0f - stoneChance))
        {
            return GetBiomeStone(biome);
        }

        // Biome-specific subsurface
        return biome switch
        {
            BiomeType.Snow => depth < 10 ? TileType.Snow : TileType.Ice,
            BiomeType.Desert or BiomeType.Ocean => TileType.Sand,
            BiomeType.Jungle or BiomeType.Mushroom => TileType.Mud,
            BiomeType.Corruption or BiomeType.Crimson or BiomeType.Hallow => TileType.Dirt,
            _ => TileType.Dirt
        };
    }

    /// <summary>Get upper underground tile (depth 21-50): Increasing stone mix (20-50%).</summary>
    private TileType GetUpperUndergroundTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        // Calculate stone probability: 20% at depth 21, up to 50% at depth 50
        float stoneChance = 0.20f + (depth - 21) * 0.01f;  // ~20% to ~50%

        float mixNoise = GetMixNoise(worldX, worldY, 0.08f);
        float clayNoise = _terrainNoise.Noise01(worldX * 0.1f + 500, worldY * 0.1f);

        // Clay pockets in forest areas (about 5% chance)
        if (biome == BiomeType.Forest && clayNoise > 0.95f)
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

        // Stone vs subsurface based on probability
        bool isStone = mixNoise > (1.0f - stoneChance);

        return biome switch
        {
            BiomeType.Snow => isStone ? TileType.Ice : TileType.Snow,
            BiomeType.Desert or BiomeType.Ocean => isStone ?
                (depth > 35 ? TileType.Sandstone : TileType.HardenedSand) : TileType.Sand,
            BiomeType.Jungle or BiomeType.Mushroom => isStone ? TileType.Stone : TileType.Mud,
            BiomeType.Corruption => isStone ? TileType.Ebonstone : TileType.Dirt,
            BiomeType.Crimson => isStone ? TileType.Crimstone : TileType.Dirt,
            BiomeType.Hallow => isStone ? TileType.Pearlstone : TileType.Dirt,
            _ => isStone ? TileType.Stone : TileType.Dirt
        };
    }

    /// <summary>Get lower underground tile (depth 51-100): Mostly stone (60-80%).</summary>
    private TileType GetLowerUndergroundTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        // Calculate stone probability: 60% at depth 51, up to 80% at depth 100
        float stoneChance = 0.60f + (depth - 51) * 0.004f;  // ~60% to ~80%
        stoneChance = Math.Min(stoneChance, 0.80f);  // Cap at 80%

        float mixNoise = GetMixNoise(worldX, worldY, 0.06f);

        // Check for evil biome ores
        if (biome == BiomeType.Corruption || biome == BiomeType.Crimson)
        {
            TileType evilOre = GetEvilOre(worldX, worldY, biome, WorldLayer.Underground);
            if (evilOre != TileType.Air)
                return evilOre;
        }

        // Dirt pockets in stone
        bool isDirt = mixNoise > stoneChance;

        return biome switch
        {
            BiomeType.Snow => isDirt ? TileType.Snow : TileType.Ice,
            BiomeType.Desert or BiomeType.Ocean => isDirt ? TileType.HardenedSand : TileType.Sandstone,
            BiomeType.Jungle or BiomeType.Mushroom => isDirt ? TileType.Mud : TileType.Stone,
            BiomeType.Corruption => isDirt ? TileType.Dirt : TileType.Ebonstone,
            BiomeType.Crimson => isDirt ? TileType.Dirt : TileType.Crimstone,
            BiomeType.Hallow => isDirt ? TileType.Dirt : TileType.Pearlstone,
            _ => isDirt ? TileType.Dirt : TileType.Stone
        };
    }

    /// <summary>Get cavern layer tile (depth 100+): Almost all stone (85%+) with mini-biomes.</summary>
    private TileType GetCavernTile(int worldX, int worldY, int depth, BiomeType biome)
    {
        // Check for evil biome ores (higher chance in cavern)
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

        // Rare dirt pockets even in cavern (15%)
        float dirtPocketNoise = GetMixNoise(worldX, worldY, 0.04f);
        if (dirtPocketNoise > 0.85f)
        {
            return biome switch
            {
                BiomeType.Jungle => TileType.Mud,
                BiomeType.Snow => TileType.Ice,
                BiomeType.Desert => TileType.Sandstone,
                _ => TileType.Dirt
            };
        }

        // Deep biome stone
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
    /// 
    /// CRITICAL: Blend factor must be CONTINUOUS across zone boundaries!
    /// The transition zone spans TRANSITION_WIDTH tiles centered on the boundary.
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
                // Transition starts TRANSITION_WIDTH/2 tiles before zone
                int transitionStart = zone.StartX - TRANSITION_WIDTH / 2;
                if (worldX >= transitionStart && worldX < zone.StartX)
                {
                    // Blend goes from 0.0 at transitionStart to 0.5 at zone.StartX
                    float blend = (float)(worldX - transitionStart) / TRANSITION_WIDTH;
                    return new BiomeBlendInfo(BiomeType.Forest, zone.Biome, blend);
                }

                // Past right edge of a zone (Zone → Forest)
                // Transition ends TRANSITION_WIDTH/2 tiles after zone
                int transitionEnd = zone.EndX + TRANSITION_WIDTH / 2;
                if (worldX >= zone.EndX && worldX < transitionEnd)
                {
                    // Blend goes from 0.5 at zone.EndX to 1.0 at transitionEnd
                    float blend = 0.5f + (float)(worldX - zone.EndX) / TRANSITION_WIDTH;
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

        // Near left edge - continuing transition FROM left biome INTO this one
        // Transition continues from 0.5 at boundary to 1.0 at TRANSITION_WIDTH/2 inside
        int halfTransition = TRANSITION_WIDTH / 2;
        if (distToStart < halfTransition)
        {
            // Blend goes from 0.5 at distToStart=0 to 1.0 at distToStart=halfTransition
            float blend = 0.5f + (float)distToStart / TRANSITION_WIDTH;
            return new BiomeBlendInfo(leftBiome, primaryBiome, blend);
        }

        // Near right edge - transitioning FROM this biome INTO right biome
        // Transition starts at TRANSITION_WIDTH/2 before end, blend goes from 0.0 to 0.5
        if (distToEnd < halfTransition)
        {
            // Blend goes from 0.0 at distToEnd=halfTransition to 0.5 at distToEnd=0
            float blend = (float)(halfTransition - distToEnd) / TRANSITION_WIDTH;
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