using Terrascent.World.Biomes;

namespace Terrascent.Maps;

/// <summary>
/// Defines a fusion recipe that combines two biome types into a hybrid map.
/// </summary>
public class FusionRecipe
{
    /// <summary>First biome in the fusion.</summary>
    public BiomeType BiomeA { get; init; }

    /// <summary>Second biome in the fusion.</summary>
    public BiomeType BiomeB { get; init; }

    /// <summary>Name of the resulting fusion biome.</summary>
    public string FusionName { get; init; } = "";

    /// <summary>Description of the fusion effect.</summary>
    public string Description { get; init; } = "";

    /// <summary>Bonus item quantity for fusion maps.</summary>
    public float BonusItemQuantity { get; init; } = 15f;

    /// <summary>Bonus item rarity for fusion maps.</summary>
    public float BonusItemRarity { get; init; } = 20f;

    /// <summary>Check if this recipe matches the given biomes (order-independent).</summary>
    public bool Matches(BiomeType a, BiomeType b)
    {
        return (BiomeA == a && BiomeB == b) || (BiomeA == b && BiomeB == a);
    }
}

/// <summary>
/// Registry of all valid fusion recipes for map combinations.
/// Fusion maps combine two different biomes into a hybrid zone with bonus rewards.
/// </summary>
public static class MapFusionRecipes
{
    private static readonly List<FusionRecipe> _recipes = new();

    static MapFusionRecipes()
    {
        RegisterAllRecipes();
    }

    private static void RegisterAllRecipes()
    {
        // === Evil Biome Fusions ===

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Desert,
            BiomeB = BiomeType.Corruption,
            FusionName = "Corrupted Dunes",
            Description = "Shadowy sands writhe with corruption",
            BonusItemQuantity = 18f,
            BonusItemRarity = 25f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Snow,
            BiomeB = BiomeType.Crimson,
            FusionName = "Crimson Tundra",
            Description = "Blood-stained ice stretches endlessly",
            BonusItemQuantity = 18f,
            BonusItemRarity = 25f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Jungle,
            BiomeB = BiomeType.Corruption,
            FusionName = "Blighted Jungle",
            Description = "Corrupted vines strangle ancient trees",
            BonusItemQuantity = 20f,
            BonusItemRarity = 28f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Jungle,
            BiomeB = BiomeType.Crimson,
            FusionName = "Bloodroot Grove",
            Description = "Crimson growths infest the jungle floor",
            BonusItemQuantity = 20f,
            BonusItemRarity = 28f
        });

        // === Hallow Fusions ===

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Jungle,
            BiomeB = BiomeType.Hallow,
            FusionName = "Enchanted Grove",
            Description = "Rainbow light filters through crystal leaves",
            BonusItemQuantity = 22f,
            BonusItemRarity = 30f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Desert,
            BiomeB = BiomeType.Hallow,
            FusionName = "Prismatic Wastes",
            Description = "Glass sand sparkles with holy light",
            BonusItemQuantity = 18f,
            BonusItemRarity = 25f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Snow,
            BiomeB = BiomeType.Hallow,
            FusionName = "Crystal Tundra",
            Description = "Frozen rainbows arch across the sky",
            BonusItemQuantity = 18f,
            BonusItemRarity = 25f
        });

        // === Natural Biome Fusions ===

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Forest,
            BiomeB = BiomeType.Snow,
            FusionName = "Frosted Woods",
            Description = "Ancient pines heavy with snow",
            BonusItemQuantity = 12f,
            BonusItemRarity = 15f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Forest,
            BiomeB = BiomeType.Desert,
            FusionName = "Arid Savanna",
            Description = "Sparse trees dot the sandy plains",
            BonusItemQuantity = 12f,
            BonusItemRarity = 15f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Desert,
            BiomeB = BiomeType.Snow,
            FusionName = "Frozen Wastes",
            Description = "Icy winds sweep across barren dunes",
            BonusItemQuantity = 15f,
            BonusItemRarity = 18f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Jungle,
            BiomeB = BiomeType.Desert,
            FusionName = "Oasis Depths",
            Description = "Hidden jungle springs in endless sand",
            BonusItemQuantity = 16f,
            BonusItemRarity = 20f
        });

        // === Underground Fusions ===

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Underground,
            BiomeB = BiomeType.Corruption,
            FusionName = "Corrupted Depths",
            Description = "Ebonstone caverns echo with whispers",
            BonusItemQuantity = 18f,
            BonusItemRarity = 22f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Underground,
            BiomeB = BiomeType.Crimson,
            FusionName = "Flesh Caverns",
            Description = "Living walls pulse in the darkness",
            BonusItemQuantity = 18f,
            BonusItemRarity = 22f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Underground,
            BiomeB = BiomeType.Hallow,
            FusionName = "Crystal Caverns",
            Description = "Pearlstone glimmers with inner light",
            BonusItemQuantity = 20f,
            BonusItemRarity = 25f
        });

        // === Evil vs Hallow (Rare Fusions) ===

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Corruption,
            BiomeB = BiomeType.Hallow,
            FusionName = "Twilight Realm",
            Description = "Light and shadow wage eternal war",
            BonusItemQuantity = 30f,
            BonusItemRarity = 40f
        });

        Register(new FusionRecipe
        {
            BiomeA = BiomeType.Crimson,
            BiomeB = BiomeType.Hallow,
            FusionName = "Sanctified Horror",
            Description = "Holy light burns through crimson flesh",
            BonusItemQuantity = 30f,
            BonusItemRarity = 40f
        });

        System.Diagnostics.Debug.WriteLine($"MapFusionRecipes: Registered {_recipes.Count} fusion recipes");
    }

    private static void Register(FusionRecipe recipe)
    {
        _recipes.Add(recipe);
    }

    /// <summary>Get all registered fusion recipes.</summary>
    public static IEnumerable<FusionRecipe> GetAll() => _recipes;

    /// <summary>Find a recipe for the given biome combination.</summary>
    public static FusionRecipe? FindRecipe(BiomeType a, BiomeType b)
    {
        return _recipes.FirstOrDefault(r => r.Matches(a, b));
    }

    /// <summary>Check if two biomes can be fused.</summary>
    public static bool CanFuse(BiomeType a, BiomeType b)
    {
        if (a == b) return false; // Can't fuse same biomes
        return FindRecipe(a, b) != null;
    }

    /// <summary>Get the fusion name for a biome combination.</summary>
    public static string GetFusionName(BiomeType a, BiomeType b)
    {
        var recipe = FindRecipe(a, b);
        return recipe?.FusionName ?? $"{a}/{b}";
    }

    /// <summary>Get all biomes that can be fused with the given biome.</summary>
    public static IEnumerable<BiomeType> GetValidFusionPartners(BiomeType biome)
    {
        foreach (var recipe in _recipes)
        {
            if (recipe.BiomeA == biome)
                yield return recipe.BiomeB;
            else if (recipe.BiomeB == biome)
                yield return recipe.BiomeA;
        }
    }

    /// <summary>Total number of registered recipes.</summary>
    public static int RecipeCount => _recipes.Count;
}