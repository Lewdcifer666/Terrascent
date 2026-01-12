using Microsoft.Xna.Framework;

namespace Terrascent.Maps;

/// <summary>
/// Map tier system (T1-T16). Higher tiers = higher monster level and rewards.
/// </summary>
public static class MapTier
{
    public const int MIN_TIER = 1;
    public const int MAX_TIER = 16;

    /// <summary>Base monster level is 68 + tier.</summary>
    public const int BASE_MONSTER_LEVEL = 68;

    /// <summary>Get the monster level for a map tier.</summary>
    public static int GetMonsterLevel(int tier)
    {
        tier = Math.Clamp(tier, MIN_TIER, MAX_TIER);
        return BASE_MONSTER_LEVEL + tier;
    }

    /// <summary>Get the item quantity bonus for a tier (percentage).</summary>
    public static float GetBaseItemQuantity(int tier)
    {
        tier = Math.Clamp(tier, MIN_TIER, MAX_TIER);
        return 5f + (tier - 1) * 3f; // 5% at T1, up to 50% at T16
    }

    /// <summary>Get the item rarity bonus for a tier (percentage).</summary>
    public static float GetBaseItemRarity(int tier)
    {
        tier = Math.Clamp(tier, MIN_TIER, MAX_TIER);
        return 10f + (tier - 1) * 5f; // 10% at T1, up to 85% at T16
    }

    /// <summary>Get the pack size bonus for a tier (percentage).</summary>
    public static float GetBasePackSize(int tier)
    {
        tier = Math.Clamp(tier, MIN_TIER, MAX_TIER);
        return (tier - 1) * 2f; // 0% at T1, up to 30% at T16
    }

    /// <summary>Get the experience multiplier for a tier.</summary>
    public static float GetExperienceMultiplier(int tier)
    {
        tier = Math.Clamp(tier, MIN_TIER, MAX_TIER);
        return 1f + (tier - 1) * 0.1f; // 1.0x at T1, up to 2.5x at T16
    }

    /// <summary>Get display name for a tier.</summary>
    public static string GetDisplayName(int tier)
    {
        return $"T{Math.Clamp(tier, MIN_TIER, MAX_TIER)}";
    }

    /// <summary>Get tier color based on difficulty.</summary>
    public static Color GetTierColor(int tier)
    {
        tier = Math.Clamp(tier, MIN_TIER, MAX_TIER);

        return tier switch
        {
            <= 5 => Color.White,           // White maps
            <= 10 => Color.Yellow,         // Yellow maps
            <= 16 => Color.Red,            // Red maps
            _ => Color.White
        };
    }

    /// <summary>Get tier category name (White/Yellow/Red).</summary>
    public static string GetTierCategory(int tier)
    {
        tier = Math.Clamp(tier, MIN_TIER, MAX_TIER);
        return tier switch
        {
            <= 5 => "White",
            <= 10 => "Yellow",
            <= 16 => "Red",
            _ => "Unknown"
        };
    }

    /// <summary>Check if tier is valid.</summary>
    public static bool IsValid(int tier) => tier >= MIN_TIER && tier <= MAX_TIER;
}