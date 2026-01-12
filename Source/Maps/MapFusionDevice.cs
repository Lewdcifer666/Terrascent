using Microsoft.Xna.Framework;
using Terrascent.World.Biomes;

namespace Terrascent.Maps;

/// <summary>
/// The Map Fusion Device allows players to combine two maps of the same tier
/// but different biomes into a hybrid fusion map with bonus rewards.
/// </summary>
public class MapFusionDevice
{
    /// <summary>World position of the device.</summary>
    public Vector2 Position { get; set; }

    /// <summary>First map slot.</summary>
    public MapItem? SlotA { get; private set; }

    /// <summary>Second map slot.</summary>
    public MapItem? SlotB { get; private set; }

    /// <summary>Interaction radius in pixels.</summary>
    public float InteractionRadius { get; set; } = 64f;

    /// <summary>Whether both slots are filled.</summary>
    public bool HasBothMaps => SlotA != null && SlotB != null;

    /// <summary>Whether a valid fusion is possible.</summary>
    public bool CanFuse
    {
        get
        {
            if (!HasBothMaps) return false;
            if (SlotA!.Tier != SlotB!.Tier) return false;
            if (SlotA.IsCorrupted || SlotB.IsCorrupted) return false;
            return MapFusionRecipes.CanFuse(SlotA.BiomeType, SlotB.BiomeType);
        }
    }

    /// <summary>Get the reason fusion is not possible, or null if it can fuse.</summary>
    public string? GetFusionBlockReason()
    {
        if (SlotA == null || SlotB == null)
            return "Insert two maps to fuse";

        if (SlotA.Tier != SlotB.Tier)
            return $"Maps must be same tier (T{SlotA.Tier} vs T{SlotB.Tier})";

        if (SlotA.IsCorrupted)
            return "Cannot fuse corrupted maps";

        if (SlotB.IsCorrupted)
            return "Cannot fuse corrupted maps";

        if (SlotA.BiomeType == SlotB.BiomeType)
            return "Maps must be different biomes";

        if (!MapFusionRecipes.CanFuse(SlotA.BiomeType, SlotB.BiomeType))
            return $"No fusion recipe for {SlotA.BiomeType} + {SlotB.BiomeType}";

        return null;
    }

    /// <summary>Insert a map into the device.</summary>
    public bool InsertMap(MapItem map)
    {
        if (SlotA == null)
        {
            SlotA = map;
            return true;
        }

        if (SlotB == null)
        {
            SlotB = map;
            return true;
        }

        return false; // Both slots full
    }

    /// <summary>Remove map from slot A.</summary>
    public MapItem? RemoveMapA()
    {
        var map = SlotA;
        SlotA = null;
        return map;
    }

    /// <summary>Remove map from slot B.</summary>
    public MapItem? RemoveMapB()
    {
        var map = SlotB;
        SlotB = null;
        return map;
    }

    /// <summary>Remove all maps from the device.</summary>
    public (MapItem? a, MapItem? b) RemoveAllMaps()
    {
        var result = (SlotA, SlotB);
        SlotA = null;
        SlotB = null;
        return result;
    }

    /// <summary>
    /// Attempt to fuse the two maps into a hybrid fusion map.
    /// Consumes both input maps if successful.
    /// </summary>
    public FusionResult TryFuse()
    {
        if (!CanFuse)
        {
            return new FusionResult
            {
                Success = false,
                ErrorMessage = GetFusionBlockReason() ?? "Cannot fuse"
            };
        }

        var recipe = MapFusionRecipes.FindRecipe(SlotA!.BiomeType, SlotB!.BiomeType);
        if (recipe == null)
        {
            return new FusionResult
            {
                Success = false,
                ErrorMessage = "No valid fusion recipe found"
            };
        }

        // Create the fusion map
        var fusionMap = MapItem.CreateFusion(
            SlotA.BiomeType,
            SlotB.BiomeType,
            SlotA.Tier
        );

        // Inherit the higher rarity of the two input maps
        var higherRarity = (MapRarity)Math.Max((int)SlotA.Rarity, (int)SlotB.Rarity);
        fusionMap.SetRarity(higherRarity);

        // Transfer affixes from both maps
        TransferAffixes(fusionMap, SlotA, SlotB);

        // Clear slots (maps consumed)
        SlotA = null;
        SlotB = null;

        return new FusionResult
        {
            Success = true,
            FusedMap = fusionMap,
            Recipe = recipe,
            BonusItemQuantity = recipe.BonusItemQuantity,
            BonusItemRarity = recipe.BonusItemRarity
        };
    }

    private void TransferAffixes(MapItem fusion, MapItem mapA, MapItem mapB)
    {
        var random = new Random();

        // Combine unique prefixes from both maps
        var allPrefixes = mapA.Prefixes.Concat(mapB.Prefixes)
            .GroupBy(p => p.Type)
            .Select(g => g.OrderByDescending(p => p.Value).First()) // Keep higher roll
            .ToList();

        // Combine unique suffixes from both maps
        var allSuffixes = mapA.Suffixes.Concat(mapB.Suffixes)
            .GroupBy(s => s.Type)
            .Select(g => g.OrderByDescending(s => s.Value).First())
            .ToList();

        // Shuffle and add up to max allowed
        allPrefixes = allPrefixes.OrderBy(_ => random.Next()).ToList();
        allSuffixes = allSuffixes.OrderBy(_ => random.Next()).ToList();

        int maxPrefixes = fusion.Rarity.MaxPrefixes();
        int maxSuffixes = fusion.Rarity.MaxSuffixes();

        // For fusion maps, allow one extra of each
        maxPrefixes = Math.Min(maxPrefixes + 1, 4);
        maxSuffixes = Math.Min(maxSuffixes + 1, 4);

        foreach (var prefix in allPrefixes.Take(maxPrefixes))
        {
            fusion.Prefixes.Add(prefix);
        }

        foreach (var suffix in allSuffixes.Take(maxSuffixes))
        {
            fusion.Suffixes.Add(suffix);
        }
    }

    /// <summary>Check if a position is within interaction range.</summary>
    public bool IsInRange(Vector2 position)
    {
        return Vector2.Distance(Position, position) <= InteractionRadius;
    }

    /// <summary>Get preview information about the potential fusion.</summary>
    public FusionPreview? GetFusionPreview()
    {
        if (!HasBothMaps) return null;

        var recipe = MapFusionRecipes.FindRecipe(SlotA!.BiomeType, SlotB!.BiomeType);

        return new FusionPreview
        {
            BiomeA = SlotA.BiomeType,
            BiomeB = SlotB.BiomeType,
            Tier = SlotA.Tier,
            FusionName = recipe?.FusionName ?? "Unknown Fusion",
            Description = recipe?.Description ?? "No recipe available",
            CanFuse = CanFuse,
            BlockReason = GetFusionBlockReason(),
            EstimatedBonusIQ = recipe?.BonusItemQuantity ?? 0,
            EstimatedBonusIR = recipe?.BonusItemRarity ?? 0,
            TotalPrefixes = SlotA.Prefixes.Count + SlotB.Prefixes.Count,
            TotalSuffixes = SlotA.Suffixes.Count + SlotB.Suffixes.Count
        };
    }
}

/// <summary>
/// Result of a fusion attempt.
/// </summary>
public class FusionResult
{
    public bool Success { get; init; }
    public MapItem? FusedMap { get; init; }
    public FusionRecipe? Recipe { get; init; }
    public string? ErrorMessage { get; init; }
    public float BonusItemQuantity { get; init; }
    public float BonusItemRarity { get; init; }
}

/// <summary>
/// Preview information for a potential fusion.
/// </summary>
public class FusionPreview
{
    public BiomeType BiomeA { get; init; }
    public BiomeType BiomeB { get; init; }
    public int Tier { get; init; }
    public string FusionName { get; init; } = "";
    public string Description { get; init; } = "";
    public bool CanFuse { get; init; }
    public string? BlockReason { get; init; }
    public float EstimatedBonusIQ { get; init; }
    public float EstimatedBonusIR { get; init; }
    public int TotalPrefixes { get; init; }
    public int TotalSuffixes { get; init; }
}