namespace Terrascent.Items.Effects;

/// <summary>
/// Defines a stackable item with one or more effects.
/// These are the "Risk of Rain style" items.
/// </summary>
public class StackableItem
{
    /// <summary>The item type this data belongs to.</summary>
    public ItemType ItemType { get; init; }

    /// <summary>Display name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Flavor text/lore.</summary>
    public string Lore { get; init; } = "";

    /// <summary>Rarity tier.</summary>
    public ItemRarity Rarity { get; init; }

    /// <summary>All effects this item provides.</summary>
    public List<ItemEffect> Effects { get; init; } = new();

    /// <summary>Is this item passive (always active) or active (must use)?</summary>
    public bool IsPassive { get; init; } = true;

    /// <summary>Cooldown for active items.</summary>
    public float ActiveCooldown { get; init; } = 0f;
}

/// <summary>
/// Registry of all stackable items and their effects.
/// </summary>
public static class StackableItemRegistry
{
    private static readonly Dictionary<ItemType, StackableItem> _items = new();

    static StackableItemRegistry()
    {
        RegisterItems();
    }

    /// <summary>
    /// Get stackable item data. Returns null if not a stackable item.
    /// </summary>
    public static StackableItem? Get(ItemType type)
    {
        return _items.GetValueOrDefault(type);
    }

    /// <summary>
    /// Check if an item type is a stackable effect item.
    /// </summary>
    public static bool IsStackable(ItemType type)
    {
        return _items.ContainsKey(type);
    }

    /// <summary>
    /// Get all items of a specific rarity.
    /// </summary>
    public static IEnumerable<StackableItem> GetByRarity(ItemRarity rarity)
    {
        return _items.Values.Where(i => i.Rarity == rarity);
    }

    private static void RegisterItems()
    {
        // ============================================
        // COMMON ITEMS (White) - Simple stat boosts
        // ============================================

        Register(new StackableItem
        {
            ItemType = ItemType.SoldiersSyringeItem,
            Name = "Soldier's Syringe",
            Lore = "A potent combat stimulant.",
            Rarity = ItemRarity.Common,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "syringe_atkspd",
                    Name = "Attack Speed",
                    Description = "Increases attack speed by {0}.",
                    Type = EffectType.AttackSpeedMult,
                    StackType = StackType.Linear,
                    BaseValue = 0.15f,
                    StackValue = 0.15f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.TougherTimesItem,
            Name = "Tougher Times",
            Lore = "Sometimes, you just get lucky.",
            Rarity = ItemRarity.Common,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "toughertimes_block",
                    Name = "Block Chance",
                    Description = "{0} chance to block incoming damage.",
                    Type = EffectType.BlockChance,
                    StackType = StackType.Hyperbolic,
                    Coefficient = 0.15f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.BisonSteakItem,
            Name = "Bison Steak",
            Lore = "A hearty meal for a hearty adventurer.",
            Rarity = ItemRarity.Common,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "bison_hp",
                    Name = "Max Health",
                    Description = "Increases maximum health by {0}.",
                    Type = EffectType.MaxHealth,
                    StackType = StackType.Linear,
                    BaseValue = 25f,
                    StackValue = 25f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.PaulsGoatHoofItem,
            Name = "Paul's Goat Hoof",
            Lore = "Increases mobility.",
            Rarity = ItemRarity.Common,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "hoof_speed",
                    Name = "Move Speed",
                    Description = "Increases movement speed by {0}.",
                    Type = EffectType.MoveSpeedMult,
                    StackType = StackType.Linear,
                    BaseValue = 0.14f,
                    StackValue = 0.14f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.CritGlassesItem,
            Name = "Lens-Maker's Glasses",
            Lore = "Precision engineered for maximum damage.",
            Rarity = ItemRarity.Common,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "glasses_crit",
                    Name = "Critical Chance",
                    Description = "{0} chance to critically strike.",
                    Type = EffectType.CritChance,
                    StackType = StackType.Linear,
                    BaseValue = 0.10f,
                    StackValue = 0.10f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.MonsterToothItem,
            Name = "Monster Tooth",
            Lore = "The remains of a defeated foe.",
            Rarity = ItemRarity.Common,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "tooth_onkill",
                    Name = "Heal On Kill",
                    Description = "Killing an enemy heals for {0} HP.",
                    Type = EffectType.OnKillHeal,
                    StackType = StackType.Linear,
                    BaseValue = 8f,
                    StackValue = 8f,
                }
            }
        });

        // ============================================
        // UNCOMMON ITEMS (Green) - More powerful effects
        // ============================================

        Register(new StackableItem
        {
            ItemType = ItemType.HopooFeatherItem,
            Name = "Hopoo Feather",
            Lore = "Feather of the legendary Hopoo bird.",
            Rarity = ItemRarity.Uncommon,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "feather_jump",
                    Name = "Extra Jump",
                    Description = "Gain {0} extra jump(s).",
                    Type = EffectType.ExtraJump,
                    StackType = StackType.Linear,
                    BaseValue = 1f,
                    StackValue = 1f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.PredatoryInstinctsItem,
            Name = "Predatory Instincts",
            Lore = "Your reflexes sharpen with each kill.",
            Rarity = ItemRarity.Uncommon,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "instinct_crit",
                    Name = "Crit Bonus",
                    Description = "Critical strikes increase attack speed by {0} for 3s. Stacks up to 3 times.",
                    Type = EffectType.AttackSpeedMult,
                    StackType = StackType.Linear,
                    BaseValue = 0.12f,
                    StackValue = 0.12f,
                    ProcChance = 1f, // Procs on crit
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.HarvestersScytheItem,
            Name = "Harvester's Scythe",
            Lore = "Reap the life force of your enemies.",
            Rarity = ItemRarity.Uncommon,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "scythe_critcrit",
                    Name = "Crit Chance",
                    Description = "Gain {0} critical chance.",
                    Type = EffectType.CritChance,
                    StackType = StackType.Flat,
                    BaseValue = 0.05f,
                },
                new ItemEffect
                {
                    Id = "scythe_heal",
                    Name = "Crit Heal",
                    Description = "Critical strikes heal for {0} HP.",
                    Type = EffectType.OnHitHeal,
                    StackType = StackType.Linear,
                    BaseValue = 8f,
                    StackValue = 4f,
                    ProcChance = 1f, // Only on crit (handled in combat)
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.UkuleleItem,
            Name = "Ukulele",
            Lore = "...and his music was electric.",
            Rarity = ItemRarity.Uncommon,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "ukulele_chain",
                    Name = "Chain Lightning",
                    Description = "{0} chance to chain lightning to nearby enemies for 80% damage.",
                    Type = EffectType.OnHitChain,
                    StackType = StackType.Hyperbolic,
                    Coefficient = 0.25f,
                    ProcChance = 0.25f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.AtgMissileItem,
            Name = "ATG Missile Mk. 1",
            Lore = "Fire and forget.",
            Rarity = ItemRarity.Uncommon,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "atg_missile",
                    Name = "Missile",
                    Description = "{0} chance to fire a missile for 300% damage.",
                    Type = EffectType.OnHitExplode,
                    StackType = StackType.Hyperbolic,
                    Coefficient = 0.10f,
                    ProcChance = 0.10f,
                }
            }
        });

        // ============================================
        // RARE ITEMS (Blue/Red) - Powerful and build-defining
        // ============================================

        Register(new StackableItem
        {
            ItemType = ItemType.BrilliantBehemothItem,
            Name = "Brilliant Behemoth",
            Lore = "Everything explodes.",
            Rarity = ItemRarity.Rare,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "behemoth_explode",
                    Name = "Explosive Attacks",
                    Description = "All attacks explode for {0} bonus damage in a 4m radius.",
                    Type = EffectType.OnHitExplode,
                    StackType = StackType.Linear,
                    BaseValue = 0.60f,  // 60% base damage as explosion
                    StackValue = 0.60f,
                    ProcChance = 1f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.ShapedGlassItem,
            Name = "Shaped Glass",
            Lore = "Double-edged and fragile.",
            Rarity = ItemRarity.Rare, // Actually Lunar in RoR2
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "glass_damage",
                    Name = "Damage Multiplier",
                    Description = "Increase base damage by {0}.",
                    Type = EffectType.DamageMult,
                    StackType = StackType.Exponential,
                    BaseValue = 2f, // 2x per stack
                },
                new ItemEffect
                {
                    Id = "glass_hp",
                    Name = "Health Reduction",
                    Description = "Reduce maximum health by 50%.",
                    Type = EffectType.MaxHealth,
                    StackType = StackType.Exponential,
                    BaseValue = 0.5f, // Halve HP per stack
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.CestiusItem,
            Name = "Cestus",
            Lore = "Ancient brass knuckles of power.",
            Rarity = ItemRarity.Rare,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "cestus_stun",
                    Name = "Stun On Hit",
                    Description = "{0} chance to stun enemies for 2 seconds.",
                    Type = EffectType.OnHitFreeze,
                    StackType = StackType.Hyperbolic,
                    Coefficient = 0.05f,
                    ProcChance = 0.05f,
                }
            }
        });

        // ============================================
        // LEGENDARY ITEMS (Orange) - Game-changing
        // ============================================

        Register(new StackableItem
        {
            ItemType = ItemType.SoulboundCatalystItem,
            Name = "Soulbound Catalyst",
            Lore = "Bound to your very essence.",
            Rarity = ItemRarity.Legendary,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "catalyst_cooldown",
                    Name = "Cooldown Reduction",
                    Description = "Killing an enemy reduces all cooldowns by {0} seconds.",
                    Type = EffectType.OnKillHeal, // Repurposed for cooldown
                    StackType = StackType.Linear,
                    BaseValue = 4f,
                    StackValue = 2f,
                }
            }
        });

        Register(new StackableItem
        {
            ItemType = ItemType.FiftySevenLeafCloverItem,
            Name = "57 Leaf Clover",
            Lore = "Luck is on your side.",
            Rarity = ItemRarity.Legendary,
            Effects = new()
            {
                new ItemEffect
                {
                    Id = "clover_luck",
                    Name = "Luck",
                    Description = "All random effects roll {0} additional time(s) for a favorable outcome.",
                    Type = EffectType.LuckBonus,
                    StackType = StackType.Linear,
                    BaseValue = 1f,
                    StackValue = 1f,
                }
            }
        });
    }

    private static void Register(StackableItem item)
    {
        _items[item.ItemType] = item;
    }
}