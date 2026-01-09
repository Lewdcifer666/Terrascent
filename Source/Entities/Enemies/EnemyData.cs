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
        // === SURFACE ENEMIES ===

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
            Aggression = AggressionType.PassiveUntilDamaged,  // Terraria: only aggro when hit
            DetectionRange = 150f,
            AttackRange = 20f,
            AttackCooldown = 0.8f,
            BaseGold = 3,
            BaseXP = 5,
            Color = (50, 200, 50),
            SpawnWeight = 3f,
            RequiresSurface = true
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
            Aggression = AggressionType.ChaseWithRetry,  // Terraria: backs off, retries
            DetectionRange = 180f,
            AttackRange = 28f,
            AttackCooldown = 1.2f,
            KnockbackResistance = 0.3f,
            BaseGold = 8,
            BaseXP = 12,
            Color = (80, 120, 80),
            SpawnWeight = 2f,
            RequiresSurface = true
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
            RequiresSurface = true
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
            Aggression = AggressionType.AlwaysAggressive,  // Always chases
            DetectionRange = 250f,
            AttackRange = 24f,
            AttackCooldown = 0.6f,
            BaseGold = 4,
            BaseXP = 8,
            Color = (60, 40, 80),
            SpawnWeight = 2f
        });

        // === UNDERGROUND ENEMIES ===

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
            MinDepth = 30
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
            MinDepth = 50
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
            Aggression = AggressionType.AlwaysAggressive,  // Always chases
            DetectionRange = 300f,
            AttackRange = 36f,
            AttackCooldown = 2.0f,
            KnockbackResistance = 1f,  // Immune to knockback
            BaseGold = 15,
            BaseXP = 20,
            Color = (200, 200, 255),
            SpawnWeight = 0.8f,
            RequiresUnderground = true,
            MinDepth = 40
        });

        // === DANGEROUS ENEMIES ===

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
            Aggression = AggressionType.AlwaysAggressive,  // Always chases
            DetectionRange = 350f,
            AttackRange = 150f,  // Ranged
            AttackCooldown = 2.5f,
            KnockbackResistance = 0.4f,
            BaseGold = 25,
            BaseXP = 35,
            Color = (180, 40, 40),
            SpawnWeight = 0.5f,
            MinDepth = 80
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
            MinDepth = 100
        });

        Register(new EnemyData
        {
            Type = EnemyType.Wraith,
            Name = "Wraith",
            BaseHealth = 40,
            BaseDamage = 25,
            BaseSpeed = 120f,
            Width = 24,
            Height = 36,
            Movement = MovementPattern.Teleporter,
            Aggression = AggressionType.AlwaysAggressive,  // Always chases
            DetectionRange = 400f,
            AttackRange = 28f,
            AttackCooldown = 1.8f,
            KnockbackResistance = 0.6f,
            BaseGold = 30,
            BaseXP = 40,
            Color = (100, 50, 150),
            SpawnWeight = 0.4f,
            MinDepth = 120
        });

        // === SPECIAL ===

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
            DetectionRange = 80f,  // Short range - ambush
            AttackRange = 32f,
            AttackCooldown = 0.8f,
            KnockbackResistance = 0.3f,
            BaseGold = 50,  // High reward
            BaseXP = 45,
            Color = (139, 90, 43),  // Chest-like color
            SpawnWeight = 0.2f,
            MinDepth = 60
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
            Aggression = AggressionType.PassiveUntilDamaged,  // Like regular slime
            DetectionRange = 200f,
            AttackRange = 28f,
            AttackCooldown = 0.6f,
            KnockbackResistance = 0.5f,
            BaseGold = 20,
            BaseXP = 30,
            Color = (100, 255, 100),
            SpawnWeight = 0.6f,
            RequiresSurface = true
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
}