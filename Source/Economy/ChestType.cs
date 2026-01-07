namespace Terrascent.Economy;

/// <summary>
/// Types of chests that can spawn in the world.
/// </summary>
public enum ChestType : byte
{
    /// <summary>
    /// Small chest - cheap, common items.
    /// </summary>
    Small = 0,

    /// <summary>
    /// Large chest - more expensive, better items.
    /// </summary>
    Large = 1,

    /// <summary>
    /// Equipment chest - guaranteed weapon drop.
    /// </summary>
    Equipment = 2,

    /// <summary>
    /// Legendary chest - very expensive, legendary items.
    /// </summary>
    Legendary = 3,

    /// <summary>
    /// Boss chest - free, appears after defeating a boss.
    /// </summary>
    Boss = 4,

    /// <summary>
    /// Lunar chest - special currency, unique items.
    /// </summary>
    Lunar = 5,
}

/// <summary>
/// Static data for each chest type.
/// </summary>
public readonly struct ChestTypeData
{
    /// <summary>Display name.</summary>
    public string Name { get; init; }

    /// <summary>Base cost in gold before scaling.</summary>
    public int BaseCost { get; init; }

    /// <summary>How much difficulty affects price (1.0 = normal).</summary>
    public float DifficultyScaling { get; init; }

    /// <summary>Chance weights for each rarity tier.</summary>
    public RarityWeights RarityWeights { get; init; }

    /// <summary>Number of items dropped.</summary>
    public int ItemCount { get; init; }

    /// <summary>Can this chest spawn naturally?</summary>
    public bool CanSpawnNaturally { get; init; }

    /// <summary>RGB color for rendering.</summary>
    public (byte R, byte G, byte B) Color { get; init; }
}

/// <summary>
/// Weights for rarity rolls. Higher = more likely.
/// </summary>
public struct RarityWeights
{
    public float Common;
    public float Uncommon;
    public float Rare;
    public float Legendary;

    public RarityWeights(float common, float uncommon, float rare, float legendary)
    {
        Common = common;
        Uncommon = uncommon;
        Rare = rare;
        Legendary = legendary;
    }

    /// <summary>
    /// Standard weights for small chests.
    /// </summary>
    public static RarityWeights Small => new(80f, 18f, 2f, 0f);

    /// <summary>
    /// Better weights for large chests.
    /// </summary>
    public static RarityWeights Large => new(55f, 35f, 9f, 1f);

    /// <summary>
    /// Equipment chest weights (weapons only).
    /// </summary>
    public static RarityWeights Equipment => new(50f, 35f, 13f, 2f);

    /// <summary>
    /// Legendary chest - guaranteed rare+.
    /// </summary>
    public static RarityWeights LegendaryChest => new(0f, 20f, 55f, 25f);

    /// <summary>
    /// Boss drops.
    /// </summary>
    public static RarityWeights Boss => new(0f, 30f, 50f, 20f);
}

/// <summary>
/// Registry of chest type data.
/// </summary>
public static class ChestTypeRegistry
{
    private static readonly Dictionary<ChestType, ChestTypeData> _data = new()
    {
        [ChestType.Small] = new ChestTypeData
        {
            Name = "Chest",
            BaseCost = 25,
            DifficultyScaling = 1.25f,
            RarityWeights = RarityWeights.Small,
            ItemCount = 1,
            CanSpawnNaturally = true,
            Color = (139, 90, 43), // Brown
        },

        [ChestType.Large] = new ChestTypeData
        {
            Name = "Large Chest",
            BaseCost = 50,
            DifficultyScaling = 1.25f,
            RarityWeights = RarityWeights.Large,
            ItemCount = 1,
            CanSpawnNaturally = true,
            Color = (184, 115, 51), // Copper/gold
        },

        [ChestType.Equipment] = new ChestTypeData
        {
            Name = "Equipment Chest",
            BaseCost = 40,
            DifficultyScaling = 1.3f,
            RarityWeights = RarityWeights.Equipment,
            ItemCount = 1,
            CanSpawnNaturally = true,
            Color = (100, 150, 200), // Blue steel
        },

        [ChestType.Legendary] = new ChestTypeData
        {
            Name = "Legendary Chest",
            BaseCost = 400,
            DifficultyScaling = 1.5f,
            RarityWeights = RarityWeights.LegendaryChest,
            ItemCount = 1,
            CanSpawnNaturally = false, // Rare spawn
            Color = (255, 180, 50), // Gold/orange
        },

        [ChestType.Boss] = new ChestTypeData
        {
            Name = "Boss Chest",
            BaseCost = 0, // Free!
            DifficultyScaling = 0f,
            RarityWeights = RarityWeights.Boss,
            ItemCount = 3,
            CanSpawnNaturally = false,
            Color = (255, 80, 80), // Red
        },

        [ChestType.Lunar] = new ChestTypeData
        {
            Name = "Lunar Chest",
            BaseCost = 0, // Uses lunar coins
            DifficultyScaling = 0f,
            RarityWeights = new RarityWeights(0f, 0f, 0f, 100f),
            ItemCount = 1,
            CanSpawnNaturally = false,
            Color = (150, 150, 255), // Pale blue
        },
    };

    public static ChestTypeData Get(ChestType type)
    {
        return _data.TryGetValue(type, out var data) ? data : _data[ChestType.Small];
    }
}