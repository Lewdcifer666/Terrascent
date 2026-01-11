namespace Terrascent.World.Biomes;

/// <summary>
/// All biome types in the game.
/// Biomes determine enemy spawns, loot tables, music, and visual atmosphere.
/// </summary>
public enum BiomeType
{
    // === Surface Biomes (0-19) ===
    Forest = 0,         // Default surface biome
    Desert = 1,         // Sandy terrain, cacti, antlions
    Snow = 2,           // Frozen terrain, ice caves
    Jungle = 3,         // Dense vegetation, dangerous enemies
    Ocean = 4,          // Beach/underwater areas
    Mushroom = 5,       // Glowing mushroom fields (surface version)

    // === Evil Biomes (20-29) ===
    Corruption = 20,    // Purple evil, chasms, Eaters
    Crimson = 21,       // Red evil, caves, Face Monsters

    // === Good Biomes (30-39) ===
    Hallow = 30,        // Post-hardmode blessing, unicorns

    // === Underground Biomes (40-59) ===
    Underground = 40,           // Generic underground
    UndergroundDesert = 41,     // Sandstone caverns
    UndergroundSnow = 42,       // Ice caverns
    UndergroundJungle = 43,     // Mud and beehives
    UndergroundMushroom = 44,   // Glowing mushroom caves
    UndergroundCorruption = 45, // Deep corruption
    UndergroundCrimson = 46,    // Deep crimson
    UndergroundHallow = 47,     // Deep hallow

    // === Special Biomes (60+) ===
    Underworld = 60,    // Hell layer, hellstone, demons
    Dungeon = 61,       // Brick structure, skeletons
    Temple = 62,        // Lihzahrd temple (post-Plantera)
    Meteor = 63,        // Meteorite crash site
    SpiderNest = 64,    // Spider caves
    BeeHive = 65,       // Honey and bees
    GraniteCave = 66,   // Granite biome
    MarbleCave = 67,    // Marble biome

    // === Space ===
    Space = 80,         // Above world, harpies
}

/// <summary>
/// Biome category for grouping similar biomes.
/// </summary>
public enum BiomeCategory
{
    Surface,        // Above ground
    Underground,    // Below surface
    Evil,           // Corruption/Crimson
    Good,           // Hallow
    Special,        // Unique structures/areas
    Cavern,         // Deep underground
    Hell            // Bottom of world
}

/// <summary>
/// Extension methods for BiomeType.
/// </summary>
public static class BiomeTypeExtensions
{
    /// <summary>
    /// Get the category of a biome.
    /// </summary>
    public static BiomeCategory GetCategory(this BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => BiomeCategory.Surface,
            BiomeType.Desert => BiomeCategory.Surface,
            BiomeType.Snow => BiomeCategory.Surface,
            BiomeType.Jungle => BiomeCategory.Surface,
            BiomeType.Ocean => BiomeCategory.Surface,
            BiomeType.Mushroom => BiomeCategory.Surface,
            BiomeType.Space => BiomeCategory.Surface,

            BiomeType.Corruption => BiomeCategory.Evil,
            BiomeType.Crimson => BiomeCategory.Evil,
            BiomeType.UndergroundCorruption => BiomeCategory.Evil,
            BiomeType.UndergroundCrimson => BiomeCategory.Evil,

            BiomeType.Hallow => BiomeCategory.Good,
            BiomeType.UndergroundHallow => BiomeCategory.Good,

            BiomeType.Underground => BiomeCategory.Underground,
            BiomeType.UndergroundDesert => BiomeCategory.Underground,
            BiomeType.UndergroundSnow => BiomeCategory.Underground,
            BiomeType.UndergroundJungle => BiomeCategory.Underground,
            BiomeType.UndergroundMushroom => BiomeCategory.Underground,
            BiomeType.GraniteCave => BiomeCategory.Underground,
            BiomeType.MarbleCave => BiomeCategory.Underground,

            BiomeType.Underworld => BiomeCategory.Hell,

            _ => BiomeCategory.Special
        };
    }

    /// <summary>
    /// Check if this is an evil biome.
    /// </summary>
    public static bool IsEvil(this BiomeType biome)
    {
        return biome == BiomeType.Corruption ||
               biome == BiomeType.Crimson ||
               biome == BiomeType.UndergroundCorruption ||
               biome == BiomeType.UndergroundCrimson;
    }

    /// <summary>
    /// Check if this is a good/hallowed biome.
    /// </summary>
    public static bool IsGood(this BiomeType biome)
    {
        return biome == BiomeType.Hallow ||
               biome == BiomeType.UndergroundHallow;
    }

    /// <summary>
    /// Check if this biome can spread to other tiles.
    /// </summary>
    public static bool CanSpread(this BiomeType biome)
    {
        return biome.IsEvil() || biome.IsGood() || biome == BiomeType.Mushroom;
    }

    /// <summary>
    /// Get the underground variant of a surface biome.
    /// </summary>
    public static BiomeType GetUndergroundVariant(this BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => BiomeType.Underground,
            BiomeType.Desert => BiomeType.UndergroundDesert,
            BiomeType.Snow => BiomeType.UndergroundSnow,
            BiomeType.Jungle => BiomeType.UndergroundJungle,
            BiomeType.Mushroom => BiomeType.UndergroundMushroom,
            BiomeType.Corruption => BiomeType.UndergroundCorruption,
            BiomeType.Crimson => BiomeType.UndergroundCrimson,
            BiomeType.Hallow => BiomeType.UndergroundHallow,
            _ => biome
        };
    }

    /// <summary>
    /// Get display name for a biome.
    /// </summary>
    public static string GetDisplayName(this BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => "Forest",
            BiomeType.Desert => "Desert",
            BiomeType.Snow => "Snow",
            BiomeType.Jungle => "Jungle",
            BiomeType.Ocean => "Ocean",
            BiomeType.Mushroom => "Glowing Mushroom",
            BiomeType.Corruption => "The Corruption",
            BiomeType.Crimson => "The Crimson",
            BiomeType.Hallow => "The Hallow",
            BiomeType.Underground => "Underground",
            BiomeType.UndergroundDesert => "Underground Desert",
            BiomeType.UndergroundSnow => "Ice Caves",
            BiomeType.UndergroundJungle => "Underground Jungle",
            BiomeType.UndergroundMushroom => "Glowing Mushroom Cave",
            BiomeType.UndergroundCorruption => "Underground Corruption",
            BiomeType.UndergroundCrimson => "Underground Crimson",
            BiomeType.UndergroundHallow => "Underground Hallow",
            BiomeType.Underworld => "The Underworld",
            BiomeType.Dungeon => "Dungeon",
            BiomeType.Temple => "Lihzahrd Temple",
            BiomeType.Meteor => "Meteor",
            BiomeType.SpiderNest => "Spider Nest",
            BiomeType.BeeHive => "Bee Hive",
            BiomeType.GraniteCave => "Granite Cave",
            BiomeType.MarbleCave => "Marble Cave",
            BiomeType.Space => "Space",
            _ => biome.ToString()
        };
    }

    /// <summary>
    /// Check if this biome requires hardmode to appear.
    /// </summary>
    public static bool RequiresHardmode(this BiomeType biome)
    {
        return biome == BiomeType.Hallow ||
               biome == BiomeType.UndergroundHallow;
    }

    /// <summary>
    /// Get the danger level of a biome (1-10).
    /// </summary>
    public static int GetBaseDangerLevel(this BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => 1,
            BiomeType.Snow => 2,
            BiomeType.Desert => 2,
            BiomeType.Ocean => 2,
            BiomeType.Jungle => 4,
            BiomeType.Mushroom => 3,
            BiomeType.Corruption => 5,
            BiomeType.Crimson => 5,
            BiomeType.Hallow => 6,
            BiomeType.Underground => 3,
            BiomeType.UndergroundDesert => 4,
            BiomeType.UndergroundSnow => 4,
            BiomeType.UndergroundJungle => 6,
            BiomeType.UndergroundMushroom => 4,
            BiomeType.UndergroundCorruption => 6,
            BiomeType.UndergroundCrimson => 6,
            BiomeType.UndergroundHallow => 7,
            BiomeType.Dungeon => 7,
            BiomeType.Temple => 9,
            BiomeType.Underworld => 8,
            BiomeType.Space => 4,
            _ => 3
        };
    }
}