using Terrascent.Items;

namespace Terrascent.Crafting;

/// <summary>
/// A single ingredient requirement for a recipe.
/// </summary>
public readonly struct Ingredient
{
    public ItemType Item { get; init; }
    public int Count { get; init; }

    public Ingredient(ItemType item, int count = 1)
    {
        Item = item;
        Count = count;
    }

    public override string ToString() => $"{Count}x {Item}";
}

/// <summary>
/// Category for organizing recipes in the UI.
/// </summary>
public enum RecipeCategory
{
    All,
    Tools,
    Weapons,
    Armor,
    Accessories,
    Potions,
    Furniture,
    Blocks,
    Materials,
    Stations,
    Misc,
    BossSummoning,
}

/// <summary>
/// Defines a crafting recipe: ingredients, output, and requirements.
/// </summary>
public class Recipe
{
    /// <summary>Unique recipe ID for saving/loading.</summary>
    public int Id { get; init; }

    /// <summary>Item produced by this recipe.</summary>
    public ItemType Result { get; init; }

    /// <summary>How many items are produced.</summary>
    public int ResultCount { get; init; } = 1;

    /// <summary>Required ingredients.</summary>
    public Ingredient[] Ingredients { get; init; } = Array.Empty<Ingredient>();

    /// <summary>Crafting station required (None = hand craft).</summary>
    public CraftingStationType Station { get; init; } = CraftingStationType.None;

    /// <summary>Additional station required (for multi-station recipes).</summary>
    public CraftingStationType? SecondaryStation { get; init; }

    /// <summary>Category for UI organization.</summary>
    public RecipeCategory Category { get; init; } = RecipeCategory.Misc;

    /// <summary>Must defeat this boss first (for progression gating).</summary>
    public Entities.Bosses.BossType? RequiresBossDefeated { get; init; }

    /// <summary>Time in seconds to craft (0 = instant).</summary>
    public float CraftTime { get; init; } = 0f;

    /// <summary>Display name override (defaults to result item name).</summary>
    public string? DisplayName { get; init; }

    /// <summary>Get the display name for this recipe.</summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(DisplayName))
            return DisplayName;

        var itemProps = ItemRegistry.Get(Result);
        return ResultCount > 1
            ? $"{itemProps.Name} ({ResultCount})"
            : itemProps.Name;
    }

    /// <summary>
    /// Check if player has all required ingredients.
    /// </summary>
    public bool CanCraft(Inventory inventory)
    {
        foreach (var ingredient in Ingredients)
        {
            if (inventory.CountItem(ingredient.Item) < ingredient.Count)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check how many times this recipe can be crafted.
    /// </summary>
    public int GetMaxCraftCount(Inventory inventory)
    {
        if (Ingredients.Length == 0) return 0;

        int maxCount = int.MaxValue;
        foreach (var ingredient in Ingredients)
        {
            int available = inventory.CountItem(ingredient.Item);
            int canMake = available / ingredient.Count;
            maxCount = Math.Min(maxCount, canMake);
        }
        return maxCount == int.MaxValue ? 0 : maxCount;
    }

    /// <summary>
    /// Consume ingredients from inventory. Returns false if not enough.
    /// </summary>
    public bool ConsumeIngredients(Inventory inventory)
    {
        if (!CanCraft(inventory)) return false;

        foreach (var ingredient in Ingredients)
        {
            inventory.RemoveItem(ingredient.Item, ingredient.Count);
        }
        return true;
    }

    /// <summary>
    /// Get a formatted string of required ingredients.
    /// </summary>
    public string GetIngredientsSummary()
    {
        if (Ingredients.Length == 0) return "None";

        return string.Join(", ", Ingredients.Select(i =>
        {
            var props = ItemRegistry.Get(i.Item);
            return $"{i.Count}x {props.Name}";
        }));
    }

    /// <summary>
    /// Get the station name for display.
    /// </summary>
    public string GetStationName()
    {
        if (Station == CraftingStationType.None)
            return "By Hand";

        var stationData = CraftingStationRegistry.Get(Station);
        if (SecondaryStation.HasValue)
        {
            var secondary = CraftingStationRegistry.Get(SecondaryStation.Value);
            return $"{stationData.Name} + {secondary.Name}";
        }
        return stationData.Name;
    }
}

/// <summary>
/// Builder pattern for creating recipes more easily.
/// </summary>
public class RecipeBuilder
{
    private int _id;
    private ItemType _result;
    private int _resultCount = 1;
    private readonly List<Ingredient> _ingredients = new();
    private CraftingStationType _station = CraftingStationType.None;
    private CraftingStationType? _secondaryStation;
    private RecipeCategory _category = RecipeCategory.Misc;
    private Entities.Bosses.BossType? _requiresBoss;
    private float _craftTime = 0f;
    private string? _displayName;

    public RecipeBuilder(int id, ItemType result)
    {
        _id = id;
        _result = result;
    }

    public RecipeBuilder Amount(int count)
    {
        _resultCount = count;
        return this;
    }

    public RecipeBuilder Requires(ItemType item, int count = 1)
    {
        _ingredients.Add(new Ingredient(item, count));
        return this;
    }

    public RecipeBuilder AtStation(CraftingStationType station)
    {
        _station = station;
        return this;
    }

    public RecipeBuilder AlsoRequires(CraftingStationType station)
    {
        _secondaryStation = station;
        return this;
    }

    public RecipeBuilder InCategory(RecipeCategory category)
    {
        _category = category;
        return this;
    }

    public RecipeBuilder AfterBoss(Entities.Bosses.BossType boss)
    {
        _requiresBoss = boss;
        return this;
    }

    public RecipeBuilder TakeTime(float seconds)
    {
        _craftTime = seconds;
        return this;
    }

    public RecipeBuilder Named(string name)
    {
        _displayName = name;
        return this;
    }

    public Recipe Build()
    {
        return new Recipe
        {
            Id = _id,
            Result = _result,
            ResultCount = _resultCount,
            Ingredients = _ingredients.ToArray(),
            Station = _station,
            SecondaryStation = _secondaryStation,
            Category = _category,
            RequiresBossDefeated = _requiresBoss,
            CraftTime = _craftTime,
            DisplayName = _displayName
        };
    }
}
