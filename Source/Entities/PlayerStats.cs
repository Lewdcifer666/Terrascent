using Terrascent.Items;
using Terrascent.Items.Effects;

namespace Terrascent.Entities;

/// <summary>
/// Tracks all player stats including base values and item modifiers.
/// Recalculates when items change.
/// </summary>
public class PlayerStats
{
    // === Base Stats (before modifiers) ===
    public float BaseMaxHealth { get; set; } = 100f;
    public float BaseMaxMana { get; set; } = 50f;
    public float BaseHealthRegen { get; set; } = 1f;  // Per second
    public float BaseManaRegen { get; set; } = 2f;
    public float BaseArmor { get; set; } = 0f;
    public float BaseDamage { get; set; } = 10f;
    public float BaseMoveSpeed { get; set; } = 180f;
    public float BaseAttackSpeed { get; set; } = 1f;
    public float BaseCritChance { get; set; } = 0.01f;  // 1%
    public float BaseCritDamage { get; set; } = 2f;     // 2x damage
    public int BaseJumps { get; set; } = 1;

    // === Current (Calculated) Stats ===
    public float MaxHealth { get; private set; }
    public float MaxMana { get; private set; }
    public float HealthRegen { get; private set; }
    public float ManaRegen { get; private set; }
    public float Armor { get; private set; }
    public float Damage { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AttackSpeed { get; private set; }
    public float CritChance { get; private set; }
    public float CritDamage { get; private set; }
    public int MaxJumps { get; private set; }

    // === Chance Stats ===
    public float BlockChance { get; private set; }
    public float DodgeChance { get; private set; }
    public float LifeSteal { get; private set; }

    // === Luck (affects proc rolls) ===
    public int LuckBonus { get; private set; }

    // === On-Hit Effect Values ===
    public float OnHitDamageBonus { get; private set; }
    public float OnHitHealAmount { get; private set; }
    public float OnHitChainChance { get; private set; }
    public float OnHitExplodeChance { get; private set; }
    public float OnHitExplodeDamage { get; private set; }

    // === On-Kill Effect Values ===
    public float OnKillHealAmount { get; private set; }
    public float OnKillGoldBonus { get; private set; }

    // Track item stacks for effect calculation
    private readonly Dictionary<ItemType, int> _itemStacks = new();

    /// <summary>
    /// Recalculate all stats based on current item stacks.
    /// Call this when inventory changes.
    /// </summary>
    public void Recalculate(Inventory inventory)
    {
        // Count item stacks
        _itemStacks.Clear();
        foreach (var (_, stack) in inventory.GetNonEmptySlots())
        {
            if (StackableItemRegistry.IsStackable(stack.Type))
            {
                if (!_itemStacks.ContainsKey(stack.Type))
                    _itemStacks[stack.Type] = 0;
                _itemStacks[stack.Type] += stack.Count;
            }
        }

        // Reset to base values
        MaxHealth = BaseMaxHealth;
        MaxMana = BaseMaxMana;
        HealthRegen = BaseHealthRegen;
        ManaRegen = BaseManaRegen;
        Armor = BaseArmor;
        Damage = BaseDamage;
        MoveSpeed = BaseMoveSpeed;
        AttackSpeed = BaseAttackSpeed;
        CritChance = BaseCritChance;
        CritDamage = BaseCritDamage;
        MaxJumps = BaseJumps;

        BlockChance = 0f;
        DodgeChance = 0f;
        LifeSteal = 0f;
        LuckBonus = 0;

        OnHitDamageBonus = 0f;
        OnHitHealAmount = 0f;
        OnHitChainChance = 0f;
        OnHitExplodeChance = 0f;
        OnHitExplodeDamage = 0f;
        OnKillHealAmount = 0f;
        OnKillGoldBonus = 0f;

        // Multipliers (apply after flat bonuses)
        float healthMult = 1f;
        float damageMult = 1f;
        float moveSpeedMult = 1f;
        float attackSpeedMult = 1f;

        // Apply all item effects
        foreach (var (itemType, stacks) in _itemStacks)
        {
            var item = StackableItemRegistry.Get(itemType);
            if (item == null) continue;

            foreach (var effect in item.Effects)
            {
                float value = effect.Calculate(stacks);

                switch (effect.Type)
                {
                    // Flat stats
                    case EffectType.MaxHealth:
                        if (effect.StackType == StackType.Exponential)
                            healthMult *= value;
                        else
                            MaxHealth += value;
                        break;
                    case EffectType.MaxMana:
                        MaxMana += value;
                        break;
                    case EffectType.HealthRegen:
                        HealthRegen += value;
                        break;
                    case EffectType.ManaRegen:
                        ManaRegen += value;
                        break;
                    case EffectType.Armor:
                        Armor += value;
                        break;
                    case EffectType.Damage:
                        Damage += value;
                        break;

                    // Multipliers
                    case EffectType.DamageMult:
                        damageMult *= value;
                        break;
                    case EffectType.AttackSpeedMult:
                        attackSpeedMult += value;
                        break;
                    case EffectType.MoveSpeedMult:
                        moveSpeedMult += value;
                        break;
                    case EffectType.CritDamageMult:
                        CritDamage += value;
                        break;

                    // Chance stats
                    case EffectType.CritChance:
                        CritChance += value;
                        break;
                    case EffectType.DodgeChance:
                        DodgeChance = 1f - (1f - DodgeChance) * (1f - value);
                        break;
                    case EffectType.BlockChance:
                        BlockChance = 1f - (1f - BlockChance) * (1f - value);
                        break;
                    case EffectType.LifeSteal:
                        LifeSteal += value;
                        break;

                    // On-hit effects
                    case EffectType.OnHitDamage:
                        OnHitDamageBonus += value;
                        break;
                    case EffectType.OnHitHeal:
                        OnHitHealAmount += value;
                        break;
                    case EffectType.OnHitChain:
                        OnHitChainChance = 1f - (1f - OnHitChainChance) * (1f - value);
                        break;
                    case EffectType.OnHitExplode:
                        OnHitExplodeChance = Math.Max(OnHitExplodeChance, effect.ProcChance);
                        OnHitExplodeDamage += value;
                        break;

                    // On-kill effects
                    case EffectType.OnKillHeal:
                        OnKillHealAmount += value;
                        break;
                    case EffectType.OnKillGold:
                        OnKillGoldBonus += value;
                        break;

                    // Special
                    case EffectType.ExtraJump:
                        MaxJumps += (int)value;
                        break;
                    case EffectType.LuckBonus:
                        LuckBonus += (int)value;
                        break;
                }
            }
        }

        // Apply multipliers
        MaxHealth *= healthMult;
        Damage *= damageMult;
        MoveSpeed *= moveSpeedMult;
        AttackSpeed *= attackSpeedMult;

        // Clamp values
        CritChance = Math.Clamp(CritChance, 0f, 1f);
        BlockChance = Math.Clamp(BlockChance, 0f, 0.99f);
        DodgeChance = Math.Clamp(DodgeChance, 0f, 0.99f);
        AttackSpeed = Math.Max(AttackSpeed, 0.1f);
    }

    /// <summary>
    /// Get the number of stacks of a specific item.
    /// </summary>
    public int GetStacks(ItemType type)
    {
        return _itemStacks.GetValueOrDefault(type, 0);
    }

    /// <summary>
    /// Roll for a proc with luck bonus.
    /// Luck gives additional rolls, taking the best result.
    /// </summary>
    public bool RollProc(float chance, Random? rng = null)
    {
        rng ??= Random.Shared;

        // With luck, roll multiple times and succeed if any roll succeeds
        for (int i = 0; i <= LuckBonus; i++)
        {
            if (rng.NextSingle() < chance)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate final damage with all modifiers.
    /// </summary>
    public int CalculateDamage(int baseDamage, bool isCrit, out bool wasBlocked)
    {
        wasBlocked = false;

        float damage = baseDamage * (Damage / BaseDamage);

        if (isCrit)
        {
            damage *= CritDamage;
        }

        return (int)Math.Ceiling(damage);
    }

    /// <summary>
    /// Get a formatted stat summary for UI.
    /// </summary>
    public string GetStatSummary()
    {
        return $"HP: {MaxHealth:F0} | DMG: {Damage:F0} | SPD: {MoveSpeed:F0}\n" +
               $"Crit: {CritChance * 100:F0}% | Block: {BlockChance * 100:F0}%";
    }
}