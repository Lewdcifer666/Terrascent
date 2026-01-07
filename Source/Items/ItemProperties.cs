using Terrascent.World;

namespace Terrascent.Items;

/// <summary>
/// Static properties for each item type.
/// </summary>
public readonly struct ItemProperties
{
    /// <summary>
    /// Display name of the item.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Maximum stack size (1 for tools/weapons, 999 for materials).
    /// </summary>
    public int MaxStack { get; init; }

    /// <summary>
    /// If this is a placeable block, which tile type does it place?
    /// </summary>
    public TileType? PlacesTile { get; init; }

    /// <summary>
    /// Item category for sorting/filtering.
    /// </summary>
    public ItemCategory Category { get; init; }

    /// <summary>
    /// Rarity affects name color and drop glow.
    /// </summary>
    public ItemRarity Rarity { get; init; }

    /// <summary>
    /// Sell value in copper coins.
    /// </summary>
    public int SellValue { get; init; }

    /// <summary>
    /// For tools: mining/chopping power.
    /// </summary>
    public int ToolPower { get; init; }

    /// <summary>
    /// For weapons: base damage.
    /// </summary>
    public int Damage { get; init; }

    /// <summary>
    /// Default properties.
    /// </summary>
    public static ItemProperties Default => new()
    {
        Name = "Unknown",
        MaxStack = 999,
        PlacesTile = null,
        Category = ItemCategory.Misc,
        Rarity = ItemRarity.Common,
        SellValue = 0,
        ToolPower = 0,
        Damage = 0,
    };
}

public enum ItemCategory
{
    Misc,
    Block,
    Tool,
    Weapon,
    Armor,
    Accessory,
    Consumable,
    Material,
}

public enum ItemRarity
{
    Common,      // White
    Uncommon,    // Green
    Rare,        // Blue
    Epic,        // Purple
    Legendary,   // Orange
    Boss,        // Red
}

/// <summary>
/// Registry of all item properties.
/// </summary>
public static class ItemRegistry
{
    private static readonly Dictionary<ItemType, ItemProperties> _properties = new()
    {
        // === Blocks ===
        [ItemType.Dirt] = new ItemProperties
        {
            Name = "Dirt",
            MaxStack = 999,
            PlacesTile = TileType.Dirt,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 0,
        },

        [ItemType.Stone] = new ItemProperties
        {
            Name = "Stone",
            MaxStack = 999,
            PlacesTile = TileType.Stone,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 0,
        },

        [ItemType.Sand] = new ItemProperties
        {
            Name = "Sand",
            MaxStack = 999,
            PlacesTile = TileType.Sand,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 0,
        },

        [ItemType.Wood] = new ItemProperties
        {
            Name = "Wood",
            MaxStack = 999,
            PlacesTile = TileType.Wood,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 1,
        },

        [ItemType.Torch] = new ItemProperties
        {
            Name = "Torch",
            MaxStack = 999,
            PlacesTile = TileType.Torch,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 0,
        },

        [ItemType.WoodPlatform] = new ItemProperties
        {
            Name = "Wood Platform",
            MaxStack = 999,
            PlacesTile = TileType.WoodPlatform,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 0,
        },

        // === Ores ===
        [ItemType.CopperOre] = new ItemProperties
        {
            Name = "Copper Ore",
            MaxStack = 999,
            PlacesTile = TileType.CopperOre,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 5,
        },

        [ItemType.IronOre] = new ItemProperties
        {
            Name = "Iron Ore",
            MaxStack = 999,
            PlacesTile = TileType.IronOre,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Common,
            SellValue = 10,
        },

        [ItemType.SilverOre] = new ItemProperties
        {
            Name = "Silver Ore",
            MaxStack = 999,
            PlacesTile = TileType.SilverOre,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Uncommon,
            SellValue = 15,
        },

        [ItemType.GoldOre] = new ItemProperties
        {
            Name = "Gold Ore",
            MaxStack = 999,
            PlacesTile = TileType.GoldOre,
            Category = ItemCategory.Block,
            Rarity = ItemRarity.Uncommon,
            SellValue = 20,
        },

        // === Materials ===
        [ItemType.CopperBar] = new ItemProperties
        {
            Name = "Copper Bar",
            MaxStack = 999,
            Category = ItemCategory.Material,
            Rarity = ItemRarity.Common,
            SellValue = 15,
        },

        [ItemType.IronBar] = new ItemProperties
        {
            Name = "Iron Bar",
            MaxStack = 999,
            Category = ItemCategory.Material,
            Rarity = ItemRarity.Common,
            SellValue = 30,
        },

        // === Tools ===
        [ItemType.WoodPickaxe] = new ItemProperties
        {
            Name = "Wood Pickaxe",
            MaxStack = 1,
            Category = ItemCategory.Tool,
            Rarity = ItemRarity.Common,
            SellValue = 20,
            ToolPower = 35,
        },

        [ItemType.CopperPickaxe] = new ItemProperties
        {
            Name = "Copper Pickaxe",
            MaxStack = 1,
            Category = ItemCategory.Tool,
            Rarity = ItemRarity.Common,
            SellValue = 100,
            ToolPower = 55,
        },

        [ItemType.IronPickaxe] = new ItemProperties
        {
            Name = "Iron Pickaxe",
            MaxStack = 1,
            Category = ItemCategory.Tool,
            Rarity = ItemRarity.Common,
            SellValue = 200,
            ToolPower = 70,
        },

        // === Weapons ===
        [ItemType.WoodSword] = new ItemProperties
        {
            Name = "Wood Sword",
            MaxStack = 1,
            Category = ItemCategory.Weapon,
            Rarity = ItemRarity.Common,
            SellValue = 20,
            Damage = 8,
        },

        [ItemType.CopperSword] = new ItemProperties
        {
            Name = "Copper Sword",
            MaxStack = 1,
            Category = ItemCategory.Weapon,
            Rarity = ItemRarity.Common,
            SellValue = 100,
            Damage = 12,
        },
    };

    /// <summary>
    /// Get properties for an item type.
    /// </summary>
    public static ItemProperties Get(ItemType type)
    {
        return _properties.TryGetValue(type, out var props)
            ? props
            : ItemProperties.Default;
    }

    /// <summary>
    /// Get max stack size for an item type.
    /// </summary>
    public static int GetMaxStack(ItemType type) => Get(type).MaxStack;

    /// <summary>
    /// Check if an item is placeable.
    /// </summary>
    public static bool IsPlaceable(ItemType type) => Get(type).PlacesTile.HasValue;

    /// <summary>
    /// Get the tile type this item places (if any).
    /// </summary>
    public static TileType? GetPlacesTile(ItemType type) => Get(type).PlacesTile;

    /// <summary>
    /// Try to get the item type that corresponds to a tile type.
    /// </summary>
    public static ItemType? GetItemForTile(TileType tileType)
    {
        // Most block items share the same ID as their tile
        var itemType = (ItemType)(ushort)tileType;

        if (_properties.ContainsKey(itemType))
            return itemType;

        return null;
    }
}