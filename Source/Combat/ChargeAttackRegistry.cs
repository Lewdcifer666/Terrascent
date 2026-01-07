namespace Terrascent.Combat;

/// <summary>
/// Registry of all charge attacks for each weapon type.
/// Each weapon type has 9 attacks (0 = basic, 1-8 = charge levels).
/// </summary>
public static class ChargeAttackRegistry
{
    // Charge attacks indexed by [WeaponType][ChargeLevel]
    private static readonly Dictionary<WeaponType, ChargeAttack[]> _attacks = new();

    static ChargeAttackRegistry()
    {
        InitializeSwordAttacks();
        InitializeSpearAttacks();
        InitializeAxeAttacks();
        InitializeBowAttacks();
        InitializeWhipAttacks();
        InitializeStaffAttacks();
        InitializeGloveAttacks();
        InitializeBoomerangAttacks();
    }

    /// <summary>
    /// Get a charge attack for a weapon type and charge level.
    /// </summary>
    public static ChargeAttack Get(WeaponType type, int chargeLevel)
    {
        chargeLevel = Math.Clamp(chargeLevel, 0, 8);

        if (_attacks.TryGetValue(type, out var attacks))
        {
            return attacks[chargeLevel];
        }

        return ChargeAttack.Default;
    }

    #region Sword Attacks (Balanced, Horizontal Slashes)

    private static void InitializeSwordAttacks()
    {
        _attacks[WeaponType.Sword] = new ChargeAttack[]
        {
            // Level 0: Basic slash
            new ChargeAttack
            {
                Name = "Slash",
                DamageMultiplier = 1.0f,
                RangeMultiplier = 1.0f,
                HitCount = 1,
                Knockback = 100f,
                IsAoE = false,
                Duration = 0.3f,
                Effect = ChargeEffect.Slash,
            },
            // Level 1: Power Slash
            new ChargeAttack
            {
                Name = "Power Slash",
                DamageMultiplier = 1.5f,
                RangeMultiplier = 1.1f,
                HitCount = 1,
                Knockback = 150f,
                IsAoE = false,
                Duration = 0.4f,
                Effect = ChargeEffect.Slash,
            },
            // Level 2: Wide Slash
            new ChargeAttack
            {
                Name = "Wide Slash",
                DamageMultiplier = 1.3f,
                RangeMultiplier = 1.3f,
                HitCount = 1,
                Knockback = 120f,
                IsAoE = true,
                Duration = 0.45f,
                Effect = ChargeEffect.Slash,
            },
            // Level 3: Double Cut
            new ChargeAttack
            {
                Name = "Double Cut",
                DamageMultiplier = 1.2f,
                RangeMultiplier = 1.2f,
                HitCount = 2,
                Knockback = 100f,
                IsAoE = false,
                Duration = 0.5f,
                Effect = ChargeEffect.Slash,
            },
            // Level 4: Crescent Slash
            new ChargeAttack
            {
                Name = "Crescent Slash",
                DamageMultiplier = 1.8f,
                RangeMultiplier = 1.4f,
                HitCount = 1,
                Knockback = 180f,
                IsAoE = true,
                Duration = 0.5f,
                Effect = ChargeEffect.Wave,
            },
            // Level 5: Triple Slash
            new ChargeAttack
            {
                Name = "Triple Slash",
                DamageMultiplier = 1.5f,
                RangeMultiplier = 1.3f,
                HitCount = 3,
                Knockback = 120f,
                IsAoE = false,
                Duration = 0.6f,
                Effect = ChargeEffect.Slash,
            },
            // Level 6: Blade Storm
            new ChargeAttack
            {
                Name = "Blade Storm",
                DamageMultiplier = 2.0f,
                RangeMultiplier = 1.5f,
                HitCount = 1,
                Knockback = 200f,
                IsAoE = true,
                Duration = 0.6f,
                Effect = ChargeEffect.Spin,
            },
            // Level 7: Sword Dance
            new ChargeAttack
            {
                Name = "Sword Dance",
                DamageMultiplier = 1.8f,
                RangeMultiplier = 1.6f,
                HitCount = 5,
                Knockback = 80f,
                IsAoE = true,
                Duration = 0.8f,
                Effect = ChargeEffect.Spin,
            },
            // Level 8: Final Strike
            new ChargeAttack
            {
                Name = "Final Strike",
                DamageMultiplier = 3.5f,
                RangeMultiplier = 2.0f,
                HitCount = 1,
                Knockback = 300f,
                IsAoE = true,
                Duration = 1.0f,
                Effect = ChargeEffect.Explosion,
            },
        };
    }

    #endregion

    #region Spear Attacks (Long Range, Thrust-based)

    private static void InitializeSpearAttacks()
    {
        _attacks[WeaponType.Spear] = new ChargeAttack[]
        {
            new ChargeAttack { Name = "Thrust", DamageMultiplier = 1.0f, RangeMultiplier = 1.3f, HitCount = 1, Knockback = 120f, Duration = 0.35f, Effect = ChargeEffect.Thrust },
            new ChargeAttack { Name = "Power Thrust", DamageMultiplier = 1.6f, RangeMultiplier = 1.4f, HitCount = 1, Knockback = 160f, Duration = 0.4f, Effect = ChargeEffect.Thrust },
            new ChargeAttack { Name = "Piercing Strike", DamageMultiplier = 1.8f, RangeMultiplier = 1.6f, HitCount = 1, Knockback = 140f, Duration = 0.45f, Effect = ChargeEffect.Thrust },
            new ChargeAttack { Name = "Double Thrust", DamageMultiplier = 1.4f, RangeMultiplier = 1.5f, HitCount = 2, Knockback = 100f, Duration = 0.5f, Effect = ChargeEffect.Thrust },
            new ChargeAttack { Name = "Dragon Fang", DamageMultiplier = 2.0f, RangeMultiplier = 1.8f, HitCount = 1, Knockback = 200f, Duration = 0.55f, Effect = ChargeEffect.Wave },
            new ChargeAttack { Name = "Rapid Thrust", DamageMultiplier = 1.3f, RangeMultiplier = 1.5f, HitCount = 4, Knockback = 60f, Duration = 0.6f, Effect = ChargeEffect.Thrust },
            new ChargeAttack { Name = "Cyclone", DamageMultiplier = 2.2f, RangeMultiplier = 1.4f, HitCount = 1, Knockback = 180f, IsAoE = true, Duration = 0.7f, Effect = ChargeEffect.Spin },
            new ChargeAttack { Name = "Storm Thrust", DamageMultiplier = 2.5f, RangeMultiplier = 2.0f, HitCount = 3, Knockback = 150f, Duration = 0.8f, Effect = ChargeEffect.Lightning },
            new ChargeAttack { Name = "Gungnir", DamageMultiplier = 4.0f, RangeMultiplier = 2.5f, HitCount = 1, Knockback = 350f, IsAoE = true, Duration = 1.0f, Effect = ChargeEffect.Explosion },
        };
    }

    #endregion

    #region Axe Attacks (Slow, High Damage, Slam-based)

    private static void InitializeAxeAttacks()
    {
        _attacks[WeaponType.Axe] = new ChargeAttack[]
        {
            new ChargeAttack { Name = "Chop", DamageMultiplier = 1.2f, RangeMultiplier = 0.9f, HitCount = 1, Knockback = 150f, Duration = 0.4f, Effect = ChargeEffect.Slam },
            new ChargeAttack { Name = "Heavy Chop", DamageMultiplier = 1.8f, RangeMultiplier = 1.0f, HitCount = 1, Knockback = 200f, Duration = 0.5f, Effect = ChargeEffect.Slam },
            new ChargeAttack { Name = "Cleave", DamageMultiplier = 1.6f, RangeMultiplier = 1.2f, HitCount = 1, Knockback = 180f, IsAoE = true, Duration = 0.5f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Skull Splitter", DamageMultiplier = 2.2f, RangeMultiplier = 1.0f, HitCount = 1, Knockback = 250f, Duration = 0.55f, Effect = ChargeEffect.Slam },
            new ChargeAttack { Name = "Whirlwind", DamageMultiplier = 1.8f, RangeMultiplier = 1.4f, HitCount = 2, Knockback = 150f, IsAoE = true, Duration = 0.6f, Effect = ChargeEffect.Spin },
            new ChargeAttack { Name = "Ground Slam", DamageMultiplier = 2.5f, RangeMultiplier = 1.6f, HitCount = 1, Knockback = 300f, IsAoE = true, Duration = 0.7f, Effect = ChargeEffect.Slam },
            new ChargeAttack { Name = "Raging Axe", DamageMultiplier = 2.0f, RangeMultiplier = 1.3f, HitCount = 4, Knockback = 120f, IsAoE = true, Duration = 0.8f, Effect = ChargeEffect.Spin },
            new ChargeAttack { Name = "Earthquake", DamageMultiplier = 3.0f, RangeMultiplier = 2.0f, HitCount = 1, Knockback = 350f, IsAoE = true, Duration = 0.9f, Effect = ChargeEffect.Explosion },
            new ChargeAttack { Name = "World Ender", DamageMultiplier = 5.0f, RangeMultiplier = 2.5f, HitCount = 1, Knockback = 500f, IsAoE = true, Duration = 1.2f, Effect = ChargeEffect.Explosion },
        };
    }

    #endregion

    #region Bow Attacks (Ranged, Projectile-based)

    private static void InitializeBowAttacks()
    {
        _attacks[WeaponType.Bow] = new ChargeAttack[]
        {
            new ChargeAttack { Name = "Quick Shot", DamageMultiplier = 0.8f, RangeMultiplier = 3.0f, HitCount = 1, Knockback = 50f, Duration = 0.25f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Aimed Shot", DamageMultiplier = 1.4f, RangeMultiplier = 4.0f, HitCount = 1, Knockback = 80f, Duration = 0.35f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Power Shot", DamageMultiplier = 1.8f, RangeMultiplier = 4.5f, HitCount = 1, Knockback = 120f, Duration = 0.4f, Effect = ChargeEffect.Wave },
            new ChargeAttack { Name = "Double Shot", DamageMultiplier = 1.2f, RangeMultiplier = 4.0f, HitCount = 2, Knockback = 60f, Duration = 0.45f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Piercing Arrow", DamageMultiplier = 2.0f, RangeMultiplier = 5.0f, HitCount = 1, Knockback = 100f, Duration = 0.5f, Effect = ChargeEffect.Wave },
            new ChargeAttack { Name = "Arrow Rain", DamageMultiplier = 1.0f, RangeMultiplier = 3.5f, HitCount = 5, Knockback = 40f, IsAoE = true, Duration = 0.7f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Flame Arrow", DamageMultiplier = 2.2f, RangeMultiplier = 4.5f, HitCount = 1, Knockback = 150f, Duration = 0.6f, Effect = ChargeEffect.Fire },
            new ChargeAttack { Name = "Storm Arrow", DamageMultiplier = 1.5f, RangeMultiplier = 4.0f, HitCount = 8, Knockback = 60f, IsAoE = true, Duration = 0.8f, Effect = ChargeEffect.Lightning },
            new ChargeAttack { Name = "Divine Arrow", DamageMultiplier = 4.5f, RangeMultiplier = 6.0f, HitCount = 1, Knockback = 400f, IsAoE = true, Duration = 1.0f, Effect = ChargeEffect.Explosion },
        };
    }

    #endregion

    #region Whip Attacks (Medium Range, Multi-hit)

    private static void InitializeWhipAttacks()
    {
        _attacks[WeaponType.Whip] = new ChargeAttack[]
        {
            new ChargeAttack { Name = "Lash", DamageMultiplier = 0.9f, RangeMultiplier = 1.5f, HitCount = 1, Knockback = 60f, Duration = 0.3f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Power Lash", DamageMultiplier = 1.3f, RangeMultiplier = 1.6f, HitCount = 1, Knockback = 100f, Duration = 0.35f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Double Lash", DamageMultiplier = 1.1f, RangeMultiplier = 1.7f, HitCount = 2, Knockback = 80f, Duration = 0.4f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Whip Crack", DamageMultiplier = 1.6f, RangeMultiplier = 1.8f, HitCount = 1, Knockback = 150f, Duration = 0.45f, Effect = ChargeEffect.Wave },
            new ChargeAttack { Name = "Serpent Strike", DamageMultiplier = 1.4f, RangeMultiplier = 2.0f, HitCount = 3, Knockback = 80f, Duration = 0.5f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Viper Dance", DamageMultiplier = 1.2f, RangeMultiplier = 1.8f, HitCount = 5, Knockback = 60f, IsAoE = true, Duration = 0.6f, Effect = ChargeEffect.Spin },
            new ChargeAttack { Name = "Thunder Whip", DamageMultiplier = 2.0f, RangeMultiplier = 2.2f, HitCount = 3, Knockback = 120f, Duration = 0.7f, Effect = ChargeEffect.Lightning },
            new ChargeAttack { Name = "Hydra Fury", DamageMultiplier = 1.5f, RangeMultiplier = 2.5f, HitCount = 8, Knockback = 50f, IsAoE = true, Duration = 0.8f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Dragon's Tail", DamageMultiplier = 3.5f, RangeMultiplier = 3.0f, HitCount = 1, Knockback = 350f, IsAoE = true, Duration = 1.0f, Effect = ChargeEffect.Fire },
        };
    }

    #endregion

    #region Staff Attacks (Magic, Mana-based)

    private static void InitializeStaffAttacks()
    {
        _attacks[WeaponType.Staff] = new ChargeAttack[]
        {
            new ChargeAttack { Name = "Magic Bolt", DamageMultiplier = 0.9f, RangeMultiplier = 2.5f, HitCount = 1, Knockback = 40f, Duration = 0.3f, ManaCost = 0, Effect = ChargeEffect.Wave },
            new ChargeAttack { Name = "Arcane Bolt", DamageMultiplier = 1.4f, RangeMultiplier = 3.0f, HitCount = 1, Knockback = 80f, Duration = 0.4f, ManaCost = 5, Effect = ChargeEffect.Wave },
            new ChargeAttack { Name = "Fireball", DamageMultiplier = 1.8f, RangeMultiplier = 3.5f, HitCount = 1, Knockback = 100f, IsAoE = true, Duration = 0.5f, ManaCost = 10, Effect = ChargeEffect.Fire },
            new ChargeAttack { Name = "Ice Spear", DamageMultiplier = 1.6f, RangeMultiplier = 4.0f, HitCount = 1, Knockback = 120f, Duration = 0.5f, ManaCost = 12, Effect = ChargeEffect.Ice },
            new ChargeAttack { Name = "Lightning", DamageMultiplier = 2.0f, RangeMultiplier = 5.0f, HitCount = 1, Knockback = 80f, Duration = 0.4f, ManaCost = 15, Effect = ChargeEffect.Lightning },
            new ChargeAttack { Name = "Heal", DamageMultiplier = 0f, RangeMultiplier = 0f, HitCount = 0, Knockback = 0f, Duration = 0.6f, ManaCost = 20, Effect = ChargeEffect.Heal },
            new ChargeAttack { Name = "Meteor", DamageMultiplier = 2.5f, RangeMultiplier = 3.0f, HitCount = 1, Knockback = 200f, IsAoE = true, Duration = 0.8f, ManaCost = 30, Effect = ChargeEffect.Fire },
            new ChargeAttack { Name = "Chain Lightning", DamageMultiplier = 1.8f, RangeMultiplier = 4.0f, HitCount = 5, Knockback = 60f, Duration = 0.7f, ManaCost = 35, Effect = ChargeEffect.Lightning },
            new ChargeAttack { Name = "Apocalypse", DamageMultiplier = 4.0f, RangeMultiplier = 4.0f, HitCount = 1, Knockback = 400f, IsAoE = true, Duration = 1.2f, ManaCost = 50, Effect = ChargeEffect.Explosion },
        };
    }

    #endregion

    #region Glove Attacks (Fast, Combo-based)

    private static void InitializeGloveAttacks()
    {
        _attacks[WeaponType.Glove] = new ChargeAttack[]
        {
            new ChargeAttack { Name = "Jab", DamageMultiplier = 0.7f, RangeMultiplier = 0.6f, HitCount = 1, Knockback = 30f, Duration = 0.15f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Power Jab", DamageMultiplier = 1.2f, RangeMultiplier = 0.7f, HitCount = 1, Knockback = 60f, Duration = 0.2f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "One-Two", DamageMultiplier = 0.9f, RangeMultiplier = 0.7f, HitCount = 2, Knockback = 50f, Duration = 0.25f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Uppercut", DamageMultiplier = 1.8f, RangeMultiplier = 0.6f, HitCount = 1, Knockback = 180f, Duration = 0.3f, Effect = ChargeEffect.Slam },
            new ChargeAttack { Name = "Combo Rush", DamageMultiplier = 0.8f, RangeMultiplier = 0.7f, HitCount = 4, Knockback = 30f, Duration = 0.35f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Palm Strike", DamageMultiplier = 2.0f, RangeMultiplier = 0.8f, HitCount = 1, Knockback = 200f, Duration = 0.35f, Effect = ChargeEffect.Wave },
            new ChargeAttack { Name = "Fist Fury", DamageMultiplier = 0.7f, RangeMultiplier = 0.8f, HitCount = 8, Knockback = 20f, Duration = 0.5f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Tiger Claw", DamageMultiplier = 2.5f, RangeMultiplier = 1.0f, HitCount = 3, Knockback = 150f, Duration = 0.6f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Dragon Fist", DamageMultiplier = 4.0f, RangeMultiplier = 1.2f, HitCount = 1, Knockback = 500f, IsAoE = true, Duration = 0.8f, Effect = ChargeEffect.Explosion },
        };
    }

    #endregion

    #region Boomerang Attacks (Thrown, Returns)

    private static void InitializeBoomerangAttacks()
    {
        _attacks[WeaponType.Boomerang] = new ChargeAttack[]
        {
            new ChargeAttack { Name = "Throw", DamageMultiplier = 0.8f, RangeMultiplier = 2.0f, HitCount = 2, Knockback = 50f, Duration = 0.5f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Power Throw", DamageMultiplier = 1.3f, RangeMultiplier = 2.5f, HitCount = 2, Knockback = 80f, Duration = 0.6f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Curved Throw", DamageMultiplier = 1.2f, RangeMultiplier = 3.0f, HitCount = 3, Knockback = 60f, Duration = 0.65f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Razor Wind", DamageMultiplier = 1.5f, RangeMultiplier = 3.5f, HitCount = 2, Knockback = 100f, Duration = 0.7f, Effect = ChargeEffect.Slash },
            new ChargeAttack { Name = "Multi Throw", DamageMultiplier = 1.0f, RangeMultiplier = 3.0f, HitCount = 4, Knockback = 50f, Duration = 0.75f, Effect = ChargeEffect.None },
            new ChargeAttack { Name = "Spiral Edge", DamageMultiplier = 1.8f, RangeMultiplier = 3.5f, HitCount = 4, Knockback = 80f, IsAoE = true, Duration = 0.8f, Effect = ChargeEffect.Spin },
            new ChargeAttack { Name = "Storm Disc", DamageMultiplier = 2.0f, RangeMultiplier = 4.0f, HitCount = 6, Knockback = 60f, IsAoE = true, Duration = 0.9f, Effect = ChargeEffect.Lightning },
            new ChargeAttack { Name = "Infinite Loop", DamageMultiplier = 1.5f, RangeMultiplier = 3.5f, HitCount = 10, Knockback = 40f, IsAoE = true, Duration = 1.0f, Effect = ChargeEffect.Spin },
            new ChargeAttack { Name = "Dimension Rift", DamageMultiplier = 3.5f, RangeMultiplier = 5.0f, HitCount = 2, Knockback = 300f, IsAoE = true, Duration = 1.2f, Effect = ChargeEffect.Explosion },
        };
    }

    #endregion
}