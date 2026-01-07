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
    // Swords (500-509)
    WoodSword = 500,
    CopperSword = 501,
    IronSword = 502,
    SilverSword = 503,
    GoldSword = 504,

    // Spears (510-519)
    WoodSpear = 510,
    CopperSpear = 511,
    IronSpear = 512,

    // Axes (520-529)
    BattleAxe = 520,
    CopperBattleAxe = 521,
    IronBattleAxe = 522,

    // Bows (530-539)
    WoodBow = 530,
    CopperBow = 531,
    IronBow = 532,

    // Whips (540-549)
    LeatherWhip = 540,
    ChainWhip = 541,

    // Staves (550-559)
    WoodStaff = 550,
    ApprenticeStaff = 551,
    MageStaff = 552,

    // Gloves (560-569)
    LeatherGloves = 560,
    IronKnuckles = 561,

    // Boomerangs (570-579)
    WoodBoomerang = 570,
    IronBoomerang = 571,
}