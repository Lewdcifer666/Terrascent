using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.Crafting;
using Terrascent.Items;

namespace Terrascent.UI;

/// <summary>
/// Terraria-style crafting UI with category tabs, recipe list, and crafting preview.
/// Integrates with inventory UI and supports keyboard shortcuts.
/// </summary>
public class CraftingUI
{
    private readonly CraftingManager _craftingManager;
    private readonly Inventory _inventory;

    // Layout constants
    private const int PANEL_WIDTH = 320;
    private const int PANEL_PADDING = 10;
    private const int TAB_HEIGHT = 28;
    private const int TAB_WIDTH = 70;
    private const int RECIPE_HEIGHT = 40;
    private const int RECIPE_PADDING = 4;
    private const int SCROLL_BAR_WIDTH = 12;
    private const int MAX_VISIBLE_RECIPES = 8;

    // Positions
    private Rectangle _panelBounds;
    private Rectangle _tabAreaBounds;
    private Rectangle _recipeListBounds;
    private Rectangle _scrollBarBounds;
    private Rectangle _craftButtonBounds;
    private Rectangle _quickCraftButtonBounds;
    private Rectangle[] _tabBounds = Array.Empty<Rectangle>();
    private Rectangle[] _recipeSlotBounds = Array.Empty<Rectangle>();

    // State
    private RecipeCategory _selectedCategory = RecipeCategory.All;
    private int _scrollOffset = 0;
    private int _hoveredRecipeIndex = -1;
    private int _selectedRecipeIndex = -1;
    private Recipe? _selectedRecipe;
    private int _hoveredTab = -1;
    private bool _showCraftableOnly = false;
    private float _craftingAnimTimer = 0f;

    // Categories to display (order matters for tabs)
    private readonly RecipeCategory[] _categories = new[]
    {
        RecipeCategory.All,
        RecipeCategory.Tools,
        RecipeCategory.Weapons,
        RecipeCategory.Armor,
        RecipeCategory.Accessories,
        RecipeCategory.Potions,
        RecipeCategory.Blocks,
        RecipeCategory.Stations,
        RecipeCategory.Misc
    };

    // Cached recipes for current view
    private List<Recipe> _filteredRecipes = new();
    private bool _needsRefresh = true;

    // Screen dimensions
    private int _screenWidth;
    private int _screenHeight;

    // Tooltip
    private Recipe? _tooltipRecipe;
    private bool _showTooltip;

    // Events
    public event Action<Recipe>? OnRecipeCrafted;

    public CraftingUI(CraftingManager craftingManager, Inventory inventory, int screenWidth, int screenHeight)
    {
        _craftingManager = craftingManager;
        _inventory = inventory;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CalculateLayout();

        // Subscribe to crafting events
        _craftingManager.OnItemCrafted += HandleItemCrafted;
    }

    /// <summary>
    /// Calculate all UI element positions.
    /// </summary>
    private void CalculateLayout()
    {
        // Panel position (left side of screen, below top margin)
        int panelHeight = TAB_HEIGHT + MAX_VISIBLE_RECIPES * RECIPE_HEIGHT + PANEL_PADDING * 4 + 60; // +60 for buttons
        int panelX = 20;
        int panelY = 70; // Below XP bar area

        _panelBounds = new Rectangle(panelX, panelY, PANEL_WIDTH, panelHeight);

        // Tab area (top of panel)
        _tabAreaBounds = new Rectangle(
            panelX + PANEL_PADDING,
            panelY + PANEL_PADDING,
            PANEL_WIDTH - PANEL_PADDING * 2,
            TAB_HEIGHT
        );

        // Calculate tab bounds
        int numTabs = Math.Min(_categories.Length, 4); // Show max 4 tabs at a time
        _tabBounds = new Rectangle[_categories.Length];
        int tabX = _tabAreaBounds.X;
        for (int i = 0; i < _categories.Length; i++)
        {
            _tabBounds[i] = new Rectangle(tabX, _tabAreaBounds.Y, TAB_WIDTH, TAB_HEIGHT);
            tabX += TAB_WIDTH + 2;
            if (tabX + TAB_WIDTH > _tabAreaBounds.Right)
            {
                tabX = _tabAreaBounds.X;
            }
        }

        // Recipe list area
        int listTop = _tabAreaBounds.Bottom + PANEL_PADDING;
        int listHeight = MAX_VISIBLE_RECIPES * RECIPE_HEIGHT;
        _recipeListBounds = new Rectangle(
            panelX + PANEL_PADDING,
            listTop,
            PANEL_WIDTH - PANEL_PADDING * 2 - SCROLL_BAR_WIDTH,
            listHeight
        );

        // Scroll bar
        _scrollBarBounds = new Rectangle(
            _recipeListBounds.Right + 2,
            listTop,
            SCROLL_BAR_WIDTH - 2,
            listHeight
        );

        // Recipe slots
        _recipeSlotBounds = new Rectangle[MAX_VISIBLE_RECIPES];
        for (int i = 0; i < MAX_VISIBLE_RECIPES; i++)
        {
            _recipeSlotBounds[i] = new Rectangle(
                _recipeListBounds.X,
                _recipeListBounds.Y + i * RECIPE_HEIGHT,
                _recipeListBounds.Width,
                RECIPE_HEIGHT - RECIPE_PADDING
            );
        }

        // Craft button
        int buttonY = _recipeListBounds.Bottom + PANEL_PADDING;
        _craftButtonBounds = new Rectangle(
            panelX + PANEL_PADDING,
            buttonY,
            120,
            32
        );

        // Quick craft button
        _quickCraftButtonBounds = new Rectangle(
            _craftButtonBounds.Right + 10,
            buttonY,
            120,
            32
        );
    }

    /// <summary>
    /// Handle screen resize.
    /// </summary>
    public void OnScreenResize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        CalculateLayout();
    }

    /// <summary>
    /// Refresh the filtered recipe list.
    /// </summary>
    private void RefreshRecipes()
    {
        _filteredRecipes.Clear();

        IEnumerable<Recipe> sourceRecipes;

        if (_showCraftableOnly)
        {
            sourceRecipes = _craftingManager.GetCraftableRecipes();
        }
        else
        {
            sourceRecipes = _selectedCategory == RecipeCategory.All
                ? RecipeRegistry.GetAll()
                : RecipeRegistry.GetByCategory(_selectedCategory);
        }

        // Filter by station availability if showing all (not just craftable)
        if (!_showCraftableOnly)
        {
            sourceRecipes = sourceRecipes.Where(r =>
                _craftingManager.NearbyStations.Contains(r.Station) &&
                (!r.SecondaryStation.HasValue || _craftingManager.NearbyStations.Contains(r.SecondaryStation.Value)));
        }

        // Apply category filter on craftable if needed
        if (_showCraftableOnly && _selectedCategory != RecipeCategory.All)
        {
            sourceRecipes = sourceRecipes.Where(r => r.Category == _selectedCategory);
        }

        _filteredRecipes = sourceRecipes.OrderBy(r => r.Category).ThenBy(r => r.GetDisplayName()).ToList();

        // Reset selection if out of bounds
        if (_selectedRecipeIndex >= _filteredRecipes.Count)
        {
            _selectedRecipeIndex = _filteredRecipes.Count - 1;
        }

        // Update selected recipe reference
        _selectedRecipe = _selectedRecipeIndex >= 0 && _selectedRecipeIndex < _filteredRecipes.Count
            ? _filteredRecipes[_selectedRecipeIndex]
            : null;

        // Clamp scroll
        int maxScroll = Math.Max(0, _filteredRecipes.Count - MAX_VISIBLE_RECIPES);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

        _needsRefresh = false;
    }

    /// <summary>
    /// Update the crafting UI.
    /// </summary>
    public void Update(InputManager input, float deltaTime)
    {
        // Refresh if needed
        if (_needsRefresh)
        {
            RefreshRecipes();
        }

        // Update crafting animation
        if (_craftingManager.IsCrafting)
        {
            _craftingAnimTimer += deltaTime * 3f;
        }
        else
        {
            _craftingAnimTimer = 0f;
        }

        Vector2 mousePos = input.MousePositionV;
        bool shiftHeld = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);

        // Check tab hover
        _hoveredTab = -1;
        for (int i = 0; i < _tabBounds.Length; i++)
        {
            if (_tabBounds[i].Contains(mousePos))
            {
                _hoveredTab = i;
                break;
            }
        }

        // Check recipe hover
        _hoveredRecipeIndex = -1;
        _showTooltip = false;
        _tooltipRecipe = null;

        for (int i = 0; i < MAX_VISIBLE_RECIPES; i++)
        {
            int recipeIndex = _scrollOffset + i;
            if (recipeIndex >= _filteredRecipes.Count) break;

            if (_recipeSlotBounds[i].Contains(mousePos))
            {
                _hoveredRecipeIndex = recipeIndex;
                _tooltipRecipe = _filteredRecipes[recipeIndex];
                _showTooltip = true;
                break;
            }
        }

        // Handle tab clicks
        if (input.IsLeftMousePressed() && _hoveredTab >= 0)
        {
            input.ConsumeMousePress(left: true);
            SelectCategory(_categories[_hoveredTab]);
        }

        // Handle recipe clicks
        if (input.IsLeftMousePressed() && _hoveredRecipeIndex >= 0)
        {
            input.ConsumeMousePress(left: true);
            _selectedRecipeIndex = _hoveredRecipeIndex;
            _selectedRecipe = _filteredRecipes[_selectedRecipeIndex];

            // Double-click detection could be added here for quick craft
        }

        // Handle right-click for quick craft
        if (input.IsRightMousePressed() && _hoveredRecipeIndex >= 0)
        {
            input.ConsumeMousePress(right: true);
            var recipe = _filteredRecipes[_hoveredRecipeIndex];
            if (shiftHeld)
            {
                _craftingManager.QuickCraft(recipe);
            }
            else
            {
                _craftingManager.StartCrafting(recipe);
            }
            _needsRefresh = true;
        }

        // Handle craft button click
        if (input.IsLeftMousePressed() && _craftButtonBounds.Contains(mousePos))
        {
            input.ConsumeMousePress(left: true);
            if (_selectedRecipe != null)
            {
                _craftingManager.StartCrafting(_selectedRecipe);
                _needsRefresh = true;
            }
        }

        // Handle quick craft button click
        if (input.IsLeftMousePressed() && _quickCraftButtonBounds.Contains(mousePos))
        {
            input.ConsumeMousePress(left: true);
            if (_selectedRecipe != null)
            {
                _craftingManager.QuickCraft(_selectedRecipe);
                _needsRefresh = true;
            }
        }

        // Handle scroll wheel
        int scroll = input.ScrollWheelDelta;
        if (scroll != 0 && _recipeListBounds.Contains(mousePos))
        {
            _scrollOffset -= Math.Sign(scroll);
            int maxScroll = Math.Max(0, _filteredRecipes.Count - MAX_VISIBLE_RECIPES);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        }

        // Keyboard shortcuts
        if (input.IsKeyPressed(Keys.Tab))
        {
            input.ConsumeKeyPress(Keys.Tab);
            _showCraftableOnly = !_showCraftableOnly;
            _needsRefresh = true;
        }

        // Arrow key navigation
        if (input.IsKeyPressed(Keys.Up))
        {
            input.ConsumeKeyPress(Keys.Up);
            if (_selectedRecipeIndex > 0)
            {
                _selectedRecipeIndex--;
                _selectedRecipe = _filteredRecipes[_selectedRecipeIndex];

                // Scroll to keep selection visible
                if (_selectedRecipeIndex < _scrollOffset)
                {
                    _scrollOffset = _selectedRecipeIndex;
                }
            }
        }

        if (input.IsKeyPressed(Keys.Down))
        {
            input.ConsumeKeyPress(Keys.Down);
            if (_selectedRecipeIndex < _filteredRecipes.Count - 1)
            {
                _selectedRecipeIndex++;
                _selectedRecipe = _filteredRecipes[_selectedRecipeIndex];

                // Scroll to keep selection visible
                if (_selectedRecipeIndex >= _scrollOffset + MAX_VISIBLE_RECIPES)
                {
                    _scrollOffset = _selectedRecipeIndex - MAX_VISIBLE_RECIPES + 1;
                }
            }
        }

        // Enter to craft
        if (input.IsKeyPressed(Keys.Enter))
        {
            input.ConsumeKeyPress(Keys.Enter);
            if (_selectedRecipe != null)
            {
                if (shiftHeld)
                {
                    _craftingManager.QuickCraft(_selectedRecipe);
                }
                else
                {
                    _craftingManager.StartCrafting(_selectedRecipe);
                }
                _needsRefresh = true;
            }
        }

        // Number keys for category selection (1-9)
        for (int i = 0; i < 9 && i < _categories.Length; i++)
        {
            Keys key = Keys.D1 + i;
            if (input.IsKeyPressed(key))
            {
                input.ConsumeKeyPress(key);
                SelectCategory(_categories[i]);
            }
        }
    }

    /// <summary>
    /// Select a category and refresh recipes.
    /// </summary>
    private void SelectCategory(RecipeCategory category)
    {
        if (_selectedCategory != category)
        {
            _selectedCategory = category;
            _scrollOffset = 0;
            _selectedRecipeIndex = -1;
            _selectedRecipe = null;
            _needsRefresh = true;
        }
    }

    /// <summary>
    /// Mark recipes as needing refresh.
    /// </summary>
    public void MarkDirty()
    {
        _needsRefresh = true;
    }

    /// <summary>
    /// Handle crafting completion.
    /// </summary>
    private void HandleItemCrafted(Recipe recipe, ItemType item, int count)
    {
        _needsRefresh = true;
        OnRecipeCrafted?.Invoke(recipe);
    }

    /// <summary>
    /// Draw the crafting UI.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        // Draw panel background
        DrawPanel(spriteBatch, pixelTexture);

        // Draw tabs
        DrawTabs(spriteBatch, pixelTexture);

        // Draw recipe list
        DrawRecipeList(spriteBatch, pixelTexture);

        // Draw scroll bar
        DrawScrollBar(spriteBatch, pixelTexture);

        // Draw craft buttons
        DrawButtons(spriteBatch, pixelTexture, mousePosition);

        // Draw crafting progress if active
        if (_craftingManager.IsCrafting)
        {
            DrawCraftingProgress(spriteBatch, pixelTexture);
        }

        // Draw tooltip
        if (_showTooltip && _tooltipRecipe != null)
        {
            DrawTooltip(spriteBatch, pixelTexture, mousePosition);
        }
    }

    /// <summary>
    /// Draw panel background with border.
    /// </summary>
    private void DrawPanel(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        // Background
        spriteBatch.Draw(pixelTexture, _panelBounds, new Color(30, 30, 40, 240));

        // Border
        int borderWidth = 2;
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.X, _panelBounds.Y, _panelBounds.Width, borderWidth),
            new Color(80, 80, 100));
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.X, _panelBounds.Bottom - borderWidth, _panelBounds.Width, borderWidth),
            new Color(80, 80, 100));
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.X, _panelBounds.Y, borderWidth, _panelBounds.Height),
            new Color(80, 80, 100));
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.Right - borderWidth, _panelBounds.Y, borderWidth, _panelBounds.Height),
            new Color(80, 80, 100));

        // Title
        DrawText(spriteBatch, pixelTexture,
            _showCraftableOnly ? "CRAFTABLE" : "RECIPES",
            _panelBounds.X + PANEL_PADDING,
            _panelBounds.Y + 2,
            Color.White);

        // Station indicator
        string stationText = _craftingManager.GetNearbyStationsSummary();
        if (stationText.Length > 35) stationText = stationText.Substring(0, 32) + "...";
        DrawTextSmall(spriteBatch, pixelTexture,
            stationText,
            _panelBounds.X + PANEL_PADDING,
            _panelBounds.Bottom - 18,
            Color.Gray);
    }

    /// <summary>
    /// Draw category tabs.
    /// </summary>
    private void DrawTabs(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        for (int i = 0; i < _categories.Length && i < 8; i++)
        {
            var bounds = _tabBounds[i];
            bool selected = _categories[i] == _selectedCategory;
            bool hovered = i == _hoveredTab;

            // Tab background
            Color bgColor = selected
                ? new Color(60, 60, 80)
                : (hovered ? new Color(50, 50, 65) : new Color(40, 40, 55));
            spriteBatch.Draw(pixelTexture, bounds, bgColor);

            // Tab border (bottom only if not selected)
            if (!selected)
            {
                spriteBatch.Draw(pixelTexture,
                    new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1),
                    new Color(80, 80, 100));
            }

            // Tab text
            string tabName = GetCategoryShortName(_categories[i]);
            Color textColor = selected ? Color.White : (hovered ? Color.LightGray : Color.Gray);
            DrawTextSmall(spriteBatch, pixelTexture, tabName, bounds.X + 4, bounds.Y + 8, textColor);
        }
    }

    /// <summary>
    /// Get short name for category tab.
    /// </summary>
    private string GetCategoryShortName(RecipeCategory category)
    {
        return category switch
        {
            RecipeCategory.All => "ALL",
            RecipeCategory.Tools => "TOOL",
            RecipeCategory.Weapons => "WEAP",
            RecipeCategory.Armor => "ARMR",
            RecipeCategory.Accessories => "ACC",
            RecipeCategory.Potions => "POT",
            RecipeCategory.Furniture => "FURN",
            RecipeCategory.Blocks => "BLCK",
            RecipeCategory.Materials => "MAT",
            RecipeCategory.Stations => "STAT",
            RecipeCategory.Misc => "MISC",
            RecipeCategory.BossSummoning => "BOSS",
            _ => category.ToString().ToUpper().Substring(0, 4)
        };
    }

    /// <summary>
    /// Draw the recipe list.
    /// </summary>
    private void DrawRecipeList(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        // Background
        spriteBatch.Draw(pixelTexture, _recipeListBounds, new Color(20, 20, 30));

        if (_filteredRecipes.Count == 0)
        {
            DrawText(spriteBatch, pixelTexture,
                "NO RECIPES",
                _recipeListBounds.X + 10,
                _recipeListBounds.Y + 10,
                Color.Gray);
            return;
        }

        // Draw visible recipes
        for (int i = 0; i < MAX_VISIBLE_RECIPES; i++)
        {
            int recipeIndex = _scrollOffset + i;
            if (recipeIndex >= _filteredRecipes.Count) break;

            var recipe = _filteredRecipes[recipeIndex];
            var bounds = _recipeSlotBounds[i];

            bool selected = recipeIndex == _selectedRecipeIndex;
            bool hovered = recipeIndex == _hoveredRecipeIndex;
            bool canCraft = recipe.CanCraft(_inventory);

            // Background
            Color bgColor;
            if (selected)
                bgColor = new Color(60, 60, 100);
            else if (hovered)
                bgColor = new Color(45, 45, 70);
            else
                bgColor = new Color(30, 30, 45);

            spriteBatch.Draw(pixelTexture, bounds, bgColor);

            // Item icon placeholder (colored square)
            var iconBounds = new Rectangle(bounds.X + 4, bounds.Y + 4, 28, 28);
            Color itemColor = InventoryUI.GetItemColor(recipe.Result);
            spriteBatch.Draw(pixelTexture, iconBounds, canCraft ? itemColor : itemColor * 0.5f);

            // Item name
            string name = recipe.GetDisplayName();
            if (name.Length > 20) name = name.Substring(0, 17) + "...";
            Color textColor = canCraft ? Color.White : Color.Gray;
            DrawTextSmall(spriteBatch, pixelTexture, name, bounds.X + 36, bounds.Y + 6, textColor);

            // Ingredients summary (abbreviated)
            string ingr = GetAbbreviatedIngredients(recipe);
            DrawTextSmall(spriteBatch, pixelTexture, ingr, bounds.X + 36, bounds.Y + 20, Color.DarkGray);

            // Craft count indicator
            if (canCraft)
            {
                int maxCraft = recipe.GetMaxCraftCount(_inventory);
                if (maxCraft > 1)
                {
                    DrawTextSmall(spriteBatch, pixelTexture,
                        $"x{maxCraft}",
                        bounds.Right - 30,
                        bounds.Y + 12,
                        Color.LightGreen);
                }
            }
        }
    }

    /// <summary>
    /// Get abbreviated ingredients string.
    /// </summary>
    private string GetAbbreviatedIngredients(Recipe recipe)
    {
        if (recipe.Ingredients.Length == 0) return "Free";

        var parts = recipe.Ingredients.Select(i =>
        {
            string name = ItemRegistry.Get(i.Item).Name;
            if (name.Length > 6) name = name.Substring(0, 5) + ".";
            return $"{i.Count}{name}";
        });

        string result = string.Join(" ", parts);
        return result.Length > 30 ? result.Substring(0, 27) + "..." : result;
    }

    /// <summary>
    /// Draw scroll bar.
    /// </summary>
    private void DrawScrollBar(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        // Track
        spriteBatch.Draw(pixelTexture, _scrollBarBounds, new Color(20, 20, 30));

        if (_filteredRecipes.Count <= MAX_VISIBLE_RECIPES) return;

        // Thumb
        float viewRatio = (float)MAX_VISIBLE_RECIPES / _filteredRecipes.Count;
        int thumbHeight = (int)(_scrollBarBounds.Height * viewRatio);
        thumbHeight = Math.Max(thumbHeight, 20);

        float scrollRatio = _filteredRecipes.Count > MAX_VISIBLE_RECIPES
            ? (float)_scrollOffset / (_filteredRecipes.Count - MAX_VISIBLE_RECIPES)
            : 0;
        int thumbY = _scrollBarBounds.Y + (int)((_scrollBarBounds.Height - thumbHeight) * scrollRatio);

        spriteBatch.Draw(pixelTexture,
            new Rectangle(_scrollBarBounds.X + 2, thumbY, _scrollBarBounds.Width - 4, thumbHeight),
            new Color(80, 80, 100));
    }

    /// <summary>
    /// Draw craft buttons.
    /// </summary>
    private void DrawButtons(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        bool canCraft = _selectedRecipe != null && _craftingManager.CanCraft(_selectedRecipe);
        bool craftHover = _craftButtonBounds.Contains(mousePosition);
        bool quickHover = _quickCraftButtonBounds.Contains(mousePosition);

        // Craft button
        Color craftBg = canCraft
            ? (craftHover ? new Color(80, 120, 80) : new Color(60, 100, 60))
            : new Color(50, 50, 50);
        spriteBatch.Draw(pixelTexture, _craftButtonBounds, craftBg);
        DrawText(spriteBatch, pixelTexture, "CRAFT",
            _craftButtonBounds.X + 30,
            _craftButtonBounds.Y + 8,
            canCraft ? Color.White : Color.Gray);

        // Quick craft button
        Color quickBg = canCraft
            ? (quickHover ? new Color(80, 80, 120) : new Color(60, 60, 100))
            : new Color(50, 50, 50);
        spriteBatch.Draw(pixelTexture, _quickCraftButtonBounds, quickBg);
        DrawText(spriteBatch, pixelTexture, "CRAFT ALL",
            _quickCraftButtonBounds.X + 15,
            _quickCraftButtonBounds.Y + 8,
            canCraft ? Color.White : Color.Gray);
    }

    /// <summary>
    /// Draw crafting progress bar.
    /// </summary>
    private void DrawCraftingProgress(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        if (_craftingManager.CurrentRecipe == null) return;

        var progressBounds = new Rectangle(
            _craftButtonBounds.X,
            _craftButtonBounds.Bottom + 5,
            _quickCraftButtonBounds.Right - _craftButtonBounds.X,
            10
        );

        // Background
        spriteBatch.Draw(pixelTexture, progressBounds, new Color(30, 30, 40));

        // Progress fill
        int fillWidth = (int)(progressBounds.Width * _craftingManager.CraftingProgress);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(progressBounds.X, progressBounds.Y, fillWidth, progressBounds.Height),
            new Color(100, 200, 100));

        // Animated shine
        int shineX = progressBounds.X + (int)(_craftingAnimTimer * 50) % progressBounds.Width;
        spriteBatch.Draw(pixelTexture,
            new Rectangle(shineX, progressBounds.Y, 3, progressBounds.Height),
            Color.White * 0.3f);
    }

    /// <summary>
    /// Draw recipe tooltip.
    /// </summary>
    private void DrawTooltip(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        if (_tooltipRecipe == null) return;

        // Calculate tooltip size
        int tooltipWidth = 200;
        int tooltipHeight = 80 + _tooltipRecipe.Ingredients.Length * 14;

        // Position tooltip to not go off screen
        int tooltipX = (int)mousePosition.X + 15;
        int tooltipY = (int)mousePosition.Y + 10;

        if (tooltipX + tooltipWidth > _screenWidth - 10)
            tooltipX = (int)mousePosition.X - tooltipWidth - 5;
        if (tooltipY + tooltipHeight > _screenHeight - 10)
            tooltipY = _screenHeight - tooltipHeight - 10;

        var bounds = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);

        // Background
        spriteBatch.Draw(pixelTexture, bounds, new Color(20, 20, 30, 250));

        // Border
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), Color.Gray);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), Color.Gray);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), Color.Gray);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), Color.Gray);

        int y = bounds.Y + 6;

        // Item name
        DrawText(spriteBatch, pixelTexture, _tooltipRecipe.GetDisplayName(), bounds.X + 6, y, Color.White);
        y += 16;

        // Station requirement
        string station = _tooltipRecipe.GetStationName();
        DrawTextSmall(spriteBatch, pixelTexture, $"Station: {station}", bounds.X + 6, y, Color.LightBlue);
        y += 14;

        // Separator
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.X + 4, y, bounds.Width - 8, 1), Color.Gray * 0.5f);
        y += 6;

        // Ingredients header
        DrawTextSmall(spriteBatch, pixelTexture, "Ingredients:", bounds.X + 6, y, Color.Yellow);
        y += 14;

        // List ingredients
        foreach (var ingredient in _tooltipRecipe.Ingredients)
        {
            var itemProps = ItemRegistry.Get(ingredient.Item);
            int have = _inventory.CountItem(ingredient.Item);
            Color color = have >= ingredient.Count ? Color.LightGreen : Color.Red;

            string line = $"  {ingredient.Count}x {itemProps.Name} ({have})";
            DrawTextSmall(spriteBatch, pixelTexture, line, bounds.X + 6, y, color);
            y += 12;
        }

        // Craft time if applicable
        if (_tooltipRecipe.CraftTime > 0)
        {
            y += 4;
            DrawTextSmall(spriteBatch, pixelTexture,
                $"Time: {_tooltipRecipe.CraftTime:F1}s",
                bounds.X + 6, y, Color.Gray);
        }
    }

    /// <summary>
    /// Draw text using the pixel font (normal size).
    /// </summary>
    private void DrawText(SpriteBatch spriteBatch, Texture2D pixelTexture, string text, int x, int y, Color color)
    {
        InventoryUI.DrawText(spriteBatch, pixelTexture, text, x, y, color);
    }

    /// <summary>
    /// Draw text using smaller font (tighter spacing).
    /// Uses same font as DrawText - InventoryUI handles rendering.
    /// </summary>
    private void DrawTextSmall(SpriteBatch spriteBatch, Texture2D pixelTexture, string text, int x, int y, Color color)
    {
        InventoryUI.DrawText(spriteBatch, pixelTexture, text, x, y, color);
    }

    /// <summary>
    /// Check if a point is within the crafting panel.
    /// </summary>
    public bool ContainsPoint(Vector2 point)
    {
        return _panelBounds.Contains(point);
    }

    /// <summary>
    /// Get the panel bounds for UI collision detection.
    /// </summary>
    public Rectangle PanelBounds => _panelBounds;
}