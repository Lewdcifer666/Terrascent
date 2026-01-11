using Microsoft.Xna.Framework;
using Terrascent.World;

namespace Terrascent.World.Biomes;

/// <summary>
/// Defines all properties for a biome including tiles, colors, spawn rates, and spreading behavior.
/// Values are tuned to match Terraria's biome system.
/// </summary>
public class BiomeData
{
    /// <summary>The biome type this data represents.</summary>
    public BiomeType Type { get; init; }

    /// <summary>Display name for the biome.</summary>
    public string Name { get; init; } = "";

    /// <summary>Tiles that count toward this biome's detection.</summary>
    public TileType[] AssociatedTiles { get; init; } = [];

    /// <summary>Primary surface tile for this biome.</summary>
    public TileType SurfaceTile { get; init; } = TileType.Grass;

    /// <summary>Subsurface tile (just below surface).</summary>
    public TileType SubsurfaceTile { get; init; } = TileType.Dirt;

    /// <summary>Stone variant for this biome.</summary>
    public TileType StoneTile { get; init; } = TileType.Stone;

    /// <summary>Minimum tile count to detect this biome. Based on Terraria's thresholds.</summary>
    public int MinTileCount { get; init; } = 50;

    /// <summary>Priority for biome detection (higher = checked first).
    /// Terraria order: Meteorite > Dungeon > Evil > Hallow > Mushroom > Snow > Jungle > Desert > Ocean > Forest</summary>
    public int Priority { get; init; } = 0;

    // === Layer Requirements ===

    /// <summary>Valid layers for this biome to appear.</summary>
    public WorldLayer[] ValidLayers { get; init; } = [WorldLayer.Surface];

    /// <summary>Whether this biome is position-based (like Ocean/Space).</summary>
    public bool IsPositionBased { get; init; } = false;

    // === Spreading Properties ===

    /// <summary>Whether this biome spreads to adjacent tiles.</summary>
    public bool CanSpread { get; init; } = false;

    /// <summary>Spread rate multiplier. 1.0 = normal, 2.0 = hardmode aggressive.</summary>
    public float SpreadRate { get; init; } = 1.0f;

    /// <summary>Chance per tick that spreading occurs (0-1).</summary>
    public float SpreadChance { get; init; } = 0.01f;

    /// <summary>Maximum radius tiles can spread (Terraria uses 3 tiles).</summary>
    public int SpreadRadius { get; init; } = 3;

    /// <summary>Tile conversions when spreading (original -> converted).</summary>
    public Dictionary<TileType, TileType> SpreadConversions { get; init; } = new();

    // === Visual Properties ===

    /// <summary>Primary color for this biome (for UI/minimap).</summary>
    public Color Color { get; init; } = Color.Green;

    /// <summary>Sky color tint for this biome.</summary>
    public Color SkyColor { get; init; } = Color.CornflowerBlue;

    /// <summary>Water color for this biome.</summary>
    public Color WaterColor { get; init; } = Color.Blue;

    // === Spawn Properties ===

    /// <summary>Enemy spawn rate multiplier.</summary>
    public float SpawnRateMultiplier { get; init; } = 1.0f;

    /// <summary>Whether this biome has unique music.</summary>
    public bool HasUniqueMusic { get; init; } = false;

    /// <summary>Whether hardmode is required for this biome.</summary>
    public bool RequiresHardmode { get; init; } = false;

    // === Loot/Economy Properties ===

    /// <summary>Danger level (1-10) affecting difficulty.</summary>
    public int DangerLevel { get; init; } = 1;

    /// <summary>Gold drop multiplier in this biome.</summary>
    public float GoldMultiplier { get; init; } = 1.0f;

    /// <summary>XP multiplier in this biome.</summary>
    public float XPMultiplier { get; init; } = 1.0f;

    /// <summary>Rare item drop chance multiplier.</summary>
    public float RareDropMultiplier { get; init; } = 1.0f;
}

/// <summary>
/// Registry containing all biome definitions with Terraria-accurate values.
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
        // ============================================================
        // SURFACE BIOMES
        // ============================================================

        // === FOREST (Default) ===
        Register(new BiomeData
        {
            Type = BiomeType.Forest,
            Name = "Forest",
            AssociatedTiles = [TileType.Grass, TileType.Dirt],
            SurfaceTile = TileType.Grass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Stone,
            MinTileCount = BiomeTileThresholds.Forest,
            Priority = -100,   // Lowest priority (fallback)
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
            Color = new Color(34, 139, 34),
            SkyColor = new Color(100, 149, 237),
            DangerLevel = 1,
            SpawnRateMultiplier = 1.0f,
            GoldMultiplier = 1.0f,
            XPMultiplier = 1.0f,
            HasUniqueMusic = true
        });

        // === OCEAN ===
        Register(new BiomeData
        {
            Type = BiomeType.Ocean,
            Name = "Ocean",
            AssociatedTiles = [TileType.Sand],
            SurfaceTile = TileType.Sand,
            SubsurfaceTile = TileType.Sand,
            StoneTile = TileType.Stone,
            MinTileCount = BiomeTileThresholds.Ocean,
            Priority = 5,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
            IsPositionBased = true,  // Detection based on X position
            Color = new Color(0, 105, 148),
            SkyColor = new Color(135, 206, 235),
            WaterColor = new Color(0, 100, 200),
            DangerLevel = 2,
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
            MinTileCount = BiomeTileThresholds.Desert,  // 1500
            Priority = 10,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
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
            MinTileCount = BiomeTileThresholds.Snow,  // 1500
            Priority = 15,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
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
            AssociatedTiles = [TileType.JungleGrass, TileType.Mud, TileType.LivingMahogany, TileType.RichMahogany, TileType.Hive],
            SurfaceTile = TileType.JungleGrass,
            SubsurfaceTile = TileType.Mud,
            StoneTile = TileType.Stone,
            MinTileCount = BiomeTileThresholds.Jungle,  // 80
            Priority = 20,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
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

        // === MUSHROOM (Surface) ===
        Register(new BiomeData
        {
            Type = BiomeType.Mushroom,
            Name = "Glowing Mushroom",
            AssociatedTiles = [TileType.MushroomGrass, TileType.GlowingMushroom],
            SurfaceTile = TileType.MushroomGrass,
            SubsurfaceTile = TileType.Mud,
            StoneTile = TileType.Stone,
            MinTileCount = BiomeTileThresholds.Mushroom,  // 100
            Priority = 25,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground, WorldLayer.Cavern],
            CanSpread = true,
            SpreadChance = 0.005f,
            SpreadRadius = 2,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.JungleGrass] = TileType.MushroomGrass
            },
            Color = new Color(50, 50, 200),
            SkyColor = new Color(30, 30, 100),
            WaterColor = new Color(100, 100, 255),
            DangerLevel = 3,
            SpawnRateMultiplier = 1.3f,
            GoldMultiplier = 1.2f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = true
        });

        // ============================================================
        // EVIL BIOMES (Higher priority than normal biomes)
        // ============================================================

        // === CORRUPTION ===
        Register(new BiomeData
        {
            Type = BiomeType.Corruption,
            Name = "The Corruption",
            AssociatedTiles = [TileType.CorruptGrass, TileType.Ebonstone, TileType.CorruptSand, TileType.CorruptIce, TileType.CorruptVines],
            SurfaceTile = TileType.CorruptGrass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Ebonstone,
            MinTileCount = BiomeTileThresholds.Corruption,  // 300
            Priority = 50,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
            CanSpread = true,
            SpreadRate = 1.0f,
            SpreadChance = 0.02f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Grass] = TileType.CorruptGrass,
                [TileType.Stone] = TileType.Ebonstone,
                [TileType.Sand] = TileType.CorruptSand,
                [TileType.Sandstone] = TileType.CorruptSandstone,
                [TileType.Ice] = TileType.CorruptIce
            },
            Color = new Color(100, 50, 150),
            SkyColor = new Color(60, 30, 80),
            WaterColor = new Color(100, 50, 150),
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
            AssociatedTiles = [TileType.CrimsonGrass, TileType.Crimstone, TileType.CrimsonSand, TileType.CrimsonIce, TileType.Flesh, TileType.CrimsonVines],
            SurfaceTile = TileType.CrimsonGrass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Crimstone,
            MinTileCount = BiomeTileThresholds.Crimson,  // 300
            Priority = 50,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
            CanSpread = true,
            SpreadRate = 1.0f,
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
            AssociatedTiles = [TileType.HallowedGrass, TileType.Pearlstone, TileType.HallowedSand, TileType.HallowedIce, TileType.CrystalBlock, TileType.HallowedVines],
            SurfaceTile = TileType.HallowedGrass,
            SubsurfaceTile = TileType.Dirt,
            StoneTile = TileType.Pearlstone,
            MinTileCount = BiomeTileThresholds.Hallow,  // 125
            Priority = 55,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground],
            CanSpread = true,
            SpreadRate = 1.0f,
            SpreadChance = 0.015f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Grass] = TileType.HallowedGrass,
                [TileType.Stone] = TileType.Pearlstone,
                [TileType.Sand] = TileType.HallowedSand,
                [TileType.Ice] = TileType.HallowedIce,
                [TileType.CorruptGrass] = TileType.HallowedGrass,
                [TileType.CrimsonGrass] = TileType.HallowedGrass,
                [TileType.Ebonstone] = TileType.Pearlstone,
                [TileType.Crimstone] = TileType.Pearlstone,
                [TileType.CorruptSand] = TileType.HallowedSand,
                [TileType.CrimsonSand] = TileType.HallowedSand,
                [TileType.CorruptIce] = TileType.HallowedIce,
                [TileType.CrimsonIce] = TileType.HallowedIce
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

        // ============================================================
        // UNDERGROUND BIOMES
        // ============================================================

        // === UNDERGROUND (Generic) ===
        Register(new BiomeData
        {
            Type = BiomeType.Underground,
            Name = "Underground",
            AssociatedTiles = [TileType.Stone, TileType.Dirt],
            MinTileCount = BiomeTileThresholds.Underground,
            Priority = -50,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
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
            MinTileCount = BiomeTileThresholds.Desert,
            Priority = 25,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            Color = new Color(200, 160, 100),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.4f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = true
        });

        // === UNDERGROUND SNOW / ICE BIOME ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundSnow,
            Name = "Ice Caves",
            AssociatedTiles = [TileType.Ice, TileType.ThinIce, TileType.Snow],
            SurfaceTile = TileType.Ice,
            StoneTile = TileType.Ice,
            MinTileCount = BiomeTileThresholds.Snow,  // 1500
            Priority = 25,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
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
            AssociatedTiles = [TileType.JungleGrass, TileType.Mud, TileType.Hive, TileType.HoneyBlock, TileType.LihzahrdBrick],
            SurfaceTile = TileType.JungleGrass,
            SubsurfaceTile = TileType.Mud,
            MinTileCount = BiomeTileThresholds.Jungle,  // 80
            Priority = 30,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            Color = new Color(50, 120, 50),
            DangerLevel = 6,
            SpawnRateMultiplier = 1.8f,
            GoldMultiplier = 1.5f,
            XPMultiplier = 1.6f,
            RareDropMultiplier = 1.4f,
            HasUniqueMusic = true
        });

        // === UNDERGROUND MUSHROOM ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundMushroom,
            Name = "Glowing Mushroom Cave",
            AssociatedTiles = [TileType.MushroomGrass, TileType.GlowingMushroom],
            SurfaceTile = TileType.MushroomGrass,
            SubsurfaceTile = TileType.Mud,
            MinTileCount = BiomeTileThresholds.Mushroom,  // 100
            Priority = 30,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            Color = new Color(50, 50, 200),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.4f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.4f,
            HasUniqueMusic = true
        });

        // === UNDERGROUND CORRUPTION ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundCorruption,
            Name = "Underground Corruption",
            AssociatedTiles = [TileType.Ebonstone, TileType.CorruptSand, TileType.CorruptIce],
            StoneTile = TileType.Ebonstone,
            MinTileCount = BiomeTileThresholds.Corruption,  // 300
            Priority = 50,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            CanSpread = true,
            SpreadChance = 0.02f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Stone] = TileType.Ebonstone,
                [TileType.Sand] = TileType.CorruptSand,
                [TileType.Ice] = TileType.CorruptIce
            },
            Color = new Color(80, 40, 120),
            DangerLevel = 6,
            SpawnRateMultiplier = 2.0f,
            GoldMultiplier = 1.6f,
            XPMultiplier = 1.7f,
            RareDropMultiplier = 1.4f,
            HasUniqueMusic = true
        });

        // === UNDERGROUND CRIMSON ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundCrimson,
            Name = "Underground Crimson",
            AssociatedTiles = [TileType.Crimstone, TileType.CrimsonSand, TileType.CrimsonIce, TileType.Flesh],
            StoneTile = TileType.Crimstone,
            MinTileCount = BiomeTileThresholds.Crimson,  // 300
            Priority = 50,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            CanSpread = true,
            SpreadChance = 0.02f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Stone] = TileType.Crimstone,
                [TileType.Sand] = TileType.CrimsonSand,
                [TileType.Ice] = TileType.CrimsonIce
            },
            Color = new Color(150, 30, 30),
            DangerLevel = 6,
            SpawnRateMultiplier = 2.0f,
            GoldMultiplier = 1.6f,
            XPMultiplier = 1.7f,
            RareDropMultiplier = 1.4f,
            HasUniqueMusic = true
        });

        // === UNDERGROUND HALLOW ===
        Register(new BiomeData
        {
            Type = BiomeType.UndergroundHallow,
            Name = "Underground Hallow",
            AssociatedTiles = [TileType.Pearlstone, TileType.HallowedSand, TileType.HallowedIce, TileType.CrystalBlock],
            StoneTile = TileType.Pearlstone,
            MinTileCount = BiomeTileThresholds.Hallow,  // 125
            Priority = 55,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            CanSpread = true,
            SpreadChance = 0.015f,
            SpreadRadius = 3,
            SpreadConversions = new Dictionary<TileType, TileType>
            {
                [TileType.Stone] = TileType.Pearlstone,
                [TileType.Sand] = TileType.HallowedSand,
                [TileType.Ice] = TileType.HallowedIce,
                [TileType.Ebonstone] = TileType.Pearlstone,
                [TileType.Crimstone] = TileType.Pearlstone
            },
            Color = new Color(200, 120, 200),
            DangerLevel = 7,
            SpawnRateMultiplier = 1.8f,
            GoldMultiplier = 1.7f,
            XPMultiplier = 1.9f,
            RareDropMultiplier = 1.6f,
            RequiresHardmode = true,
            HasUniqueMusic = true
        });

        // ============================================================
        // SPECIAL BIOMES
        // ============================================================

        // === SPACE ===
        Register(new BiomeData
        {
            Type = BiomeType.Space,
            Name = "Space",
            AssociatedTiles = [],
            MinTileCount = BiomeTileThresholds.Space,
            Priority = 90,
            ValidLayers = [WorldLayer.Space],
            IsPositionBased = true,
            Color = new Color(20, 20, 50),
            SkyColor = new Color(10, 10, 30),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.5f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.4f,
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
            MinTileCount = BiomeTileThresholds.Underworld,  // 50
            Priority = 100,
            ValidLayers = [WorldLayer.Underworld],
            Color = new Color(200, 50, 0),
            SkyColor = new Color(80, 20, 0),
            WaterColor = new Color(255, 100, 0),
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
            MinTileCount = BiomeTileThresholds.Dungeon,  // 250
            Priority = 80,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground, WorldLayer.Cavern],
            Color = new Color(50, 50, 100),
            SkyColor = new Color(20, 20, 40),
            DangerLevel = 7,
            SpawnRateMultiplier = 2.5f,
            GoldMultiplier = 1.8f,
            XPMultiplier = 1.8f,
            RareDropMultiplier = 1.6f,
            HasUniqueMusic = true
        });

        // === METEOR ===
        Register(new BiomeData
        {
            Type = BiomeType.Meteor,
            Name = "Meteor",
            AssociatedTiles = [],
            MinTileCount = BiomeTileThresholds.Meteorite,  // 75
            Priority = 95,
            ValidLayers = [WorldLayer.Surface, WorldLayer.Underground, WorldLayer.Cavern],
            Color = new Color(150, 50, 50),
            DangerLevel = 5,
            SpawnRateMultiplier = 3.0f,
            GoldMultiplier = 1.5f,
            XPMultiplier = 1.5f,
            HasUniqueMusic = true
        });

        // === GRANITE CAVE ===
        Register(new BiomeData
        {
            Type = BiomeType.GraniteCave,
            Name = "Granite Cave",
            AssociatedTiles = [TileType.GraniteBlock],
            StoneTile = TileType.GraniteBlock,
            MinTileCount = BiomeTileThresholds.GraniteCave,  // 50
            Priority = 35,
            ValidLayers = [WorldLayer.Cavern],
            Color = new Color(50, 50, 80),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.5f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = false
        });

        // === MARBLE CAVE ===
        Register(new BiomeData
        {
            Type = BiomeType.MarbleCave,
            Name = "Marble Cave",
            AssociatedTiles = [TileType.MarbleBlock],
            StoneTile = TileType.MarbleBlock,
            MinTileCount = BiomeTileThresholds.MarbleCave,  // 50
            Priority = 35,
            ValidLayers = [WorldLayer.Cavern],
            Color = new Color(220, 220, 220),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.5f,
            GoldMultiplier = 1.3f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = false
        });

        // === SPIDER NEST ===
        Register(new BiomeData
        {
            Type = BiomeType.SpiderNest,
            Name = "Spider Nest",
            AssociatedTiles = [],
            MinTileCount = BiomeTileThresholds.SpiderNest,  // 30
            Priority = 40,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            Color = new Color(80, 80, 80),
            DangerLevel = 5,
            SpawnRateMultiplier = 2.0f,
            GoldMultiplier = 1.2f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = false
        });

        // === BEE HIVE ===
        Register(new BiomeData
        {
            Type = BiomeType.BeeHive,
            Name = "Bee Hive",
            AssociatedTiles = [TileType.Hive, TileType.HoneyBlock],
            MinTileCount = BiomeTileThresholds.BeeHive,  // 20
            Priority = 45,
            ValidLayers = [WorldLayer.Underground, WorldLayer.Cavern],
            Color = new Color(255, 200, 50),
            DangerLevel = 4,
            SpawnRateMultiplier = 1.8f,
            GoldMultiplier = 1.2f,
            XPMultiplier = 1.3f,
            HasUniqueMusic = false
        });

        // === TEMPLE ===
        Register(new BiomeData
        {
            Type = BiomeType.Temple,
            Name = "Lihzahrd Temple",
            AssociatedTiles = [TileType.LihzahrdBrick],
            StoneTile = TileType.LihzahrdBrick,
            MinTileCount = 50,
            Priority = 85,
            ValidLayers = [WorldLayer.Cavern],
            Color = new Color(150, 100, 50),
            DangerLevel = 9,
            SpawnRateMultiplier = 2.5f,
            GoldMultiplier = 2.0f,
            XPMultiplier = 2.0f,
            RareDropMultiplier = 1.8f,
            HasUniqueMusic = true
        });
    }

    private static void Register(BiomeData data)
    {
        _biomes[data.Type] = data;
    }

    /// <summary>Get biome data by type.</summary>
    public static BiomeData Get(BiomeType type)
    {
        return _biomes.TryGetValue(type, out var data) ? data : _biomes[BiomeType.Forest];
    }

    /// <summary>Get all registered biomes.</summary>
    public static IEnumerable<BiomeData> GetAll() => _biomes.Values;

    /// <summary>Get biomes sorted by detection priority (highest first).</summary>
    public static IEnumerable<BiomeData> GetByPriority()
    {
        return _biomes.Values.OrderByDescending(b => b.Priority);
    }

    /// <summary>Get biomes that can spread.</summary>
    public static IEnumerable<BiomeData> GetSpreadingBiomes()
    {
        return _biomes.Values.Where(b => b.CanSpread);
    }

    /// <summary>Check if a tile type belongs to any biome.</summary>
    public static BiomeType? GetBiomeForTile(TileType tile)
    {
        foreach (var biome in GetByPriority())
        {
            if (biome.AssociatedTiles.Contains(tile))
                return biome.Type;
        }
        return null;
    }

    /// <summary>Get the underground variant for a surface biome.</summary>
    public static BiomeType GetUndergroundVariant(BiomeType surface)
    {
        return surface switch
        {
            BiomeType.Forest => BiomeType.Underground,
            BiomeType.Desert => BiomeType.UndergroundDesert,
            BiomeType.Snow => BiomeType.UndergroundSnow,
            BiomeType.Jungle => BiomeType.UndergroundJungle,
            BiomeType.Mushroom => BiomeType.UndergroundMushroom,
            BiomeType.Corruption => BiomeType.UndergroundCorruption,
            BiomeType.Crimson => BiomeType.UndergroundCrimson,
            BiomeType.Hallow => BiomeType.UndergroundHallow,
            _ => BiomeType.Underground
        };
    }
}