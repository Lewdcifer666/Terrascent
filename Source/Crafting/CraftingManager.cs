using Microsoft.Xna.Framework;
using Terrascent.Entities;
using Terrascent.Entities.Bosses;
using Terrascent.Items;
using Terrascent.World;

namespace Terrascent.Crafting;

/// <summary>
/// Manages crafting operations, station detection, and recipe filtering.
/// </summary>
public class CraftingManager
{
    private readonly Player _player;
    private readonly BossManager? _bossManager;

    // Available stations near player
    private readonly HashSet<CraftingStationType> _nearbyStations = new();

    // Placed stations in world (tile positions)
    private readonly Dictionary<Point, CraftingStationType> _placedStations = new();

    // Current crafting state
    private Recipe? _currentRecipe;
    private float _craftingProgress;
    private bool _isCrafting;

    // Detection range for stations
    private const float STATION_DETECT_RANGE = 96f;  // 6 tiles

    // Events
    public event Action<Recipe>? OnCraftingStarted;
    public event Action<Recipe, ItemType, int>? OnItemCrafted;
    public event Action<Recipe>? OnCraftingCancelled;

    /// <summary>Currently available stations near player.</summary>
    public IReadOnlySet<CraftingStationType> NearbyStations => _nearbyStations;

    /// <summary>Is player currently crafting something?</summary>
    public bool IsCrafting => _isCrafting;

    /// <summary>Current crafting progress (0-1).</summary>
    public float CraftingProgress => _craftingProgress;

    /// <summary>Recipe currently being crafted.</summary>
    public Recipe? CurrentRecipe => _currentRecipe;

    public CraftingManager(Player player, BossManager? bossManager = null)
    {
        _player = player;
        _bossManager = bossManager;

        // Initialize registry
        RecipeRegistry.Initialize();
        CraftingStationRegistry.Initialize();
    }

    /// <summary>
    /// Update crafting state. Call every frame.
    /// </summary>
    public void Update(float deltaTime, ChunkManager chunks)
    {
        // Update nearby stations
        UpdateNearbyStations(chunks);

        // Update crafting progress
        if (_isCrafting && _currentRecipe != null)
        {
            if (_currentRecipe.CraftTime <= 0)
            {
                // Instant craft
                CompleteCrafting();
            }
            else
            {
                _craftingProgress += deltaTime / _currentRecipe.CraftTime;
                if (_craftingProgress >= 1f)
                {
                    CompleteCrafting();
                }
            }
        }
    }

    /// <summary>
    /// Detect which crafting stations are near the player.
    /// </summary>
    private void UpdateNearbyStations(ChunkManager chunks)
    {
        _nearbyStations.Clear();

        // Always have "None" (hand crafting)
        _nearbyStations.Add(CraftingStationType.None);

        // Check placed stations
        foreach (var (pos, stationType) in _placedStations)
        {
            Vector2 stationWorld = WorldCoordinates.TileToWorld(pos) + new Vector2(8, 8);  // Center
            float distance = Vector2.Distance(_player.Center, stationWorld);

            if (distance <= STATION_DETECT_RANGE)
            {
                _nearbyStations.Add(stationType);
            }
        }

        // Check for liquid stations
        Point playerTile = WorldCoordinates.WorldToTile(_player.Center);

        // Check surrounding tiles for liquids
        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dy = -3; dy <= 3; dy++)
            {
                Point checkPos = new Point(playerTile.X + dx, playerTile.Y + dy);
                var tile = chunks.GetTileAt(checkPos);

                // TODO: Add liquid detection when liquids are implemented
                // For now, we'll skip liquid station detection
            }
        }

        // For testing: Add workbench always available
        // Remove this in production when proper station placing is implemented
        _nearbyStations.Add(CraftingStationType.Workbench);
        _nearbyStations.Add(CraftingStationType.Furnace);
        _nearbyStations.Add(CraftingStationType.Anvil);
    }

    /// <summary>
    /// Register a placed crafting station.
    /// </summary>
    public void RegisterStation(Point tilePos, CraftingStationType type)
    {
        _placedStations[tilePos] = type;
        System.Diagnostics.Debug.WriteLine($"[CRAFTING] Registered {type} at {tilePos}");
    }

    /// <summary>
    /// Remove a crafting station (when tile is destroyed).
    /// </summary>
    public void UnregisterStation(Point tilePos)
    {
        if (_placedStations.Remove(tilePos, out var type))
        {
            System.Diagnostics.Debug.WriteLine($"[CRAFTING] Removed {type} at {tilePos}");
        }
    }

    /// <summary>
    /// Get all recipes craftable with current inventory and stations.
    /// </summary>
    public IEnumerable<Recipe> GetAvailableRecipes()
    {
        var defeatedBosses = _bossManager != null
            ? new HashSet<BossType>(
                Enum.GetValues<BossType>().Where(b => _bossManager.IsBossDefeated(b)))
            : null;

        return RecipeRegistry.GetCraftable(_player.Inventory, _nearbyStations, defeatedBosses);
    }

    /// <summary>
    /// Get all recipes for a specific category that could be crafted
    /// (may not have materials, but have station access).
    /// </summary>
    public IEnumerable<Recipe> GetRecipesForCategory(RecipeCategory category)
    {
        var defeatedBosses = _bossManager != null
            ? new HashSet<BossType>(
                Enum.GetValues<BossType>().Where(b => _bossManager.IsBossDefeated(b)))
            : null;

        foreach (var recipe in RecipeRegistry.GetByCategory(category))
        {
            // Check station availability
            if (!_nearbyStations.Contains(recipe.Station))
                continue;

            if (recipe.SecondaryStation.HasValue && !_nearbyStations.Contains(recipe.SecondaryStation.Value))
                continue;

            // Check boss requirement
            if (recipe.RequiresBossDefeated.HasValue)
            {
                if (defeatedBosses == null || !defeatedBosses.Contains(recipe.RequiresBossDefeated.Value))
                    continue;
            }

            yield return recipe;
        }
    }

    /// <summary>
    /// Get all recipes the player can currently craft (has materials).
    /// </summary>
    public IEnumerable<Recipe> GetCraftableRecipes()
    {
        return GetAvailableRecipes();
    }

    /// <summary>
    /// Check if a specific recipe can be crafted right now.
    /// </summary>
    public bool CanCraft(Recipe recipe)
    {
        // Check if we're already crafting
        if (_isCrafting) return false;

        // Check station availability
        if (!_nearbyStations.Contains(recipe.Station))
            return false;

        if (recipe.SecondaryStation.HasValue && !_nearbyStations.Contains(recipe.SecondaryStation.Value))
            return false;

        // Check boss requirement
        if (recipe.RequiresBossDefeated.HasValue && _bossManager != null)
        {
            if (!_bossManager.IsBossDefeated(recipe.RequiresBossDefeated.Value))
                return false;
        }

        // Check ingredients
        return recipe.CanCraft(_player.Inventory);
    }

    /// <summary>
    /// Start crafting a recipe.
    /// </summary>
    public bool StartCrafting(Recipe recipe)
    {
        if (!CanCraft(recipe)) return false;

        // Consume ingredients
        if (!recipe.ConsumeIngredients(_player.Inventory))
            return false;

        _currentRecipe = recipe;
        _craftingProgress = 0f;
        _isCrafting = true;

        OnCraftingStarted?.Invoke(recipe);

        // If instant, complete immediately
        if (recipe.CraftTime <= 0)
        {
            CompleteCrafting();
        }

        return true;
    }

    /// <summary>
    /// Craft a recipe multiple times at once.
    /// </summary>
    public int CraftMultiple(Recipe recipe, int count)
    {
        int maxCraftable = recipe.GetMaxCraftCount(_player.Inventory);
        int actualCount = Math.Min(count, maxCraftable);

        if (actualCount <= 0) return 0;

        int totalCrafted = 0;
        for (int i = 0; i < actualCount; i++)
        {
            // For timed recipes, only craft one at a time
            if (recipe.CraftTime > 0 && i > 0)
                break;

            if (StartCrafting(recipe))
            {
                totalCrafted++;

                // For instant recipes, continue
                if (recipe.CraftTime <= 0)
                    continue;
                else
                    break;  // Wait for current craft to complete
            }
            else
            {
                break;
            }
        }

        return totalCrafted;
    }

    /// <summary>
    /// Complete the current crafting operation.
    /// </summary>
    private void CompleteCrafting()
    {
        if (_currentRecipe == null) return;

        // Add crafted item to inventory
        int overflow = _player.Inventory.AddItem(_currentRecipe.Result, _currentRecipe.ResultCount);

        if (overflow > 0)
        {
            // TODO: Drop overflow items on ground
            System.Diagnostics.Debug.WriteLine($"[CRAFTING] Overflow: {overflow}x {_currentRecipe.Result}");
        }

        OnItemCrafted?.Invoke(_currentRecipe, _currentRecipe.Result, _currentRecipe.ResultCount);
        System.Diagnostics.Debug.WriteLine($"[CRAFTING] Crafted {_currentRecipe.ResultCount}x {_currentRecipe.Result}");

        _currentRecipe = null;
        _craftingProgress = 0f;
        _isCrafting = false;
    }

    /// <summary>
    /// Cancel current crafting operation.
    /// </summary>
    public void CancelCrafting()
    {
        if (!_isCrafting || _currentRecipe == null) return;

        // Refund ingredients
        foreach (var ingredient in _currentRecipe.Ingredients)
        {
            _player.Inventory.AddItem(ingredient.Item, ingredient.Count);
        }

        OnCraftingCancelled?.Invoke(_currentRecipe);

        _currentRecipe = null;
        _craftingProgress = 0f;
        _isCrafting = false;
    }

    /// <summary>
    /// Quick craft: craft as many as possible of a recipe.
    /// </summary>
    public int QuickCraft(Recipe recipe)
    {
        int maxCraftable = recipe.GetMaxCraftCount(_player.Inventory);

        // For instant recipes, craft all
        if (recipe.CraftTime <= 0)
        {
            return CraftMultiple(recipe, maxCraftable);
        }
        else
        {
            // For timed recipes, just craft one
            return StartCrafting(recipe) ? 1 : 0;
        }
    }

    /// <summary>
    /// Get a summary of what stations are nearby.
    /// </summary>
    public string GetNearbyStationsSummary()
    {
        if (_nearbyStations.Count <= 1)  // Only "None"
            return "No crafting stations nearby";

        var names = _nearbyStations
            .Where(s => s != CraftingStationType.None)
            .Select(s => CraftingStationRegistry.Get(s).Name);

        return string.Join(", ", names);
    }

    /// <summary>
    /// Clear all placed stations (for world reset).
    /// </summary>
    public void Clear()
    {
        _placedStations.Clear();
        _nearbyStations.Clear();
        _nearbyStations.Add(CraftingStationType.None);
        CancelCrafting();
    }
}
