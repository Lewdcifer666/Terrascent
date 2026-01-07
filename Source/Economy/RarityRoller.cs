using Terrascent.Items;
using Terrascent.Items.Effects;

namespace Terrascent.Economy;

/// <summary>
/// Handles weighted random selection of item rarities and items.
/// </summary>
public class RarityRoller
{
    private readonly Random _rng;

    public RarityRoller(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Roll for an item rarity based on weights.
    /// </summary>
    public ItemRarity RollRarity(RarityWeights weights, int luckBonus = 0)
    {
        // Luck gives additional rolls, taking the best result
        ItemRarity bestRarity = ItemRarity.Common;

        for (int i = 0; i <= luckBonus; i++)
        {
            ItemRarity rolled = RollRarityOnce(weights);
            if ((int)rolled > (int)bestRarity)
            {
                bestRarity = rolled;
            }
        }

        return bestRarity;
    }

    /// <summary>
    /// Single rarity roll without luck.
    /// </summary>
    private ItemRarity RollRarityOnce(RarityWeights weights)
    {
        float total = weights.Common + weights.Uncommon + weights.Rare + weights.Legendary;
        float roll = (float)_rng.NextDouble() * total;

        if (roll < weights.Legendary)
            return ItemRarity.Legendary;

        roll -= weights.Legendary;
        if (roll < weights.Rare)
            return ItemRarity.Rare;

        roll -= weights.Rare;
        if (roll < weights.Uncommon)
            return ItemRarity.Uncommon;

        return ItemRarity.Common;
    }

    /// <summary>
    /// Roll for a random stackable item of a specific rarity.
    /// </summary>
    public ItemType? RollStackableItem(ItemRarity targetRarity)
    {
        var itemsOfRarity = StackableItemRegistry.GetByRarity(targetRarity).ToList();

        if (itemsOfRarity.Count == 0)
        {
            // Fallback to lower rarity if none available
            return targetRarity switch
            {
                ItemRarity.Legendary => RollStackableItem(ItemRarity.Rare),
                ItemRarity.Rare => RollStackableItem(ItemRarity.Uncommon),
                ItemRarity.Uncommon => RollStackableItem(ItemRarity.Common),
                _ => null
            };
        }

        int index = _rng.Next(itemsOfRarity.Count);
        return itemsOfRarity[index].ItemType;
    }

    /// <summary>
    /// Roll for a complete chest drop (rarity + item).
    /// </summary>
    public ChestDrop RollChestDrop(ChestType chestType, int luckBonus = 0)
    {
        var chestData = ChestTypeRegistry.Get(chestType);
        var drops = new List<ItemType>();

        for (int i = 0; i < chestData.ItemCount; i++)
        {
            ItemRarity rarity = RollRarity(chestData.RarityWeights, luckBonus);
            ItemType? item = RollStackableItem(rarity);

            if (item.HasValue)
            {
                drops.Add(item.Value);
            }
        }

        return new ChestDrop
        {
            ChestType = chestType,
            Items = drops,
        };
    }

    /// <summary>
    /// Roll for equipment (weapons) from an equipment chest.
    /// </summary>
    public ItemType? RollEquipment(ItemRarity targetRarity)
    {
        var weapons = Combat.WeaponRegistry.GetAllWeaponTypes()
            .Where(w => ItemRegistry.Get(w).Rarity == targetRarity)
            .ToList();

        if (weapons.Count == 0)
        {
            // Fallback
            return targetRarity switch
            {
                ItemRarity.Legendary => RollEquipment(ItemRarity.Rare),
                ItemRarity.Rare => RollEquipment(ItemRarity.Uncommon),
                ItemRarity.Uncommon => RollEquipment(ItemRarity.Common),
                _ => ItemType.WoodSword
            };
        }

        return weapons[_rng.Next(weapons.Count)];
    }
}

/// <summary>
/// Result of opening a chest.
/// </summary>
public struct ChestDrop
{
    public ChestType ChestType;
    public List<ItemType> Items;

    public bool IsEmpty => Items == null || Items.Count == 0;
}