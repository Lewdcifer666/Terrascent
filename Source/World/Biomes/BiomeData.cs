using Microsoft.Xna.Framework;

namespace Terrascent.World.Biomes;

/// <summary>
/// Defines all properties for a biome including tiles, colors, spawn rates, and spreading behavior.
/// </summary>
public class BiomeData
{
    /// <summary>
    /// The biome type this data represents.
    /// </summary>
    public BiomeType Type { get; init; }

    /// <summary>
    /// Display name for the biome.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Tiles that count toward this biome's detection.
    /// </summary>
    public TileType[] AssociatedTiles { get; init; } = [];

    /// <summary>
    /// Primary surface tile for this biome.
    /// </summary>
    public TileType SurfaceTile { get; init; } = TileType.Grass;

    /// <summary>
    /// Subsurface tile (just below surface).
    /// </summary>
    public TileType SubsurfaceTile { get; init; } = TileType.Dirt;

    /// <summary>
    /// Stone variant for this biome.
    /// </summary>
    public TileType StoneTile { get; init; } = TileType.Stone;

    /// <summary>
    /// Minimum tile count to detect this biome.
    /// </summary>
    public int MinTileCount { get; init; } = 50;

    /// <summary>
    /// Detection radius in tiles.
    /// </summary>
    public int DetectionRadius { get; init; } = 42;

    /// <summary>
    /// Priority for biome detection (higher = checked first).
    /// </summary>
    public int Priority { get; init; } = 0;

    // === Spreading Properties ===

    /// <summary>
    /// Whether this biome spreads to adjacent tiles.
    /// </summary>
    public bool CanSpread { get; init; } = false;

    /// <summary>
    /// Chance per tick that spreading occurs (0-1).
    /// </summary>
    public float SpreadChance { get; init; } = 0.01f;

    /// <summary>
    /// Maximum radius tiles can spread per operation.
    /// </summary>
    public int SpreadRadius { get; init; } = 3;

    /// <summary>
    /// Tile conversions when spreading (original -> converted).
    /// </summary>
    public Dictionary<TileType, TileType> SpreadConversions { get; init; } = new();

    // === Visual Properties ===

    /// <summary>
    /// Primary color for this biome (for UI/minimap).
    /// </summary>
    public Color Color { get; init; } = Color.Green;

    /// <summary>
    /// Sky color tint for this biome.
    /// </summary>
    public Color SkyColor { get; init; } = Color.CornflowerBlue;

    /// <summary>
    /// Water color for this biome.
    /// </summary>
    public Color WaterColor { get; init; } = Color.Blue;

    // === Spawn Properties ===

    /// <summary>
    /// Minimum depth (Y) for this biome. 0 = surface.
    /// </summary>
    public int MinDepth { get; init; } = 0;

    /// <summary>
    /// Maximum depth (Y) for this biome.
    /// </summary>
    public int MaxDepth { get; init; } = 1000;

    /// <summary>
    /// Enemy spawn rate multiplier.
    /// </summary>
    public float SpawnRateMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Whether this biome has unique music.
    /// </summary>
    public bool HasUniqueMusic { get; init; } = false;

    /// <summary>
    /// Whether hardmode is required for this biome.
    /// </summary>
    public bool RequiresHardmode { get; init; } = false;

    // === Loot/Economy Properties ===

    /// <summary>
    /// Danger level (1-10) affecting difficulty.
    /// </summary>
    public int DangerLevel { get; init; } = 1;

    /// <summary>
    /// Gold drop multiplier in this biome.
    /// </summary>
    public float GoldMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// XP multiplier in this biome.
    /// </summary>
    public float XPMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Rare item drop chance multiplier.
    /// </summary>
    public float RareDropMultiplier { get; init; } = 1.0f;
}

/// <summary>
/// Registry containing all biome definitions.
/// </summary>
public static class BiomeRegistry
{
    private static readonly Dictionary<BiomeType, BiomeData> _biomes = new();

    static BiomeRegistry()
    {
        RegisterAll();
    }

    private static void RegisterAll()
    {
        // === FOREST (Default) ===
        Register(new BiomeData
        {
            Type = BiomeType.Forest,
            Name = "Forest",
            AssociatedTiles = [TileType.Grass, TileType.Dirt],
            SurfaceTile = TileType.Grass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Stone,
            MinTileCount = 0,  // Default biome
            Priority = -100,   // Lowest priority (fallback)
            Color = new Color(34, 139, 34),
            SkyColor = new Color(100, 149, 237),
            DangerLevel = 1,
            SpawnRateMultiplier = 1.0f,
            GoldMultiplier = 1.0f,
            XPMultiplier = 1.0f,
            HasUniqueMusic = true
        });

        // === DESERT ===
        Register(new BiomeData
        {
            Type = BiomeType.Desert,
            Name = "Desert",
            AssociatedTiles = [TileType.Sand, TileType.Sandstone, TileType.HardenedSand, TileType.Cactus],
            SurfaceTile = TileType.Sand,
            SubsurfaceTile = TileType.HardenedSand,
            StoneTile = TileType.Sandstone,
            MinTileCount = 400,
            Priority = 10,
            Color = new Color(237, 201, 175),
            SkyColor = new Color(255, 200, 100),
            WaterColor = new Color(100, 200, 200),
            DangerLevel = 2,
            SpawnRateMultiplier = 1.2f,
            GoldMultiplier = 1.1f,
            XPMultiplier = 1.1f,
            HasUniqueMusic = true
        });

        // === SNOW ===
        Register(new BiomeData
        {
            Type = BiomeType.Snow,
            Name = "Snow",
            AssociatedTiles = [TileType.Snow, TileType.Ice, TileType.ThinIce, TileType.SnowBrick],
            SurfaceTile = TileType.Snow,
            SubsurfaceTile = TileType.Snow,
            StoneTile = TileType.Ice,
            MinTileCount = 300,
            Priority = 10,
            Color = new Color(255, 250, 250),
            SkyColor = new Color(200, 220, 255),
            WaterColor = new Color(100, 180, 255),
            DangerLevel = 2,
            SpawnRateMultiplier = 1.1f,
            GoldMultiplier = 1.0f,
            XPMultiplier = 1.1f,
            HasUniqueMusic = true
        });

        // === JUNGLE ===
        Register(new BiomeData
        {
            Type = BiomeType.Jungle,
            Name = "Jungle",
            AssociatedTiles = [TileType.JungleGrass, TileType.Mud, TileType.LivingMahogany, TileType.RichMahogany],
            SurfaceTile = TileType.JungleGrass,
            SubsurfaceTile = TileType.Mud,
            StoneTile = TileType.Stone,
            MinTileCount = 140,
            Priority = 15,
            Color = new Color(0, 100, 0),
            SkyColor = new Color(80, 180, 80),
            WaterColor = new Color(50, 150, 50),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.5f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.4f,
            RareDropMultiplier = 1.2f,
            HasUniqueMusic = true
        });

        // === MUSHROOM ===
        Register(new BiomeData
        {
            Type = BiomeType.Mushroom,
            Name = "Glowing Mushroom",
            AssociatedTiles = [TileType.MushroomGrass, TileType.GlowingMushroom],
            SurfaceTile = TileType.MushroomGrass,
            SubsurfaceTile = TileType.Mud,
            MinTileCount = 100,
            Priority = 20,
            CanSpread = true,
            SpreadChance = 0.005f,
            SpreadRadius = 2,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Grass] = TileType.MushroomGrass,
                [TileType.JungleGrass] = TileType.MushroomGrass
            },
            Color = new Color(0, 100, 255),
            SkyColor = new Color(30, 30, 80),
            DangerLevel = 3,
            SpawnRateMultiplier = 0.8f,
            GoldMultiplier = 1.2f,
            XPMultiplier = 1.2f,
            HasUniqueMusic = true
        });

        // === CORRUPTION ===
        Register(new BiomeData
        {
            Type = BiomeType.Corruption,
            Name = "The Corruption",
            AssociatedTiles = [TileType.CorruptGrass, TileType.Ebonstone, TileType.CorruptSand, TileType.CorruptIce],
            SurfaceTile = TileType.CorruptGrass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Ebonstone,
            MinTileCount = 200,
            Priority = 50,
            CanSpread = true,
            SpreadChance = 0.02f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Grass] = TileType.CorruptGrass,
                [TileType.Dirt] = TileType.Dirt,  // Dirt stays but grass converts
                [TileType.Stone] = TileType.Ebonstone,
                [TileType.Sand] = TileType.CorruptSand,
                [TileType.Sandstone] = TileType.CorruptSandstone,
                [TileType.Ice] = TileType.CorruptIce
            },
            Color = new Color(100, 50, 150),
            SkyColor = new Color(80, 40, 100),
            WaterColor = new Color(100, 60, 140),
            DangerLevel = 5,
            SpawnRateMultiplier = 1.8f,
            GoldMultiplier = 1.5f,
            XPMultiplier = 1.6f,
            RareDropMultiplier = 1.3f,
            HasUniqueMusic = true
        });

        // === CRIMSON ===
        Register(new BiomeData
        {
            Type = BiomeType.Crimson,
            Name = "The Crimson",
            AssociatedTiles = [TileType.CrimsonGrass, TileType.Crimstone, TileType.CrimsonSand, TileType.CrimsonIce, TileType.Flesh],
            SurfaceTile = TileType.CrimsonGrass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Crimstone,
            MinTileCount = 200,
            Priority = 50,
            CanSpread = true,
            SpreadChance = 0.02f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Grass] = TileType.CrimsonGrass,
                [TileType.Stone] = TileType.Crimstone,
                [TileType.Sand] = TileType.CrimsonSand,
                [TileType.Sandstone] = TileType.CrimsonSandstone,
                [TileType.Ice] = TileType.CrimsonIce
            },
            Color = new Color(180, 40, 40),
            SkyColor = new Color(140, 40, 40),
            WaterColor = new Color(150, 50, 50),
            DangerLevel = 5,
            SpawnRateMultiplier = 1.8f,
            GoldMultiplier = 1.5f,
            XPMultiplier = 1.6f,
            RareDropMultiplier = 1.3f,
            HasUniqueMusic = true
        });

        // === HALLOW ===
        Register(new BiomeData
        {
            Type = BiomeType.Hallow,
            Name = "The Hallow",
            AssociatedTiles = [TileType.HallowedGrass, TileType.Pearlstone, TileType.HallowedSand, TileType.HallowedIce],
            SurfaceTile = TileType.HallowedGrass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Pearlstone,
            MinTileCount = 125,
            Priority = 55,
            CanSpread = true,
            SpreadChance = 0.015f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Grass] = TileType.HallowedGrass,
                [TileType.Stone] = TileType.Pearlstone,
                [TileType.Sand] = TileType.HallowedSand,
                [TileType.Ice] = TileType.HallowedIce,
                // Hallow can convert evil biomes back
                [TileType.CorruptGrass] = TileType.HallowedGrass,
                [TileType.CrimsonGrass] = TileType.HallowedGrass,
                [TileType.Ebonstone] = TileType.Pearlstone,
                [TileType.Crimstone] = TileType.Pearlstone
            },
            Color = new Color(255, 150, 255),
            SkyColor = new Color(255, 200, 255),
            WaterColor = new Color(200, 150, 255),
            DangerLevel = 6,
            SpawnRateMultiplier = 1.6f,
            GoldMultiplier = 1.6f,
            XPMultiplier = 1.8f,
            RareDropMultiplier = 1.5f,
            RequiresHardmode = true,
            HasUniqueMusic = true
        });

        // === UNDERGROUND ===
        Register(new BiomeData
        {
            Type = BiomeType.Underground,
            Name = "Underground",
            AssociatedTiles = [TileType.Stone, TileType.Dirt],
            MinTileCount = 0,
            MinDepth = 50,
            MaxDepth = 500,
            Priority = -50,
            Color = new Color(100, 80, 60),
            SkyColor = new Color(50, 40, 30),
            DangerLevel = 3,
            SpawnRateMultiplier = 1.3f,
            GoldMultiplier = 1.2f,
            XPMultiplier = 1.3f
        });

        // === UNDERGROUND DESERT ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundDesert,
            Name = "Underground Desert",
            AssociatedTiles = [TileType.Sandstone, TileType.HardenedSand, TileType.DesertFossil],
            SurfaceTile = TileType.HardenedSand,
            StoneTile = TileType.Sandstone,
            MinTileCount = 300,
            MinDepth = 50,
            Priority = 25,
            Color = new Color(200, 160, 100),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.4f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = true
        });

        // === UNDERGROUND SNOW ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundSnow,
            Name = "Ice Caves",
            AssociatedTiles = [TileType.Ice, TileType.ThinIce, TileType.Snow],
            SurfaceTile = TileType.Ice,
            StoneTile = TileType.Ice,
            MinTileCount = 250,
            MinDepth = 50,
            Priority = 25,
            Color = new Color(150, 200, 255),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.3f,
            GoldMultiplier = 1.2f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = true
        });

        // === UNDERGROUND JUNGLE ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundJungle,
            Name = "Underground Jungle",
            AssociatedTiles = [TileType.JungleGrass, TileType.Mud, TileType.Hive, TileType.HoneyBlock],
            SurfaceTile = TileType.JungleGrass,
            SubsurfaceTile = TileType.Mud,
            MinTileCount = 140,
            MinDepth = 50,
            Priority = 30,
            Color = new Color(50, 120, 50),
            DangerLevel = 6,
            SpawnRateMultiplier = 1.8f,
            GoldMultiplier = 1.5f,
            XPMultiplier = 1.6f,
            RareDropMultiplier = 1.4f,
            HasUniqueMusic = true
        });

        // === UNDERWORLD ===
        Register(new BiomeData
        {
            Type = BiomeType.Underworld,
            Name = "The Underworld",
            AssociatedTiles = [TileType.Ash, TileType.Hellstone, TileType.Obsidian],
            SurfaceTile = TileType.Ash,
            StoneTile = TileType.Hellstone,
            MinTileCount = 50,
            MinDepth = 400,
            Priority = 100,
            Color = new Color(200, 50, 0),
            SkyColor = new Color(80, 20, 0),
            WaterColor = new Color(255, 100, 0),  // Lava color
            DangerLevel = 8,
            SpawnRateMultiplier = 2.0f,
            GoldMultiplier = 2.0f,
            XPMultiplier = 2.0f,
            RareDropMultiplier = 1.5f,
            HasUniqueMusic = true
        });

        // === DUNGEON ===
        Register(new BiomeData
        {
            Type = BiomeType.Dungeon,
            Name = "Dungeon",
            AssociatedTiles = [TileType.DungeonBrick, TileType.CrackedDungeonBrick],
            SurfaceTile = TileType.DungeonBrick,
            StoneTile = TileType.DungeonBrick,
            MinTileCount = 250,
            Priority = 80,
            Color = new Color(50, 50, 100),
            SkyColor = new Color(20, 20, 40),
            DangerLevel = 7,
            SpawnRateMultiplier = 2.5f,
            GoldMultiplier = 1.8f,
            XPMultiplier = 1.8f,
            RareDropMultiplier = 1.6f,
            HasUniqueMusic = true
        });

        // === SPACE ===
        Register(new BiomeData
        {
            Type = BiomeType.Space,
            Name = "Space",
            AssociatedTiles = [],  // Detection by height only
            MinTileCount = 0,
            MaxDepth = -100,  // Negative = above world
            Priority = 90,
            Color = new Color(20, 20, 50),
            SkyColor = new Color(10, 10, 30),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.5f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.4f,
            HasUniqueMusic = true
        });
    }

    private static void Register(BiomeData data)
    {
        _biomes[data.Type] = data;
    }

    /// <summary>
    /// Get biome data by type.
    /// </summary>
    public static BiomeData Get(BiomeType type)
    {
        return _biomes.TryGetValue(type, out var data) ? data : _biomes[BiomeType.Forest];
    }

    /// <summary>
    /// Get all registered biomes.
    /// </summary>
    public static IEnumerable<BiomeData> GetAll() => _biomes.Values;

    /// <summary>
    /// Get biomes sorted by detection priority (highest first).
    /// </summary>
    public static IEnumerable<BiomeData> GetByPriority()
    {
        return _biomes.Values.OrderByDescending(b => b.Priority);
    }

    /// <summary>
    /// Get biomes that can spread.
    /// </summary>
    public static IEnumerable<BiomeData> GetSpreadingBiomes()
    {
        return _biomes.Values.Where(b => b.CanSpread);
    }

    /// <summary>
    /// Check if a tile type belongs to any biome.
    /// </summary>
    public static BiomeType? GetBiomeForTile(TileType tile)
    {
        foreach (var biome in _biomes.Values)
        {
            if (biome.AssociatedTiles.Contains(tile))
                return biome.Type;
        }
        return null;
    }
}