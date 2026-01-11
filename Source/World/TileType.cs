namespace Terrascent.World;

/// <summary>
/// All tile types in the game. ID 0 is always Air (empty).
/// Organized by category for easy reference.
/// </summary>
public enum TileType : ushort
{
    // === Empty ===
    Air = 0,

    // === Natural Terrain (1-49) ===
    Dirt = 1,
    Stone = 2,
    Grass = 3,
    Sand = 4,
    Clay = 5,
    Mud = 6,
    Snow = 7,
    Ice = 8,
    Ash = 9,

    // === Desert Tiles (10-19) ===
    Sandstone = 10,
    HardenedSand = 11,
    DesertFossil = 12,

    // === Snow/Ice Tiles (20-29) ===
    SnowBrick = 20,
    ThinIce = 21,

    // === Jungle Tiles (30-39) ===
    JungleGrass = 30,
    LivingMahogany = 31,
    Hive = 32,
    HoneyBlock = 33,

    // === Mushroom Tiles (40-44) ===
    MushroomGrass = 40,

    // === Corruption Tiles (45-54) ===
    CorruptGrass = 45,
    Ebonstone = 46,
    CorruptSand = 47,
    CorruptSandstone = 48,
    CorruptIce = 49,

    // === Crimson Tiles (55-64) ===
    CrimsonGrass = 55,
    Crimstone = 56,
    CrimsonSand = 57,
    CrimsonSandstone = 58,
    CrimsonIce = 59,
    Flesh = 60,

    // === Hallow Tiles (65-74) ===
    HallowedGrass = 65,
    Pearlstone = 66,
    HallowedSand = 67,
    HallowedSandstone = 68,
    HallowedIce = 69,

    // === Ores (75-99) ===
    CopperOre = 75,
    IronOre = 76,
    SilverOre = 77,
    GoldOre = 78,
    CobaltOre = 79,      // Hardmode
    MythrilOre = 80,     // Hardmode
    AdamantiteOre = 81,  // Hardmode
    Hellstone = 82,
    DemoniteOre = 83,
    CrimtaneOre = 84,

    // === Wood & Plants (100-129) ===
    Wood = 100,
    LivingWood = 101,
    Leaves = 102,
    Cactus = 103,
    Mushroom = 104,
    GlowingMushroom = 105,
    BorealWood = 106,       // Snow biome wood
    PalmWood = 107,         // Desert/Ocean wood
    RichMahogany = 108,     // Jungle wood
    Ebonwood = 109,         // Corruption wood
    Shadewood = 110,        // Crimson wood
    Pearlwood = 111,        // Hallow wood

    // === Vines (130-139) ===
    Vines = 130,
    JungleVines = 131,
    CorruptVines = 132,
    CrimsonVines = 133,
    HallowedVines = 134,

    // === Bricks & Crafted (140-169) ===
    StoneBrick = 140,
    WoodPlatform = 141,
    Torch = 142,
    GrayBrick = 143,
    RedBrick = 144,
    DungeonBrick = 145,
    CrackedDungeonBrick = 146,
    LihzahrdBrick = 147,
    Obsidian = 148,
    CrystalBlock = 149,
    GraniteBlock = 150,
    MarbleBlock = 151,

    // === Special (200+) ===
    Bedrock = 200,  // Unbreakable
}

/// <summary>
/// Wall types for backgrounds behind tiles.
/// </summary>
public enum WallType : ushort
{
    None = 0,

    // Natural walls (generated)
    Dirt = 1,
    Stone = 2,

    // Biome walls
    Sandstone = 3,
    Snow = 4,
    Ice = 5,
    Jungle = 6,
    Mushroom = 7,
    Corruption = 8,
    Crimson = 9,
    Hallow = 10,
    Underworld = 11,
    Granite = 12,
    Marble = 13,

    // Player-placed walls
    WoodWall = 20,
    StoneBrickWall = 21,
    DungeonWall = 22,
}