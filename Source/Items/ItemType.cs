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

    // === Currency (320-329) ===
    GoldCoin = 320,
    SilverCoin = 321,
    CopperCoin = 322,

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

    // === Stackable Effect Items (600-799) ===
    // Common (600-649)
    SoldiersSyringeItem = 600,
    TougherTimesItem = 601,
    BisonSteakItem = 602,
    PaulsGoatHoofItem = 603,
    CritGlassesItem = 604,
    MonsterToothItem = 605,
    CautiousSlugItem = 606,
    ArmorPlateItem = 607,
    TriTipDaggerItem = 608,
    BundleOfFireworksItem = 609,

    // Uncommon (650-699)
    HopooFeatherItem = 650,
    PredatoryInstinctsItem = 651,
    HarvestersScytheItem = 652,
    UkuleleItem = 653,
    AtgMissileItem = 654,
    WillOTheWispItem = 655,
    BandolierItem = 656,
    WarHornItem = 657,
    BerzerkersPauldronsItem = 658,
    InfusionItem = 659,

    // Rare (700-749)
    BrilliantBehemothItem = 700,
    ShapedGlassItem = 701,
    CestiusItem = 702,
    AlienHeadItem = 703,
    HappiestMaskItem = 704,
    FrostRelicItem = 705,
    UnstableTeslaCoilItem = 706,

    // Legendary (750-799)
    SoulboundCatalystItem = 750,
    FiftySevenLeafCloverItem = 751,
    BrainStalksItem = 752,
    HardlightAfterburnerItem = 753,
    SentientMeatHookItem = 754,

    // === Boss Summoning Items (800-849) ===
    /// <summary>Summons Eye of Terror at night.</summary>
    SuspiciousLookingEye = 800,

    /// <summary>Summons King Slime anywhere.</summary>
    SlimeCrown = 801,

    /// <summary>Summons Brain of Depths underground.</summary>
    BloodySpine = 802,

    /// <summary>Summons Skeletal Warlord at dungeon.</summary>
    AncientSkull = 803,

    /// <summary>Summons Queen Bee in jungle.</summary>
    Abeemination = 804,

    /// <summary>Summons Wall of Shadows in underworld (throw in lava).</summary>
    GuideVoodooDoll = 805,

    // === Boss Trophies (850-899) ===
    EyeOfTerrorTrophy = 850,
    KingSlimeTrophy = 851,
    BrainOfDepthsTrophy = 852,
    SkeletalWarlordTrophy = 853,
    QueenBeeTrophy = 854,
    WallOfShadowsTrophy = 855,

    // === Boss Drops (900-999) ===
    /// <summary>Dropped by Eye of Terror.</summary>
    DemoniteOre = 900,
    ShadowScale = 901,

    /// <summary>Dropped by King Slime.</summary>
    RoyalGel = 902,
    SlimySaddle = 903,

    /// <summary>Dropped by Brain of Depths.</summary>
    CrimtaneOre = 904,
    TissueSample = 905,

    /// <summary>Dropped by Skeletal Warlord.</summary>
    BoneKey = 906,
    SkeletronHand = 907,

    /// <summary>Dropped by Queen Bee.</summary>
    BeeWax = 908,
    Honeycomb = 909,
    BeeGun = 910,

    /// <summary>Dropped by Wall of Shadows.</summary>
    Pwnhammer = 911,
    EmblemWarrior = 912,
    EmblemRanger = 913,
    EmblemSorcerer = 914,

    // === Map Items (1000-1099) ===
    /// <summary>Tier 1 Map (White, Monster Level 69).</summary>
    Map_T1 = 1000,
    /// <summary>Tier 2 Map (White, Monster Level 70).</summary>
    Map_T2 = 1001,
    /// <summary>Tier 3 Map (White, Monster Level 71).</summary>
    Map_T3 = 1002,
    /// <summary>Tier 4 Map (White, Monster Level 72).</summary>
    Map_T4 = 1003,
    /// <summary>Tier 5 Map (White, Monster Level 73).</summary>
    Map_T5 = 1004,
    /// <summary>Tier 6 Map (Yellow, Monster Level 74).</summary>
    Map_T6 = 1005,
    /// <summary>Tier 7 Map (Yellow, Monster Level 75).</summary>
    Map_T7 = 1006,
    /// <summary>Tier 8 Map (Yellow, Monster Level 76).</summary>
    Map_T8 = 1007,
    /// <summary>Tier 9 Map (Yellow, Monster Level 77).</summary>
    Map_T9 = 1008,
    /// <summary>Tier 10 Map (Yellow, Monster Level 78).</summary>
    Map_T10 = 1009,
    /// <summary>Tier 11 Map (Red, Monster Level 79).</summary>
    Map_T11 = 1010,
    /// <summary>Tier 12 Map (Red, Monster Level 80).</summary>
    Map_T12 = 1011,
    /// <summary>Tier 13 Map (Red, Monster Level 81).</summary>
    Map_T13 = 1012,
    /// <summary>Tier 14 Map (Red, Monster Level 82).</summary>
    Map_T14 = 1013,
    /// <summary>Tier 15 Map (Red, Monster Level 83).</summary>
    Map_T15 = 1014,
    /// <summary>Tier 16 Map (Red, Monster Level 84).</summary>
    Map_T16 = 1015,

    // === Map Currency (1100-1149) ===
    /// <summary>Upgrades Normal map to Magic (1-2 mods).</summary>
    OrbOfTransmutation = 1100,
    /// <summary>Rerolls a Magic map.</summary>
    OrbOfAlteration = 1101,
    /// <summary>Upgrades Magic map to Rare.</summary>
    RegalOrb = 1102,
    /// <summary>Upgrades Normal map to Rare (4-6 mods).</summary>
    OrbOfAlchemy = 1103,
    /// <summary>Rerolls a Rare map.</summary>
    ChaosOrb = 1104,
    /// <summary>Randomizes mod values on a map.</summary>
    DivineOrb = 1105,
    /// <summary>Removes all mods from a map.</summary>
    OrbOfScouring = 1106,
    /// <summary>Corrupts a map with unpredictable results.</summary>
    VaalOrb = 1107,
    /// <summary>Adds a mod to a Magic map.</summary>
    OrbOfAugmentation = 1108,
    /// <summary>Adds a mod to a Rare map.</summary>
    ExaltedOrb = 1109,
}