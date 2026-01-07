namespace Terrascent.Items;

/// <summary>
/// All item types in the game.
/// Many correspond to tile types for block items.
/// </summary>
public enum ItemType : ushort
{
    None = 0,

    // === Block Items (1-199) - Match TileType IDs where possible ===
    Dirt = 1,
    Stone = 2,
    Grass = 3,
    Sand = 4,
    Clay = 5,
    Mud = 6,
    Snow = 7,
    Ice = 8,

    // Ores
    CopperOre = 50,
    IronOre = 51,
    SilverOre = 52,
    GoldOre = 53,

    // Wood & Plants
    Wood = 100,
    Leaves = 102,

    // Crafted Blocks
    StoneBrick = 150,
    WoodPlatform = 151,
    Torch = 152,

    // === Tools (200-299) ===
    WoodPickaxe = 200,
    StonePickaxe = 201,
    CopperPickaxe = 202,
    IronPickaxe = 203,

    WoodAxe = 210,
    StoneAxe = 211,
    CopperAxe = 212,
    IronAxe = 213,

    WoodHammer = 220,
    StoneHammer = 221,

    // === Materials (300-399) ===
    CopperBar = 300,
    IronBar = 301,
    SilverBar = 302,
    GoldBar = 303,

    Gel = 310,
    Lens = 311,

    // === Consumables (400-499) ===
    LesserHealingPotion = 400,
    HealingPotion = 401,

    // === Weapons (500-599) ===
    WoodSword = 500,
    CopperSword = 501,
    IronSword = 502,
}