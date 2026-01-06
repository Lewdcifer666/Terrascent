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

    // === Ores (50-99) ===
    CopperOre = 50,
    IronOre = 51,
    SilverOre = 52,
    GoldOre = 53,
    CobaltOre = 54,      // Hardmode
    MythrilOre = 55,     // Hardmode
    AdamantiteOre = 56,  // Hardmode

    // === Wood & Plants (100-149) ===
    Wood = 100,
    LivingWood = 101,
    Leaves = 102,
    Cactus = 103,
    Mushroom = 104,
    GlowingMushroom = 105,

    // === Bricks & Crafted (150-199) ===
    StoneBrick = 150,
    WoodPlatform = 151,
    Torch = 152,

    // === Liquids (handled separately, but reserve IDs) ===
    // Water, Lava, Honey use a different system

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

    // Player-placed walls
    WoodWall = 10,
    StoneBrickWall = 11,
}