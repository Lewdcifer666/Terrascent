namespace Terrascent.Progression;

/// <summary>
/// Registry of all available upgrades for level-up choices.
/// Contains 30+ upgrades organized by category with synergy relationships.
/// </summary>
public static class UpgradeRegistry
{
    private static readonly Dictionary<string, Upgrade> _upgrades = new();
    private static readonly List<Upgrade> _allUpgrades = new();

    static UpgradeRegistry()
    {
        RegisterAllUpgrades();
    }

    /// <summary>Get an upgrade by ID.</summary>
    public static Upgrade? Get(string id) => _upgrades.GetValueOrDefault(id);

    /// <summary>Get all registered upgrades.</summary>
    public static IReadOnlyList<Upgrade> GetAll() => _allUpgrades;

    /// <summary>Get upgrades by category.</summary>
    public static IEnumerable<Upgrade> GetByCategory(UpgradeCategory category)
        => _allUpgrades.Where(u => u.Category == category);

    /// <summary>Get upgrades by rarity.</summary>
    public static IEnumerable<Upgrade> GetByRarity(UpgradeRarity rarity)
        => _allUpgrades.Where(u => u.Rarity == rarity);

    private static void Register(Upgrade upgrade)
    {
        _upgrades[upgrade.Id] = upgrade;
        _allUpgrades.Add(upgrade);
    }

    private static void RegisterAllUpgrades()
    {
        // ===========================================
        // OFFENSE UPGRADES (Damage, Crit, Attack Speed)
        // ===========================================

        Register(new Upgrade
        {
            Id = "might",
            Name = "Might",
            Description = "Increases damage by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 5,
            StatType = UpgradeStatType.DamagePercent,
            Value = 0.10f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "power_surge",
            Name = "Power Surge",
            Description = "Increases damage by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 3,
            StatType = UpgradeStatType.DamagePercent,
            Value = 0.20f,
            IsPercent = true,
            SynergyWith = new[] { "might", "critical_eye" }
        });

        Register(new Upgrade
        {
            Id = "critical_eye",
            Name = "Critical Eye",
            Description = "Increases critical chance by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 10,
            StatType = UpgradeStatType.CritChance,
            Value = 0.05f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "deadly_strikes",
            Name = "Deadly Strikes",
            Description = "Increases critical damage by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.CritDamage,
            Value = 0.25f,
            IsPercent = true,
            SynergyWith = new[] { "critical_eye" }
        });

        Register(new Upgrade
        {
            Id = "swift_attacks",
            Name = "Swift Attacks",
            Description = "Increases attack speed by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 5,
            StatType = UpgradeStatType.AttackSpeedPercent,
            Value = 0.10f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "berserker",
            Name = "Berserker",
            Description = "Increases attack speed by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 3,
            StatType = UpgradeStatType.AttackSpeedPercent,
            Value = 0.25f,
            IsPercent = true,
            SynergyWith = new[] { "swift_attacks", "might" },
            MinLevel = 10
        });

        Register(new Upgrade
        {
            Id = "flat_damage",
            Name = "Sharpened Blade",
            Description = "Increases base damage by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 10,
            StatType = UpgradeStatType.FlatDamage,
            Value = 5f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "executioner",
            Name = "Executioner",
            Description = "Critical hits deal {0} more damage.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Epic,
            BaseWeight = 15f,
            MaxStacks = 3,
            StatType = UpgradeStatType.CritDamage,
            Value = 0.50f,
            IsPercent = true,
            SynergyWith = new[] { "critical_eye", "deadly_strikes" },
            RequiredUpgrades = new[] { "critical_eye" },
            MinLevel = 15
        });

        // ===========================================
        // DEFENSE UPGRADES (Health, Armor, Block)
        // ===========================================

        Register(new Upgrade
        {
            Id = "vitality",
            Name = "Vitality",
            Description = "Increases max health by {0}.",
            Category = UpgradeCategory.Defense,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 10,
            StatType = UpgradeStatType.MaxHealth,
            Value = 20f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "iron_skin",
            Name = "Iron Skin",
            Description = "Increases armor by {0}.",
            Category = UpgradeCategory.Defense,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 10,
            StatType = UpgradeStatType.Armor,
            Value = 5f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "shield_mastery",
            Name = "Shield Mastery",
            Description = "Increases block chance by {0}.",
            Category = UpgradeCategory.Defense,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.BlockChance,
            Value = 0.05f,
            IsPercent = true,
            SynergyWith = new[] { "iron_skin" }
        });

        Register(new Upgrade
        {
            Id = "evasion",
            Name = "Evasion",
            Description = "Increases dodge chance by {0}.",
            Category = UpgradeCategory.Defense,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.DodgeChance,
            Value = 0.05f,
            IsPercent = true,
            SynergyWith = new[] { "swift_movement" }
        });

        Register(new Upgrade
        {
            Id = "fortitude",
            Name = "Fortitude",
            Description = "Increases max health by {0}.",
            Category = UpgradeCategory.Defense,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 3,
            StatType = UpgradeStatType.MaxHealth,
            Value = 50f,
            IsPercent = false,
            SynergyWith = new[] { "vitality", "iron_skin" },
            MinLevel = 10
        });

        Register(new Upgrade
        {
            Id = "juggernaut",
            Name = "Juggernaut",
            Description = "Increases armor by {0}. Reduces move speed by 5%.",
            Category = UpgradeCategory.Defense,
            Rarity = UpgradeRarity.Epic,
            BaseWeight = 15f,
            MaxStacks = 3,
            StatType = UpgradeStatType.Armor,
            Value = 20f,
            IsPercent = false,
            SynergyWith = new[] { "iron_skin", "fortitude" },
            MinLevel = 15
        });

        // ===========================================
        // MOBILITY UPGRADES (Speed, Jumps)
        // ===========================================

        Register(new Upgrade
        {
            Id = "swift_movement",
            Name = "Swift Movement",
            Description = "Increases movement speed by {0}.",
            Category = UpgradeCategory.Mobility,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 5,
            StatType = UpgradeStatType.MoveSpeedPercent,
            Value = 0.10f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "wings",
            Name = "Wings",
            Description = "Gain {0} extra jump(s).",
            Category = UpgradeCategory.Mobility,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 2,
            StatType = UpgradeStatType.ExtraJump,
            Value = 1f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "wind_runner",
            Name = "Wind Runner",
            Description = "Increases movement speed by {0}.",
            Category = UpgradeCategory.Mobility,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 3,
            StatType = UpgradeStatType.MoveSpeedPercent,
            Value = 0.15f,
            IsPercent = true,
            SynergyWith = new[] { "swift_movement" }
        });

        Register(new Upgrade
        {
            Id = "dash_master",
            Name = "Dash Master",
            Description = "Increases movement speed by {0} and dodge chance by 3%.",
            Category = UpgradeCategory.Mobility,
            Rarity = UpgradeRarity.Epic,
            BaseWeight = 15f,
            MaxStacks = 2,
            StatType = UpgradeStatType.MoveSpeedPercent,
            Value = 0.20f,
            IsPercent = true,
            SynergyWith = new[] { "swift_movement", "evasion" },
            MinLevel = 15
        });

        // ===========================================
        // SURVIVAL UPGRADES (Regen, Lifesteal)
        // ===========================================

        Register(new Upgrade
        {
            Id = "regeneration",
            Name = "Regeneration",
            Description = "Increases health regen by {0} per second.",
            Category = UpgradeCategory.Survival,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 5,
            StatType = UpgradeStatType.HealthRegen,
            Value = 1f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "vampirism",
            Name = "Vampirism",
            Description = "Gain {0} life steal.",
            Category = UpgradeCategory.Survival,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.LifeSteal,
            Value = 0.03f,
            IsPercent = true,
            SynergyWith = new[] { "critical_eye" }
        });

        Register(new Upgrade
        {
            Id = "blood_feast",
            Name = "Blood Feast",
            Description = "Gain {0} life steal.",
            Category = UpgradeCategory.Survival,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 3,
            StatType = UpgradeStatType.LifeSteal,
            Value = 0.05f,
            IsPercent = true,
            SynergyWith = new[] { "vampirism" },
            MinLevel = 10
        });

        Register(new Upgrade
        {
            Id = "phoenix_blessing",
            Name = "Phoenix Blessing",
            Description = "Increases health regen by {0} per second.",
            Category = UpgradeCategory.Survival,
            Rarity = UpgradeRarity.Epic,
            BaseWeight = 15f,
            MaxStacks = 2,
            StatType = UpgradeStatType.HealthRegen,
            Value = 5f,
            IsPercent = false,
            SynergyWith = new[] { "regeneration", "vitality" },
            MinLevel = 20
        });

        // ===========================================
        // ON-KILL UPGRADES
        // ===========================================

        Register(new Upgrade
        {
            Id = "soul_harvest",
            Name = "Soul Harvest",
            Description = "Heal {0} HP on kill.",
            Category = UpgradeCategory.OnKill,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.OnKillHeal,
            Value = 5f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "gold_rush",
            Name = "Gold Rush",
            Description = "Gain {0} extra gold on kill.",
            Category = UpgradeCategory.OnKill,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.OnKillGold,
            Value = 2f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "chain_reaction",
            Name = "Chain Reaction",
            Description = "Kills have a {0} chance to explode.",
            Category = UpgradeCategory.OnKill,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 5,
            StatType = UpgradeStatType.OnKillExplode,
            Value = 0.10f,
            IsPercent = true,
            SynergyWith = new[] { "soul_harvest" },
            MinLevel = 10
        });

        // ===========================================
        // ON-HIT UPGRADES
        // ===========================================

        Register(new Upgrade
        {
            Id = "leech",
            Name = "Leech",
            Description = "Heal {0} HP on hit.",
            Category = UpgradeCategory.OnHit,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.OnHitHeal,
            Value = 2f,
            IsPercent = false
        });

        Register(new Upgrade
        {
            Id = "static_discharge",
            Name = "Static Discharge",
            Description = "{0} chance to chain lightning on hit.",
            Category = UpgradeCategory.OnHit,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 5,
            StatType = UpgradeStatType.OnHitChainLightning,
            Value = 0.10f,
            IsPercent = true,
            MinLevel = 10
        });

        Register(new Upgrade
        {
            Id = "burning_strikes",
            Name = "Burning Strikes",
            Description = "{0} chance to burn enemies on hit.",
            Category = UpgradeCategory.OnHit,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.OnHitBurn,
            Value = 0.15f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "frost_touch",
            Name = "Frost Touch",
            Description = "{0} chance to freeze enemies on hit.",
            Category = UpgradeCategory.OnHit,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 5,
            StatType = UpgradeStatType.OnHitFreeze,
            Value = 0.08f,
            IsPercent = true,
            MinLevel = 10
        });

        // ===========================================
        // UTILITY UPGRADES (XP, Gold, Luck)
        // ===========================================

        Register(new Upgrade
        {
            Id = "growth",
            Name = "Growth",
            Description = "Increases XP gain by {0}.",
            Category = UpgradeCategory.Utility,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 5,
            StatType = UpgradeStatType.XPGain,
            Value = 0.10f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "greed",
            Name = "Greed",
            Description = "Increases gold gain by {0}.",
            Category = UpgradeCategory.Utility,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 5,
            StatType = UpgradeStatType.GoldGain,
            Value = 0.10f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "magnet",
            Name = "Magnet",
            Description = "Increases pickup radius by {0}.",
            Category = UpgradeCategory.Utility,
            Rarity = UpgradeRarity.Common,
            BaseWeight = 100f,
            MaxStacks = 5,
            StatType = UpgradeStatType.PickupRadius,
            Value = 0.20f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "lucky_star",
            Name = "Lucky Star",
            Description = "Gain {0} luck bonus.",
            Category = UpgradeCategory.Utility,
            Rarity = UpgradeRarity.Rare,
            BaseWeight = 30f,
            MaxStacks = 3,
            StatType = UpgradeStatType.LuckBonus,
            Value = 1f,
            IsPercent = false,
            MinLevel = 10
        });

        Register(new Upgrade
        {
            Id = "cooldown_master",
            Name = "Cooldown Master",
            Description = "Reduces all cooldowns by {0}.",
            Category = UpgradeCategory.Utility,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.CooldownReduction,
            Value = 0.05f,
            IsPercent = true
        });

        // ===========================================
        // WEAPON-SPECIFIC UPGRADES
        // ===========================================

        Register(new Upgrade
        {
            Id = "melee_mastery",
            Name = "Melee Mastery",
            Description = "Increases melee damage by {0}.",
            Category = UpgradeCategory.Weapon,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.MeleeDamage,
            Value = 0.15f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "ranged_mastery",
            Name = "Ranged Mastery",
            Description = "Increases ranged damage by {0}.",
            Category = UpgradeCategory.Weapon,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.RangedDamage,
            Value = 0.15f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "magic_mastery",
            Name = "Magic Mastery",
            Description = "Increases magic damage by {0}.",
            Category = UpgradeCategory.Weapon,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.MagicDamage,
            Value = 0.15f,
            IsPercent = true
        });

        Register(new Upgrade
        {
            Id = "area_expert",
            Name = "Area Expert",
            Description = "Increases area of effect by {0}.",
            Category = UpgradeCategory.Weapon,
            Rarity = UpgradeRarity.Uncommon,
            BaseWeight = 60f,
            MaxStacks = 5,
            StatType = UpgradeStatType.AreaOfEffect,
            Value = 0.10f,
            IsPercent = true
        });

        // ===========================================
        // LEGENDARY UPGRADES
        // ===========================================

        Register(new Upgrade
        {
            Id = "glass_cannon",
            Name = "Glass Cannon",
            Description = "Doubles damage but halves max health.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Legendary,
            BaseWeight = 5f,
            MaxStacks = 1,
            StatType = UpgradeStatType.DamagePercent,
            Value = 1.0f,
            IsPercent = true,
            MinLevel = 20
        });

        Register(new Upgrade
        {
            Id = "immortal",
            Name = "Immortal",
            Description = "Increases max health by {0}.",
            Category = UpgradeCategory.Defense,
            Rarity = UpgradeRarity.Legendary,
            BaseWeight = 5f,
            MaxStacks = 1,
            StatType = UpgradeStatType.MaxHealth,
            Value = 200f,
            IsPercent = false,
            SynergyWith = new[] { "vitality", "fortitude" },
            MinLevel = 25
        });

        Register(new Upgrade
        {
            Id = "time_warp",
            Name = "Time Warp",
            Description = "Increases attack speed by {0}.",
            Category = UpgradeCategory.Offense,
            Rarity = UpgradeRarity.Legendary,
            BaseWeight = 5f,
            MaxStacks = 1,
            StatType = UpgradeStatType.AttackSpeedPercent,
            Value = 0.50f,
            IsPercent = true,
            SynergyWith = new[] { "swift_attacks", "berserker" },
            MinLevel = 25
        });

        Register(new Upgrade
        {
            Id = "clover",
            Name = "Four-Leaf Clover",
            Description = "Gain {0} luck bonus.",
            Category = UpgradeCategory.Utility,
            Rarity = UpgradeRarity.Legendary,
            BaseWeight = 5f,
            MaxStacks = 1,
            StatType = UpgradeStatType.LuckBonus,
            Value = 3f,
            IsPercent = false,
            SynergyWith = new[] { "lucky_star" },
            MinLevel = 20
        });
    }
}