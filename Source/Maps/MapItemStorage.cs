using Terrascent.Items;

namespace Terrascent.Maps;

/// <summary>
/// Storage system for MapItem data linked to inventory slots.
/// Maps in the inventory store their tier via ItemType (Map_T1-T16), 
/// but the actual MapItem data (rarity, affixes, biome) is stored here.
/// </summary>
public class MapItemStorage
{
    /// <summary>Dictionary mapping slot indices to MapItem data.</summary>
    private readonly Dictionary<int, MapItem> _slotToMap = new();

    /// <summary>Dictionary mapping MapItem GUIDs to their slot indices.</summary>
    private readonly Dictionary<Guid, int> _mapToSlot = new();

    /// <summary>
    /// Store a MapItem at a specific inventory slot.
    /// </summary>
    public void StoreMap(int slotIndex, MapItem map)
    {
        // Remove any existing map at this slot
        if (_slotToMap.TryGetValue(slotIndex, out var existingMap))
        {
            _mapToSlot.Remove(existingMap.Id);
        }

        _slotToMap[slotIndex] = map;
        _mapToSlot[map.Id] = slotIndex;

        System.Diagnostics.Debug.WriteLine($"MapItemStorage: Stored {map.GetDisplayName()} at slot {slotIndex}");
    }

    /// <summary>
    /// Get the MapItem at a specific inventory slot.
    /// </summary>
    public MapItem? GetMap(int slotIndex)
    {
        return _slotToMap.TryGetValue(slotIndex, out var map) ? map : null;
    }

    /// <summary>
    /// Remove the MapItem at a specific inventory slot.
    /// </summary>
    public MapItem? RemoveMap(int slotIndex)
    {
        if (_slotToMap.TryGetValue(slotIndex, out var map))
        {
            _slotToMap.Remove(slotIndex);
            _mapToSlot.Remove(map.Id);
            System.Diagnostics.Debug.WriteLine($"MapItemStorage: Removed {map.GetDisplayName()} from slot {slotIndex}");
            return map;
        }
        return null;
    }

    /// <summary>
    /// Move a map from one slot to another.
    /// </summary>
    public bool MoveMap(int fromSlot, int toSlot)
    {
        var map = RemoveMap(fromSlot);
        if (map == null) return false;

        // If there's a map at the destination, swap them
        var destMap = RemoveMap(toSlot);

        StoreMap(toSlot, map);

        if (destMap != null)
        {
            StoreMap(fromSlot, destMap);
        }

        return true;
    }

    /// <summary>
    /// Check if a slot contains a map.
    /// </summary>
    public bool HasMap(int slotIndex)
    {
        return _slotToMap.ContainsKey(slotIndex);
    }

    /// <summary>
    /// Get the slot index for a specific map.
    /// </summary>
    public int? GetSlot(MapItem map)
    {
        return _mapToSlot.TryGetValue(map.Id, out var slot) ? slot : null;
    }

    /// <summary>
    /// Get all stored maps.
    /// </summary>
    public IEnumerable<(int slot, MapItem map)> GetAllMaps()
    {
        foreach (var kvp in _slotToMap)
        {
            yield return (kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Get total number of stored maps.
    /// </summary>
    public int Count => _slotToMap.Count;

    /// <summary>
    /// Clear all stored maps.
    /// </summary>
    public void Clear()
    {
        _slotToMap.Clear();
        _mapToSlot.Clear();
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(_slotToMap.Count);
        foreach (var kvp in _slotToMap)
        {
            writer.Write(kvp.Key);
            kvp.Value.SaveTo(writer);
        }
    }

    public static MapItemStorage LoadFrom(BinaryReader reader)
    {
        var storage = new MapItemStorage();
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            int slot = reader.ReadInt32();
            var map = MapItem.LoadFrom(reader);
            storage.StoreMap(slot, map);
        }

        return storage;
    }

    #endregion
}

/// <summary>
/// Extension methods for ItemType related to maps.
/// </summary>
public static class MapItemTypeExtensions
{
    /// <summary>
    /// Check if an ItemType is a map.
    /// </summary>
    public static bool IsMap(this ItemType type)
    {
        return type >= ItemType.Map_T1 && type <= ItemType.Map_T16;
    }

    /// <summary>
    /// Check if an ItemType is map currency.
    /// </summary>
    public static bool IsMapCurrency(this ItemType type)
    {
        return type >= ItemType.OrbOfTransmutation && type <= ItemType.ExaltedOrb;
    }

    /// <summary>
    /// Get the map tier for a map ItemType.
    /// </summary>
    public static int GetMapTier(this ItemType type)
    {
        if (!type.IsMap()) return 0;
        return (int)type - (int)ItemType.Map_T1 + 1;
    }

    /// <summary>
    /// Get the ItemType for a specific map tier.
    /// </summary>
    public static ItemType GetMapItemType(int tier)
    {
        tier = Math.Clamp(tier, 1, 16);
        return (ItemType)((int)ItemType.Map_T1 + tier - 1);
    }

    /// <summary>
    /// Convert MapCurrency enum to ItemType.
    /// </summary>
    public static ItemType ToItemType(this MapCurrency currency)
    {
        return currency switch
        {
            MapCurrency.OrbOfTransmutation => ItemType.OrbOfTransmutation,
            MapCurrency.OrbOfAlteration => ItemType.OrbOfAlteration,
            MapCurrency.RegalOrb => ItemType.RegalOrb,
            MapCurrency.OrbOfAlchemy => ItemType.OrbOfAlchemy,
            MapCurrency.ChaosOrb => ItemType.ChaosOrb,
            MapCurrency.DivineOrb => ItemType.DivineOrb,
            MapCurrency.OrbOfScouring => ItemType.OrbOfScouring,
            MapCurrency.VaalOrb => ItemType.VaalOrb,
            MapCurrency.OrbOfAugmentation => ItemType.OrbOfAugmentation,
            MapCurrency.ExaltedOrb => ItemType.ExaltedOrb,
            _ => ItemType.None
        };
    }

    /// <summary>
    /// Convert ItemType to MapCurrency enum.
    /// </summary>
    public static MapCurrency? ToMapCurrency(this ItemType type)
    {
        if (!type.IsMapCurrency()) return null;

        return type switch
        {
            ItemType.OrbOfTransmutation => MapCurrency.OrbOfTransmutation,
            ItemType.OrbOfAlteration => MapCurrency.OrbOfAlteration,
            ItemType.RegalOrb => MapCurrency.RegalOrb,
            ItemType.OrbOfAlchemy => MapCurrency.OrbOfAlchemy,
            ItemType.ChaosOrb => MapCurrency.ChaosOrb,
            ItemType.DivineOrb => MapCurrency.DivineOrb,
            ItemType.OrbOfScouring => MapCurrency.OrbOfScouring,
            ItemType.VaalOrb => MapCurrency.VaalOrb,
            ItemType.OrbOfAugmentation => MapCurrency.OrbOfAugmentation,
            ItemType.ExaltedOrb => MapCurrency.ExaltedOrb,
            _ => null
        };
    }
}