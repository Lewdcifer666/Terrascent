using Terrascent.Items;

namespace Terrascent.Economy;

/// <summary>
/// Manages chest economy, pricing, and tracking.
/// Implements Risk of Rain-style scaling costs.
/// </summary>
public class ChestManager
{
    private readonly DifficultyManager _difficulty;
    private readonly RarityRoller _roller;

    /// <summary>
    /// Total number of chests opened this run.
    /// </summary>
    public int TotalChestsOpened { get; private set; }

    /// <summary>
    /// Chests opened by type.
    /// </summary>
    private readonly Dictionary<ChestType, int> _chestsOpenedByType = new();

    /// <summary>
    /// Per-chest cost increase rate (5% per chest opened).
    /// </summary>
    public float CostIncreasePerChest { get; set; } = 0.05f;

    /// <summary>
    /// Event fired when a chest is opened.
    /// </summary>
    public event Action<ChestType, ChestDrop>? OnChestOpened;

    public ChestManager(DifficultyManager difficulty, int? seed = null)
    {
        _difficulty = difficulty;
        _roller = new RarityRoller(seed);

        // Initialize counters
        foreach (ChestType type in Enum.GetValues<ChestType>())
        {
            _chestsOpenedByType[type] = 0;
        }
    }

    /// <summary>
    /// Calculate the current cost of a chest type.
    /// Cost scales with difficulty and number of chests opened.
    /// </summary>
    public int GetChestCost(ChestType type)
    {
        var data = ChestTypeRegistry.Get(type);

        // Boss chests are free
        if (data.BaseCost <= 0)
            return 0;

        // Base cost
        float cost = data.BaseCost;

        // Difficulty scaling: cost * difficulty^scalingFactor
        cost *= MathF.Pow(_difficulty.Coefficient, data.DifficultyScaling);

        // Chests opened scaling: +5% per chest
        cost *= 1f + TotalChestsOpened * CostIncreasePerChest;

        return (int)MathF.Ceiling(cost);
    }

    /// <summary>
    /// Get the base cost of a chest (before scaling).
    /// </summary>
    public int GetBaseChestCost(ChestType type)
    {
        return ChestTypeRegistry.Get(type).BaseCost;
    }

    /// <summary>
    /// Try to open a chest. Returns the drops if successful.
    /// </summary>
    /// <param name="type">Type of chest to open</param>
    /// <param name="currency">Player's currency to deduct from</param>
    /// <param name="luckBonus">Luck bonus for better rolls</param>
    /// <returns>Chest contents if opened, null if can't afford</returns>
    public ChestDrop? TryOpenChest(ChestType type, Currency currency, int luckBonus = 0)
    {
        int cost = GetChestCost(type);

        // Check if player can afford
        if (!currency.TrySpend(cost))
        {
            System.Diagnostics.Debug.WriteLine($"Can't afford {type} chest (cost: {cost}, have: {currency.Gold})");
            return null;
        }

        // Roll the drops
        ChestDrop drop = _roller.RollChestDrop(type, luckBonus);

        // Handle equipment chests specially
        if (type == ChestType.Equipment)
        {
            var equipDrop = new ChestDrop { ChestType = type, Items = new List<ItemType>() };
            var data = ChestTypeRegistry.Get(type);
            ItemRarity rarity = _roller.RollRarity(data.RarityWeights, luckBonus);
            ItemType? weapon = _roller.RollEquipment(rarity);
            if (weapon.HasValue)
            {
                equipDrop.Items.Add(weapon.Value);
            }
            drop = equipDrop;
        }

        // Track statistics
        TotalChestsOpened++;
        _chestsOpenedByType[type]++;

        // Fire event
        OnChestOpened?.Invoke(type, drop);

        System.Diagnostics.Debug.WriteLine($"Opened {type} chest for {cost} gold. Contents: {string.Join(", ", drop.Items)}");

        return drop;
    }

    /// <summary>
    /// Open a free chest (boss drops, etc).
    /// </summary>
    public ChestDrop OpenFreeChest(ChestType type, int luckBonus = 0)
    {
        ChestDrop drop = _roller.RollChestDrop(type, luckBonus);

        TotalChestsOpened++;
        _chestsOpenedByType[type]++;

        OnChestOpened?.Invoke(type, drop);

        return drop;
    }

    /// <summary>
    /// Get number of chests opened of a specific type.
    /// </summary>
    public int GetChestsOpened(ChestType type)
    {
        return _chestsOpenedByType.GetValueOrDefault(type, 0);
    }

    /// <summary>
    /// Preview what opening a chest might give (for UI tooltips).
    /// </summary>
    public string GetChestPreview(ChestType type)
    {
        var data = ChestTypeRegistry.Get(type);
        var weights = data.RarityWeights;
        float total = weights.Common + weights.Uncommon + weights.Rare + weights.Legendary;

        string preview = $"{data.Name}\n";
        preview += $"Cost: {GetChestCost(type)} gold\n";
        preview += $"---\n";

        if (weights.Common > 0)
            preview += $"Common: {weights.Common / total * 100:F0}%\n";
        if (weights.Uncommon > 0)
            preview += $"Uncommon: {weights.Uncommon / total * 100:F0}%\n";
        if (weights.Rare > 0)
            preview += $"Rare: {weights.Rare / total * 100:F0}%\n";
        if (weights.Legendary > 0)
            preview += $"Legendary: {weights.Legendary / total * 100:F0}%\n";

        return preview.TrimEnd();
    }

    /// <summary>
    /// Reset for a new run.
    /// </summary>
    public void Reset()
    {
        TotalChestsOpened = 0;
        foreach (var key in _chestsOpenedByType.Keys.ToList())
        {
            _chestsOpenedByType[key] = 0;
        }
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(TotalChestsOpened);
        writer.Write(_chestsOpenedByType.Count);
        foreach (var (type, count) in _chestsOpenedByType)
        {
            writer.Write((byte)type);
            writer.Write(count);
        }
    }

    public void LoadFrom(BinaryReader reader)
    {
        TotalChestsOpened = reader.ReadInt32();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var type = (ChestType)reader.ReadByte();
            int opened = reader.ReadInt32();
            _chestsOpenedByType[type] = opened;
        }
    }

    #endregion
}