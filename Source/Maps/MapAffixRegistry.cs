namespace Terrascent.Maps;

/// <summary>
/// Registry of all map affixes (22+ defined).
/// Provides weighted random selection for map rolling.
/// </summary>
public static class MapAffixRegistry
{
    private static readonly Dictionary<string, MapAffix> _affixes = new();
    private static readonly List<MapAffix> _prefixes = new();
    private static readonly List<MapAffix> _suffixes = new();

    static MapAffixRegistry()
    {
        RegisterAllAffixes();
    }

    private static void RegisterAllAffixes()
    {
        // === PREFIXES (Monster Modifiers) ===

        Register(new MapAffix
        {
            Id = "monster_phys_damage",
            Name = "Brutal",
            Description = "Monsters deal {0}% increased Physical Damage",
            Type = MapAffixType.MonsterPhysicalDamage,
            IsPrefix = true,
            MinValue = 20,
            MaxValue = 40,
            Weight = 1000,
            ItemQuantityBonus = 3f,
            Tags = new() { "damage" }
        });

        Register(new MapAffix
        {
            Id = "monster_ele_damage",
            Name = "Infernal",
            Description = "Monsters deal {0}% increased Elemental Damage",
            Type = MapAffixType.MonsterElementalDamage,
            IsPrefix = true,
            MinValue = 25,
            MaxValue = 45,
            Weight = 1000,
            ItemQuantityBonus = 4f,
            Tags = new() { "damage", "elemental" }
        });

        Register(new MapAffix
        {
            Id = "monster_attack_speed",
            Name = "Frenzied",
            Description = "Monsters have {0}% increased Attack Speed",
            Type = MapAffixType.MonsterAttackSpeed,
            IsPrefix = true,
            MinValue = 15,
            MaxValue = 30,
            Weight = 800,
            ItemQuantityBonus = 4f,
            Tags = new() { "speed" }
        });

        Register(new MapAffix
        {
            Id = "monster_move_speed",
            Name = "Fleet",
            Description = "Monsters have {0}% increased Movement Speed",
            Type = MapAffixType.MonsterMovementSpeed,
            IsPrefix = true,
            MinValue = 20,
            MaxValue = 40,
            Weight = 900,
            ItemQuantityBonus = 3f,
            Tags = new() { "speed" }
        });

        Register(new MapAffix
        {
            Id = "monster_life",
            Name = "Massive",
            Description = "Monsters have {0}% increased Maximum Life",
            Type = MapAffixType.MonsterLife,
            IsPrefix = true,
            MinValue = 30,
            MaxValue = 60,
            Weight = 1000,
            ItemQuantityBonus = 5f,
            Tags = new() { "life" }
        });

        Register(new MapAffix
        {
            Id = "monster_crit",
            Name = "Deadly",
            Description = "Monsters have {0}% increased Critical Strike Chance",
            Type = MapAffixType.MonsterCritChance,
            IsPrefix = true,
            MinValue = 100,
            MaxValue = 200,
            MinTier = 3,
            Weight = 700,
            ItemQuantityBonus = 4f,
            Tags = new() { "crit" }
        });

        Register(new MapAffix
        {
            Id = "physical_reflect",
            Name = "Thorned",
            Description = "Monsters reflect {0}% of Physical Damage",
            Type = MapAffixType.PhysicalReflect,
            IsPrefix = true,
            MinValue = 10,
            MaxValue = 18,
            MinTier = 5,
            Weight = 400,
            ItemQuantityBonus = 6f,
            Tags = new() { "reflect", "physical" }
        });

        Register(new MapAffix
        {
            Id = "elemental_reflect",
            Name = "Mirrored",
            Description = "Monsters reflect {0}% of Elemental Damage",
            Type = MapAffixType.ElementalReflect,
            IsPrefix = true,
            MinValue = 10,
            MaxValue = 18,
            MinTier = 5,
            Weight = 400,
            ItemQuantityBonus = 6f,
            Tags = new() { "reflect", "elemental" }
        });

        Register(new MapAffix
        {
            Id = "extra_packs",
            Name = "Teeming",
            Description = "Area contains {0} additional Monster Packs",
            Type = MapAffixType.ExtraPacks,
            IsPrefix = true,
            MinValue = 2,
            MaxValue = 5,
            Weight = 600,
            PackSizeBonus = 10f,
            Tags = new() { "quantity" }
        });

        Register(new MapAffix
        {
            Id = "ailment_avoid",
            Name = "Hexproof",
            Description = "Monsters have {0}% chance to Avoid Ailments",
            Type = MapAffixType.AilmentAvoidance,
            IsPrefix = true,
            MinValue = 30,
            MaxValue = 60,
            MinTier = 4,
            Weight = 600,
            ItemQuantityBonus = 4f,
            Tags = new() { "ailment" }
        });

        Register(new MapAffix
        {
            Id = "chained_attacks",
            Name = "Chaining",
            Description = "Monsters' Projectiles Chain {0} additional times",
            Type = MapAffixType.ChainedAttacks,
            IsPrefix = true,
            MinValue = 1,
            MaxValue = 2,
            MinTier = 8,
            Weight = 300,
            ItemQuantityBonus = 8f,
            Tags = new() { "projectile" }
        });

        Register(new MapAffix
        {
            Id = "monster_accuracy",
            Name = "Precise",
            Description = "Monsters have {0}% increased Accuracy",
            Type = MapAffixType.MonsterAccuracy,
            IsPrefix = true,
            MinValue = 30,
            MaxValue = 50,
            Weight = 800,
            ItemQuantityBonus = 3f,
            Tags = new() { "accuracy" }
        });

        // === SUFFIXES (Player Debuffs / Rewards) ===

        Register(new MapAffix
        {
            Id = "reduced_armour",
            Name = "of Exposure",
            Description = "Players have {0}% less Armour",
            Type = MapAffixType.ReducedArmour,
            IsPrefix = false,
            MinValue = 20,
            MaxValue = 40,
            Weight = 800,
            ItemRarityBonus = 5f,
            Tags = new() { "defence" }
        });

        Register(new MapAffix
        {
            Id = "reduced_resists",
            Name = "of Vulnerability",
            Description = "Players have -{0}% to Maximum Resistances",
            Type = MapAffixType.ReducedResistances,
            IsPrefix = false,
            MinValue = 5,
            MaxValue = 12,
            MinTier = 6,
            Weight = 400,
            ItemRarityBonus = 8f,
            Tags = new() { "resistance" }
        });

        Register(new MapAffix
        {
            Id = "no_regen",
            Name = "of Drought",
            Description = "Players cannot Regenerate Life",
            Type = MapAffixType.NoRegeneration,
            IsPrefix = false,
            MinValue = 1,
            MaxValue = 1,
            MinTier = 4,
            Weight = 500,
            ItemRarityBonus = 6f,
            Tags = new() { "recovery" }
        });

        Register(new MapAffix
        {
            Id = "reduced_recovery",
            Name = "of Smothering",
            Description = "Players have {0}% reduced Life Recovery Rate",
            Type = MapAffixType.ReducedRecovery,
            IsPrefix = false,
            MinValue = 40,
            MaxValue = 60,
            Weight = 700,
            ItemRarityBonus = 4f,
            Tags = new() { "recovery" }
        });

        Register(new MapAffix
        {
            Id = "item_quantity",
            Name = "of Bounty",
            Description = "{0}% increased Quantity of Items found",
            Type = MapAffixType.ItemQuantity,
            IsPrefix = false,
            MinValue = 10,
            MaxValue = 25,
            Weight = 1000,
            ItemQuantityBonus = 15f,
            Tags = new() { "reward" }
        });

        Register(new MapAffix
        {
            Id = "item_rarity",
            Name = "of Fortune",
            Description = "{0}% increased Rarity of Items found",
            Type = MapAffixType.ItemRarity,
            IsPrefix = false,
            MinValue = 15,
            MaxValue = 35,
            Weight = 1000,
            ItemRarityBonus = 20f,
            Tags = new() { "reward" }
        });

        Register(new MapAffix
        {
            Id = "pack_size",
            Name = "of the Horde",
            Description = "{0}% increased Pack Size",
            Type = MapAffixType.PackSize,
            IsPrefix = false,
            MinValue = 10,
            MaxValue = 25,
            Weight = 800,
            PackSizeBonus = 15f,
            Tags = new() { "quantity" }
        });

        Register(new MapAffix
        {
            Id = "more_experience",
            Name = "of Wisdom",
            Description = "{0}% more Experience from Monsters",
            Type = MapAffixType.MoreExperience,
            IsPrefix = false,
            MinValue = 10,
            MaxValue = 20,
            Weight = 600,
            Tags = new() { "experience" }
        });

        Register(new MapAffix
        {
            Id = "player_slowed",
            Name = "of Lethargy",
            Description = "Players have {0}% reduced Movement Speed",
            Type = MapAffixType.PlayerSlowed,
            IsPrefix = false,
            MinValue = 10,
            MaxValue = 20,
            Weight = 700,
            ItemRarityBonus = 4f,
            Tags = new() { "speed" }
        });

        Register(new MapAffix
        {
            Id = "reduced_cooldowns",
            Name = "of Inhibition",
            Description = "Players have {0}% reduced Cooldown Recovery Rate",
            Type = MapAffixType.ReducedCooldownRecovery,
            IsPrefix = false,
            MinValue = 20,
            MaxValue = 40,
            MinTier = 5,
            Weight = 500,
            ItemRarityBonus = 5f,
            Tags = new() { "cooldown" }
        });

        Register(new MapAffix
        {
            Id = "additional_boss",
            Name = "of the Twins",
            Description = "Area contains an additional Unique Boss",
            Type = MapAffixType.AdditionalBoss,
            IsPrefix = false,
            MinValue = 1,
            MaxValue = 1,
            MinTier = 8,
            Weight = 200,
            ItemQuantityBonus = 15f,
            ItemRarityBonus = 15f,
            Tags = new() { "boss" }
        });

        Register(new MapAffix
        {
            Id = "boss_drops_more",
            Name = "of Treasure",
            Description = "Unique Boss drops {0}% increased Items",
            Type = MapAffixType.BossDropsMore,
            IsPrefix = false,
            MinValue = 30,
            MaxValue = 50,
            Weight = 600,
            Tags = new() { "boss", "reward" }
        });

        System.Diagnostics.Debug.WriteLine($"MapAffixRegistry: Registered {_affixes.Count} affixes ({_prefixes.Count} prefixes, {_suffixes.Count} suffixes)");
    }

    private static void Register(MapAffix affix)
    {
        _affixes[affix.Id] = affix;
        if (affix.IsPrefix)
            _prefixes.Add(affix);
        else
            _suffixes.Add(affix);
    }

    /// <summary>Get an affix by ID.</summary>
    public static MapAffix? Get(string id) => _affixes.TryGetValue(id, out var affix) ? affix : null;

    /// <summary>Get all registered affixes.</summary>
    public static IEnumerable<MapAffix> GetAll() => _affixes.Values;

    /// <summary>Get all prefixes.</summary>
    public static IEnumerable<MapAffix> GetAllPrefixes() => _prefixes;

    /// <summary>Get all suffixes.</summary>
    public static IEnumerable<MapAffix> GetAllSuffixes() => _suffixes;

    /// <summary>Get all prefixes valid for a tier.</summary>
    public static IEnumerable<MapAffix> GetPrefixesForTier(int tier)
    {
        return _prefixes.Where(a => a.CanAppearOnTier(tier));
    }

    /// <summary>Get all suffixes valid for a tier.</summary>
    public static IEnumerable<MapAffix> GetSuffixesForTier(int tier)
    {
        return _suffixes.Where(a => a.CanAppearOnTier(tier));
    }

    /// <summary>Roll a random prefix for a tier, respecting existing affixes.</summary>
    public static MapAffixInstance? RollPrefix(int tier, Random rng, List<MapAffixInstance>? existing = null)
    {
        var validAffixes = GetPrefixesForTier(tier).ToList();

        // Filter out affixes that conflict with existing ones
        if (existing != null && existing.Count > 0)
        {
            var existingTags = existing.SelectMany(e => e.Affix.Tags).ToHashSet();
            var existingTypes = existing.Select(e => e.Type).ToHashSet();

            validAffixes = validAffixes
                .Where(a => !existingTypes.Contains(a.Type))
                .Where(a => !a.Tags.Any(t => existingTags.Contains(t)))
                .ToList();
        }

        if (validAffixes.Count == 0) return null;

        var selected = WeightedSelect(validAffixes, rng);
        return new MapAffixInstance(selected, selected.RollValue(rng));
    }

    /// <summary>Roll a random suffix for a tier, respecting existing affixes.</summary>
    public static MapAffixInstance? RollSuffix(int tier, Random rng, List<MapAffixInstance>? existing = null)
    {
        var validAffixes = GetSuffixesForTier(tier).ToList();

        if (existing != null && existing.Count > 0)
        {
            var existingTags = existing.SelectMany(e => e.Affix.Tags).ToHashSet();
            var existingTypes = existing.Select(e => e.Type).ToHashSet();

            validAffixes = validAffixes
                .Where(a => !existingTypes.Contains(a.Type))
                .Where(a => !a.Tags.Any(t => existingTags.Contains(t)))
                .ToList();
        }

        if (validAffixes.Count == 0) return null;

        var selected = WeightedSelect(validAffixes, rng);
        return new MapAffixInstance(selected, selected.RollValue(rng));
    }

    private static MapAffix WeightedSelect(List<MapAffix> affixes, Random rng)
    {
        int totalWeight = affixes.Sum(a => a.Weight);
        int roll = rng.Next(totalWeight);

        int cumulative = 0;
        foreach (var affix in affixes)
        {
            cumulative += affix.Weight;
            if (roll < cumulative)
                return affix;
        }

        return affixes[^1];
    }

    /// <summary>Get total affix count.</summary>
    public static int TotalCount => _affixes.Count;

    /// <summary>Get prefix count.</summary>
    public static int PrefixCount => _prefixes.Count;

    /// <summary>Get suffix count.</summary>
    public static int SuffixCount => _suffixes.Count;
}