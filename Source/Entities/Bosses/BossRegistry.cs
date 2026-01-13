using Terrascent.Entities.Enemies;
using Terrascent.Items;

namespace Terrascent.Entities.Bosses;

/// <summary>
/// Registry of all boss definitions.
/// Contains static data for each boss type.
/// </summary>
public static class BossRegistry
{
    private static readonly Dictionary<BossType, BossData> _bosses = new();
    private static bool _initialized = false;

    /// <summary>
    /// Initialize the boss registry. Called automatically on first access.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        RegisterPreHardmodeBosses();
        RegisterHardmodeBosses();
    }

    /// <summary>
    /// Get boss data by type.
    /// </summary>
    public static BossData Get(BossType type)
    {
        Initialize();

        if (_bosses.TryGetValue(type, out var data))
            return data;

        throw new ArgumentException($"Unknown boss type: {type}");
    }

    /// <summary>
    /// Get all bosses.
    /// </summary>
    public static IEnumerable<BossData> GetAll()
    {
        Initialize();
        return _bosses.Values;
    }

    /// <summary>
    /// Get all Pre-Hardmode bosses.
    /// </summary>
    public static IEnumerable<BossData> GetPreHardmodeBosses()
    {
        Initialize();
        return _bosses.Values.Where(b => !b.IsHardmode);
    }

    private static void Register(BossData data)
    {
        _bosses[data.Type] = data;
    }

    #region Pre-Hardmode Bosses

    private static void RegisterPreHardmodeBosses()
    {
        // === EYE OF TERROR ===
        // First boss - large floating eye that dashes at player
        Register(new BossData
        {
            Type = BossType.EyeOfTerror,
            Name = "Eye of Terror",
            Title = "has awoken!",
            BaseHealth = 2800,
            BaseDamage = 18,
            BaseSpeed = 120f,
            BaseDefense = 0,
            KnockbackResistance = 1f,
            Width = 80,
            Height = 80,
            Movement = BossMovement.HoverDash,
            EnrageTime = 300f,
            EnrageMultiplier = 1.5f,
            DespawnDistance = 2500f,
            SpawnCondition = BossSpawnCondition.Nighttime,
            Color = (200, 50, 50),
            HealthBarColor = (220, 60, 60),
            BaseGoldReward = 300,
            BaseXPReward = 500,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 0
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.3f,
                    AttackSpeedMultiplier = 1.5f,
                    DamageMultiplier = 1.2f,
                    Defense = 2,
                    SpawnsMinions = true
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Dash",
                    Damage = 18,
                    Cooldown = 2f,
                    Range = 400f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Rapid Dash",
                    Damage = 22,
                    Cooldown = 1.2f,
                    Range = 500f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Spawn Servants",
                    Damage = 0,
                    Cooldown = 5f,
                    MinPhase = BossPhase.Phase2
                }
            },

            MinionType = EnemyType.Bat,  // Servant of Cthulhu equivalent
            MaxMinions = 4,
            MinionSpawnInterval = 8f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 3, MaxCount = 5, Guaranteed = true },
                new BossLoot { Item = ItemType.Lens, MinCount = 2, MaxCount = 4, DropChance = 1f },
                new BossLoot { Item = ItemType.CopperBar, MinCount = 10, MaxCount = 20, DropChance = 0.5f },
            }
        });

        // === KING SLIME ===
        // Bouncing slime boss, spawns slimes, can teleport
        Register(new BossData
        {
            Type = BossType.KingSlime,
            Name = "King Slime",
            Title = "has arrived!",
            BaseHealth = 2000,
            BaseDamage = 20,
            BaseSpeed = 80f,
            BaseDefense = 4,
            KnockbackResistance = 0.8f,
            Width = 100,
            Height = 80,
            Movement = BossMovement.Bouncing,
            EnrageTime = 300f,
            EnrageMultiplier = 1.4f,
            DespawnDistance = 2000f,
            SpawnCondition = BossSpawnCondition.None,
            Color = (80, 120, 220),
            HealthBarColor = (100, 140, 240),
            BaseGoldReward = 250,
            BaseXPReward = 400,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 4,
                    SpawnsMinions = true
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.4f,
                    AttackSpeedMultiplier = 1.3f,
                    DamageMultiplier = 1.15f,
                    Defense = 6,
                    SpawnsMinions = true
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase3,
                    HealthThreshold = 0.25f,
                    SpeedMultiplier = 1.8f,
                    AttackSpeedMultiplier = 1.6f,
                    DamageMultiplier = 1.3f,
                    Defense = 8,
                    SpawnsMinions = true
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Bounce",
                    Damage = 20,
                    Cooldown = 1.5f,
                    Range = 200f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Teleport Slam",
                    Damage = 30,
                    Cooldown = 4f,
                    Range = 600f,
                    IsAoE = true,
                    AoERadius = 80f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Slime Rain",
                    Damage = 15,
                    Cooldown = 6f,
                    IsProjectile = true,
                    ProjectileCount = 5,
                    SpreadAngle = 45f,
                    ProjectileSpeed = 200f,
                    MinPhase = BossPhase.Phase3
                }
            },

            MinionType = EnemyType.Slime,
            MaxMinions = 6,
            MinionSpawnInterval = 4f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 2, MaxCount = 4, Guaranteed = true },
                new BossLoot { Item = ItemType.Gel, MinCount = 30, MaxCount = 50, DropChance = 1f },
                new BossLoot { Item = ItemType.IronBar, MinCount = 5, MaxCount = 10, DropChance = 0.4f },
            }
        });

        // === BRAIN OF DEPTHS ===
        // Floating brain with creeper minions, teleports
        Register(new BossData
        {
            Type = BossType.BrainOfDepths,
            Name = "Brain of Depths",
            Title = "has emerged from the darkness!",
            BaseHealth = 3200,
            BaseDamage = 25,
            BaseSpeed = 90f,
            BaseDefense = 6,
            KnockbackResistance = 1f,
            Width = 72,
            Height = 72,
            Movement = BossMovement.FloatTeleport,
            EnrageTime = 300f,
            EnrageMultiplier = 1.6f,
            DespawnDistance = 2200f,
            SpawnCondition = BossSpawnCondition.Underground,
            Color = (180, 60, 120),
            HealthBarColor = (200, 80, 140),
            BaseGoldReward = 400,
            BaseXPReward = 650,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 0.8f,  // Slower initially
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 12,  // High defense until creepers dead
                    SpawnsMinions = true,
                    HasShield = true  // Protected by creepers
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.5f,
                    AttackSpeedMultiplier = 1.4f,
                    DamageMultiplier = 1.3f,
                    Defense = 4
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Teleport Strike",
                    Damage = 25,
                    Cooldown = 2.5f,
                    Range = 500f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Psychic Blast",
                    Damage = 35,
                    Cooldown = 3f,
                    Range = 300f,
                    IsAoE = true,
                    AoERadius = 100f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Confusion",
                    Damage = 15,
                    Cooldown = 8f,
                    Range = 400f,
                    MinPhase = BossPhase.Phase2
                }
            },

            MinionType = EnemyType.Ghost,  // Creeper equivalent
            MaxMinions = 8,
            MinionSpawnInterval = 0f,  // All spawned at start

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 4, MaxCount = 6, Guaranteed = true },
                new BossLoot { Item = ItemType.Lens, MinCount = 3, MaxCount = 5, DropChance = 0.8f },
                new BossLoot { Item = ItemType.SilverBar, MinCount = 8, MaxCount = 15, DropChance = 0.5f },
            }
        });

        // === SKELETAL WARLORD ===
        // Dungeon guardian, melee focused with spinning attacks
        Register(new BossData
        {
            Type = BossType.SkeletalWarlord,
            Name = "Skeletal Warlord",
            Title = "has been disturbed!",
            BaseHealth = 4500,
            BaseDamage = 35,
            BaseSpeed = 70f,
            BaseDefense = 10,
            KnockbackResistance = 1f,
            Width = 96,
            Height = 120,
            Movement = BossMovement.WalkLeap,
            EnrageTime = 300f,
            EnrageMultiplier = 1.8f,
            DespawnDistance = 2500f,
            SpawnCondition = BossSpawnCondition.Dungeon,
            Color = (200, 200, 180),
            HealthBarColor = (220, 220, 200),
            BaseGoldReward = 550,
            BaseXPReward = 850,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 10
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.3f,
                    AttackSpeedMultiplier = 1.5f,
                    DamageMultiplier = 1.25f,
                    Defense = 12,
                    SpawnsMinions = true
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase3,
                    HealthThreshold = 0.2f,
                    SpeedMultiplier = 1.6f,
                    AttackSpeedMultiplier = 2f,
                    DamageMultiplier = 1.5f,
                    Defense = 15
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Sword Slash",
                    Damage = 35,
                    Cooldown = 1.5f,
                    Range = 100f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Bone Throw",
                    Damage = 25,
                    Cooldown = 2f,
                    Range = 400f,
                    IsProjectile = true,
                    ProjectileSpeed = 350f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Spin Attack",
                    Damage = 45,
                    Cooldown = 4f,
                    Range = 150f,
                    IsAoE = true,
                    AoERadius = 120f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Skull Barrage",
                    Damage = 30,
                    Cooldown = 3f,
                    Range = 500f,
                    IsProjectile = true,
                    ProjectileCount = 3,
                    SpreadAngle = 30f,
                    ProjectileSpeed = 400f,
                    MinPhase = BossPhase.Phase3
                }
            },

            MinionType = EnemyType.Skeleton,
            MaxMinions = 4,
            MinionSpawnInterval = 10f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 5, MaxCount = 8, Guaranteed = true },
                new BossLoot { Item = ItemType.GoldBar, MinCount = 5, MaxCount = 12, DropChance = 0.6f },
                new BossLoot { Item = ItemType.IronSword, MinCount = 1, MaxCount = 1, DropChance = 0.15f },
            }
        });

        // === QUEEN BEE ===
        // Jungle flying boss with stinger attacks
        Register(new BossData
        {
            Type = BossType.QueenBee,
            Name = "Queen Bee",
            Title = "is enraged!",
            BaseHealth = 3400,
            BaseDamage = 22,
            BaseSpeed = 140f,
            BaseDefense = 4,
            KnockbackResistance = 1f,
            Width = 88,
            Height = 72,
            Movement = BossMovement.FlyPattern,
            EnrageTime = 240f,  // 4 minutes
            EnrageMultiplier = 1.5f,
            DespawnDistance = 2000f,
            SpawnCondition = BossSpawnCondition.Jungle,
            Color = (220, 180, 50),
            HealthBarColor = (240, 200, 70),
            BaseGoldReward = 350,
            BaseXPReward = 600,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 4
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.5f,
                    AttackSpeedMultiplier = 1.4f,
                    DamageMultiplier = 1.2f,
                    Defense = 6,
                    SpawnsMinions = true
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Charge",
                    Damage = 22,
                    Cooldown = 1.8f,
                    Range = 450f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Stinger",
                    Damage = 20,
                    Cooldown = 1f,
                    Range = 350f,
                    IsProjectile = true,
                    ProjectileSpeed = 300f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Stinger Barrage",
                    Damage = 18,
                    Cooldown = 2f,
                    Range = 400f,
                    IsProjectile = true,
                    ProjectileCount = 6,
                    SpreadAngle = 40f,
                    ProjectileSpeed = 280f,
                    MinPhase = BossPhase.Phase2
                }
            },

            MinionType = EnemyType.Bat,  // Bee equivalent
            MaxMinions = 8,
            MinionSpawnInterval = 3f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 3, MaxCount = 5, Guaranteed = true },
                new BossLoot { Item = ItemType.Gel, MinCount = 20, MaxCount = 35, DropChance = 0.7f },
                new BossLoot { Item = ItemType.SilverBar, MinCount = 6, MaxCount = 12, DropChance = 0.45f },
            }
        });

        // === WALL OF SHADOWS ===
        // Final pre-hardmode boss, horizontal sweep
        Register(new BossData
        {
            Type = BossType.WallOfShadows,
            Name = "Wall of Shadows",
            Title = "has awakened!",
            BaseHealth = 8000,
            BaseDamage = 50,
            BaseSpeed = 50f,  // Slow but relentless
            BaseDefense = 12,
            KnockbackResistance = 1f,
            Width = 200,
            Height = 400,  // Very tall
            Movement = BossMovement.HorizontalSweep,
            EnrageTime = 420f,  // 7 minutes
            EnrageMultiplier = 2f,
            DespawnDistance = 3000f,
            SpawnCondition = BossSpawnCondition.Underworld,
            Color = (100, 50, 80),
            HealthBarColor = (150, 70, 100),
            BaseGoldReward = 800,
            BaseXPReward = 1500,
            UnlocksHardmode = true,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 12,
                    SpawnsMinions = true
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.4f,
                    AttackSpeedMultiplier = 1.5f,
                    DamageMultiplier = 1.3f,
                    Defense = 15,
                    SpawnsMinions = true
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase3,
                    HealthThreshold = 0.2f,
                    SpeedMultiplier = 1.8f,
                    AttackSpeedMultiplier = 2f,
                    DamageMultiplier = 1.6f,
                    Defense = 18,
                    SpawnsMinions = true
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Laser",
                    Damage = 45,
                    Cooldown = 2f,
                    Range = 800f,
                    IsProjectile = true,
                    ProjectileSpeed = 500f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Fire Breath",
                    Damage = 40,
                    Cooldown = 1.5f,
                    Range = 300f,
                    IsAoE = true,
                    AoERadius = 150f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Laser Barrage",
                    Damage = 35,
                    Cooldown = 3f,
                    Range = 800f,
                    IsProjectile = true,
                    ProjectileCount = 5,
                    SpreadAngle = 60f,
                    ProjectileSpeed = 450f,
                    MinPhase = BossPhase.Phase2
                }
            },

            MinionType = EnemyType.Demon,
            MaxMinions = 10,
            MinionSpawnInterval = 3f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 8, MaxCount = 12, Guaranteed = true },
                new BossLoot { Item = ItemType.GoldBar, MinCount = 15, MaxCount = 25, DropChance = 0.8f },
                new BossLoot { Item = ItemType.IronBar, MinCount = 20, MaxCount = 35, DropChance = 0.7f },
            }
        });
    }

    #endregion

    #region Hardmode Bosses

    private static void RegisterHardmodeBosses()
    {
        // === THE TWINS ===
        // Dual-boss fight: Retinazer (laser) + Spazmatism (fire)
        Register(new BossData
        {
            Type = BossType.TheTwins,
            Name = "The Twins",
            Title = "have awoken!",
            BaseHealth = 24000,  // Combined (12k each)
            BaseDamage = 50,
            BaseSpeed = 150f,
            BaseDefense = 10,
            KnockbackResistance = 1f,
            Width = 80,
            Height = 80,
            Movement = BossMovement.HoverDash,
            EnrageTime = 300f,
            EnrageMultiplier = 2f,
            DespawnDistance = 2500f,
            SpawnCondition = BossSpawnCondition.Nighttime,
            IsHardmode = true,
            Color = (100, 200, 100),
            HealthBarColor = (120, 220, 120),
            BaseGoldReward = 600,
            BaseXPReward = 1200,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 10
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.4f,
                    SpeedMultiplier = 1.5f,
                    AttackSpeedMultiplier = 1.8f,
                    DamageMultiplier = 1.3f,
                    Defense = 25  // Armored second form
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Retinazer Laser",
                    Damage = 45,
                    Cooldown = 0.5f,
                    Range = 600f,
                    IsProjectile = true,
                    ProjectileSpeed = 400f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Spazmatism Fireball",
                    Damage = 50,
                    Cooldown = 1f,
                    Range = 400f,
                    IsProjectile = true,
                    ProjectileSpeed = 350f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Death Laser",
                    Damage = 55,
                    Cooldown = 0.3f,
                    Range = 700f,
                    IsProjectile = true,
                    ProjectileSpeed = 500f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Cursed Flames",
                    Damage = 60,
                    Cooldown = 2f,
                    Range = 300f,
                    IsAoE = true,
                    AoERadius = 120f,
                    MinPhase = BossPhase.Phase2
                }
            },

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 6, MaxCount = 10, Guaranteed = true },
                new BossLoot { Item = ItemType.IronBar, MinCount = 15, MaxCount = 30, DropChance = 0.7f },
                new BossLoot { Item = ItemType.SilverBar, MinCount = 10, MaxCount = 20, DropChance = 0.6f },
            }
        });

        // === THE DESTROYER ===
        // Massive worm boss with 82 segments, spawns probes
        Register(new BossData
        {
            Type = BossType.TheDestroyer,
            Name = "The Destroyer",
            Title = "has awoken!",
            BaseHealth = 80000,  // Highest HP boss
            BaseDamage = 60,
            BaseSpeed = 100f,
            BaseDefense = 0,  // Body has no defense, head has more
            KnockbackResistance = 1f,
            Width = 40,  // Head segment
            Height = 40,
            Movement = BossMovement.Worm,
            HasSegments = true,
            SegmentCount = 82,
            EnrageTime = 300f,
            EnrageMultiplier = 2f,
            DespawnDistance = 3000f,
            SpawnCondition = BossSpawnCondition.Nighttime,
            IsHardmode = true,
            Color = (150, 150, 170),
            HealthBarColor = (170, 170, 190),
            BaseGoldReward = 700,
            BaseXPReward = 1400,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 0,
                    SpawnsMinions = true  // Probes spawn from body
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.3f,
                    AttackSpeedMultiplier = 1.5f,
                    DamageMultiplier = 1.2f,
                    Defense = 0,
                    SpawnsMinions = true
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Body Slam",
                    Damage = 60,
                    Cooldown = 0.5f,
                    Range = 100f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Probe Laser",
                    Damage = 35,
                    Cooldown = 2f,
                    Range = 400f,
                    IsProjectile = true,
                    ProjectileSpeed = 350f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Death Laser",
                    Damage = 45,
                    Cooldown = 1.5f,
                    Range = 500f,
                    IsProjectile = true,
                    ProjectileCount = 3,
                    SpreadAngle = 30f,
                    ProjectileSpeed = 400f,
                    MinPhase = BossPhase.Phase2
                }
            },

            MinionType = EnemyType.Bat,  // Probes
            MaxMinions = 12,
            MinionSpawnInterval = 2f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 7, MaxCount = 12, Guaranteed = true },
                new BossLoot { Item = ItemType.IronBar, MinCount = 20, MaxCount = 40, DropChance = 0.75f },
                new BossLoot { Item = ItemType.GoldBar, MinCount = 10, MaxCount = 20, DropChance = 0.5f },
            }
        });

        // === SKELETRON PRIME ===
        // Mechanical skull with 4 arms (saw, vice, laser, cannon)
        Register(new BossData
        {
            Type = BossType.SkeletronPrime,
            Name = "Skeletron Prime",
            Title = "has awoken!",
            BaseHealth = 28000,
            BaseDamage = 55,
            BaseSpeed = 80f,
            BaseDefense = 24,  // High defense
            KnockbackResistance = 1f,
            Width = 100,
            Height = 100,
            Movement = BossMovement.HoverDash,
            EnrageTime = 300f,
            EnrageMultiplier = 2.5f,  // Very aggressive when enraged
            DespawnDistance = 2500f,
            SpawnCondition = BossSpawnCondition.Nighttime,
            IsHardmode = true,
            Color = (180, 180, 180),
            HealthBarColor = (200, 200, 200),
            BaseGoldReward = 650,
            BaseXPReward = 1300,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 24
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.4f,
                    AttackSpeedMultiplier = 1.6f,
                    DamageMultiplier = 1.2f,
                    Defense = 15  // Head becomes vulnerable when spinning
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Prime Saw",
                    Damage = 52,
                    Cooldown = 1f,
                    Range = 200f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Prime Vice",
                    Damage = 45,
                    Cooldown = 1.2f,
                    Range = 250f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Prime Cannon",
                    Damage = 40,
                    Cooldown = 0.8f,
                    Range = 500f,
                    IsProjectile = true,
                    ProjectileSpeed = 300f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Prime Laser",
                    Damage = 35,
                    Cooldown = 0.5f,
                    Range = 600f,
                    IsProjectile = true,
                    ProjectileSpeed = 400f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Skull Spin",
                    Damage = 70,
                    Cooldown = 3f,
                    Range = 150f,
                    IsAoE = true,
                    AoERadius = 100f,
                    MinPhase = BossPhase.Phase2
                }
            },

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 6, MaxCount = 11, Guaranteed = true },
                new BossLoot { Item = ItemType.SilverBar, MinCount = 15, MaxCount = 30, DropChance = 0.7f },
                new BossLoot { Item = ItemType.GoldBar, MinCount = 8, MaxCount = 18, DropChance = 0.55f },
            }
        });

        // === PLANTERA ===
        // Jungle boss, spawns after all mechanical bosses
        Register(new BossData
        {
            Type = BossType.Plantera,
            Name = "Plantera",
            Title = "has awoken!",
            BaseHealth = 30000,
            BaseDamage = 50,
            BaseSpeed = 60f,  // Slow initially
            BaseDefense = 14,
            KnockbackResistance = 1f,
            Width = 120,
            Height = 120,
            Movement = BossMovement.HoverDash,
            EnrageTime = 300f,
            EnrageMultiplier = 2.5f,  // Enrages if taken out of jungle
            DespawnDistance = 2000f,
            SpawnCondition = BossSpawnCondition.Jungle,
            IsHardmode = true,
            Color = (150, 220, 100),
            HealthBarColor = (170, 240, 120),
            BaseGoldReward = 750,
            BaseXPReward = 1600,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 14
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.5f,
                    SpeedMultiplier = 1.8f,  // Much faster in phase 2
                    AttackSpeedMultiplier = 2f,
                    DamageMultiplier = 1.4f,
                    Defense = 10,
                    SpawnsMinions = true  // Tentacles
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Thorn Ball",
                    Damage = 44,
                    Cooldown = 1f,
                    Range = 400f,
                    IsProjectile = true,
                    ProjectileSpeed = 250f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Seed Shot",
                    Damage = 36,
                    Cooldown = 0.6f,
                    Range = 350f,
                    IsProjectile = true,
                    ProjectileCount = 3,
                    SpreadAngle = 25f,
                    ProjectileSpeed = 300f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Tentacle Lash",
                    Damage = 60,
                    Cooldown = 1.5f,
                    Range = 250f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Spore Cloud",
                    Damage = 70,
                    Cooldown = 2f,
                    Range = 200f,
                    IsAoE = true,
                    AoERadius = 150f,
                    MinPhase = BossPhase.Phase2
                }
            },

            MinionType = EnemyType.Bat,  // Planteras Tentacles
            MaxMinions = 8,
            MinionSpawnInterval = 3f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 8, MaxCount = 14, Guaranteed = true },
                new BossLoot { Item = ItemType.Gel, MinCount = 30, MaxCount = 50, DropChance = 0.6f },
                new BossLoot { Item = ItemType.GoldBar, MinCount = 12, MaxCount = 22, DropChance = 0.65f },
            }
        });

        // === GOLEM ===
        // Temple boss, fought in Lihzahrd Temple
        Register(new BossData
        {
            Type = BossType.Golem,
            Name = "Golem",
            Title = "has awoken!",
            BaseHealth = 39000,
            BaseDamage = 72,
            BaseSpeed = 40f,  // Slow but powerful
            BaseDefense = 26,
            KnockbackResistance = 1f,
            Width = 140,
            Height = 160,
            Movement = BossMovement.StationaryTeleport,  // Stays in place, teleports
            EnrageTime = 360f,  // 6 minutes
            EnrageMultiplier = 1.8f,
            DespawnDistance = 1500f,
            SpawnCondition = BossSpawnCondition.Jungle,  // Temple
            IsHardmode = true,
            Color = (180, 140, 80),
            HealthBarColor = (200, 160, 100),
            BaseGoldReward = 850,
            BaseXPReward = 1800,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 26
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.6f,
                    SpeedMultiplier = 1.2f,
                    AttackSpeedMultiplier = 1.4f,
                    DamageMultiplier = 1.2f,
                    Defense = 20  // Fists detach
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase3,
                    HealthThreshold = 0.3f,
                    SpeedMultiplier = 1.5f,
                    AttackSpeedMultiplier = 1.8f,
                    DamageMultiplier = 1.4f,
                    Defense = 12  // Head detaches and flies
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Eye Laser",
                    Damage = 60,
                    Cooldown = 0.8f,
                    Range = 500f,
                    IsProjectile = true,
                    ProjectileSpeed = 350f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Fist Slam",
                    Damage = 72,
                    Cooldown = 2f,
                    Range = 200f,
                    IsAoE = true,
                    AoERadius = 80f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Fireball Barrage",
                    Damage = 50,
                    Cooldown = 1.5f,
                    Range = 600f,
                    IsProjectile = true,
                    ProjectileCount = 4,
                    SpreadAngle = 45f,
                    ProjectileSpeed = 280f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Jump Slam",
                    Damage = 90,
                    Cooldown = 4f,
                    Range = 400f,
                    IsAoE = true,
                    AoERadius = 120f,
                    MinPhase = BossPhase.Phase3
                }
            },

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 9, MaxCount = 16, Guaranteed = true },
                new BossLoot { Item = ItemType.GoldBar, MinCount = 15, MaxCount = 30, DropChance = 0.7f },
                new BossLoot { Item = ItemType.IronBar, MinCount = 25, MaxCount = 45, DropChance = 0.6f },
            }
        });

        // === MOON LORD ===
        // Final boss of the game
        Register(new BossData
        {
            Type = BossType.MoonLord,
            Name = "Moon Lord",
            Title = "has awoken!",
            BaseHealth = 145000,  // Massive HP pool
            BaseDamage = 100,
            BaseSpeed = 50f,
            BaseDefense = 50,
            KnockbackResistance = 1f,
            Width = 200,
            Height = 400,
            Movement = BossMovement.HoverDash,
            EnrageTime = 600f,  // 10 minutes
            EnrageMultiplier = 3f,  // Extreme enrage
            DespawnDistance = 4000f,
            SpawnCondition = BossSpawnCondition.None,  // Spawns after Lunar events
            IsHardmode = true,
            Color = (100, 150, 200),
            HealthBarColor = (120, 170, 220),
            BaseGoldReward = 1500,
            BaseXPReward = 5000,

            Phases = new[]
            {
                new BossPhaseData
                {
                    Phase = BossPhase.Phase1,
                    HealthThreshold = 1f,
                    SpeedMultiplier = 1f,
                    AttackSpeedMultiplier = 1f,
                    DamageMultiplier = 1f,
                    Defense = 50,
                    HasShield = true  // Shield blocks damage
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase2,
                    HealthThreshold = 0.6f,
                    SpeedMultiplier = 1.2f,
                    AttackSpeedMultiplier = 1.5f,
                    DamageMultiplier = 1.3f,
                    Defense = 40,
                    SpawnsMinions = true  // True Eyes of Cthulhu
                },
                new BossPhaseData
                {
                    Phase = BossPhase.Phase3,
                    HealthThreshold = 0.3f,
                    SpeedMultiplier = 1.4f,
                    AttackSpeedMultiplier = 2f,
                    DamageMultiplier = 1.5f,
                    Defense = 30,
                    SpawnsMinions = true
                }
            },

            Attacks = new[]
            {
                new BossAttack
                {
                    Name = "Phantasmal Eye",
                    Damage = 80,
                    Cooldown = 0.5f,
                    Range = 800f,
                    IsProjectile = true,
                    ProjectileSpeed = 400f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Phantasmal Sphere",
                    Damage = 100,
                    Cooldown = 2f,
                    Range = 600f,
                    IsProjectile = true,
                    ProjectileCount = 6,
                    SpreadAngle = 60f,
                    ProjectileSpeed = 300f,
                    MinPhase = BossPhase.Phase1
                },
                new BossAttack
                {
                    Name = "Phantasmal Deathray",
                    Damage = 150,
                    Cooldown = 4f,
                    Range = 1200f,
                    IsProjectile = true,
                    ProjectileSpeed = 600f,
                    MinPhase = BossPhase.Phase2
                },
                new BossAttack
                {
                    Name = "Moon Bite",
                    Damage = 120,
                    Cooldown = 10f,
                    Range = 400f,
                    IsAoE = true,
                    AoERadius = 200f,
                    MinPhase = BossPhase.Phase2  // Prevents healing
                },
                new BossAttack
                {
                    Name = "True Eye of Cthulhu",
                    Damage = 90,
                    Cooldown = 5f,
                    Range = 700f,
                    IsAoE = true,
                    AoERadius = 100f,
                    MinPhase = BossPhase.Phase3
                }
            },

            MinionType = EnemyType.Demon,  // True Eyes
            MaxMinions = 3,
            MinionSpawnInterval = 15f,

            LootTable = new[]
            {
                new BossLoot { Item = ItemType.GoldCoin, MinCount = 15, MaxCount = 25, Guaranteed = true },
                new BossLoot { Item = ItemType.GoldBar, MinCount = 30, MaxCount = 50, DropChance = 1f },
                new BossLoot { Item = ItemType.SilverBar, MinCount = 40, MaxCount = 60, DropChance = 0.8f },
            }
        });
    }

    #endregion
}
