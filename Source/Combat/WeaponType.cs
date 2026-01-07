namespace Terrascent.Combat;

/// <summary>
/// The 8 weapon categories inspired by Secret of Mana.
/// Each type has unique attack patterns and charge attacks.
/// </summary>
public enum WeaponType : byte
{
    /// <summary>Balanced melee weapon with wide horizontal swings.</summary>
    Sword = 0,

    /// <summary>Long reach with thrust attacks, good for keeping distance.</summary>
    Spear = 1,

    /// <summary>Slow but powerful, bonus damage to wooden objects/enemies.</summary>
    Axe = 2,

    /// <summary>Ranged weapon, charge increases arrow count/spread.</summary>
    Bow = 3,

    /// <summary>Medium range, can hit multiple times, wraps around obstacles.</summary>
    Whip = 4,

    /// <summary>Magic weapon, charge attacks are spells, uses mana.</summary>
    Staff = 5,

    /// <summary>Very fast, short range, combo-focused.</summary>
    Glove = 6,

    /// <summary>Thrown weapon that returns, hits on way out and back.</summary>
    Boomerang = 7,
}

/// <summary>
/// Attack direction/pattern for weapon swings.
/// </summary>
public enum AttackDirection : byte
{
    Horizontal,
    Vertical,
    Thrust,
    Spin,
    Projectile,
}