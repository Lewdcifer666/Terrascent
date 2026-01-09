namespace Terrascent.Progression;

/// <summary>
/// Tracks and applies stat bonuses from level-up upgrades.
/// Integrates with the existing PlayerStats system.
/// </summary>
public class UpgradeStats
{
    private readonly LevelUpManager _levelUpManager;

    // === Cached Stat Values ===

    // Flat Stats
    public float MaxHealth { get; private set; }
    public float HealthRegen { get; private set; }
    public float Armor { get; private set; }
    public float FlatDamage { get; private set; }

    // Percentage Stats
    public float DamagePercent { get; private set; }
    public float AttackSpeedPercent { get; private set; }
    public float MoveSpeedPercent { get; private set; }
    public float CritChance { get; private set; }
    public float CritDamage { get; private set; }
    public float DodgeChance { get; private set; }
    public float BlockChance { get; private set; }
    public float LifeSteal { get; private set; }

    // Special Stats
    public int ExtraJumps { get; private set; }
    public float AreaOfEffect { get; private set; }
    public float CooldownReduction { get; private set; }
    public float XPGain { get; private set; }
    public float GoldGain { get; private set; }
    public float PickupRadius { get; private set; }
    public int LuckBonus { get; private set; }

    // On-Hit Stats
    public float OnHitHeal { get; private set; }
    public float OnHitChainChance { get; private set; }
    public float OnHitBurnChance { get; private set; }
    public float OnHitFreezeChance { get; private set; }

    // On-Kill Stats
    public float OnKillHeal { get; private set; }
    public float OnKillGold { get; private set; }
    public float OnKillExplodeChance { get; private set; }

    // Weapon-Specific
    public float MeleeDamage { get; private set; }
    public float RangedDamage { get; private set; }
    public float MagicDamage { get; private set; }

    // Glass Cannon special handling
    public bool HasGlassCannon { get; private set; }

    public UpgradeStats(LevelUpManager levelUpManager)
    {
        _levelUpManager = levelUpManager;
    }

    /// <summary>
    /// Recalculate all upgrade bonuses.
    /// Call this when an upgrade is selected.
    /// </summary>
    public void Recalculate()
    {
        // Reset all values
        MaxHealth = 0f;
        HealthRegen = 0f;
        Armor = 0f;
        FlatDamage = 0f;
        DamagePercent = 0f;
        AttackSpeedPercent = 0f;
        MoveSpeedPercent = 0f;
        CritChance = 0f;
        CritDamage = 0f;
        DodgeChance = 0f;
        BlockChance = 0f;
        LifeSteal = 0f;
        ExtraJumps = 0;
        AreaOfEffect = 0f;
        CooldownReduction = 0f;
        XPGain = 0f;
        GoldGain = 0f;
        PickupRadius = 0f;
        LuckBonus = 0;
        OnHitHeal = 0f;
        OnHitChainChance = 0f;
        OnHitBurnChance = 0f;
        OnHitFreezeChance = 0f;
        OnKillHeal = 0f;
        OnKillGold = 0f;
        OnKillExplodeChance = 0f;
        MeleeDamage = 0f;
        RangedDamage = 0f;
        MagicDamage = 0f;
        HasGlassCannon = false;

        // Accumulate from all selected upgrades
        foreach (var (upgrade, stacks) in _levelUpManager.GetSelectedUpgrades())
        {
            ApplyUpgrade(upgrade, stacks);
        }
    }

    /// <summary>
    /// Apply a single upgrade's bonuses.
    /// </summary>
    private void ApplyUpgrade(Upgrade upgrade, int stacks)
    {
        float totalValue = upgrade.Value * stacks;

        switch (upgrade.StatType)
        {
            // Flat Stats
            case UpgradeStatType.MaxHealth:
                MaxHealth += totalValue;
                break;
            case UpgradeStatType.HealthRegen:
                HealthRegen += totalValue;
                break;
            case UpgradeStatType.Armor:
                Armor += totalValue;
                break;
            case UpgradeStatType.FlatDamage:
                FlatDamage += totalValue;
                break;

            // Percentage Stats
            case UpgradeStatType.DamagePercent:
                DamagePercent += totalValue;
                // Special handling for Glass Cannon
                if (upgrade.Id == "glass_cannon")
                    HasGlassCannon = true;
                break;
            case UpgradeStatType.AttackSpeedPercent:
                AttackSpeedPercent += totalValue;
                break;
            case UpgradeStatType.MoveSpeedPercent:
                MoveSpeedPercent += totalValue;
                break;
            case UpgradeStatType.CritChance:
                CritChance += totalValue;
                break;
            case UpgradeStatType.CritDamage:
                CritDamage += totalValue;
                break;
            case UpgradeStatType.DodgeChance:
                DodgeChance += totalValue;
                break;
            case UpgradeStatType.BlockChance:
                BlockChance += totalValue;
                break;
            case UpgradeStatType.LifeSteal:
                LifeSteal += totalValue;
                break;

            // Special Stats
            case UpgradeStatType.ExtraJump:
                ExtraJumps += (int)totalValue;
                break;
            case UpgradeStatType.AreaOfEffect:
                AreaOfEffect += totalValue;
                break;
            case UpgradeStatType.CooldownReduction:
                CooldownReduction += totalValue;
                break;
            case UpgradeStatType.XPGain:
                XPGain += totalValue;
                break;
            case UpgradeStatType.GoldGain:
                GoldGain += totalValue;
                break;
            case UpgradeStatType.PickupRadius:
                PickupRadius += totalValue;
                break;
            case UpgradeStatType.LuckBonus:
                LuckBonus += (int)totalValue;
                break;

            // On-Hit Stats
            case UpgradeStatType.OnHitHeal:
                OnHitHeal += totalValue;
                break;
            case UpgradeStatType.OnHitChainLightning:
                OnHitChainChance += totalValue;
                break;
            case UpgradeStatType.OnHitBurn:
                OnHitBurnChance += totalValue;
                break;
            case UpgradeStatType.OnHitFreeze:
                OnHitFreezeChance += totalValue;
                break;

            // On-Kill Stats
            case UpgradeStatType.OnKillHeal:
                OnKillHeal += totalValue;
                break;
            case UpgradeStatType.OnKillGold:
                OnKillGold += totalValue;
                break;
            case UpgradeStatType.OnKillExplode:
                OnKillExplodeChance += totalValue;
                break;

            // Weapon-Specific
            case UpgradeStatType.MeleeDamage:
                MeleeDamage += totalValue;
                break;
            case UpgradeStatType.RangedDamage:
                RangedDamage += totalValue;
                break;
            case UpgradeStatType.MagicDamage:
                MagicDamage += totalValue;
                break;
        }
    }

    /// <summary>
    /// Get a formatted summary of active upgrades.
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();

        if (DamagePercent != 0) parts.Add($"DMG: {DamagePercent * 100:+0;-0}%");
        if (AttackSpeedPercent != 0) parts.Add($"ASPD: {AttackSpeedPercent * 100:+0;-0}%");
        if (CritChance != 0) parts.Add($"CRIT: {CritChance * 100:+0;-0}%");
        if (MaxHealth != 0) parts.Add($"HP: {MaxHealth:+0;-0}");
        if (MoveSpeedPercent != 0) parts.Add($"SPD: {MoveSpeedPercent * 100:+0;-0}%");

        return parts.Count > 0 ? string.Join(" | ", parts) : "No upgrades";
    }
}