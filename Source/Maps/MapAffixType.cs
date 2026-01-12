namespace Terrascent.Maps;

/// <summary>
/// Types of map affixes. Each affix modifies gameplay in the map zone.
/// </summary>
public enum MapAffixType
{
    // === PREFIXES (Monster Modifiers) ===

    /// <summary>Monsters deal X% increased physical damage.</summary>
    MonsterPhysicalDamage = 0,

    /// <summary>Monsters deal X% increased elemental damage.</summary>
    MonsterElementalDamage = 1,

    /// <summary>Monsters have X% increased attack speed.</summary>
    MonsterAttackSpeed = 2,

    /// <summary>Monsters have X% increased movement speed.</summary>
    MonsterMovementSpeed = 3,

    /// <summary>Monsters have X% increased maximum life.</summary>
    MonsterLife = 4,

    /// <summary>Monsters have X% increased critical strike chance.</summary>
    MonsterCritChance = 5,

    /// <summary>Monsters have X% increased accuracy.</summary>
    MonsterAccuracy = 6,

    /// <summary>Monsters reflect X% of physical damage.</summary>
    PhysicalReflect = 7,

    /// <summary>Monsters reflect X% of elemental damage.</summary>
    ElementalReflect = 8,

    /// <summary>Area contains X extra packs of monsters.</summary>
    ExtraPacks = 9,

    /// <summary>Monsters have a X% chance to avoid ailments.</summary>
    AilmentAvoidance = 10,

    /// <summary>Monsters' skills chain X additional times.</summary>
    ChainedAttacks = 11,

    // === SUFFIXES (Player Debuffs / Rewards) ===

    /// <summary>Players have X% less armour.</summary>
    ReducedArmour = 20,

    /// <summary>Players have X% reduced maximum resistances.</summary>
    ReducedResistances = 21,

    /// <summary>Players cannot regenerate life.</summary>
    NoRegeneration = 22,

    /// <summary>Players have X% less recovery rate of life.</summary>
    ReducedRecovery = 23,

    /// <summary>X% increased item quantity.</summary>
    ItemQuantity = 24,

    /// <summary>X% increased item rarity.</summary>
    ItemRarity = 25,

    /// <summary>X% increased pack size.</summary>
    PackSize = 26,

    /// <summary>X% more experience from monsters.</summary>
    MoreExperience = 27,

    /// <summary>Players have X% reduced movement speed.</summary>
    PlayerSlowed = 28,

    /// <summary>Players have X% reduced ability cooldown recovery.</summary>
    ReducedCooldownRecovery = 29,

    /// <summary>Area contains additional boss.</summary>
    AdditionalBoss = 30,

    /// <summary>Unique boss drops additional loot.</summary>
    BossDropsMore = 31,
}

/// <summary>
/// Extension methods for MapAffixType.
/// </summary>
public static class MapAffixTypeExtensions
{
    /// <summary>Check if this affix type is a prefix (monster modifier).</summary>
    public static bool IsPrefix(this MapAffixType type)
    {
        return (int)type < 20;
    }

    /// <summary>Check if this affix type is a suffix (player/reward modifier).</summary>
    public static bool IsSuffix(this MapAffixType type)
    {
        return (int)type >= 20;
    }

    /// <summary>Check if this affix type is a damage modifier.</summary>
    public static bool IsDamageModifier(this MapAffixType type)
    {
        return type == MapAffixType.MonsterPhysicalDamage ||
               type == MapAffixType.MonsterElementalDamage;
    }

    /// <summary>Check if this affix type is a reflect modifier.</summary>
    public static bool IsReflect(this MapAffixType type)
    {
        return type == MapAffixType.PhysicalReflect ||
               type == MapAffixType.ElementalReflect;
    }

    /// <summary>Check if this affix type is a reward modifier.</summary>
    public static bool IsRewardModifier(this MapAffixType type)
    {
        return type == MapAffixType.ItemQuantity ||
               type == MapAffixType.ItemRarity ||
               type == MapAffixType.PackSize ||
               type == MapAffixType.MoreExperience ||
               type == MapAffixType.BossDropsMore;
    }
}