namespace Terrascent.Items.Effects;

/// <summary>
/// Defines an effect that an item provides when held/equipped.
/// Effects can stack and scale differently based on StackType.
/// </summary>
public class ItemEffect
{
    /// <summary>Unique identifier for this effect.</summary>
    public string Id { get; init; } = "";

    /// <summary>Display name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Description with {0} placeholder for current value.</summary>
    public string Description { get; init; } = "";

    /// <summary>Which stat or trigger this affects.</summary>
    public EffectType Type { get; init; }

    /// <summary>How the effect scales with stacks.</summary>
    public StackType StackType { get; init; } = StackType.Linear;

    /// <summary>Base value for the first stack.</summary>
    public float BaseValue { get; init; }

    /// <summary>Value added per additional stack (for Linear).</summary>
    public float StackValue { get; init; }

    /// <summary>Coefficient for Hyperbolic/Exponential scaling.</summary>
    public float Coefficient { get; init; } = 1f;

    /// <summary>Proc chance (0-1) for on-hit/on-kill effects.</summary>
    public float ProcChance { get; init; } = 1f;

    /// <summary>Cooldown in seconds between procs (0 = no cooldown).</summary>
    public float Cooldown { get; init; } = 0f;

    /// <summary>
    /// Calculate the effect value for a given number of stacks.
    /// </summary>
    public float Calculate(int stacks)
    {
        if (stacks <= 0) return 0f;

        return StackType switch
        {
            StackType.Linear => BaseValue + StackValue * (stacks - 1),

            StackType.Hyperbolic => 1f - 1f / (1f + Coefficient * stacks),

            StackType.Exponential => MathF.Pow(BaseValue, stacks),

            StackType.Flat => BaseValue,

            StackType.Diminishing => BaseValue * (1f - MathF.Pow(1f - Coefficient, stacks)),

            _ => BaseValue
        };
    }

    /// <summary>
    /// Get a formatted description with the current value.
    /// </summary>
    public string GetDescription(int stacks)
    {
        float value = Calculate(stacks);

        // Format as percentage if it's a rate/chance
        string formatted = Type switch
        {
            EffectType.CritChance or
            EffectType.DodgeChance or
            EffectType.LifeSteal or
            EffectType.AttackSpeedMult or
            EffectType.MoveSpeedMult => $"{value * 100:F0}%",

            _ => $"{value:F1}"
        };

        return string.Format(Description, formatted);
    }
}

/// <summary>
/// Types of effects items can have.
/// </summary>
public enum EffectType
{
    // === Flat Stat Bonuses ===
    MaxHealth,
    MaxMana,
    HealthRegen,
    ManaRegen,
    Armor,
    Damage,

    // === Multiplier Stats ===
    DamageMult,
    AttackSpeedMult,
    MoveSpeedMult,
    CritDamageMult,

    // === Chance Stats (use Hyperbolic) ===
    CritChance,
    DodgeChance,
    BlockChance,

    // === On-Hit Effects ===
    OnHitDamage,
    OnHitHeal,
    OnHitBleed,
    OnHitBurn,
    OnHitFreeze,
    OnHitChain,
    OnHitExplode,
    LifeSteal,

    // === On-Kill Effects ===
    OnKillHeal,
    OnKillGold,
    OnKillExplode,
    OnKillSpeedBoost,

    // === On-Hurt Effects ===
    OnHurtReflect,
    OnHurtShield,
    OnHurtThorns,

    // === Special ===
    ExtraJump,
    Flight,
    Magnet,
    LuckBonus,
}