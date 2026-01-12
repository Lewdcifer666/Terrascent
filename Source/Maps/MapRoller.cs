namespace Terrascent.Maps;

/// <summary>
/// Result of applying currency to a map.
/// </summary>
public enum MapRollResult
{
    Success,
    CannotApply,
    Corrupted,
    Destroyed,
    Upgraded,
    NoChange
}

/// <summary>
/// Handles applying currency to maps and rolling affixes.
/// </summary>
public static class MapRoller
{
    private static readonly Random _rng = new();

    /// <summary>
    /// Apply a currency to a map.
    /// </summary>
    public static MapRollResult ApplyCurrency(MapItem map, MapCurrency currency, Random? rng = null)
    {
        rng ??= _rng;

        if (!currency.CanApplyTo(map))
            return MapRollResult.CannotApply;

        return currency switch
        {
            MapCurrency.OrbOfTransmutation => ApplyTransmutation(map, rng),
            MapCurrency.OrbOfAlteration => ApplyAlteration(map, rng),
            MapCurrency.RegalOrb => ApplyRegal(map, rng),
            MapCurrency.OrbOfAlchemy => ApplyAlchemy(map, rng),
            MapCurrency.ChaosOrb => ApplyChaos(map, rng),
            MapCurrency.DivineOrb => ApplyDivine(map, rng),
            MapCurrency.OrbOfScouring => ApplyScouring(map),
            MapCurrency.VaalOrb => ApplyVaal(map, rng),
            MapCurrency.OrbOfAugmentation => ApplyAugmentation(map, rng),
            MapCurrency.ExaltedOrb => ApplyExalted(map, rng),
            _ => MapRollResult.NoChange
        };
    }

    /// <summary>
    /// Transmutation: Normal → Magic (1-2 mods).
    /// </summary>
    private static MapRollResult ApplyTransmutation(MapItem map, Random rng)
    {
        map.SetRarity(MapRarity.Magic);
        map.ClearAffixes();

        // Roll 1-2 mods (50% chance for 2)
        int modCount = rng.Next(100) < 50 ? 2 : 1;

        // Decide prefix/suffix distribution
        if (modCount == 1)
        {
            // 50/50 prefix or suffix
            if (rng.Next(100) < 50)
                AddRandomPrefix(map, rng);
            else
                AddRandomSuffix(map, rng);
        }
        else
        {
            // One prefix, one suffix
            AddRandomPrefix(map, rng);
            AddRandomSuffix(map, rng);
        }

        return MapRollResult.Success;
    }

    /// <summary>
    /// Alteration: Reroll Magic map.
    /// </summary>
    private static MapRollResult ApplyAlteration(MapItem map, Random rng)
    {
        map.ClearAffixes();

        // Roll 1-2 mods
        int modCount = rng.Next(100) < 50 ? 2 : 1;

        if (modCount == 1)
        {
            if (rng.Next(100) < 50)
                AddRandomPrefix(map, rng);
            else
                AddRandomSuffix(map, rng);
        }
        else
        {
            AddRandomPrefix(map, rng);
            AddRandomSuffix(map, rng);
        }

        return MapRollResult.Success;
    }

    /// <summary>
    /// Regal: Magic → Rare (add 3-4 mods to existing).
    /// </summary>
    private static MapRollResult ApplyRegal(MapItem map, Random rng)
    {
        map.SetRarity(MapRarity.Rare);

        // Add 3-4 more mods (keeping existing)
        int additionalMods = rng.Next(100) < 50 ? 4 : 3;

        for (int i = 0; i < additionalMods; i++)
        {
            // Alternate between prefix and suffix
            if (i % 2 == 0)
                AddRandomPrefix(map, rng);
            else
                AddRandomSuffix(map, rng);
        }

        return MapRollResult.Upgraded;
    }

    /// <summary>
    /// Alchemy: Normal → Rare (4-6 mods).
    /// </summary>
    private static MapRollResult ApplyAlchemy(MapItem map, Random rng)
    {
        map.SetRarity(MapRarity.Rare);
        map.ClearAffixes();

        // Roll 4-6 mods
        int modCount = 4 + rng.Next(3); // 4, 5, or 6

        // Distribute between prefixes and suffixes (2-3 each)
        int prefixCount = Math.Min(3, modCount / 2 + rng.Next(2));
        int suffixCount = modCount - prefixCount;
        suffixCount = Math.Min(3, suffixCount);

        for (int i = 0; i < prefixCount; i++)
            AddRandomPrefix(map, rng);

        for (int i = 0; i < suffixCount; i++)
            AddRandomSuffix(map, rng);

        return MapRollResult.Success;
    }

    /// <summary>
    /// Chaos: Reroll Rare map (4-6 new mods).
    /// </summary>
    private static MapRollResult ApplyChaos(MapItem map, Random rng)
    {
        map.ClearAffixes();

        // Roll 4-6 mods
        int modCount = 4 + rng.Next(3);

        int prefixCount = Math.Min(3, modCount / 2 + rng.Next(2));
        int suffixCount = Math.Min(3, modCount - prefixCount);

        for (int i = 0; i < prefixCount; i++)
            AddRandomPrefix(map, rng);

        for (int i = 0; i < suffixCount; i++)
            AddRandomSuffix(map, rng);

        return MapRollResult.Success;
    }

    /// <summary>
    /// Divine: Reroll all mod values.
    /// </summary>
    private static MapRollResult ApplyDivine(MapItem map, Random rng)
    {
        foreach (var prefix in map.Prefixes)
            prefix.Reroll(rng);

        foreach (var suffix in map.Suffixes)
            suffix.Reroll(rng);

        return MapRollResult.Success;
    }

    /// <summary>
    /// Scouring: Remove all mods, downgrade to Normal.
    /// </summary>
    private static MapRollResult ApplyScouring(MapItem map)
    {
        map.ClearAffixes();
        map.SetRarity(MapRarity.Normal);
        return MapRollResult.Success;
    }

    /// <summary>
    /// Vaal: Corrupt with unpredictable results.
    /// Outcomes:
    /// - 25%: +1 tier
    /// - 25%: 8 mods (if rare) or reroll with 8 mods
    /// - 20%: Nothing (just corrupted)
    /// - 20%: Reroll all mods
    /// - 10%: Map destroyed
    /// </summary>
    private static MapRollResult ApplyVaal(MapItem map, Random rng)
    {
        int roll = rng.Next(100);

        if (roll < 10)
        {
            // 10%: Destroyed
            return MapRollResult.Destroyed;
        }
        else if (roll < 35)
        {
            // 25%: +1 tier
            map.IncreaseTier(1);
            map.Corrupt();
            return MapRollResult.Upgraded;
        }
        else if (roll < 60)
        {
            // 25%: 8 mods
            map.ClearAffixes();
            map.SetRarity(MapRarity.Rare);

            // Add 4 prefixes and 4 suffixes (max corruption)
            for (int i = 0; i < 4; i++)
                AddRandomPrefix(map, rng);
            for (int i = 0; i < 4; i++)
                AddRandomSuffix(map, rng);

            map.Corrupt();
            return MapRollResult.Corrupted;
        }
        else if (roll < 80)
        {
            // 20%: Reroll all mods
            map.ClearAffixes();
            int modCount = 4 + rng.Next(3);
            int prefixCount = Math.Min(3, modCount / 2 + rng.Next(2));
            int suffixCount = Math.Min(3, modCount - prefixCount);

            for (int i = 0; i < prefixCount; i++)
                AddRandomPrefix(map, rng);
            for (int i = 0; i < suffixCount; i++)
                AddRandomSuffix(map, rng);

            map.Corrupt();
            return MapRollResult.Corrupted;
        }
        else
        {
            // 20%: Nothing, just corrupted
            map.Corrupt();
            return MapRollResult.NoChange;
        }
    }

    /// <summary>
    /// Augmentation: Add a mod to Magic map.
    /// </summary>
    private static MapRollResult ApplyAugmentation(MapItem map, Random rng)
    {
        // Add whichever type is missing or random if both empty
        bool hasPrefix = map.Prefixes.Count > 0;
        bool hasSuffix = map.Suffixes.Count > 0;

        if (!hasPrefix && !hasSuffix)
        {
            if (rng.Next(100) < 50)
                AddRandomPrefix(map, rng);
            else
                AddRandomSuffix(map, rng);
        }
        else if (!hasPrefix)
        {
            AddRandomPrefix(map, rng);
        }
        else if (!hasSuffix)
        {
            AddRandomSuffix(map, rng);
        }
        else
        {
            return MapRollResult.CannotApply; // Already has 2 mods
        }

        return MapRollResult.Success;
    }

    /// <summary>
    /// Exalted: Add a mod to Rare map.
    /// </summary>
    private static MapRollResult ApplyExalted(MapItem map, Random rng)
    {
        // Add prefix if under 3, otherwise suffix if under 3
        if (map.Prefixes.Count < 3)
        {
            AddRandomPrefix(map, rng);
            return MapRollResult.Success;
        }
        else if (map.Suffixes.Count < 3)
        {
            AddRandomSuffix(map, rng);
            return MapRollResult.Success;
        }

        return MapRollResult.CannotApply;
    }

    /// <summary>
    /// Add a random prefix to the map.
    /// </summary>
    private static void AddRandomPrefix(MapItem map, Random rng)
    {
        var prefix = MapAffixRegistry.RollPrefix(map.Tier, rng, map.Prefixes.ToList());
        if (prefix != null)
            map.AddPrefix(prefix);
    }

    /// <summary>
    /// Add a random suffix to the map.
    /// </summary>
    private static void AddRandomSuffix(MapItem map, Random rng)
    {
        var suffix = MapAffixRegistry.RollSuffix(map.Tier, rng, map.Suffixes.ToList());
        if (suffix != null)
            map.AddSuffix(suffix);
    }

    /// <summary>
    /// Generate a random map with specified tier and rarity.
    /// </summary>
    public static MapItem GenerateMap(
        Terrascent.World.Biomes.BiomeType biome,
        int tier,
        MapRarity rarity = MapRarity.Normal,
        Random? rng = null)
    {
        rng ??= _rng;
        var map = new MapItem(biome, tier);

        // Apply rarity
        switch (rarity)
        {
            case MapRarity.Magic:
                ApplyTransmutation(map, rng);
                break;
            case MapRarity.Rare:
                ApplyAlchemy(map, rng);
                break;
            case MapRarity.Corrupted:
                ApplyAlchemy(map, rng);
                ApplyVaal(map, rng);
                break;
        }

        return map;
    }

    /// <summary>
    /// Generate a random map drop from a boss or chest.
    /// </summary>
    public static MapItem? GenerateMapDrop(
        int baseTier,
        float itemQuantityBonus = 0f,
        float itemRarityBonus = 0f,
        Random? rng = null)
    {
        rng ??= _rng;

        // Tier variance (+/- 1, weighted toward current tier)
        int tierRoll = rng.Next(100);
        int tier = baseTier;
        if (tierRoll < 15) tier = Math.Max(1, baseTier - 1);
        else if (tierRoll > 85) tier = Math.Min(16, baseTier + 1);

        // Pick random biome for map
        var biomes = new[]
        {
            Terrascent.World.Biomes.BiomeType.Forest,
            Terrascent.World.Biomes.BiomeType.Desert,
            Terrascent.World.Biomes.BiomeType.Snow,
            Terrascent.World.Biomes.BiomeType.Jungle,
            Terrascent.World.Biomes.BiomeType.Underground,
            Terrascent.World.Biomes.BiomeType.Corruption,
            Terrascent.World.Biomes.BiomeType.Crimson
        };
        var biome = biomes[rng.Next(biomes.Length)];

        var map = new MapItem(biome, tier);

        // Roll rarity based on IIR bonus
        float rarityRoll = rng.Next(100) + itemRarityBonus * 0.3f;

        if (rarityRoll > 95)
        {
            // Rare map
            ApplyAlchemy(map, rng);
        }
        else if (rarityRoll > 70)
        {
            // Magic map
            ApplyTransmutation(map, rng);
        }
        // Else: Normal map

        return map;
    }
}