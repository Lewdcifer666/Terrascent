namespace Terrascent.Progression;

/// <summary>
/// Defines an upgrade that can be selected at level-up.
/// Inspired by Vampire Survivors' upgrade system.
/// </summary>
public class Upgrade
{
    /// <summary>Unique identifier for this upgrade.</summary>
    public string Id { get; init; } = "";

    /// <summary>Display name shown in level-up UI.</summary>
    public string Name { get; init; } = "";

    /// <summary>Description with {0} placeholder for current value.</summary>
    public string Description { get; init; } = "";

    /// <summary>Category for synergy bonuses.</summary>
    public UpgradeCategory Category { get; init; } = UpgradeCategory.General;

    /// <summary>Rarity affects base weight and visual styling.</summary>
    public UpgradeRarity Rarity { get; init; } = UpgradeRarity.Common;

    /// <summary>Base weight for selection (before synergy bonuses).</summary>
    public float BaseWeight { get; init; } = 100f;

    /// <summary>Maximum times this upgrade can be selected (0 = unlimited).</summary>
    public int MaxStacks { get; init; } = 0;

    /// <summary>Type of stat this upgrade affects.</summary>
    public UpgradeStatType StatType { get; init; }

    /// <summary>Value applied per level of this upgrade.</summary>
    public float Value { get; init; }

    /// <summary>Is this a percentage-based upgrade?</summary>
    public bool IsPercent { get; init; }

    /// <summary>Other upgrade IDs that boost this upgrade's weight.</summary>
    public string[] SynergyWith { get; init; } = Array.Empty<string>();

    /// <summary>Other upgrade IDs required before this can appear.</summary>
    public string[] RequiredUpgrades { get; init; } = Array.Empty<string>();

    /// <summary>Minimum player level before this can appear.</summary>
    public int MinLevel { get; init; } = 1;

    /// <summary>
    /// Get the formatted value for display.
    /// </summary>
    public string GetFormattedValue(int stacks = 1)
    {
        float totalValue = Value * stacks;

        if (IsPercent)
            return $"{totalValue * 100:F0}%";

        return totalValue % 1 == 0 ? $"{(int)totalValue}" : $"{totalValue:F1}";
    }

    /// <summary>
    /// Get the description with current value filled in.
    /// </summary>
    public string GetDescription(int stacks = 1)
    {
        return string.Format(Description, GetFormattedValue(stacks));
    }
}

/// <summary>
/// Categories for upgrade synergy grouping.
/// </summary>
public enum UpgradeCategory
{
    General,        // No specific synergy
    Offense,        // Damage, crit, attack speed
    Defense,        // Health, armor, block
    Mobility,       // Speed, jumps, dash
    Utility,        // Luck, XP, gold
    OnHit,          // On-hit effects
    OnKill,         // On-kill effects
    Weapon,         // Weapon-specific
    Survival        // Regen, lifesteal
}

/// <summary>
/// Upgrade rarity affects appearance and base weight.
/// </summary>
public enum UpgradeRarity
{
    Common,         // White - 100 base weight
    Uncommon,       // Green - 60 base weight
    Rare,           // Blue - 30 base weight
    Epic,           // Purple - 15 base weight
    Legendary       // Orange - 5 base weight
}

/// <summary>
/// Types of stats upgrades can modify.
/// </summary>
public enum UpgradeStatType
{
    // === Flat Stats ===
    MaxHealth,
    MaxMana,
    HealthRegen,
    ManaRegen,
    Armor,
    FlatDamage,

    // === Percentage Stats ===
    DamagePercent,
    AttackSpeedPercent,
    MoveSpeedPercent,
    CritChance,
    CritDamage,
    DodgeChance,
    BlockChance,
    LifeSteal,

    // === Special ===
    ExtraJump,
    ExtraProjectile,
    AreaOfEffect,
    CooldownReduction,
    XPGain,
    GoldGain,
    PickupRadius,
    LuckBonus,

    // === On-Hit ===
    OnHitHeal,
    OnHitChainLightning,
    OnHitBurn,
    OnHitFreeze,
    OnHitExplode,

    // === On-Kill ===
    OnKillHeal,
    OnKillGold,
    OnKillExplode,

    // === Weapon-Specific ===
    MeleeDamage,
    RangedDamage,
    MagicDamage,
    WeaponLevel
}