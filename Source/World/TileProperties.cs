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
    };
}

/// <summary>
/// Registry of all tile properties, indexed by TileType.
/// </summary>
public static class TileRegistry
{
    private static readonly Dictionary<TileType, TileProperties> _properties = new()
    {
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
        },

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
        },

        [TileType.Leaves] = new TileProperties
        {
            Name = "Leaves",
            IsSolid = false,  // Can walk through leaves
            BlocksLight = false,
            MiningTime = 1,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
        },

        [TileType.Sand] = new TileProperties
        {
            Name = "Sand",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 20,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = true,  // Falls!
            CanMerge = true,
        },

        [TileType.CopperOre] = new TileProperties
        {
            Name = "Copper Ore",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = 90,
            PickaxeRequired = 35,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = false,  // Ores don't merge visually
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
        },

        [TileType.Torch] = new TileProperties
        {
            Name = "Torch",
            IsSolid = false,
            BlocksLight = false,
            MiningTime = 1,
            PickaxeRequired = 0,
            LightEmission = 200,  // Emits light!
            AffectedByGravity = false,
            CanMerge = false,
        },

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
        },

        [TileType.WoodPlatform] = new TileProperties
        {
            Name = "Wood Platform",
            IsSolid = false,  // Can pass through (with special logic)
            BlocksLight = false,
            MiningTime = 15,
            PickaxeRequired = 0,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
        },

        [TileType.Bedrock] = new TileProperties
        {
            Name = "Bedrock",
            IsSolid = true,
            BlocksLight = true,
            MiningTime = -1,  // Cannot be mined
            PickaxeRequired = int.MaxValue,
            LightEmission = 0,
            AffectedByGravity = false,
            CanMerge = true,
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
}