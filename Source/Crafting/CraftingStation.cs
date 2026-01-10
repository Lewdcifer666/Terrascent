using Terrascent.World;

namespace Terrascent.Crafting;

/// <summary>
/// All crafting station types in the game.
/// Players must be near a station to craft its recipes.
/// </summary>
public enum CraftingStationType
{
    /// <summary>Hand crafting - no station required.</summary>
    None = 0,

    // === BASIC STATIONS (1-20) ===

    /// <summary>Basic wood/furniture crafting.</summary>
    Workbench = 1,

    /// <summary>Smelting ores into bars.</summary>
    Furnace = 2,

    /// <summary>Metal tools and weapons.</summary>
    Anvil = 3,

    /// <summary>Alchemy and potions.</summary>
    AlchemyTable = 4,

    /// <summary>Cooking food items.</summary>
    CookingPot = 5,

    /// <summary>Sawing wood into planks.</summary>
    Sawmill = 6,

    /// <summary>Weaving cloth and fabric.</summary>
    Loom = 7,

    /// <summary>Tinkering accessories.</summary>
    TinkersWorkshop = 8,

    // === ADVANCED STATIONS (21-40) ===

    /// <summary>Hardmode metal crafting.</summary>
    MythrilAnvil = 21,

    /// <summary>Hardmode smelting.</summary>
    AdamantiteForge = 22,

    /// <summary>Magic imbuing.</summary>
    CrystalBall = 23,

    /// <summary>Dye crafting.</summary>
    DyeVat = 24,

    /// <summary>Heavy smithing.</summary>
    HeavyWorkbench = 25,

    /// <summary>Bone and undead items.</summary>
    BoneWelder = 26,

    /// <summary>Glass crafting.</summary>
    GlassKiln = 27,

    /// <summary>Honey-based items.</summary>
    HoneyDispenser = 28,

    // === SPECIAL STATIONS (41-60) ===

    /// <summary>Ancient crafting recipes.</summary>
    AncientManipulator = 41,

    /// <summary>Lunar crafting.</summary>
    LunarCraftingStation = 42,

    /// <summary>Demonic crafting.</summary>
    DemonAltar = 43,

    /// <summary>Corruption crafting.</summary>
    CorruptionAltar = 44,

    /// <summary>Crimson crafting.</summary>
    CrimsonAltar = 45,

    /// <summary>Sky/cloud items.</summary>
    SkyMill = 46,

    /// <summary>Living wood items.</summary>
    LivingLoom = 47,

    /// <summary>Ice and frost items.</summary>
    IceMachine = 48,

    // === LIQUID STATIONS (61-70) ===

    /// <summary>Near water source.</summary>
    Water = 61,

    /// <summary>Near lava source.</summary>
    Lava = 62,

    /// <summary>Near honey pool.</summary>
    Honey = 63,

    /// <summary>Near shimmer liquid.</summary>
    Shimmer = 64,
}

/// <summary>
/// Static data for crafting stations.
/// </summary>
public class CraftingStationData
{
    public CraftingStationType Type { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";

    /// <summary>Tile type to place this station.</summary>
    public TileType? PlacesTile { get; init; }

    /// <summary>Required station to craft this station.</summary>
    public CraftingStationType RequiredStation { get; init; } = CraftingStationType.None;

    /// <summary>Visual color for UI.</summary>
    public (byte R, byte G, byte B) Color { get; init; } = (150, 150, 150);

    /// <summary>Detection range in pixels.</summary>
    public float Range { get; init; } = 64f;

    /// <summary>Is this a liquid requirement?</summary>
    public bool IsLiquid { get; init; }
}

/// <summary>
/// Registry of all crafting station data.
/// </summary>
public static class CraftingStationRegistry
{
    private static readonly Dictionary<CraftingStationType, CraftingStationData> _stations = new();
    private static bool _initialized = false;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // === BASIC STATIONS ===
        Register(new CraftingStationData
        {
            Type = CraftingStationType.None,
            Name = "By Hand",
            Description = "Craft anywhere without a station",
            Color = (200, 200, 200)
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.Workbench,
            Name = "Workbench",
            Description = "Basic furniture and tool crafting",
            Color = (160, 82, 45),
            RequiredStation = CraftingStationType.None
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.Furnace,
            Name = "Furnace",
            Description = "Smelt ores into bars",
            Color = (200, 100, 50),
            RequiredStation = CraftingStationType.Workbench
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.Anvil,
            Name = "Iron Anvil",
            Description = "Forge metal weapons and armor",
            Color = (120, 120, 130),
            RequiredStation = CraftingStationType.Workbench
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.AlchemyTable,
            Name = "Alchemy Table",
            Description = "Brew potions and elixirs",
            Color = (100, 50, 150),
            RequiredStation = CraftingStationType.Workbench
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.CookingPot,
            Name = "Cooking Pot",
            Description = "Prepare food and buffs",
            Color = (80, 80, 90),
            RequiredStation = CraftingStationType.Workbench
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.Sawmill,
            Name = "Sawmill",
            Description = "Process wood into planks",
            Color = (139, 90, 43),
            RequiredStation = CraftingStationType.Workbench
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.Loom,
            Name = "Loom",
            Description = "Weave fabric and cloth items",
            Color = (180, 150, 100),
            RequiredStation = CraftingStationType.Sawmill
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.TinkersWorkshop,
            Name = "Tinker's Workshop",
            Description = "Combine and modify accessories",
            Color = (200, 180, 100),
            RequiredStation = CraftingStationType.Anvil
        });

        // === ADVANCED STATIONS ===
        Register(new CraftingStationData
        {
            Type = CraftingStationType.MythrilAnvil,
            Name = "Mythril Anvil",
            Description = "Forge hardmode metals",
            Color = (100, 200, 150),
            RequiredStation = CraftingStationType.Anvil
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.AdamantiteForge,
            Name = "Adamantite Forge",
            Description = "Smelt hardmode ores",
            Color = (200, 50, 100),
            RequiredStation = CraftingStationType.Furnace
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.CrystalBall,
            Name = "Crystal Ball",
            Description = "Imbue items with magic",
            Color = (150, 100, 200),
            RequiredStation = CraftingStationType.AlchemyTable
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.DyeVat,
            Name = "Dye Vat",
            Description = "Create dyes and paints",
            Color = (100, 150, 200),
            RequiredStation = CraftingStationType.Workbench
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.HeavyWorkbench,
            Name = "Heavy Workbench",
            Description = "Craft large furniture",
            Color = (100, 70, 40),
            RequiredStation = CraftingStationType.Sawmill
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.BoneWelder,
            Name = "Bone Welder",
            Description = "Craft bone and undead items",
            Color = (230, 220, 200),
            RequiredStation = CraftingStationType.Anvil
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.GlassKiln,
            Name = "Glass Kiln",
            Description = "Create glass items",
            Color = (180, 220, 240),
            RequiredStation = CraftingStationType.Furnace
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.HoneyDispenser,
            Name = "Honey Dispenser",
            Description = "Craft honey items",
            Color = (255, 200, 50),
            RequiredStation = CraftingStationType.Workbench
        });

        // === SPECIAL STATIONS ===
        Register(new CraftingStationData
        {
            Type = CraftingStationType.AncientManipulator,
            Name = "Ancient Manipulator",
            Description = "Craft lunar and endgame items",
            Color = (50, 200, 200),
            RequiredStation = CraftingStationType.MythrilAnvil
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.DemonAltar,
            Name = "Demon Altar",
            Description = "Craft demonic items",
            Color = (150, 50, 100),
            RequiredStation = CraftingStationType.None  // Found, not crafted
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.SkyMill,
            Name = "Sky Mill",
            Description = "Craft cloud and sky items",
            Color = (200, 220, 255),
            RequiredStation = CraftingStationType.Workbench
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.LivingLoom,
            Name = "Living Loom",
            Description = "Craft living wood items",
            Color = (80, 160, 60),
            RequiredStation = CraftingStationType.Loom
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.IceMachine,
            Name = "Ice Machine",
            Description = "Craft ice and frost items",
            Color = (150, 200, 255),
            RequiredStation = CraftingStationType.Workbench
        });

        // === LIQUID STATIONS ===
        Register(new CraftingStationData
        {
            Type = CraftingStationType.Water,
            Name = "Water",
            Description = "Stand near water",
            Color = (64, 164, 223),
            IsLiquid = true,
            Range = 32f
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.Lava,
            Name = "Lava",
            Description = "Stand near lava (carefully!)",
            Color = (253, 62, 3),
            IsLiquid = true,
            Range = 32f
        });

        Register(new CraftingStationData
        {
            Type = CraftingStationType.Honey,
            Name = "Honey",
            Description = "Stand near honey",
            Color = (255, 156, 12),
            IsLiquid = true,
            Range = 32f
        });
    }

    private static void Register(CraftingStationData data)
    {
        _stations[data.Type] = data;
    }

    public static CraftingStationData Get(CraftingStationType type)
    {
        Initialize();
        return _stations.TryGetValue(type, out var data)
            ? data
            : _stations[CraftingStationType.None];
    }

    public static IEnumerable<CraftingStationData> GetAll()
    {
        Initialize();
        return _stations.Values;
    }

    public static IEnumerable<CraftingStationData> GetCraftableStations()
    {
        Initialize();
        return _stations.Values.Where(s =>
            s.Type != CraftingStationType.None &&
            !s.IsLiquid &&
            s.Type != CraftingStationType.DemonAltar);
    }
}
