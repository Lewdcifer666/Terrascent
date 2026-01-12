using Terrascent.World.Biomes;

namespace Terrascent.Maps;

/// <summary>
/// Configuration for map drop rates from various sources.
/// </summary>
public record MapDropConfig
{
    /// <summary>Base chance to drop a map (0-100).</summary>
    public float BaseDropChance { get; init; } = 5f;

    /// <summary>Minimum tier that can drop.</summary>
    public int MinTier { get; init; } = 1;

    /// <summary>Maximum tier that can drop.</summary>
    public int MaxTier { get; init; } = 16;

    /// <summary>Weight towards lower tiers (higher = more low tier maps).</summary>
    public float TierWeightDecay { get; init; } = 0.85f;

    /// <summary>Chance for dropped map to be Magic instead of Normal (0-100).</summary>
    public float MagicChance { get; init; } = 20f;

    /// <summary>Chance for dropped map to be Rare instead of Normal (0-100).</summary>
    public float RareChance { get; init; } = 5f;

    /// <summary>Item quantity bonus affects drop rate.</summary>
    public bool AffectedByIIQ { get; init; } = true;

    /// <summary>Item rarity bonus affects rarity of dropped maps.</summary>
    public bool AffectedByIIR { get; init; } = true;
}

/// <summary>
/// Handles map drops from bosses, chests, and other sources.
/// </summary>
public static class MapDropTable
{
    private static readonly Random _rng = new();

    /// <summary>Default drop config for regular monsters.</summary>
    public static readonly MapDropConfig MonsterConfig = new()
    {
        BaseDropChance = 0.5f,
        MinTier = 1,
        MaxTier = 5,
        TierWeightDecay = 0.9f,
        MagicChance = 10f,
        RareChance = 1f,
    };

    /// <summary>Default drop config for elite monsters.</summary>
    public static readonly MapDropConfig EliteConfig = new()
    {
        BaseDropChance = 5f,
        MinTier = 1,
        MaxTier = 8,
        TierWeightDecay = 0.85f,
        MagicChance = 25f,
        RareChance = 5f,
    };

    /// <summary>Default drop config for chests.</summary>
    public static readonly MapDropConfig ChestConfig = new()
    {
        BaseDropChance = 15f,
        MinTier = 1,
        MaxTier = 10,
        TierWeightDecay = 0.8f,
        MagicChance = 30f,
        RareChance = 10f,
    };

    /// <summary>Default drop config for bosses.</summary>
    public static readonly MapDropConfig BossConfig = new()
    {
        BaseDropChance = 100f, // Bosses always drop maps
        MinTier = 1,
        MaxTier = 16,
        TierWeightDecay = 0.75f,
        MagicChance = 40f,
        RareChance = 15f,
    };

    /// <summary>Drop config for map zone completion.</summary>
    public static readonly MapDropConfig ZoneCompletionConfig = new()
    {
        BaseDropChance = 100f,
        MinTier = 1,
        MaxTier = 16,
        TierWeightDecay = 0.7f,
        MagicChance = 50f,
        RareChance = 20f,
    };

    /// <summary>
    /// Try to generate a map drop.
    /// </summary>
    /// <param name="config">Drop configuration to use.</param>
    /// <param name="baseTier">Base tier for the drop (e.g., current zone tier).</param>
    /// <param name="itemQuantityBonus">Player's item quantity bonus (percentage).</param>
    /// <param name="itemRarityBonus">Player's item rarity bonus (percentage).</param>
    /// <param name="biome">Optional biome to use for the map.</param>
    /// <returns>A MapItem if drop succeeded, null otherwise.</returns>
    public static MapItem? TryDrop(
        MapDropConfig config,
        int baseTier = 1,
        float itemQuantityBonus = 0f,
        float itemRarityBonus = 0f,
        BiomeType? biome = null,
        Random? rng = null)
    {
        rng ??= _rng;

        // Calculate drop chance
        float dropChance = config.BaseDropChance;
        if (config.AffectedByIIQ)
        {
            dropChance *= (1f + itemQuantityBonus / 100f);
        }

        // Roll for drop
        if (rng.NextDouble() * 100 > dropChance)
        {
            return null;
        }

        // Determine tier
        int tier = RollTier(config, baseTier, rng);

        // Determine biome
        BiomeType mapBiome = biome ?? GetRandomBiome(rng);

        // Create the map
        var map = new MapItem(mapBiome, tier);

        // Determine rarity
        float rarityMod = config.AffectedByIIR ? itemRarityBonus : 0f;
        var rarity = RollRarity(config, rarityMod, rng);

        // Apply rarity
        if (rarity == MapRarity.Magic)
        {
            MapRoller.ApplyCurrency(map, MapCurrency.OrbOfTransmutation, rng);
        }
        else if (rarity == MapRarity.Rare)
        {
            MapRoller.ApplyCurrency(map, MapCurrency.OrbOfAlchemy, rng);
        }

        System.Diagnostics.Debug.WriteLine($"MapDropTable: Generated {map.GetDisplayName()}");
        return map;
    }

    /// <summary>
    /// Generate map drop from boss defeat.
    /// </summary>
    public static MapItem? TryDropFromBoss(
        string bossId,
        float itemQuantityBonus = 0f,
        float itemRarityBonus = 0f,
        Random? rng = null)
    {
        // Boss-specific tier ranges
        int baseTier = GetBossMapTier(bossId);
        return TryDrop(BossConfig, baseTier, itemQuantityBonus, itemRarityBonus, null, rng);
    }

    /// <summary>
    /// Generate multiple map drops from zone completion.
    /// </summary>
    public static List<MapItem> GenerateZoneCompletionDrops(
        int zoneTier,
        float itemQuantityBonus,
        float itemRarityBonus,
        bool bossKilled,
        Random? rng = null)
    {
        rng ??= _rng;
        var drops = new List<MapItem>();

        // Base drops: 1-2 maps for completion
        int baseDrops = 1 + (bossKilled ? 1 : 0);

        // Bonus from IIQ (up to +2 extra)
        float extraChance = itemQuantityBonus / 100f;
        while (extraChance > 0 && drops.Count < 5)
        {
            if (rng.NextDouble() < extraChance)
            {
                baseDrops++;
            }
            extraChance -= 1f;
        }

        // Generate drops
        for (int i = 0; i < baseDrops; i++)
        {
            var map = TryDrop(ZoneCompletionConfig, zoneTier, itemQuantityBonus, itemRarityBonus, null, rng);
            if (map != null)
            {
                drops.Add(map);
            }
        }

        // Chance for +1 tier map if boss killed
        if (bossKilled && zoneTier < 16 && rng.NextDouble() < 0.3)
        {
            var upgradeConfig = ZoneCompletionConfig with { MinTier = zoneTier + 1, MaxTier = zoneTier + 1 };
            var bonusMap = TryDrop(upgradeConfig, zoneTier + 1, itemQuantityBonus, itemRarityBonus, null, rng);
            if (bonusMap != null)
            {
                drops.Add(bonusMap);
            }
        }

        return drops;
    }

    /// <summary>
    /// Try to drop map currency.
    /// </summary>
    public static MapCurrency? TryDropCurrency(
        float dropChance = 2f,
        float itemQuantityBonus = 0f,
        Random? rng = null)
    {
        rng ??= _rng;

        float chance = dropChance * (1f + itemQuantityBonus / 100f);
        if (rng.NextDouble() * 100 > chance)
        {
            return null;
        }

        return RollCurrency(rng);
    }

    /// <summary>
    /// Roll which currency drops based on rarity weights.
    /// </summary>
    private static MapCurrency RollCurrency(Random rng)
    {
        // Weights: lower = rarer
        var weights = new (MapCurrency currency, int weight)[]
        {
            (MapCurrency.OrbOfTransmutation, 100),
            (MapCurrency.OrbOfAlteration, 80),
            (MapCurrency.OrbOfAugmentation, 70),
            (MapCurrency.OrbOfScouring, 40),
            (MapCurrency.OrbOfAlchemy, 30),
            (MapCurrency.ChaosOrb, 15),
            (MapCurrency.RegalOrb, 12),
            (MapCurrency.VaalOrb, 10),
            (MapCurrency.DivineOrb, 3),
            (MapCurrency.ExaltedOrb, 1),
        };

        int totalWeight = weights.Sum(w => w.weight);
        int roll = rng.Next(totalWeight);
        int cumulative = 0;

        foreach (var (currency, weight) in weights)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                return currency;
            }
        }

        return MapCurrency.OrbOfTransmutation;
    }

    /// <summary>
    /// Roll the tier for a dropped map.
    /// </summary>
    private static int RollTier(MapDropConfig config, int baseTier, Random rng)
    {
        // Start with base tier, apply variance
        int minTier = Math.Max(config.MinTier, baseTier - 2);
        int maxTier = Math.Min(config.MaxTier, baseTier + 1);

        // Weight towards lower tiers using decay
        var tierWeights = new List<(int tier, float weight)>();
        float weight = 1f;

        for (int t = minTier; t <= maxTier; t++)
        {
            tierWeights.Add((t, weight));
            weight *= config.TierWeightDecay;
        }

        // Reverse so higher tiers are rarer
        tierWeights.Reverse();

        float totalWeight = tierWeights.Sum(tw => tw.weight);
        float roll = (float)rng.NextDouble() * totalWeight;
        float cumulative = 0f;

        foreach (var (tier, w) in tierWeights)
        {
            cumulative += w;
            if (roll < cumulative)
            {
                return tier;
            }
        }

        return minTier;
    }

    /// <summary>
    /// Roll the rarity for a dropped map.
    /// </summary>
    private static MapRarity RollRarity(MapDropConfig config, float rarityBonus, Random rng)
    {
        float rareChance = config.RareChance * (1f + rarityBonus / 100f);
        float magicChance = config.MagicChance * (1f + rarityBonus / 100f);

        float roll = (float)rng.NextDouble() * 100f;

        if (roll < rareChance)
            return MapRarity.Rare;
        if (roll < rareChance + magicChance)
            return MapRarity.Magic;

        return MapRarity.Normal;
    }

    /// <summary>
    /// Get a random biome for map generation.
    /// </summary>
    private static BiomeType GetRandomBiome(Random rng)
    {
        var biomes = new BiomeType[]
        {
            BiomeType.Forest,
            BiomeType.Desert,
            BiomeType.Snow,
            BiomeType.Jungle,
            BiomeType.Underground,
            BiomeType.Corruption,
            BiomeType.Crimson,
        };

        return biomes[rng.Next(biomes.Length)];
    }

    /// <summary>
    /// Get base map tier for a specific boss.
    /// </summary>
    private static int GetBossMapTier(string bossId)
    {
        return bossId.ToLowerInvariant() switch
        {
            "king_slime" => 1,
            "eye_of_terror" => 2,
            "brain_of_depths" => 3,
            "queen_bee" => 4,
            "skeletal_warlord" => 5,
            "wall_of_shadows" => 8,
            // Hardmode bosses
            "destroyer" => 10,
            "twins" => 11,
            "skeletron_prime" => 12,
            "plantera" => 13,
            "golem" => 14,
            "lunatic_cultist" => 15,
            "moon_lord" => 16,
            _ => 1
        };
    }
}

/// <summary>
/// Currency drop configuration.
/// </summary>
public class CurrencyDropConfig
{
    /// <summary>Base chance to drop currency (0-100).</summary>
    public float BaseDropChance { get; set; } = 2f;

    /// <summary>Affected by item quantity bonus.</summary>
    public bool AffectedByIIQ { get; set; } = true;

    /// <summary>Default config for monster drops.</summary>
    public static readonly CurrencyDropConfig Monster = new()
    {
        BaseDropChance = 0.5f,
    };

    /// <summary>Default config for elite drops.</summary>
    public static readonly CurrencyDropConfig Elite = new()
    {
        BaseDropChance = 3f,
    };

    /// <summary>Default config for chest drops.</summary>
    public static readonly CurrencyDropConfig Chest = new()
    {
        BaseDropChance = 10f,
    };

    /// <summary>Default config for boss drops.</summary>
    public static readonly CurrencyDropConfig Boss = new()
    {
        BaseDropChance = 100f, // Bosses always drop some currency
    };
}