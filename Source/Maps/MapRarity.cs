using Microsoft.Xna.Framework;

namespace Terrascent.Maps;

/// <summary>
/// Map rarity determines the number of affixes.
/// Normal = 0, Magic = 1-2, Rare = 4-6, Corrupted = special
/// </summary>
public enum MapRarity
{
    /// <summary>No affixes. Can be upgraded with currency.</summary>
    Normal = 0,

    /// <summary>1-2 affixes (1 prefix and/or 1 suffix).</summary>
    Magic = 1,

    /// <summary>4-6 affixes (up to 3 prefixes, up to 3 suffixes).</summary>
    Rare = 2,

    /// <summary>Cannot be modified. May have 8 mods or special properties.</summary>
    Corrupted = 3
}

public static class MapRarityExtensions
{
    /// <summary>Get the maximum number of prefixes for this rarity.</summary>
    public static int MaxPrefixes(this MapRarity rarity) => rarity switch
    {
        MapRarity.Normal => 0,
        MapRarity.Magic => 1,
        MapRarity.Rare => 3,
        MapRarity.Corrupted => 4,
        _ => 0
    };

    /// <summary>Get the maximum number of suffixes for this rarity.</summary>
    public static int MaxSuffixes(this MapRarity rarity) => rarity switch
    {
        MapRarity.Normal => 0,
        MapRarity.Magic => 1,
        MapRarity.Rare => 3,
        MapRarity.Corrupted => 4,
        _ => 0
    };

    /// <summary>Get display color for this rarity.</summary>
    public static Color GetColor(this MapRarity rarity) => rarity switch
    {
        MapRarity.Normal => Color.White,
        MapRarity.Magic => Color.DodgerBlue,
        MapRarity.Rare => Color.Gold,
        MapRarity.Corrupted => Color.Red,
        _ => Color.White
    };

    /// <summary>Get display name for this rarity.</summary>
    public static string GetDisplayName(this MapRarity rarity) => rarity switch
    {
        MapRarity.Normal => "Normal",
        MapRarity.Magic => "Magic",
        MapRarity.Rare => "Rare",
        MapRarity.Corrupted => "Corrupted",
        _ => "Unknown"
    };
}