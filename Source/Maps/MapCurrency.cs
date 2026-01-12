using Microsoft.Xna.Framework;

namespace Terrascent.Maps;

/// <summary>
/// Currency types for modifying maps (Path of Exile style).
/// </summary>
public enum MapCurrency
{
    /// <summary>Upgrade Normal map to Magic (1-2 mods).</summary>
    OrbOfTransmutation = 0,

    /// <summary>Reroll Magic map mods.</summary>
    OrbOfAlteration = 1,

    /// <summary>Upgrade Magic map to Rare (add 3-4 mods).</summary>
    RegalOrb = 2,

    /// <summary>Upgrade Normal map to Rare (4-6 mods).</summary>
    OrbOfAlchemy = 3,

    /// <summary>Reroll all Rare map mods.</summary>
    ChaosOrb = 4,

    /// <summary>Reroll numeric values of all mods.</summary>
    DivineOrb = 5,

    /// <summary>Remove all mods from a map.</summary>
    OrbOfScouring = 6,

    /// <summary>Corrupt the map (unpredictable outcomes).</summary>
    VaalOrb = 7,

    /// <summary>Add a random mod to Magic map (up to 2 total).</summary>
    OrbOfAugmentation = 8,

    /// <summary>Add a crafted mod.</summary>
    ExaltedOrb = 9
}

/// <summary>
/// Extension methods for MapCurrency.
/// </summary>
public static class MapCurrencyExtensions
{
    /// <summary>Get display name for a currency.</summary>
    public static string GetDisplayName(this MapCurrency currency) => currency switch
    {
        MapCurrency.OrbOfTransmutation => "Orb of Transmutation",
        MapCurrency.OrbOfAlteration => "Orb of Alteration",
        MapCurrency.RegalOrb => "Regal Orb",
        MapCurrency.OrbOfAlchemy => "Orb of Alchemy",
        MapCurrency.ChaosOrb => "Chaos Orb",
        MapCurrency.DivineOrb => "Divine Orb",
        MapCurrency.OrbOfScouring => "Orb of Scouring",
        MapCurrency.VaalOrb => "Vaal Orb",
        MapCurrency.OrbOfAugmentation => "Orb of Augmentation",
        MapCurrency.ExaltedOrb => "Exalted Orb",
        _ => "Unknown Currency"
    };

    /// <summary>Get description for a currency.</summary>
    public static string GetDescription(this MapCurrency currency) => currency switch
    {
        MapCurrency.OrbOfTransmutation => "Upgrades a Normal map to Magic",
        MapCurrency.OrbOfAlteration => "Rerolls a Magic map",
        MapCurrency.RegalOrb => "Upgrades a Magic map to Rare",
        MapCurrency.OrbOfAlchemy => "Upgrades a Normal map to Rare",
        MapCurrency.ChaosOrb => "Rerolls a Rare map",
        MapCurrency.DivineOrb => "Randomizes mod values",
        MapCurrency.OrbOfScouring => "Removes all mods from a map",
        MapCurrency.VaalOrb => "Corrupts a map with unpredictable results",
        MapCurrency.OrbOfAugmentation => "Adds a mod to a Magic map",
        MapCurrency.ExaltedOrb => "Adds a mod to a Rare map",
        _ => "Unknown effect"
    };

    /// <summary>Get display color for a currency.</summary>
    public static Color GetColor(this MapCurrency currency) => currency switch
    {
        MapCurrency.OrbOfTransmutation => Color.Gray,
        MapCurrency.OrbOfAlteration => Color.DodgerBlue,
        MapCurrency.RegalOrb => Color.Gold,
        MapCurrency.OrbOfAlchemy => Color.Gold,
        MapCurrency.ChaosOrb => Color.Purple,
        MapCurrency.DivineOrb => Color.Orange,
        MapCurrency.OrbOfScouring => Color.White,
        MapCurrency.VaalOrb => Color.Red,
        MapCurrency.OrbOfAugmentation => Color.LightBlue,
        MapCurrency.ExaltedOrb => Color.Goldenrod,
        _ => Color.White
    };

    /// <summary>Check if this currency can be applied to a map.</summary>
    public static bool CanApplyTo(this MapCurrency currency, MapItem map)
    {
        if (map.IsCorrupted && currency != MapCurrency.VaalOrb)
            return false;

        return currency switch
        {
            MapCurrency.OrbOfTransmutation => map.Rarity == MapRarity.Normal,
            MapCurrency.OrbOfAlteration => map.Rarity == MapRarity.Magic,
            MapCurrency.RegalOrb => map.Rarity == MapRarity.Magic,
            MapCurrency.OrbOfAlchemy => map.Rarity == MapRarity.Normal,
            MapCurrency.ChaosOrb => map.Rarity == MapRarity.Rare,
            MapCurrency.DivineOrb => map.AffixCount > 0 && !map.IsCorrupted,
            MapCurrency.OrbOfScouring => map.Rarity != MapRarity.Normal && !map.IsCorrupted,
            MapCurrency.VaalOrb => !map.IsCorrupted,
            MapCurrency.OrbOfAugmentation => map.Rarity == MapRarity.Magic && map.AffixCount < 2,
            MapCurrency.ExaltedOrb => map.Rarity == MapRarity.Rare && map.AffixCount < 6,
            _ => false
        };
    }

    /// <summary>Get the relative rarity tier of this currency (1-5, higher = rarer).</summary>
    public static int GetRarityTier(this MapCurrency currency) => currency switch
    {
        MapCurrency.OrbOfTransmutation => 1,
        MapCurrency.OrbOfAlteration => 1,
        MapCurrency.OrbOfAugmentation => 1,
        MapCurrency.OrbOfScouring => 2,
        MapCurrency.OrbOfAlchemy => 2,
        MapCurrency.ChaosOrb => 3,
        MapCurrency.RegalOrb => 3,
        MapCurrency.VaalOrb => 3,
        MapCurrency.DivineOrb => 4,
        MapCurrency.ExaltedOrb => 5,
        _ => 1
    };
}