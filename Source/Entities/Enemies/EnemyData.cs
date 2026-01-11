using Terrascent.World.Biomes;

namespace Terrascent.Entities.Enemies;

/// <summary>
/// Defines when an enemy will become aggressive toward the player.
/// Based on Terraria's different AI aggression patterns.
/// </summary>
public enum AggressionType
{
    /// <summary>
    /// Always targets and chases the player when in detection range.
    /// Used by: Demon Eyes, Bats, Demons, Ghosts
    /// </summary>
    AlwaysAggressive,

    /// <summary>
    /// Only becomes aggressive when damaged by the player.
    /// Otherwise just wanders/patrols without chasing.
    /// Used by: Slimes (daytime)
    /// </summary>
    PassiveUntilDamaged,

    /// <summary>
    /// Chases player but backs off and retries when stuck/blocked.
    /// Will eventually lose interest and wander away.
    /// Used by: Zombies, Skeletons, most Fighter AI
    /// </summary>
    ChaseWithRetry
}

/// <summary>
/// Static data defining an enemy type's base stats and behavior.
/// </summary>
public class EnemyData
{
    public EnemyType Type { get; init; }
    public string Name { get; init; } = "";

    // Base stats (before difficulty scaling)
    public int BaseHealth { get; init; } = 20;
    public int BaseDamage { get; init; } = 10;
    public float BaseSpeed { get; init; } = 60f;
    public float KnockbackResistance { get; init; } = 0f;  // 0-1, reduces knockback

    // Size
    public int Width { get; init; } = 24;
    public int Height { get; init; } = 24;

    // Behavior
    public MovementPattern Movement { get; init; } = MovementPattern.Walker;
    public AggressionType Aggression { get; init; } = AggressionType.ChaseWithRetry;
    public float DetectionRange { get; init; } = 200f;
    public float AttackRange { get; init; } = 32f;
    public float AttackCooldown { get; init; } = 1.5f;

    // Rewards
    public int BaseGold { get; init; } = 5;
    public int BaseXP { get; init; } = 10;

    // Visual (for rendering)
    public (byte R, byte G, byte B) Color { get; init; } = (255, 0, 0);

    // Spawn conditions
    public float SpawnWeight { get; init; } = 1f;  // Relative spawn chance
    public int MinDepth { get; init; } = 0;        // Minimum Y (0 = surface)
    public int MaxDepth { get; init; } = 1000;     // Maximum Y depth
    public bool RequiresSurface { get; init; } = false;
    public bool RequiresUnderground { get; init; } = false;

    // === BIOME ASSOCIATIONS ===
    /// <summary>
    /// Biomes where this enemy can spawn. Empty = spawns anywhere.
    /// </summary>
    public BiomeType[] Biomes { get; init; } = Array.Empty<BiomeType>();

    /// <summary>
    /// If true, this enemy ONLY spawns in its designated biomes.
    /// If false, it can spawn elsewhere but prefers its biomes.
    /// </summary>
    public bool BiomeExclusive { get; init; } = false;

    /// <summary>
    /// Spawn weight multiplier when in a preferred biome.
    /// </summary>
    public float BiomeSpawnBonus { get; init; } = 2f;

    /// <summary>
    /// If true, this enemy only spawns after hardmode is enabled.
    /// </summary>
    public bool RequiresHardmode { get; init; } = false;
}

/// <summary>
/// Registry of all enemy types and their data.
/// </summary>
public static class EnemyRegistry
{
    private static readonly Dictionary<EnemyType, EnemyData> _enemies = new();

    static EnemyRegistry()
    {
        RegisterAll();
    }

    private static void RegisterAll()
    {
        // === FOREST/SURFACE ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.Slime,
            Name = "Slime",
            BaseHealth = 20,
            BaseDamage = 8,
            BaseSpeed = 40f,
            Width = 24,
            Height = 20,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.PassiveUntilDamaged,
            DetectionRange = 150f,
            AttackRange = 20f,
            AttackCooldown = 0.8f,
            BaseGold = 3,
            BaseXP = 5,
            Color = (50, 200, 50),
            SpawnWeight = 3f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Forest }
        });

        Register(new EnemyData
        {
            Type = EnemyType.Zombie,
            Name = "Zombie",
            BaseHealth = 45,
            BaseDamage = 14,
            BaseSpeed = 35f,
            Width = 24,
            Height = 44,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 180f,
            AttackRange = 28f,
            AttackCooldown = 1.2f,
            KnockbackResistance = 0.3f,
            BaseGold = 8,
            BaseXP = 12,
            Color = (80, 120, 80),
            SpawnWeight = 2f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Forest }
        });

        Register(new EnemyData
        {
            Type = EnemyType.Skeleton,
            Name = "Skeleton",
            BaseHealth = 30,
            BaseDamage = 12,
            BaseSpeed = 70f,
            Width = 20,
            Height = 44,
            Movement = MovementPattern.Walker,
            DetectionRange = 220f,
            AttackRange = 32f,
            AttackCooldown = 1.0f,
            BaseGold = 10,
            BaseXP = 15,
            Color = (220, 220, 200),
            SpawnWeight = 1.5f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Forest }
        });

        Register(new EnemyData
        {
            Type = EnemyType.Bat,
            Name = "Bat",
            BaseHealth = 15,
            BaseDamage = 10,
            BaseSpeed = 100f,
            Width = 20,
            Height = 16,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 250f,
            AttackRange = 24f,
            AttackCooldown = 0.6f,
            BaseGold = 4,
            BaseXP = 8,
            Color = (60, 40, 80),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Forest, BiomeType.Underground }
        });

        Register(new EnemyData
        {
            Type = EnemyType.DemonEye,
            Name = "Demon Eye",
            BaseHealth = 28,
            BaseDamage = 12,
            BaseSpeed = 90f,
            Width = 24,
            Height = 24,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 300f,
            AttackRange = 28f,
            AttackCooldown = 0.8f,
            BaseGold = 6,
            BaseXP = 10,
            Color = (180, 50, 50),
            SpawnWeight = 1.5f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Forest }
        });

        // === DESERT BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.Antlion,
            Name = "Antlion",
            BaseHealth = 35,
            BaseDamage = 18,
            BaseSpeed = 80f,
            Width = 28,
            Height = 20,
            Movement = MovementPattern.Burrower,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 180f,
            AttackRange = 30f,
            AttackCooldown = 1.0f,
            BaseGold = 12,
            BaseXP = 15,
            Color = (194, 158, 89),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Desert, BiomeType.UndergroundDesert },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Vulture,
            Name = "Vulture",
            BaseHealth = 30,
            BaseDamage = 15,
            BaseSpeed = 110f,
            Width = 32,
            Height = 24,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 350f,
            AttackRange = 28f,
            AttackCooldown = 1.2f,
            BaseGold = 10,
            BaseXP = 12,
            Color = (139, 90, 43),
            SpawnWeight = 1.5f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Desert },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Mummy,
            Name = "Mummy",
            BaseHealth = 55,
            BaseDamage = 20,
            BaseSpeed = 45f,
            Width = 24,
            Height = 44,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 200f,
            AttackRange = 32f,
            AttackCooldown = 1.5f,
            KnockbackResistance = 0.4f,
            BaseGold = 15,
            BaseXP = 20,
            Color = (200, 180, 140),
            SpawnWeight = 1f,
            Biomes = new[] { BiomeType.Desert, BiomeType.UndergroundDesert },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.DesertSlime,
            Name = "Desert Slime",
            BaseHealth = 25,
            BaseDamage = 10,
            BaseSpeed = 45f,
            Width = 24,
            Height = 20,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.PassiveUntilDamaged,
            DetectionRange = 150f,
            AttackRange = 20f,
            AttackCooldown = 0.8f,
            BaseGold = 4,
            BaseXP = 6,
            Color = (230, 190, 90),
            SpawnWeight = 2.5f,
            Biomes = new[] { BiomeType.Desert },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Scorpion,
            Name = "Scorpion",
            BaseHealth = 22,
            BaseDamage = 14,
            BaseSpeed = 85f,
            Width = 24,
            Height = 16,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 160f,
            AttackRange = 24f,
            AttackCooldown = 0.6f,
            BaseGold = 6,
            BaseXP = 8,
            Color = (160, 80, 40),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Desert, BiomeType.UndergroundDesert },
            BiomeExclusive = true
        });

        // === SNOW BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.IceSlime,
            Name = "Ice Slime",
            BaseHealth = 25,
            BaseDamage = 10,
            BaseSpeed = 35f,
            Width = 24,
            Height = 20,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.PassiveUntilDamaged,
            DetectionRange = 150f,
            AttackRange = 20f,
            AttackCooldown = 0.9f,
            BaseGold = 4,
            BaseXP = 6,
            Color = (100, 180, 255),
            SpawnWeight = 2.5f,
            Biomes = new[] { BiomeType.Snow, BiomeType.UndergroundSnow },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.ZombieEskimo,
            Name = "Zombie Eskimo",
            BaseHealth = 50,
            BaseDamage = 16,
            BaseSpeed = 30f,
            Width = 24,
            Height = 44,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 180f,
            AttackRange = 28f,
            AttackCooldown = 1.3f,
            KnockbackResistance = 0.35f,
            BaseGold = 10,
            BaseXP = 14,
            Color = (120, 140, 180),
            SpawnWeight = 1.5f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Snow },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.IceBat,
            Name = "Ice Bat",
            BaseHealth = 18,
            BaseDamage = 12,
            BaseSpeed = 95f,
            Width = 20,
            Height = 16,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 250f,
            AttackRange = 24f,
            AttackCooldown = 0.6f,
            BaseGold = 5,
            BaseXP = 9,
            Color = (80, 150, 220),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Snow, BiomeType.UndergroundSnow },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.UndeadViking,
            Name = "Undead Viking",
            BaseHealth = 65,
            BaseDamage = 22,
            BaseSpeed = 55f,
            Width = 28,
            Height = 48,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 220f,
            AttackRange = 36f,
            AttackCooldown = 1.4f,
            KnockbackResistance = 0.5f,
            BaseGold = 18,
            BaseXP = 25,
            Color = (150, 160, 180),
            SpawnWeight = 0.8f,
            Biomes = new[] { BiomeType.UndergroundSnow },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.IceGolem,
            Name = "Ice Golem",
            BaseHealth = 150,
            BaseDamage = 35,
            BaseSpeed = 30f,
            Width = 40,
            Height = 52,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 280f,
            AttackRange = 200f,
            AttackCooldown = 2.0f,
            KnockbackResistance = 0.9f,
            BaseGold = 50,
            BaseXP = 60,
            Color = (120, 200, 255),
            SpawnWeight = 0.3f,
            Biomes = new[] { BiomeType.Snow },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        // === JUNGLE BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.JungleSlime,
            Name = "Jungle Slime",
            BaseHealth = 30,
            BaseDamage = 12,
            BaseSpeed = 45f,
            Width = 26,
            Height = 22,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.PassiveUntilDamaged,
            DetectionRange = 160f,
            AttackRange = 22f,
            AttackCooldown = 0.7f,
            BaseGold = 5,
            BaseXP = 8,
            Color = (60, 180, 60),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Jungle, BiomeType.UndergroundJungle },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Hornet,
            Name = "Hornet",
            BaseHealth = 35,
            BaseDamage = 16,
            BaseSpeed = 120f,
            Width = 28,
            Height = 20,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 300f,
            AttackRange = 180f,
            AttackCooldown = 1.5f,
            BaseGold = 12,
            BaseXP = 15,
            Color = (200, 180, 50),
            SpawnWeight = 1.5f,
            Biomes = new[] { BiomeType.Jungle, BiomeType.UndergroundJungle },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.ManEater,
            Name = "Man Eater",
            BaseHealth = 45,
            BaseDamage = 22,
            BaseSpeed = 0f,
            Width = 24,
            Height = 32,
            Movement = MovementPattern.Stationary,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 150f,
            AttackRange = 120f,
            AttackCooldown = 0.8f,
            BaseGold = 15,
            BaseXP = 18,
            Color = (100, 160, 60),
            SpawnWeight = 1f,
            Biomes = new[] { BiomeType.UndergroundJungle },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.SnatcherPlant,
            Name = "Snatcher Plant",
            BaseHealth = 40,
            BaseDamage = 18,
            BaseSpeed = 0f,
            Width = 28,
            Height = 28,
            Movement = MovementPattern.Stationary,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 100f,
            AttackRange = 80f,
            AttackCooldown = 1.0f,
            BaseGold = 10,
            BaseXP = 12,
            Color = (80, 140, 40),
            SpawnWeight = 1.2f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Jungle },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.JungleBat,
            Name = "Jungle Bat",
            BaseHealth = 22,
            BaseDamage = 14,
            BaseSpeed = 105f,
            Width = 22,
            Height = 18,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 270f,
            AttackRange = 26f,
            AttackCooldown = 0.5f,
            BaseGold = 6,
            BaseXP = 10,
            Color = (80, 160, 80),
            SpawnWeight = 1.8f,
            Biomes = new[] { BiomeType.Jungle, BiomeType.UndergroundJungle },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Piranha,
            Name = "Piranha",
            BaseHealth = 25,
            BaseDamage = 15,
            BaseSpeed = 130f,
            Width = 20,
            Height = 16,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 200f,
            AttackRange = 20f,
            AttackCooldown = 0.4f,
            BaseGold = 8,
            BaseXP = 10,
            Color = (200, 80, 80),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Jungle, BiomeType.Ocean },
            BiomeExclusive = true
        });

        // === CORRUPTION BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.Eater,
            Name = "Eater of Souls",
            BaseHealth = 32,
            BaseDamage = 14,
            BaseSpeed = 85f,
            Width = 24,
            Height = 24,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 280f,
            AttackRange = 28f,
            AttackCooldown = 0.7f,
            BaseGold = 10,
            BaseXP = 12,
            Color = (120, 80, 150),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Corruption },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.DevourerHead,
            Name = "Devourer",
            BaseHealth = 50,
            BaseDamage = 20,
            BaseSpeed = 60f,
            Width = 20,
            Height = 20,
            Movement = MovementPattern.Burrower,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 200f,
            AttackRange = 24f,
            AttackCooldown = 1.0f,
            KnockbackResistance = 0.6f,
            BaseGold = 18,
            BaseXP = 22,
            Color = (100, 60, 130),
            SpawnWeight = 0.8f,
            Biomes = new[] { BiomeType.Corruption },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.CorruptSlime,
            Name = "Corrupt Slime",
            BaseHealth = 35,
            BaseDamage = 14,
            BaseSpeed = 50f,
            Width = 28,
            Height = 24,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 180f,
            AttackRange = 24f,
            AttackCooldown = 0.7f,
            BaseGold = 8,
            BaseXP = 10,
            Color = (90, 50, 120),
            SpawnWeight = 1.5f,
            Biomes = new[] { BiomeType.Corruption },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.ShadowZombie,
            Name = "Shadow Zombie",
            BaseHealth = 55,
            BaseDamage = 18,
            BaseSpeed = 40f,
            Width = 24,
            Height = 44,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 200f,
            AttackRange = 30f,
            AttackCooldown = 1.1f,
            KnockbackResistance = 0.35f,
            BaseGold = 12,
            BaseXP = 16,
            Color = (60, 40, 90),
            SpawnWeight = 1.2f,
            Biomes = new[] { BiomeType.Corruption },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.DarkMummy,
            Name = "Dark Mummy",
            BaseHealth = 80,
            BaseDamage = 28,
            BaseSpeed = 55f,
            Width = 26,
            Height = 46,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 220f,
            AttackRange = 34f,
            AttackCooldown = 1.3f,
            KnockbackResistance = 0.45f,
            BaseGold = 25,
            BaseXP = 30,
            Color = (80, 50, 100),
            SpawnWeight = 0.6f,
            Biomes = new[] { BiomeType.Corruption },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Corruptor,
            Name = "Corruptor",
            BaseHealth = 90,
            BaseDamage = 24,
            BaseSpeed = 70f,
            Width = 32,
            Height = 28,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 350f,
            AttackRange = 250f,
            AttackCooldown = 2.0f,
            KnockbackResistance = 0.4f,
            BaseGold = 30,
            BaseXP = 35,
            Color = (100, 70, 140),
            SpawnWeight = 0.5f,
            Biomes = new[] { BiomeType.Corruption },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        // === CRIMSON BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.FaceMonster,
            Name = "Face Monster",
            BaseHealth = 38,
            BaseDamage = 16,
            BaseSpeed = 95f,
            Width = 28,
            Height = 32,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 260f,
            AttackRange = 32f,
            AttackCooldown = 0.8f,
            BaseGold = 12,
            BaseXP = 14,
            Color = (180, 60, 70),
            SpawnWeight = 1.8f,
            Biomes = new[] { BiomeType.Crimson },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.BloodCrawler,
            Name = "Blood Crawler",
            BaseHealth = 28,
            BaseDamage = 18,
            BaseSpeed = 100f,
            Width = 30,
            Height = 18,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 220f,
            AttackRange = 26f,
            AttackCooldown = 0.5f,
            BaseGold = 10,
            BaseXP = 12,
            Color = (160, 40, 50),
            SpawnWeight = 1.5f,
            Biomes = new[] { BiomeType.Crimson },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.CrimsonSlime,
            Name = "Crimson Slime",
            BaseHealth = 38,
            BaseDamage = 16,
            BaseSpeed = 55f,
            Width = 28,
            Height = 24,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 180f,
            AttackRange = 24f,
            AttackCooldown = 0.6f,
            BaseGold = 9,
            BaseXP = 11,
            Color = (170, 50, 60),
            SpawnWeight = 1.4f,
            Biomes = new[] { BiomeType.Crimson },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Crimera,
            Name = "Crimera",
            BaseHealth = 35,
            BaseDamage = 16,
            BaseSpeed = 90f,
            Width = 26,
            Height = 26,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 300f,
            AttackRange = 30f,
            AttackCooldown = 0.6f,
            BaseGold = 11,
            BaseXP = 13,
            Color = (200, 70, 80),
            SpawnWeight = 1.6f,
            Biomes = new[] { BiomeType.Crimson },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.BloodZombie,
            Name = "Blood Zombie",
            BaseHealth = 60,
            BaseDamage = 20,
            BaseSpeed = 45f,
            Width = 24,
            Height = 44,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.ChaseWithRetry,
            DetectionRange = 200f,
            AttackRange = 30f,
            AttackCooldown = 1.0f,
            KnockbackResistance = 0.4f,
            BaseGold = 14,
            BaseXP = 18,
            Color = (140, 40, 50),
            SpawnWeight = 1.2f,
            Biomes = new[] { BiomeType.Crimson },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Ichor,
            Name = "Ichor Sticker",
            BaseHealth = 85,
            BaseDamage = 26,
            BaseSpeed = 75f,
            Width = 28,
            Height = 24,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 320f,
            AttackRange = 220f,
            AttackCooldown = 1.8f,
            KnockbackResistance = 0.35f,
            BaseGold = 28,
            BaseXP = 32,
            Color = (220, 180, 50),
            SpawnWeight = 0.5f,
            Biomes = new[] { BiomeType.Crimson },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        // === HALLOW BIOME ENEMIES (Hardmode) ===
        Register(new EnemyData
        {
            Type = EnemyType.Pixie,
            Name = "Pixie",
            BaseHealth = 60,
            BaseDamage = 22,
            BaseSpeed = 140f,
            Width = 20,
            Height = 20,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 350f,
            AttackRange = 24f,
            AttackCooldown = 0.4f,
            BaseGold = 20,
            BaseXP = 25,
            Color = (255, 200, 255),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Hallow },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Unicorn,
            Name = "Unicorn",
            BaseHealth = 100,
            BaseDamage = 30,
            BaseSpeed = 160f,
            Width = 40,
            Height = 36,
            Movement = MovementPattern.Charger,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 400f,
            AttackRange = 36f,
            AttackCooldown = 1.2f,
            KnockbackResistance = 0.6f,
            BaseGold = 35,
            BaseXP = 40,
            Color = (255, 255, 255),
            SpawnWeight = 1f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Hallow },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Gastropod,
            Name = "Gastropod",
            BaseHealth = 75,
            BaseDamage = 28,
            BaseSpeed = 50f,
            Width = 28,
            Height = 24,
            Movement = MovementPattern.Floater,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 380f,
            AttackRange = 300f,
            AttackCooldown = 1.5f,
            KnockbackResistance = 0.3f,
            BaseGold = 25,
            BaseXP = 30,
            Color = (255, 150, 200),
            SpawnWeight = 1.2f,
            Biomes = new[] { BiomeType.Hallow },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.IlluminantBat,
            Name = "Illuminant Bat",
            BaseHealth = 55,
            BaseDamage = 24,
            BaseSpeed = 120f,
            Width = 24,
            Height = 20,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 300f,
            AttackRange = 28f,
            AttackCooldown = 0.5f,
            BaseGold = 18,
            BaseXP = 22,
            Color = (200, 180, 255),
            SpawnWeight = 1.5f,
            Biomes = new[] { BiomeType.Hallow },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.IlluminantSlime,
            Name = "Illuminant Slime",
            BaseHealth = 70,
            BaseDamage = 26,
            BaseSpeed = 60f,
            Width = 30,
            Height = 26,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 200f,
            AttackRange = 28f,
            AttackCooldown = 0.6f,
            BaseGold = 22,
            BaseXP = 28,
            Color = (220, 200, 255),
            SpawnWeight = 1.3f,
            Biomes = new[] { BiomeType.Hallow },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.ChaosMage,
            Name = "Chaos Elemental",
            BaseHealth = 95,
            BaseDamage = 32,
            BaseSpeed = 80f,
            Width = 24,
            Height = 40,
            Movement = MovementPattern.Teleporter,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 400f,
            AttackRange = 250f,
            AttackCooldown = 2.5f,
            KnockbackResistance = 0.5f,
            BaseGold = 40,
            BaseXP = 45,
            Color = (180, 150, 255),
            SpawnWeight = 0.4f,
            Biomes = new[] { BiomeType.Hallow },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        // === UNDERWORLD BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.FireImp,
            Name = "Fire Imp",
            BaseHealth = 55,
            BaseDamage = 20,
            BaseSpeed = 45f,
            Width = 24,
            Height = 36,
            Movement = MovementPattern.Teleporter,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 350f,
            AttackRange = 280f,
            AttackCooldown = 2.0f,
            KnockbackResistance = 0.3f,
            BaseGold = 20,
            BaseXP = 25,
            Color = (255, 100, 50),
            SpawnWeight = 1.2f,
            Biomes = new[] { BiomeType.Underworld },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.HellBat,
            Name = "Hellbat",
            BaseHealth = 40,
            BaseDamage = 18,
            BaseSpeed = 110f,
            Width = 24,
            Height = 20,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 280f,
            AttackRange = 28f,
            AttackCooldown = 0.5f,
            BaseGold = 12,
            BaseXP = 15,
            Color = (200, 60, 40),
            SpawnWeight = 1.8f,
            Biomes = new[] { BiomeType.Underworld },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.LavaBat,
            Name = "Lava Bat",
            BaseHealth = 70,
            BaseDamage = 26,
            BaseSpeed = 100f,
            Width = 28,
            Height = 22,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 300f,
            AttackRange = 30f,
            AttackCooldown = 0.6f,
            BaseGold = 22,
            BaseXP = 28,
            Color = (255, 120, 60),
            SpawnWeight = 1f,
            Biomes = new[] { BiomeType.Underworld },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.Hellhound,
            Name = "Hellhound",
            BaseHealth = 65,
            BaseDamage = 24,
            BaseSpeed = 150f,
            Width = 36,
            Height = 28,
            Movement = MovementPattern.Charger,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 350f,
            AttackRange = 32f,
            AttackCooldown = 1.0f,
            KnockbackResistance = 0.4f,
            BaseGold = 18,
            BaseXP = 22,
            Color = (180, 50, 30),
            SpawnWeight = 1f,
            Biomes = new[] { BiomeType.Underworld },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.BoneSerpent,
            Name = "Bone Serpent",
            BaseHealth = 80,
            BaseDamage = 25,
            BaseSpeed = 70f,
            Width = 24,
            Height = 24,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 320f,
            AttackRange = 28f,
            AttackCooldown = 0.8f,
            KnockbackResistance = 0.5f,
            BaseGold = 25,
            BaseXP = 30,
            Color = (200, 180, 160),
            SpawnWeight = 0.8f,
            Biomes = new[] { BiomeType.Underworld },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.VoodooDemon,
            Name = "Voodoo Demon",
            BaseHealth = 70,
            BaseDamage = 22,
            BaseSpeed = 65f,
            Width = 28,
            Height = 40,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 380f,
            AttackRange = 32f,
            AttackCooldown = 1.2f,
            KnockbackResistance = 0.35f,
            BaseGold = 30,
            BaseXP = 35,
            Color = (150, 80, 120),
            SpawnWeight = 0.3f,
            Biomes = new[] { BiomeType.Underworld },
            BiomeExclusive = true
        });

        // === DUNGEON BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.AngryBones,
            Name = "Angry Bones",
            BaseHealth = 50,
            BaseDamage = 20,
            BaseSpeed = 85f,
            Width = 22,
            Height = 46,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 250f,
            AttackRange = 34f,
            AttackCooldown = 0.9f,
            BaseGold = 15,
            BaseXP = 18,
            Color = (200, 200, 180),
            SpawnWeight = 2f,
            Biomes = new[] { BiomeType.Dungeon },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.DarkCaster,
            Name = "Dark Caster",
            BaseHealth = 40,
            BaseDamage = 18,
            BaseSpeed = 40f,
            Width = 24,
            Height = 44,
            Movement = MovementPattern.Walker,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 400f,
            AttackRange = 350f,
            AttackCooldown = 2.5f,
            KnockbackResistance = 0.2f,
            BaseGold = 18,
            BaseXP = 22,
            Color = (80, 60, 120),
            SpawnWeight = 1f,
            Biomes = new[] { BiomeType.Dungeon },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.CursedSkull,
            Name = "Cursed Skull",
            BaseHealth = 35,
            BaseDamage = 22,
            BaseSpeed = 100f,
            Width = 28,
            Height = 28,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 320f,
            AttackRange = 30f,
            AttackCooldown = 0.7f,
            KnockbackResistance = 1f,
            BaseGold = 14,
            BaseXP = 16,
            Color = (150, 200, 150),
            SpawnWeight = 1.5f,
            Biomes = new[] { BiomeType.Dungeon },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.DungeonSlime,
            Name = "Dungeon Slime",
            BaseHealth = 45,
            BaseDamage = 16,
            BaseSpeed = 50f,
            Width = 26,
            Height = 22,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 180f,
            AttackRange = 24f,
            AttackCooldown = 0.7f,
            BaseGold = 12,
            BaseXP = 14,
            Color = (60, 80, 140),
            SpawnWeight = 1.2f,
            Biomes = new[] { BiomeType.Dungeon },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.SpikeBall,
            Name = "Spike Ball",
            BaseHealth = 200,
            BaseDamage = 30,
            BaseSpeed = 60f,
            Width = 32,
            Height = 32,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 150f,
            AttackRange = 32f,
            AttackCooldown = 0.5f,
            KnockbackResistance = 1f,
            BaseGold = 0,
            BaseXP = 0,
            Color = (100, 100, 100),
            SpawnWeight = 0.5f,
            Biomes = new[] { BiomeType.Dungeon },
            BiomeExclusive = true
        });

        // === MUSHROOM BIOME ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.SporeBat,
            Name = "Spore Bat",
            BaseHealth = 25,
            BaseDamage = 14,
            BaseSpeed = 90f,
            Width = 22,
            Height = 18,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 240f,
            AttackRange = 26f,
            AttackCooldown = 0.6f,
            BaseGold = 7,
            BaseXP = 10,
            Color = (80, 100, 200),
            SpawnWeight = 1.8f,
            Biomes = new[] { BiomeType.Mushroom },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.FungiSwarmer,
            Name = "Fungi Bulb",
            BaseHealth = 18,
            BaseDamage = 10,
            BaseSpeed = 70f,
            Width = 16,
            Height = 16,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 200f,
            AttackRange = 20f,
            AttackCooldown = 0.4f,
            BaseGold = 4,
            BaseXP = 6,
            Color = (60, 80, 180),
            SpawnWeight = 2.5f,
            Biomes = new[] { BiomeType.Mushroom },
            BiomeExclusive = true
        });

        Register(new EnemyData
        {
            Type = EnemyType.GiantFungus,
            Name = "Giant Fungi Bulb",
            BaseHealth = 85,
            BaseDamage = 28,
            BaseSpeed = 40f,
            Width = 36,
            Height = 40,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 220f,
            AttackRange = 36f,
            AttackCooldown = 1.5f,
            KnockbackResistance = 0.5f,
            BaseGold = 25,
            BaseXP = 30,
            Color = (70, 90, 190),
            SpawnWeight = 0.5f,
            Biomes = new[] { BiomeType.Mushroom },
            BiomeExclusive = true,
            RequiresHardmode = true
        });

        // === UNDERGROUND GENERAL ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.CaveSpider,
            Name = "Cave Spider",
            BaseHealth = 22,
            BaseDamage = 15,
            BaseSpeed = 90f,
            Width = 28,
            Height = 16,
            Movement = MovementPattern.Walker,
            DetectionRange = 200f,
            AttackRange = 24f,
            AttackCooldown = 0.5f,
            BaseGold = 7,
            BaseXP = 10,
            Color = (100, 60, 40),
            SpawnWeight = 2f,
            RequiresUnderground = true,
            MinDepth = 30,
            Biomes = new[] { BiomeType.Underground }
        });

        Register(new EnemyData
        {
            Type = EnemyType.Worm,
            Name = "Giant Worm",
            BaseHealth = 35,
            BaseDamage = 18,
            BaseSpeed = 50f,
            Width = 16,
            Height = 32,
            Movement = MovementPattern.Burrower,
            DetectionRange = 120f,
            AttackRange = 20f,
            AttackCooldown = 1.5f,
            KnockbackResistance = 0.5f,
            BaseGold = 12,
            BaseXP = 18,
            Color = (180, 140, 100),
            SpawnWeight = 1f,
            RequiresUnderground = true,
            MinDepth = 50,
            Biomes = new[] { BiomeType.Underground }
        });

        Register(new EnemyData
        {
            Type = EnemyType.Ghost,
            Name = "Ghost",
            BaseHealth = 25,
            BaseDamage = 12,
            BaseSpeed = 45f,
            Width = 24,
            Height = 32,
            Movement = MovementPattern.Floater,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 300f,
            AttackRange = 36f,
            AttackCooldown = 2.0f,
            KnockbackResistance = 1f,
            BaseGold = 15,
            BaseXP = 20,
            Color = (200, 200, 255),
            SpawnWeight = 0.8f,
            RequiresUnderground = true,
            MinDepth = 40,
            Biomes = new[] { BiomeType.Underground, BiomeType.Dungeon }
        });

        // === DANGEROUS/RARE ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.Demon,
            Name = "Demon",
            BaseHealth = 55,
            BaseDamage = 22,
            BaseSpeed = 55f,
            Width = 28,
            Height = 40,
            Movement = MovementPattern.Flyer,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 350f,
            AttackRange = 150f,
            AttackCooldown = 2.5f,
            KnockbackResistance = 0.4f,
            BaseGold = 25,
            BaseXP = 35,
            Color = (180, 40, 40),
            SpawnWeight = 0.5f,
            MinDepth = 80,
            Biomes = new[] { BiomeType.Underworld }
        });

        Register(new EnemyData
        {
            Type = EnemyType.Golem,
            Name = "Stone Golem",
            BaseHealth = 120,
            BaseDamage = 30,
            BaseSpeed = 25f,
            Width = 36,
            Height = 48,
            Movement = MovementPattern.Walker,
            DetectionRange = 150f,
            AttackRange = 40f,
            AttackCooldown = 2.0f,
            KnockbackResistance = 0.8f,
            BaseGold = 40,
            BaseXP = 50,
            Color = (120, 120, 130),
            SpawnWeight = 0.3f,
            MinDepth = 100,
            Biomes = new[] { BiomeType.Underground }
        });

        Register(new EnemyData
        {
            Type = EnemyType.Wraith,
            Name = "Wraith",
            BaseHealth = 80,
            BaseDamage = 35,
            BaseSpeed = 120f,
            Width = 24,
            Height = 36,
            Movement = MovementPattern.Teleporter,
            Aggression = AggressionType.AlwaysAggressive,
            DetectionRange = 400f,
            AttackRange = 28f,
            AttackCooldown = 1.8f,
            KnockbackResistance = 0.6f,
            BaseGold = 45,
            BaseXP = 55,
            Color = (100, 50, 150),
            SpawnWeight = 0.4f,
            MinDepth = 120,
            Biomes = new[] { BiomeType.Underground, BiomeType.Corruption, BiomeType.Crimson },
            RequiresHardmode = true
        });

        // === SPECIAL ENEMIES ===
        Register(new EnemyData
        {
            Type = EnemyType.Mimic,
            Name = "Mimic",
            BaseHealth = 60,
            BaseDamage = 28,
            BaseSpeed = 80f,
            Width = 32,
            Height = 32,
            Movement = MovementPattern.Hopper,
            DetectionRange = 80f,
            AttackRange = 32f,
            AttackCooldown = 0.8f,
            KnockbackResistance = 0.3f,
            BaseGold = 50,
            BaseXP = 45,
            Color = (139, 90, 43),
            SpawnWeight = 0.2f,
            MinDepth = 60,
            Biomes = new[] { BiomeType.Underground, BiomeType.Dungeon }
        });

        Register(new EnemyData
        {
            Type = EnemyType.EliteSlime,
            Name = "Elite Slime",
            BaseHealth = 80,
            BaseDamage = 18,
            BaseSpeed = 50f,
            Width = 40,
            Height = 32,
            Movement = MovementPattern.Hopper,
            Aggression = AggressionType.PassiveUntilDamaged,
            DetectionRange = 200f,
            AttackRange = 28f,
            AttackCooldown = 0.6f,
            KnockbackResistance = 0.5f,
            BaseGold = 20,
            BaseXP = 30,
            Color = (100, 255, 100),
            SpawnWeight = 0.6f,
            RequiresSurface = true,
            Biomes = new[] { BiomeType.Forest }
        });
    }

    private static void Register(EnemyData data)
    {
        _enemies[data.Type] = data;
    }

    public static EnemyData Get(EnemyType type)
    {
        return _enemies.TryGetValue(type, out var data) ? data : _enemies[EnemyType.Slime];
    }

    public static IEnumerable<EnemyData> GetAll() => _enemies.Values;

    public static IEnumerable<EnemyData> GetSpawnableAt(int depth, bool isSurface)
    {
        foreach (var data in _enemies.Values)
        {
            if (depth < data.MinDepth || depth > data.MaxDepth)
                continue;
            if (data.RequiresSurface && !isSurface)
                continue;
            if (data.RequiresUnderground && isSurface)
                continue;

            yield return data;
        }
    }

    /// <summary>
    /// Gets enemies that can spawn in the specified biome.
    /// </summary>
    public static IEnumerable<EnemyData> GetSpawnableInBiome(BiomeType biome, int depth, bool isSurface, bool isHardmode)
    {
        foreach (var data in _enemies.Values)
        {
            // Check hardmode requirement
            if (data.RequiresHardmode && !isHardmode)
                continue;

            // Check depth requirements
            if (depth < data.MinDepth || depth > data.MaxDepth)
                continue;
            if (data.RequiresSurface && !isSurface)
                continue;
            if (data.RequiresUnderground && isSurface)
                continue;

            // Check biome requirements
            if (data.Biomes.Length > 0)
            {
                bool matchesBiome = data.Biomes.Contains(biome);

                // If biome exclusive, must match
                if (data.BiomeExclusive && !matchesBiome)
                    continue;
            }

            yield return data;
        }
    }

    /// <summary>
    /// Gets the spawn weight for an enemy in a specific biome.
    /// </summary>
    public static float GetSpawnWeight(EnemyData data, BiomeType biome)
    {
        float weight = data.SpawnWeight;

        // Apply biome bonus if this is a preferred biome
        if (data.Biomes.Length > 0 && data.Biomes.Contains(biome))
        {
            weight *= data.BiomeSpawnBonus;
        }

        return weight;
    }
}