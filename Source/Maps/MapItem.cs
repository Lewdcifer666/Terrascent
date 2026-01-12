using Microsoft.Xna.Framework;
using Terrascent.World.Biomes;

namespace Terrascent.Maps;

/// <summary>
/// A map item represents an endgame map that can be placed in the Map Device.
/// Maps have tiers (T1-T16), rarity, and affixes that modify gameplay.
/// </summary>
public class MapItem
{
    private readonly Random _rng;

    /// <summary>Unique identifier for this map instance.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>The biome type this map is based on.</summary>
    public BiomeType BiomeType { get; private set; }

    /// <summary>Map tier (T1-T16).</summary>
    public int Tier { get; private set; }

    /// <summary>Map rarity (Normal/Magic/Rare/Corrupted).</summary>
    public MapRarity Rarity { get; private set; }

    /// <summary>List of prefix affixes (monster modifiers).</summary>
    public List<MapAffixInstance> Prefixes { get; } = new();

    /// <summary>List of suffix affixes (player/reward modifiers).</summary>
    public List<MapAffixInstance> Suffixes { get; } = new();

    /// <summary>Whether this map has been corrupted (cannot be modified).</summary>
    public bool IsCorrupted => Rarity == MapRarity.Corrupted;

    /// <summary>Whether this is a fusion/hybrid map.</summary>
    public bool IsFusion { get; private set; }

    /// <summary>Secondary biome for fusion maps.</summary>
    public BiomeType? SecondaryBiome { get; private set; }

    /// <summary>Custom name for rare/corrupted maps.</summary>
    public string? CustomName { get; private set; }

    /// <summary>Seed for zone generation.</summary>
    public int ZoneSeed { get; private set; }

    // === Calculated Stats ===

    /// <summary>Monster level in this map.</summary>
    public int MonsterLevel => MapTier.GetMonsterLevel(Tier);

    /// <summary>Total item quantity bonus (base + affixes).</summary>
    public float ItemQuantity
    {
        get
        {
            float baseIQ = MapTier.GetBaseItemQuantity(Tier);
            float affixIQ = Prefixes.Concat(Suffixes).Sum(a => a.Affix.ItemQuantityBonus);

            // Add affix values for quantity affixes
            foreach (var affix in Suffixes.Where(a => a.Type == MapAffixType.ItemQuantity))
                affixIQ += affix.Value;

            return baseIQ + affixIQ;
        }
    }

    /// <summary>Total item rarity bonus (base + affixes).</summary>
    public float ItemRarity
    {
        get
        {
            float baseIR = MapTier.GetBaseItemRarity(Tier);
            float affixIR = Prefixes.Concat(Suffixes).Sum(a => a.Affix.ItemRarityBonus);

            foreach (var affix in Suffixes.Where(a => a.Type == MapAffixType.ItemRarity))
                affixIR += affix.Value;

            return baseIR + affixIR;
        }
    }

    /// <summary>Total pack size bonus (base + affixes).</summary>
    public float PackSize
    {
        get
        {
            float basePS = MapTier.GetBasePackSize(Tier);
            float affixPS = Prefixes.Concat(Suffixes).Sum(a => a.Affix.PackSizeBonus);

            foreach (var affix in Suffixes.Where(a => a.Type == MapAffixType.PackSize))
                affixPS += affix.Value;

            return basePS + affixPS;
        }
    }

    /// <summary>Experience multiplier.</summary>
    public float ExperienceMultiplier
    {
        get
        {
            float baseMult = MapTier.GetExperienceMultiplier(Tier);

            foreach (var affix in Suffixes.Where(a => a.Type == MapAffixType.MoreExperience))
                baseMult += affix.Value / 100f;

            return baseMult;
        }
    }

    /// <summary>Create a new map item.</summary>
    public MapItem(BiomeType biome, int tier, int? seed = null)
    {
        BiomeType = biome;
        Tier = Math.Clamp(tier, MapTier.MIN_TIER, MapTier.MAX_TIER);
        Rarity = MapRarity.Normal;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        ZoneSeed = _rng.Next();
    }

    /// <summary>Create a fusion map from two biomes.</summary>
    public static MapItem CreateFusion(BiomeType primary, BiomeType secondary, int tier, int? seed = null)
    {
        var map = new MapItem(primary, tier, seed)
        {
            IsFusion = true,
            SecondaryBiome = secondary
        };
        return map;
    }

    /// <summary>Get the display name for this map.</summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(CustomName))
            return CustomName;

        string biomeName = BiomeType.GetDisplayName();
        if (IsFusion && SecondaryBiome.HasValue)
            biomeName = $"{biomeName}/{SecondaryBiome.Value.GetDisplayName()}";

        string tierPrefix = MapTier.GetDisplayName(Tier);

        return Rarity switch
        {
            MapRarity.Magic => GetMagicName(biomeName, tierPrefix),
            MapRarity.Rare => GetRareName(biomeName, tierPrefix),
            MapRarity.Corrupted => $"Corrupted {biomeName} Map ({tierPrefix})",
            _ => $"{biomeName} Map ({tierPrefix})"
        };
    }

    private string GetMagicName(string biomeName, string tierPrefix)
    {
        var prefixName = Prefixes.FirstOrDefault()?.Affix.Name ?? "";
        var suffixName = Suffixes.FirstOrDefault()?.Affix.Name ?? "";

        if (!string.IsNullOrEmpty(prefixName) && !string.IsNullOrEmpty(suffixName))
            return $"{prefixName} {biomeName} Map {suffixName} ({tierPrefix})";
        if (!string.IsNullOrEmpty(prefixName))
            return $"{prefixName} {biomeName} Map ({tierPrefix})";
        if (!string.IsNullOrEmpty(suffixName))
            return $"{biomeName} Map {suffixName} ({tierPrefix})";

        return $"{biomeName} Map ({tierPrefix})";
    }

    private string GetRareName(string biomeName, string tierPrefix)
    {
        // Generate a random rare name
        string[] rareNames = {
            "Vaal Halls", "Twilight Strand", "Shattered Keep", "Corrupted Depths",
            "Ancient Ruins", "Forgotten Tomb", "Cursed Grounds", "Blighted Grove",
            "Burning Cathedral", "Frozen Reaches", "Storm Basin", "Plague Ward",
            "Shadow Realm", "Crystal Cavern", "Doom Fortress", "Nightmare Spire"
        };

        string rareName = rareNames[Math.Abs(Id.GetHashCode()) % rareNames.Length];
        return $"{rareName} ({tierPrefix})";
    }

    /// <summary>Get the display color based on rarity.</summary>
    public Color GetDisplayColor() => Rarity.GetColor();

    /// <summary>Get all affixes.</summary>
    public IEnumerable<MapAffixInstance> AllAffixes => Prefixes.Concat(Suffixes);

    /// <summary>Total affix count.</summary>
    public int AffixCount => Prefixes.Count + Suffixes.Count;

    /// <summary>Check if an affix type is present on this map.</summary>
    public bool HasAffix(MapAffixType type)
    {
        return AllAffixes.Any(a => a.Type == type);
    }

    /// <summary>Get the value of a specific affix type (0 if not present).</summary>
    public float GetAffixValue(MapAffixType type)
    {
        var affix = AllAffixes.FirstOrDefault(a => a.Type == type);
        return affix?.Value ?? 0f;
    }

    /// <summary>Clear all affixes (for rerolling).</summary>
    public void ClearAffixes()
    {
        if (IsCorrupted) return;
        Prefixes.Clear();
        Suffixes.Clear();
    }

    /// <summary>Add a prefix affix.</summary>
    public bool AddPrefix(MapAffixInstance prefix)
    {
        if (IsCorrupted) return false;
        if (Prefixes.Count >= Rarity.MaxPrefixes()) return false;
        if (Prefixes.Any(p => p.Type == prefix.Type)) return false;

        Prefixes.Add(prefix);
        return true;
    }

    /// <summary>Add a suffix affix.</summary>
    public bool AddSuffix(MapAffixInstance suffix)
    {
        if (IsCorrupted) return false;
        if (Suffixes.Count >= Rarity.MaxSuffixes()) return false;
        if (Suffixes.Any(s => s.Type == suffix.Type)) return false;

        Suffixes.Add(suffix);
        return true;
    }

    /// <summary>Set the rarity of this map.</summary>
    public void SetRarity(MapRarity rarity)
    {
        if (IsCorrupted) return;
        Rarity = rarity;
    }

    /// <summary>Corrupt this map (makes it unmodifiable).</summary>
    public void Corrupt()
    {
        Rarity = MapRarity.Corrupted;
    }

    /// <summary>Increase map tier (usually from Vaal corruption).</summary>
    public void IncreaseTier(int amount = 1)
    {
        Tier = Math.Min(MapTier.MAX_TIER, Tier + amount);
    }

    /// <summary>Get a formatted summary of map stats.</summary>
    public string GetStatsSummary()
    {
        return $"Level {MonsterLevel} | IIQ: +{ItemQuantity:F0}% | IIR: +{ItemRarity:F0}% | Pack: +{PackSize:F0}%";
    }

    /// <summary>Get formatted affix descriptions.</summary>
    public IEnumerable<string> GetAffixDescriptions()
    {
        foreach (var prefix in Prefixes)
            yield return prefix.GetDescription();
        foreach (var suffix in Suffixes)
            yield return suffix.GetDescription();
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(Id.ToByteArray());
        writer.Write((int)BiomeType);
        writer.Write(Tier);
        writer.Write((int)Rarity);
        writer.Write(IsFusion);
        writer.Write(SecondaryBiome.HasValue ? (int)SecondaryBiome.Value : -1);
        writer.Write(CustomName ?? "");
        writer.Write(ZoneSeed);

        // Prefixes
        writer.Write(Prefixes.Count);
        foreach (var prefix in Prefixes)
        {
            writer.Write(prefix.Affix.Id);
            writer.Write(prefix.Value);
        }

        // Suffixes
        writer.Write(Suffixes.Count);
        foreach (var suffix in Suffixes)
        {
            writer.Write(suffix.Affix.Id);
            writer.Write(suffix.Value);
        }
    }

    public static MapItem LoadFrom(BinaryReader reader)
    {
        var id = new Guid(reader.ReadBytes(16));
        var biome = (BiomeType)reader.ReadInt32();
        var tier = reader.ReadInt32();
        var rarity = (MapRarity)reader.ReadInt32();
        var isFusion = reader.ReadBoolean();
        var secondaryBiomeInt = reader.ReadInt32();
        var customName = reader.ReadString();
        var zoneSeed = reader.ReadInt32();

        var map = new MapItem(biome, tier)
        {
            Id = id,
            Rarity = rarity,
            IsFusion = isFusion,
            SecondaryBiome = secondaryBiomeInt >= 0 ? (BiomeType)secondaryBiomeInt : null,
            CustomName = string.IsNullOrEmpty(customName) ? null : customName,
            ZoneSeed = zoneSeed
        };

        // Prefixes
        int prefixCount = reader.ReadInt32();
        for (int i = 0; i < prefixCount; i++)
        {
            var affixId = reader.ReadString();
            var value = reader.ReadSingle();
            var affix = MapAffixRegistry.Get(affixId);
            if (affix != null)
                map.Prefixes.Add(new MapAffixInstance(affix, value));
        }

        // Suffixes
        int suffixCount = reader.ReadInt32();
        for (int i = 0; i < suffixCount; i++)
        {
            var affixId = reader.ReadString();
            var value = reader.ReadSingle();
            var affix = MapAffixRegistry.Get(affixId);
            if (affix != null)
                map.Suffixes.Add(new MapAffixInstance(affix, value));
        }

        return map;
    }

    #endregion
}