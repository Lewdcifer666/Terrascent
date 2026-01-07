namespace Terrascent.Combat;

/// <summary>
/// Defines a charge attack for a specific weapon type and charge level.
/// Each weapon has 8 charge attacks (levels 1-8, level 0 is basic attack).
/// </summary>
public readonly struct ChargeAttack
{
    /// <summary>Display name of the charge attack.</summary>
    public string Name { get; init; }

    /// <summary>Damage multiplier applied to base weapon damage.</summary>
    public float DamageMultiplier { get; init; }

    /// <summary>Range multiplier (1.0 = normal range).</summary>
    public float RangeMultiplier { get; init; }

    /// <summary>How many hits this attack can do (for multi-hit attacks).</summary>
    public int HitCount { get; init; }

    /// <summary>Knockback force applied to enemies.</summary>
    public float Knockback { get; init; }

    /// <summary>Does this attack hit all enemies in range (AoE)?</summary>
    public bool IsAoE { get; init; }

    /// <summary>Duration of the attack animation in seconds.</summary>
    public float Duration { get; init; }

    /// <summary>Mana cost (mainly for Staff weapons).</summary>
    public int ManaCost { get; init; }

    /// <summary>Special effect type (for visual/audio feedback).</summary>
    public ChargeEffect Effect { get; init; }

    public static ChargeAttack Default => new()
    {
        Name = "Attack",
        DamageMultiplier = 1f,
        RangeMultiplier = 1f,
        HitCount = 1,
        Knockback = 100f,
        IsAoE = false,
        Duration = 0.3f,
        ManaCost = 0,
        Effect = ChargeEffect.None,
    };
}

/// <summary>
/// Visual/audio effects for charge attacks.
/// </summary>
public enum ChargeEffect : byte
{
    None,
    Slash,
    Thrust,
    Slam,
    Spin,
    Wave,
    Explosion,
    Lightning,
    Fire,
    Ice,
    Heal,
}