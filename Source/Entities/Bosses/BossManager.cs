using Microsoft.Xna.Framework;
using Terrascent.Economy;
using Terrascent.Entities.Drops;
using Terrascent.Entities.Enemies;
using Terrascent.Items;
using Terrascent.World;

namespace Terrascent.Entities.Bosses;

/// <summary>
/// Manages active bosses including spawning, updating, combat, and loot.
/// </summary>
public class BossManager
{
    private readonly List<Boss> _bosses = new();
    private readonly DifficultyManager _difficulty;
    private readonly DropManager _dropManager;
    private readonly EnemyManager _enemyManager;
    private readonly Random _random;

    // Track defeated bosses for progression
    private readonly HashSet<BossType> _defeatedBosses = new();

    // Currently active boss (for UI)
    public Boss? ActiveBoss => _bosses.FirstOrDefault(b => !b.IsDead);
    public bool HasActiveBoss => ActiveBoss != null;

    // Boss announcement
    private string _bossAnnouncement = "";
    private float _announcementTimer;
    private const float ANNOUNCEMENT_DURATION = 4f;
    public string CurrentAnnouncement => _announcementTimer > 0 ? _bossAnnouncement : "";

    // Events
    public event Action<Boss>? OnBossSpawned;
    public event Action<Boss>? OnBossDefeated;
    public event Action<Boss, BossPhase>? OnBossPhaseChanged;
    public event Action<Boss>? OnBossEnraged;
    public event Action<Boss>? OnBossDespawned;
    public event Action<ItemType, int, Vector2>? OnBossItemDropped;

    /// <summary>
    /// Fired when a hardmode-unlocking boss is defeated (Wall of Shadows).
    /// Subscribe to this event to trigger hardmode world transformation.
    /// </summary>
    public event Action? OnHardmodeTriggered;

    public BossManager(DifficultyManager difficulty, DropManager dropManager, EnemyManager enemyManager, int seed)
    {
        _difficulty = difficulty;
        _dropManager = dropManager;
        _enemyManager = enemyManager;
        _random = new Random(seed);
    }

    /// <summary>
    /// Update all active bosses.
    /// </summary>
    public void Update(float deltaTime, Player player, ChunkManager chunks)
    {
        // Update announcement timer
        if (_announcementTimer > 0)
        {
            _announcementTimer -= deltaTime;
        }

        // Update all bosses
        for (int i = _bosses.Count - 1; i >= 0; i--)
        {
            var boss = _bosses[i];

            // Remove fully dead bosses
            if (boss.IsFullyDead)
            {
                _bosses.RemoveAt(i);
                continue;
            }

            // Remove despawned bosses
            if (boss.ShouldDespawn)
            {
                OnBossDespawned?.Invoke(boss);
                _bosses.RemoveAt(i);
                continue;
            }

            // Update boss
            boss.SetTarget(player);
            boss.Update(deltaTime);
            boss.ApplyMovement(deltaTime, chunks);

            // Check boss contact damage
            if (!boss.IsDead && boss.CanDamageOnContact(player.Hitbox))
            {
                DamagePlayer(boss, player, boss.GetContactDamage());
            }

            // Check boss attack damage
            var attackHitbox = boss.GetAttackHitbox();
            if (attackHitbox.HasValue && attackHitbox.Value.Intersects(player.Hitbox))
            {
                DamagePlayer(boss, player, boss.GetAttackDamage());
            }
        }
    }

    /// <summary>
    /// Summon a boss at the specified position.
    /// </summary>
    public Boss? SummonBoss(BossType type, Vector2 position)
    {
        // Check if already fighting this boss type
        if (_bosses.Any(b => b.BossType == type && !b.IsDead))
        {
            Console.WriteLine($"[BOSS] Already fighting {type}!");
            return null;
        }

        // Create the boss
        var boss = new Boss(type, position, _difficulty, _random.Next());

        // Subscribe to boss events
        boss.OnPhaseChanged += (b, phase) =>
        {
            OnBossPhaseChanged?.Invoke(b, phase);
        };

        boss.OnEnraged += b =>
        {
            SetAnnouncement($"{b.Data.Name} has ENRAGED!");
            OnBossEnraged?.Invoke(b);
        };

        boss.OnDeath += b =>
        {
            HandleBossDefeat(b);
        };

        boss.OnMinionSpawn += b =>
        {
            SpawnBossMinion(b);
        };

        _bosses.Add(boss);

        // Announcement
        SetAnnouncement($"{boss.Data.Name} {boss.Data.Title}");

        OnBossSpawned?.Invoke(boss);
        Console.WriteLine($"[BOSS] {boss.Data.Name} has been summoned!");

        return boss;
    }

    /// <summary>
    /// Try to use a summoning item to spawn a boss.
    /// </summary>
    public bool TrySummonWithItem(ItemType itemType, Vector2 playerPosition, float worldTime, int playerDepth)
    {
        // Get the boss type for this item
        BossType? bossType = GetBossForItem(itemType);
        if (!bossType.HasValue) return false;

        var bossData = BossRegistry.Get(bossType.Value);

        // Check spawn conditions
        if (!CheckSpawnConditions(bossData, worldTime, playerDepth))
        {
            Console.WriteLine($"[BOSS] Cannot summon {bossData.Name} - conditions not met!");
            return false;
        }

        // Spawn position above/in front of player
        Vector2 spawnPos = playerPosition + new Vector2(0, -200);

        // Summon the boss
        var boss = SummonBoss(bossType.Value, spawnPos);
        return boss != null;
    }

    /// <summary>
    /// Get the boss type associated with a summoning item.
    /// </summary>
    private static BossType? GetBossForItem(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.SuspiciousLookingEye => BossType.EyeOfTerror,
            ItemType.SlimeCrown => BossType.KingSlime,
            ItemType.BloodySpine => BossType.BrainOfDepths,
            ItemType.AncientSkull => BossType.SkeletalWarlord,
            ItemType.Abeemination => BossType.QueenBee,
            ItemType.GuideVoodooDoll => BossType.WallOfShadows,
            _ => null
        };
    }

    /// <summary>
    /// Check if spawn conditions are met for a boss.
    /// </summary>
    private bool CheckSpawnConditions(BossData data, float worldTime, int playerDepth)
    {
        // TODO: Implement proper day/night cycle
        bool isNight = false;  // worldTime >= 0.75f || worldTime < 0.25f;
        bool isSurface = playerDepth < 50;
        bool isUnderground = playerDepth >= 50;

        return data.SpawnCondition switch
        {
            BossSpawnCondition.None => true,
            BossSpawnCondition.Nighttime => isNight,
            BossSpawnCondition.Daytime => !isNight,
            BossSpawnCondition.Surface => isSurface,
            BossSpawnCondition.Underground => isUnderground,
            // TODO: Implement biome checks
            BossSpawnCondition.Jungle => true,  // Placeholder
            BossSpawnCondition.Underworld => playerDepth > 300,
            BossSpawnCondition.Dungeon => true,  // Placeholder
            _ => true
        };
    }

    /// <summary>
    /// Handle boss defeat - drops, progression, etc.
    /// </summary>
    private void HandleBossDefeat(Boss boss)
    {
        // Mark as defeated
        _defeatedBosses.Add(boss.BossType);

        // Spawn loot
        SpawnBossLoot(boss);

        // Announcement
        SetAnnouncement($"{boss.Data.Name} has been defeated!");

        // Check for hardmode unlock
        if (boss.Data.UnlocksHardmode)
        {
            Console.WriteLine("[WORLD] HARDMODE UNLOCKED! Triggering world transformation...");
            OnHardmodeTriggered?.Invoke();
        }

        OnBossDefeated?.Invoke(boss);
    }

    /// <summary>
    /// Spawn boss loot drops.
    /// </summary>
    private void SpawnBossLoot(Boss boss)
    {
        Vector2 dropPos = boss.Center;

        // Drop gold
        _dropManager.SpawnGoldDrop(dropPos, boss.GoldReward);

        // Drop XP
        _dropManager.SpawnXPDrop(dropPos, boss.XPReward);

        // Roll loot table
        foreach (var loot in boss.Data.LootTable)
        {
            if (loot.Guaranteed || _random.NextSingle() <= loot.DropChance)
            {
                int count = _random.Next(loot.MinCount, loot.MaxCount + 1);

                // Fire event for each item drop (main game handles adding to inventory)
                OnBossItemDropped?.Invoke(loot.Item, count, dropPos);

                Console.WriteLine($"[LOOT] {boss.Data.Name} dropped {count}x {loot.Item}");
            }
        }
    }

    /// <summary>
    /// Spawn a minion for a boss.
    /// </summary>
    private void SpawnBossMinion(Boss boss)
    {
        if (!boss.Data.MinionType.HasValue) return;

        // Spawn position near boss
        float angle = _random.NextSingle() * MathF.PI * 2f;
        float distance = 50f + _random.NextSingle() * 50f;
        Vector2 spawnPos = boss.Center + new Vector2(
            MathF.Cos(angle) * distance,
            MathF.Sin(angle) * distance
        );

        _enemyManager.SpawnEnemy(boss.Data.MinionType.Value, spawnPos);
        Console.WriteLine($"[BOSS] {boss.Data.Name} spawned a minion!");
    }

    /// <summary>
    /// Damage a player from boss attack.
    /// </summary>
    private void DamagePlayer(Boss boss, Player player, int damage)
    {
        bool died = player.TakeDamage(damage, boss.Center);

        if (died)
        {
            Console.WriteLine($"[BOSS] Player killed by {boss.Data.Name}!");
        }
    }

    /// <summary>
    /// Damage bosses in an area from player attack.
    /// </summary>
    public void DamageBoss(Rectangle attackBox, int damage, float knockback, Vector2 attackerPosition)
    {
        foreach (var boss in _bosses)
        {
            if (boss.IsDead) continue;
            if (!attackBox.Intersects(boss.Hitbox)) continue;

            Vector2 knockDir = boss.Center - attackerPosition;
            if (knockDir.Length() > 0.1f)
                knockDir.Normalize();
            else
                knockDir = Vector2.UnitX;

            boss.TakeDamage(damage, knockDir, knockback);
        }
    }

    /// <summary>
    /// Set the current announcement message.
    /// </summary>
    private void SetAnnouncement(string message)
    {
        _bossAnnouncement = message;
        _announcementTimer = ANNOUNCEMENT_DURATION;
    }

    /// <summary>
    /// Check if a boss type has been defeated.
    /// </summary>
    public bool IsBossDefeated(BossType type)
    {
        return _defeatedBosses.Contains(type);
    }

    /// <summary>
    /// Get all active bosses for rendering.
    /// </summary>
    public IEnumerable<Boss> GetBosses() => _bosses;

    /// <summary>
    /// Clear all bosses (for world reset).
    /// </summary>
    public void Clear()
    {
        _bosses.Clear();
        _defeatedBosses.Clear();
        _announcementTimer = 0f;
    }

    /// <summary>
    /// Save boss defeat state.
    /// </summary>
    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(_defeatedBosses.Count);
        foreach (var type in _defeatedBosses)
        {
            writer.Write((int)type);
        }
    }

    /// <summary>
    /// Load boss defeat state.
    /// </summary>
    public void LoadFrom(BinaryReader reader)
    {
        _defeatedBosses.Clear();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            _defeatedBosses.Add((BossType)reader.ReadInt32());
        }
    }
}
