namespace Terrascent.World;

/// <summary>
/// Static properties for each tile type.
/// </summary>
public readonly struct TileProperties
{
    /// <summary>
    /// Display name of the tile.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Is this tile solid (blocks movement)?
    /// </summary>
    public bool IsSolid { get; init; }

    /// <summary>
    /// Can the player walk through this tile?
    /// </summary>
    public bool IsPassable => !IsSolid;

    /// <summary>
    /// Does this tile block light?
    /// </summary>
    public bool BlocksLight { get; init; }

    /// <summary>
    /// Base mining time in ticks (60 = 1 second).
    /// </summary>
    public int MiningTime { get; init; }

    /// <summary>
    /// Minimum pickaxe power required to mine (0 = any tool).
    /// </summary>
    public int PickaxeRequired { get; init; }

    /// <summary>
    /// Light emitted by this tile (0-255).
    /// </summary>
    public byte LightEmission { get; init; }

    /// <summary>
    /// Does this tile need support from below (like sand)?
    /// </summary>
    public bool AffectedByGravity { get; init; }

    /// <summary>
    /// Can this tile merge with adjacent tiles of the same type for auto-tiling?
    /// </summary>
    public bool CanMerge { get; init; }

    /// <summary>
    /// Tile render color (R, G, B).
    /// </summary>
    public (byte R, byte G, byte B) Color { get; init; }

    /// <summary>
    /// Default properties for unknown tiles.
    /// </summary>
    public static TileProperties Default => new()
    {
        Name = "Unknown",
        IsSolid = true,
        BlocksLight = true,
        MiningTime = 60,
        PickaxeRequired = 0,
        LightEmission = 0,
        AffectedByGravity = false,
        CanMerge = true,
        Color = (128, 128, 128),
    };
}

/// <summary>
/// Registry of all tile properties, indexed by TileType.
/// </summary>
public static class TileRegistry
{
    private static readonly Dictionary<TileType, TileProperties> _properties = new()
    {
        // === AIR ===
        [TileType.Air] = new TileProperties
        {
            Name = "Air",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 0,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (0, 0, 0),
        },

        // === NATURAL TERRAIN ===
        [TileType.Dirt] = new TileProperties
        {
            Name = "Dirt",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (139, 90, 43),
        },

        [TileType.Stone] = new TileProperties
        {
            Name = "Stone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 60,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (128, 128, 128),
        },

        [TileType.Grass] = new TileProperties
        {
            Name = "Grass",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (34, 139, 34),
        },

        [TileType.Sand] = new TileProperties
        {
            Name = "Sand",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 20,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = true,
            CanMerge = true,
            Color = (237, 201, 175),
        },

        [TileType.Clay] = new TileProperties
        {
            Name = "Clay",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 35,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (165, 42, 42),
        },

        [TileType.Mud] = new TileProperties
        {
            Name = "Mud",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 25,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (89, 60, 31),
        },

        [TileType.Snow] = new TileProperties
        {
            Name = "Snow",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 20,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (255, 250, 250),
        },

        [TileType.Ice] = new TileProperties
        {
            Name = "Ice",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 40,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (173, 216, 230),
        },

        [TileType.Ash] = new TileProperties
        {
            Name = "Ash",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 25,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (50, 50, 50),
        },

        // === DESERT TILES ===
        [TileType.Sandstone] = new TileProperties
        {
            Name = "Sandstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 50,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (210, 180, 140),
        },

        [TileType.HardenedSand] = new TileProperties
        {
            Name = "Hardened Sand",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 35,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (200, 170, 120),
        },

        [TileType.DesertFossil] = new TileProperties
        {
            Name = "Desert Fossil",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 80,
            PickaxeRequired = 65,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (180, 160, 100),
        },

        // === SNOW/ICE TILES ===
        [TileType.SnowBrick] = new TileProperties
        {
            Name = "Snow Brick",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (240, 248, 255),
        },

        [TileType.ThinIce] = new TileProperties
        {
            Name = "Thin Ice",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 20,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (200, 230, 255),
        },

        // === JUNGLE TILES ===
        [TileType.JungleGrass] = new TileProperties
        {
            Name = "Jungle Grass",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (0, 100, 0),
        },

        [TileType.LivingMahogany] = new TileProperties
        {
            Name = "Living Mahogany",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 50,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (139, 69, 19),
        },

        [TileType.Hive] = new TileProperties
        {
            Name = "Hive",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 40,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (218, 165, 32),
        },

        [TileType.HoneyBlock] = new TileProperties
        {
            Name = "Honey Block",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 20,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (255, 200, 50),
        },

        // === MUSHROOM TILES ===
        [TileType.MushroomGrass] = new TileProperties
        {
            Name = "Mushroom Grass",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 30,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (0, 100, 200),
        },

        [TileType.Mushroom] = new TileProperties
        {
            Name = "Mushroom",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 10,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (200, 50, 50),
        },

        [TileType.GlowingMushroom] = new TileProperties
        {
            Name = "Glowing Mushroom",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 15,
            PickaxeRequired = 0,
            LightEmission = 100,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (50, 100, 255),
        },

        // === CORRUPTION TILES ===
        [TileType.CorruptGrass] = new TileProperties
        {
            Name = "Corrupt Grass",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (100, 50, 150),
        },

        [TileType.Ebonstone] = new TileProperties
        {
            Name = "Ebonstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 100,
            PickaxeRequired = 65,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (70, 40, 90),
        },

        [TileType.CorruptSand] = new TileProperties
        {
            Name = "Corrupt Sand",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 20,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = true,
            CanMerge = true,
            Color = (120, 80, 150),
        },

        [TileType.CorruptSandstone] = new TileProperties
        {
            Name = "Corrupt Sandstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 50,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (100, 70, 130),
        },

        [TileType.CorruptIce] = new TileProperties
        {
            Name = "Corrupt Ice",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 40,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (130, 100, 180),
        },

        // === CRIMSON TILES ===
        [TileType.CrimsonGrass] = new TileProperties
        {
            Name = "Crimson Grass",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (180, 40, 40),
        },

        [TileType.Crimstone] = new TileProperties
        {
            Name = "Crimstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 100,
            PickaxeRequired = 65,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (150, 30, 30),
        },

        [TileType.CrimsonSand] = new TileProperties
        {
            Name = "Crimson Sand",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 20,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = true,
            CanMerge = true,
            Color = (180, 60, 60),
        },

        [TileType.CrimsonSandstone] = new TileProperties
        {
            Name = "Crimson Sandstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 50,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (160, 50, 50),
        },

        [TileType.CrimsonIce] = new TileProperties
        {
            Name = "Crimson Ice",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 40,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (200, 80, 100),
        },

        [TileType.Flesh] = new TileProperties
        {
            Name = "Flesh",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 35,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (140, 50, 50),
        },

        // === HALLOW TILES ===
        [TileType.HallowedGrass] = new TileProperties
        {
            Name = "Hallowed Grass",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 15,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (100, 200, 255),
        },

        [TileType.Pearlstone] = new TileProperties
        {
            Name = "Pearlstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 60,
            PickaxeRequired = 65,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (255, 200, 255),
        },

        [TileType.HallowedSand] = new TileProperties
        {
            Name = "Hallowed Sand",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 20,
            PickaxeRequired = 0,
            LightEmission = 10,
            AffectedByGravity = true,
            CanMerge = true,
            Color = (255, 220, 255),
        },

        [TileType.HallowedSandstone] = new TileProperties
        {
            Name = "Hallowed Sandstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 50,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (240, 200, 240),
        },

        [TileType.HallowedIce] = new TileProperties
        {
            Name = "Hallowed Ice",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 40,
            PickaxeRequired = 0,
            LightEmission = 20,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (200, 180, 255),
        },

        // === ORES ===
        [TileType.CopperOre] = new TileProperties
        {
            Name = "Copper Ore",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 90,
            PickaxeRequired = 35,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (184, 115, 51),
        },

        [TileType.IronOre] = new TileProperties
        {
            Name = "Iron Ore",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 120,
            PickaxeRequired = 35,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (161, 157, 148),
        },

        [TileType.SilverOre] = new TileProperties
        {
            Name = "Silver Ore",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 135,
            PickaxeRequired = 55,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (192, 192, 192),
        },

        [TileType.GoldOre] = new TileProperties
        {
            Name = "Gold Ore",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 150,
            PickaxeRequired = 55,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (255, 215, 0),
        },

        [TileType.Hellstone] = new TileProperties
        {
            Name = "Hellstone",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 180,
            PickaxeRequired = 65,
            LightEmission = 50,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (200, 50, 0),
        },

        [TileType.DemoniteOre] = new TileProperties
        {
            Name = "Demonite Ore",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 120,
            PickaxeRequired = 55,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (100, 50, 150),
        },

        [TileType.CrimtaneOre] = new TileProperties
        {
            Name = "Crimtane Ore",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 120,
            PickaxeRequired = 55,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (180, 40, 60),
        },

        // === WOOD & PLANTS ===
        [TileType.Wood] = new TileProperties
        {
            Name = "Wood",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (139, 90, 43),
        },

        [TileType.LivingWood] = new TileProperties
        {
            Name = "Living Wood",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 55,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (160, 120, 60),
        },

        [TileType.Leaves] = new TileProperties
        {
            Name = "Leaves",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 1,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (34, 139, 34),
        },

        [TileType.Cactus] = new TileProperties
        {
            Name = "Cactus",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 30,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (34, 139, 34),
        },

        [TileType.BorealWood] = new TileProperties
        {
            Name = "Boreal Wood",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (100, 80, 60),
        },

        [TileType.PalmWood] = new TileProperties
        {
            Name = "Palm Wood",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (180, 140, 80),
        },

        [TileType.RichMahogany] = new TileProperties
        {
            Name = "Rich Mahogany",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (139, 69, 19),
        },

        [TileType.Ebonwood] = new TileProperties
        {
            Name = "Ebonwood",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (60, 40, 80),
        },

        [TileType.Shadewood] = new TileProperties
        {
            Name = "Shadewood",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (100, 40, 40),
        },

        [TileType.Pearlwood] = new TileProperties
        {
            Name = "Pearlwood",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 45,
            PickaxeRequired = 0,
            LightEmission = 10,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (255, 220, 255),
        },

        // === VINES ===
        [TileType.Vines] = new TileProperties
        {
            Name = "Vines",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 5,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (0, 150, 0),
        },

        [TileType.JungleVines] = new TileProperties
        {
            Name = "Jungle Vines",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 5,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (0, 120, 0),
        },

        [TileType.CorruptVines] = new TileProperties
        {
            Name = "Corrupt Vines",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 5,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (80, 50, 120),
        },

        [TileType.CrimsonVines] = new TileProperties
        {
            Name = "Crimson Vines",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 5,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (150, 40, 40),
        },

        [TileType.HallowedVines] = new TileProperties
        {
            Name = "Hallowed Vines",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 5,
            PickaxeRequired = 0,
            LightEmission = 15,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (200, 150, 255),
        },

        // === BRICKS & CRAFTED ===
        [TileType.StoneBrick] = new TileProperties
        {
            Name = "Stone Brick",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 70,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (100, 100, 100),
        },

        [TileType.WoodPlatform] = new TileProperties
        {
            Name = "Wood Platform",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 15,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (139, 90, 43),
        },

        [TileType.Torch] = new TileProperties
        {
            Name = "Torch",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 1,
            PickaxeRequired = 0,
            LightEmission = 200,
            AffectedByGravity = false,
            CanMerge = false,
            Color = (255, 200, 50),
        },

        [TileType.DungeonBrick] = new TileProperties
        {
            Name = "Dungeon Brick",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 120,
            PickaxeRequired = 65,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (50, 50, 100),
        },

        [TileType.CrackedDungeonBrick] = new TileProperties
        {
            Name = "Cracked Dungeon Brick",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 100,
            PickaxeRequired = 55,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (60, 60, 90),
        },

        [TileType.Obsidian] = new TileProperties
        {
            Name = "Obsidian",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 150,
            PickaxeRequired = 65,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (30, 20, 40),
        },

        [TileType.CrystalBlock] = new TileProperties
        {
            Name = "Crystal Block",
            IsSolid = true,
            BlocksLight = false,
            MiningTime = 80,
            PickaxeRequired = 0,
            LightEmission = 60,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (200, 100, 255),
        },

        [TileType.GraniteBlock] = new TileProperties
        {
            Name = "Granite Block",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 80,
            PickaxeRequired = 55,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (40, 40, 60),
        },

        [TileType.MarbleBlock] = new TileProperties
        {
            Name = "Marble Block",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 80,
            PickaxeRequired = 55,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (240, 240, 250),
        },

        // === SPECIAL ===
        [TileType.Bedrock] = new TileProperties
        {
            Name = "Bedrock",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = -1,
            PickaxeRequired = int.MaxValue,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
            Color = (20, 20, 20),
        },
    };

    /// <summary>
    /// Get the properties for a tile type.
    /// </summary>
    public static TileProperties Get(TileType type)
    {
        return _properties.TryGetValue(type, out var props)
            ? props
            : TileProperties.Default;
    }

    /// <summary>
    /// Check if a tile type is solid.
    /// </summary>
    public static bool IsSolid(TileType type) => Get(type).IsSolid;

    /// <summary>
    /// Check if a tile type blocks light.
    /// </summary>
    public static bool BlocksLight(TileType type) => Get(type).BlocksLight;

    /// <summary>
    /// Get light emission for a tile type.
    /// </summary>
    public static byte GetLightEmission(TileType type) => Get(type).LightEmission;

    /// <summary>
    /// Get the color for a tile type.
    /// </summary>
    public static (byte R, byte G, byte B) GetColor(TileType type) => Get(type).Color;
}