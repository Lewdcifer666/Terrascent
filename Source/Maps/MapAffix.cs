namespace Terrascent.Maps;

/// <summary>
/// A map affix modifies gameplay within the map zone.
/// Can be a prefix (monster modifier) or suffix (player/reward modifier).
/// </summary>
public class MapAffix
{
    /// <summary>Unique identifier for this affix.</summary>
    public string Id { get; init; } = "";

    /// <summary>Display name shown on map item.</summary>
    public string Name { get; init; } = "";

    /// <summary>Description with value placeholder ({0}).</summary>
    public string Description { get; init; } = "";

    /// <summary>Type of affix for effect application.</summary>
    public MapAffixType Type { get; init; }

    /// <summary>True if prefix (monster mod), false if suffix (player/reward).</summary>
    public bool IsPrefix { get; init; }

    /// <summary>Minimum roll value.</summary>
    public float MinValue { get; init; }

    /// <summary>Maximum roll value.</summary>
    public float MaxValue { get; init; }

    /// <summary>Minimum map tier this affix can appear on.</summary>
    public int MinTier { get; init; } = 1;

    /// <summary>Maximum map tier this affix can appear on (0 = no limit).</summary>
    public int MaxTier { get; init; } = 0;

    /// <summary>Weight for rolling (higher = more common).</summary>
    public int Weight { get; init; } = 1000;

    /// <summary>Item quantity bonus granted by this affix.</summary>
    public float ItemQuantityBonus { get; init; } = 0f;

    /// <summary>Item rarity bonus granted by this affix.</summary>
    public float ItemRarityBonus { get; init; } = 0f;

    /// <summary>Pack size bonus granted by this affix.</summary>
    public float PackSizeBonus { get; init; } = 0f;

    /// <summary>Tags for affix grouping (prevents same-group affixes).</summary>
    public HashSet<string> Tags { get; init; } = new();

    /// <summary>Roll a random value within the affix range.</summary>
    public float RollValue(Random rng)
    {
        if (MinValue == MaxValue) return MinValue;
        return MinValue + (float)(rng.NextDouble() * (MaxValue - MinValue));
    }

    /// <summary>Get the formatted description with a specific value.</summary>
    public string GetDescription(float value)
    {
        return string.Format(Description, value.ToString("F0"));
    }

    /// <summary>Check if this affix can appear on a map of the given tier.</summary>
    public bool CanAppearOnTier(int tier)
    {
        if (tier < MinTier) return false;
        if (MaxTier > 0 && tier > MaxTier) return false;
        return true;
    }

    /// <summary>Get the value range as a string.</summary>
    public string GetValueRange()
    {
        if (MinValue == MaxValue)
            return MinValue.ToString("F0");
        return $"{MinValue:F0}-{MaxValue:F0}";
    }
}

/// <summary>
/// An instance of an affix on a specific map, with its rolled value.
/// </summary>
public class MapAffixInstance
{
    /// <summary>The base affix definition.</summary>
    public MapAffix Affix { get; }

    /// <summary>The rolled value for this instance.</summary>
    public float Value { get; private set; }

    public MapAffixInstance(MapAffix affix, float value)
    {
        Affix = affix;
        Value = value;
    }

    /// <summary>Get the formatted description.</summary>
    public string GetDescription() => Affix.GetDescription(Value);

    /// <summary>Type shortcut.</summary>
    public MapAffixType Type => Affix.Type;

    /// <summary>Is this a prefix?</summary>
    public bool IsPrefix => Affix.IsPrefix;

    /// <summary>Affix ID shortcut.</summary>
    public string Id => Affix.Id;

    /// <summary>Affix name shortcut.</summary>
    public string Name => Affix.Name;

    /// <summary>Item quantity bonus from this instance.</summary>
    public float ItemQuantityBonus => Affix.ItemQuantityBonus;

    /// <summary>Item rarity bonus from this instance.</summary>
    public float ItemRarityBonus => Affix.ItemRarityBonus;

    /// <summary>Pack size bonus from this instance.</summary>
    public float PackSizeBonus => Affix.PackSizeBonus;

    /// <summary>Reroll the value within the affix's range.</summary>
    public void Reroll(Random rng)
    {
        Value = Affix.RollValue(rng);
    }

    /// <summary>Check if the value is at maximum roll.</summary>
    public bool IsMaxRoll => Value >= Affix.MaxValue - 0.5f;

    /// <summary>Check if the value is at minimum roll.</summary>
    public bool IsMinRoll => Value <= Affix.MinValue + 0.5f;

    /// <summary>Get the tier of the roll (0-3, where 3 is best).</summary>
    public int GetRollTier()
    {
        if (Affix.MinValue == Affix.MaxValue) return 2;

        float range = Affix.MaxValue - Affix.MinValue;
        float normalized = (Value - Affix.MinValue) / range;

        return normalized switch
        {
            >= 0.75f => 3,
            >= 0.50f => 2,
            >= 0.25f => 1,
            _ => 0
        };
    }
}