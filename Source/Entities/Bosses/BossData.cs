using Terrascent.Items;

namespace Terrascent.Entities.Bosses;

/// <summary>
/// Defines a boss attack pattern.
/// </summary>
public class BossAttack
{
    public string Name { get; init; } = "";
    public int Damage { get; init; }
    public float Cooldown { get; init; } = 2f;
    public float Range { get; init; } = 100f;
    public bool IsProjectile { get; init; }
    public bool IsAoE { get; init; }
    public float AoERadius { get; init; } = 50f;
    public float ProjectileSpeed { get; init; } = 300f;
    public int ProjectileCount { get; init; } = 1;
    public float SpreadAngle { get; init; } = 0f;  // For multi-projectile spread

    /// <summary>
    /// Minimum phase required to use this attack.
    /// </summary>
    public BossPhase MinPhase { get; init; } = BossPhase.Phase1;
}

/// <summary>
/// Defines phase transition thresholds and behaviors.
/// </summary>
public class BossPhaseData
{
    public BossPhase Phase { get; init; }

    /// <summary>
    /// Health percentage threshold to enter this phase (0-1).
    /// Phase1 = 1.0, Phase2 might be 0.5, Phase3 might be 0.25.
    /// </summary>
    public float HealthThreshold { get; init; } = 1f;

    /// <summary>
    /// Speed multiplier for this phase.
    /// </summary>
    public float SpeedMultiplier { get; init; } = 1f;

    /// <summary>
    /// Attack speed multiplier (lower cooldowns).
    /// </summary>
    public float AttackSpeedMultiplier { get; init; } = 1f;

    /// <summary>
    /// Damage multiplier for this phase.
    /// </summary>
    public float DamageMultiplier { get; init; } = 1f;

    /// <summary>
    /// Defense (damage reduction) for this phase.
    /// </summary>
    public int Defense { get; init; } = 0;

    /// <summary>
    /// Special behavior flags for this phase.
    /// </summary>
    public bool SpawnsMinions { get; init; }
    public bool HasShield { get; init; }
    public bool IsInvulnerable { get; init; }
}

/// <summary>
/// Defines a boss loot drop.
/// </summary>
public class BossLoot
{
    public ItemType Item { get; init; }
    public int MinCount { get; init; } = 1;
    public int MaxCount { get; init; } = 1;
    public float DropChance { get; init; } = 1f;  // 0-1, 1 = 100%

    /// <summary>
    /// If true, this item is always dropped (trophy/bag).
    /// </summary>
    public bool Guaranteed { get; init; }
}

/// <summary>
/// Static data defining a boss type's stats, phases, attacks, and loot.
/// </summary>
public class BossData
{
    // === IDENTITY ===
    public BossType Type { get; init; }
    public string Name { get; init; } = "";
    public string Title { get; init; } = "";  // e.g., "has awoken!"

    // === BASE STATS ===
    public int BaseHealth { get; init; } = 1000;
    public int BaseDamage { get; init; } = 30;
    public float BaseSpeed { get; init; } = 100f;
    public int BaseDefense { get; init; } = 0;
    public float KnockbackResistance { get; init; } = 1f;  // Bosses usually immune

    // === SIZE ===
    public int Width { get; init; } = 64;
    public int Height { get; init; } = 64;

    // === BEHAVIOR ===
    public BossMovement Movement { get; init; } = BossMovement.HoverDash;

    /// <summary>
    /// Time in seconds before boss enrages. Default 5 minutes.
    /// </summary>
    public float EnrageTime { get; init; } = 300f;

    /// <summary>
    /// Multiplier applied to stats when enraged.
    /// </summary>
    public float EnrageMultiplier { get; init; } = 1.5f;

    /// <summary>
    /// Maximum distance from player before boss starts despawning.
    /// </summary>
    public float DespawnDistance { get; init; } = 2000f;

    /// <summary>
    /// Required condition to fight (e.g., nighttime, underground).
    /// </summary>
    public BossSpawnCondition SpawnCondition { get; init; } = BossSpawnCondition.None;

    // === PHASES ===
    public BossPhaseData[] Phases { get; init; } = Array.Empty<BossPhaseData>();

    // === ATTACKS ===
    public BossAttack[] Attacks { get; init; } = Array.Empty<BossAttack>();

    // === MINIONS ===
    /// <summary>
    /// Enemy type spawned as minions (if any).
    /// </summary>
    public Enemies.EnemyType? MinionType { get; init; }
    public int MaxMinions { get; init; } = 5;
    public float MinionSpawnInterval { get; init; } = 5f;

    // === LOOT ===
    public BossLoot[] LootTable { get; init; } = Array.Empty<BossLoot>();
    public int BaseGoldReward { get; init; } = 500;
    public int BaseXPReward { get; init; } = 1000;

    // === VISUAL ===
    public (byte R, byte G, byte B) Color { get; init; } = (255, 0, 0);
    public (byte R, byte G, byte B) HealthBarColor { get; init; } = (200, 0, 0);

    // === FLAGS ===
    public bool IsHardmode { get; init; }
    public bool UnlocksHardmode { get; init; }  // Wall of Shadows
    public bool HasSegments { get; init; }  // For worm bosses

    /// <summary>
    /// Number of segments for worm-type bosses (e.g., The Destroyer has 82).
    /// </summary>
    public int SegmentCount { get; init; } = 0;

    /// <summary>
    /// Get the phase data for a given health percentage.
    /// </summary>
    public BossPhaseData GetPhaseForHealth(float healthPercent)
    {
        BossPhaseData currentPhase = Phases[0];

        foreach (var phase in Phases)
        {
            if (healthPercent <= phase.HealthThreshold)
            {
                currentPhase = phase;
            }
        }

        return currentPhase;
    }

    /// <summary>
    /// Get attacks available for a given phase.
    /// </summary>
    public IEnumerable<BossAttack> GetAttacksForPhase(BossPhase phase)
    {
        return Attacks.Where(a => phase >= a.MinPhase);
    }
}

/// <summary>
/// Conditions required to summon/fight a boss.
/// </summary>
public enum BossSpawnCondition
{
    None,
    Nighttime,
    Daytime,
    Underground,
    Surface,
    Jungle,
    Underworld,
    Dungeon,
    Corruption,
    Hallow,
}
