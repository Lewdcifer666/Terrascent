using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.Items;

namespace Terrascent.UI;

/// <summary>
/// Terraria-style inventory UI with drag and drop functionality.
/// </summary>
public class InventoryUI
{
    private readonly Inventory _inventory;
    private readonly UIManager _uiManager;

    // Layout constants
    public const int SLOT_SIZE = 44;
    public const int SLOT_PADDING = 4;
    public const int COLUMNS = 10;
    public const int ROWS = 4;
    public const int PANEL_PADDING = 12;

    // Panel position and dimensions
    private Rectangle _panelBounds;
    private Rectangle[] _slotBounds;

    // State
    private int _hoveredSlot = -1;
    private int _screenWidth;
    private int _screenHeight;

    // Tooltip
    private string _tooltipText = "";
    private bool _showTooltip;

    public InventoryUI(Inventory inventory, UIManager uiManager, int screenWidth, int screenHeight)
    {
        _inventory = inventory;
        _uiManager = uiManager;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CalculateLayout();
    }

    /// <summary>
    /// Recalculate panel and slot positions based on screen size.
    /// </summary>
    public void CalculateLayout()
    {
        int gridWidth = COLUMNS * (SLOT_SIZE + SLOT_PADDING) - SLOT_PADDING;
        int gridHeight = ROWS * (SLOT_SIZE + SLOT_PADDING) - SLOT_PADDING;

        int panelWidth = gridWidth + PANEL_PADDING * 2;
        int panelHeight = gridHeight + PANEL_PADDING * 2 + 30; // +30 for title

        // Center panel on screen (upper area)
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = 50; // Some padding from top

        _panelBounds = new Rectangle(panelX, panelY, panelWidth, panelHeight);

        // Calculate slot positions
        _slotBounds = new Rectangle[_inventory.Size];

        int startX = panelX + PANEL_PADDING;
        int startY = panelY + PANEL_PADDING + 24; // After title

        for (int i = 0; i < _inventory.Size; i++)
        {
            int col = i % COLUMNS;
            int row = i / COLUMNS;

            int x = startX + col * (SLOT_SIZE + SLOT_PADDING);
            int y = startY + row * (SLOT_SIZE + SLOT_PADDING);

            _slotBounds[i] = new Rectangle(x, y, SLOT_SIZE, SLOT_SIZE);
        }
    }

    /// <summary>
    /// Update screen dimensions (call on resize).
    /// </summary>
    public void UpdateScreenSize(int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;
        CalculateLayout();
    }

    /// <summary>
    /// Update inventory UI state.
    /// </summary>
    public void Update(InputManager input, float deltaTime)
    {
        Vector2 mousePos = input.MousePositionV;
        bool shiftHeld = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);

        // Find hovered slot
        _hoveredSlot = -1;
        _showTooltip = false;

        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (_slotBounds[i].Contains(mousePos))
            {
                _hoveredSlot = i;

                // Show tooltip for items
                var stack = _inventory.GetSlot(i);
                if (!stack.IsEmpty)
                {
                    _tooltipText = $"{stack.Properties.Name}";
                    if (stack.Count > 1)
                        _tooltipText += $" ({stack.Count})";
                    _showTooltip = true;
                }
                break;
            }
        }

        // Handle left click
        if (input.IsLeftMousePressed())
        {
            if (_hoveredSlot >= 0)
            {
                _uiManager.HandleSlotClick(_hoveredSlot, rightClick: false, shiftHeld);
            }
            else if (!_panelBounds.Contains(mousePos))
            {
                // Clicked outside inventory - drop held item
                if (_uiManager.IsHoldingItem)
                {
                    _uiManager.DropHeldItem();
                }
            }
        }

        // Handle right click
        if (input.IsRightMousePressed())
        {
            if (_hoveredSlot >= 0)
            {
                _uiManager.HandleSlotClick(_hoveredSlot, rightClick: true, shiftHeld);
            }
        }
    }

    /// <summary>
    /// Draw the inventory panel.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        // Draw panel background
        DrawPanel(spriteBatch, pixelTexture);

        // Draw title
        // Note: For now we'll skip text since we don't have a font loaded
        // We'll draw a simple header bar instead
        DrawHeader(spriteBatch, pixelTexture);

        // Draw hotbar separator line (between row 0 and row 1)
        DrawHotbarSeparator(spriteBatch, pixelTexture);

        // Draw all slots
        for (int i = 0; i < _slotBounds.Length; i++)
        {
            DrawSlot(spriteBatch, pixelTexture, i);
        }

        // Draw tooltip
        if (_showTooltip && !_uiManager.IsHoldingItem)
        {
            DrawTooltip(spriteBatch, pixelTexture, mousePosition);
        }
    }

    /// <summary>
    /// Draw the panel background.
    /// </summary>
    private void DrawPanel(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        // Main background
        spriteBatch.Draw(
            pixelTexture,
            _panelBounds,
            new Color(30, 30, 50, 230)
        );

        // Border
        int borderWidth = 2;
        Color borderColor = new Color(80, 80, 120, 255);

        // Top
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.X, _panelBounds.Y, _panelBounds.Width, borderWidth),
            borderColor);
        // Bottom
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.X, _panelBounds.Bottom - borderWidth, _panelBounds.Width, borderWidth),
            borderColor);
        // Left
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.X, _panelBounds.Y, borderWidth, _panelBounds.Height),
            borderColor);
        // Right
        spriteBatch.Draw(pixelTexture,
            new Rectangle(_panelBounds.Right - borderWidth, _panelBounds.Y, borderWidth, _panelBounds.Height),
            borderColor);
    }

    /// <summary>
    /// Draw header bar (where title would go).
    /// </summary>
    private void DrawHeader(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        Rectangle headerBounds = new(
            _panelBounds.X + PANEL_PADDING,
            _panelBounds.Y + PANEL_PADDING,
            _panelBounds.Width - PANEL_PADDING * 2,
            20
        );

        spriteBatch.Draw(pixelTexture, headerBounds, new Color(50, 50, 80, 200));
    }

    /// <summary>
    /// Draw separator between hotbar and main inventory.
    /// </summary>
    private void DrawHotbarSeparator(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        // Draw a line below the first row (hotbar)
        if (_slotBounds.Length > COLUMNS)
        {
            int y = _slotBounds[COLUMNS].Y - SLOT_PADDING / 2 - 1;
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(_panelBounds.X + PANEL_PADDING, y, _panelBounds.Width - PANEL_PADDING * 2, 2),
                new Color(100, 100, 140, 180)
            );
        }
    }

    /// <summary>
    /// Draw a single inventory slot.
    /// </summary>
    private void DrawSlot(SpriteBatch spriteBatch, Texture2D pixelTexture, int slotIndex)
    {
        Rectangle bounds = _slotBounds[slotIndex];
        var stack = _inventory.GetSlot(slotIndex);

        bool isHovered = slotIndex == _hoveredSlot;
        bool isHotbar = slotIndex < _inventory.HotbarSize;
        bool isSelected = slotIndex == _inventory.SelectedSlot;

        // Slot background
        Color slotBgColor;
        if (isSelected)
            slotBgColor = new Color(80, 80, 130, 220);
        else if (isHotbar)
            slotBgColor = new Color(50, 50, 70, 200);
        else
            slotBgColor = new Color(40, 40, 55, 200);

        spriteBatch.Draw(pixelTexture, bounds, slotBgColor);

        // Hover highlight
        if (isHovered)
        {
            spriteBatch.Draw(pixelTexture, bounds, new Color(255, 255, 255, 40));
        }

        // Slot border
        Color borderColor = isSelected ? Color.Yellow : (isHotbar ? new Color(100, 100, 140) : new Color(70, 70, 90));
        int borderWidth = isSelected ? 2 : 1;

        // Top
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.X, bounds.Y, bounds.Width, borderWidth),
            borderColor);
        // Bottom
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.X, bounds.Bottom - borderWidth, bounds.Width, borderWidth),
            borderColor);
        // Left
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.X, bounds.Y, borderWidth, bounds.Height),
            borderColor);
        // Right
        spriteBatch.Draw(pixelTexture,
            new Rectangle(bounds.Right - borderWidth, bounds.Y, borderWidth, bounds.Height),
            borderColor);

        // Draw item if slot has one
        if (!stack.IsEmpty)
        {
            DrawItem(spriteBatch, pixelTexture, bounds, stack);
        }

        // Draw slot number for hotbar (1-0)
        if (isHotbar)
        {
            DrawSlotNumber(spriteBatch, pixelTexture, bounds, slotIndex);
        }
    }

    /// <summary>
    /// Draw an item in a slot.
    /// </summary>
    private void DrawItem(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle slotBounds, ItemStack stack)
    {
        int itemPadding = 6;
        Rectangle itemBounds = new(
            slotBounds.X + itemPadding,
            slotBounds.Y + itemPadding,
            slotBounds.Width - itemPadding * 2,
            slotBounds.Height - itemPadding * 2
        );

        Color itemColor = GetItemColor(stack.Type);
        spriteBatch.Draw(pixelTexture, itemBounds, itemColor);

        // Draw rarity glow
        Color rarityColor = GetRarityColor(stack.Properties.Rarity);
        if (rarityColor != Color.White)
        {
            // Draw subtle glow border
            int glowWidth = 2;
            spriteBatch.Draw(pixelTexture,
                new Rectangle(itemBounds.X - glowWidth, itemBounds.Y - glowWidth, itemBounds.Width + glowWidth * 2, glowWidth),
                rarityColor * 0.5f);
            spriteBatch.Draw(pixelTexture,
                new Rectangle(itemBounds.X - glowWidth, itemBounds.Bottom, itemBounds.Width + glowWidth * 2, glowWidth),
                rarityColor * 0.5f);
            spriteBatch.Draw(pixelTexture,
                new Rectangle(itemBounds.X - glowWidth, itemBounds.Y, glowWidth, itemBounds.Height),
                rarityColor * 0.5f);
            spriteBatch.Draw(pixelTexture,
                new Rectangle(itemBounds.Right, itemBounds.Y, glowWidth, itemBounds.Height),
                rarityColor * 0.5f);
        }

        // Draw stack count (bottom right of slot)
        if (stack.Count > 1)
        {
            int digitCount = stack.Count >= 100 ? 3 : (stack.Count >= 10 ? 2 : 1);
            int countWidth = digitCount * 6 + 4;
            int countHeight = 10;

            Rectangle countBg = new(
                slotBounds.Right - countWidth - 2,
                slotBounds.Bottom - countHeight - 2,
                countWidth,
                countHeight
            );

            spriteBatch.Draw(pixelTexture, countBg, new Color(0, 0, 0, 180));

            // Draw simple number representation (colored bars for now)
            // In a real implementation, you'd draw text here
            DrawStackCount(spriteBatch, pixelTexture, countBg, stack.Count);
        }
    }

    /// <summary>
    /// Draw stack count as visual indicator (until we have fonts).
    /// </summary>
    private void DrawStackCount(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, int count)
    {
        // Draw a simple visual representation
        // Hundreds = tall bar, tens = medium bar, ones = short bar
        int ones = count % 10;
        int tens = (count / 10) % 10;
        int hundreds = count / 100;

        int x = bounds.X + 2;
        int y = bounds.Bottom - 2;
        int barWidth = 4;
        int spacing = 1;

        // Draw bars from right to left
        if (ones > 0 || tens > 0 || hundreds > 0)
        {
            // Ones place
            int onesHeight = Math.Max(2, ones * bounds.Height / 12);
            spriteBatch.Draw(pixelTexture,
                new Rectangle(bounds.Right - barWidth - 1, y - onesHeight, barWidth - 1, onesHeight),
                Color.White * 0.9f);
        }

        if (tens > 0 || hundreds > 0)
        {
            // Tens place
            int tensHeight = Math.Max(2, tens * bounds.Height / 12);
            spriteBatch.Draw(pixelTexture,
                new Rectangle(bounds.Right - barWidth * 2 - spacing - 1, y - tensHeight, barWidth - 1, tensHeight),
                Color.LightGray * 0.9f);
        }

        if (hundreds > 0)
        {
            // Hundreds place
            int hundredsHeight = Math.Max(2, hundreds * bounds.Height / 12);
            spriteBatch.Draw(pixelTexture,
                new Rectangle(bounds.Right - barWidth * 3 - spacing * 2 - 1, y - hundredsHeight, barWidth - 1, hundredsHeight),
                Color.Gold * 0.9f);
        }
    }

    /// <summary>
    /// Draw slot number indicator for hotbar.
    /// </summary>
    private void DrawSlotNumber(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, int slotIndex)
    {
        // Draw a small indicator in top-left corner
        int number = (slotIndex + 1) % 10; // 1-9, then 0

        int indicatorSize = 10;
        Rectangle indicatorBounds = new(
            bounds.X + 2,
            bounds.Y + 2,
            indicatorSize,
            indicatorSize
        );

        // Background
        spriteBatch.Draw(pixelTexture, indicatorBounds, new Color(0, 0, 0, 150));

        // Simple visual for number (small bar representing 1-9, 0)
        int barHeight = number == 0 ? 8 : Math.Min(8, number + 1);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(indicatorBounds.X + 2, indicatorBounds.Bottom - barHeight - 1, indicatorSize - 4, barHeight),
            new Color(150, 150, 180, 200));
    }

    /// <summary>
    /// Draw tooltip near mouse cursor.
    /// </summary>
    private void DrawTooltip(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        // Simple tooltip background
        // In real implementation, we'd measure text and draw it
        int tooltipWidth = 120;
        int tooltipHeight = 24;
        int offsetX = 15;
        int offsetY = 15;

        int x = (int)mousePosition.X + offsetX;
        int y = (int)mousePosition.Y + offsetY;

        // Keep tooltip on screen
        if (x + tooltipWidth > _screenWidth)
            x = (int)mousePosition.X - tooltipWidth - 5;
        if (y + tooltipHeight > _screenHeight)
            y = (int)mousePosition.Y - tooltipHeight - 5;

        Rectangle tooltipBounds = new(x, y, tooltipWidth, tooltipHeight);

        // Background
        spriteBatch.Draw(pixelTexture, tooltipBounds, new Color(20, 20, 30, 240));

        // Border
        Color borderColor = new Color(100, 100, 140);
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, tooltipWidth, 1), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y + tooltipHeight - 1, tooltipWidth, 1), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, 1, tooltipHeight), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x + tooltipWidth - 1, y, 1, tooltipHeight), borderColor);

        // For now, draw a colored bar representing the item
        if (_hoveredSlot >= 0)
        {
            var stack = _inventory.GetSlot(_hoveredSlot);
            if (!stack.IsEmpty)
            {
                Color itemColor = GetItemColor(stack.Type);
                spriteBatch.Draw(pixelTexture,
                    new Rectangle(x + 4, y + 4, 16, 16),
                    itemColor);

                // Rarity-colored line
                Color rarityColor = GetRarityColor(stack.Properties.Rarity);
                spriteBatch.Draw(pixelTexture,
                    new Rectangle(x + 24, y + 10, tooltipWidth - 32, 4),
                    rarityColor);
            }
        }
    }

    /// <summary>
    /// Get color for item type (matches hotbar colors).
    /// </summary>
    public static Color GetItemColor(ItemType type)
    {
        return type switch
        {
            // Blocks
            ItemType.Dirt => new Color(139, 90, 43),
            ItemType.Stone => new Color(128, 128, 128),
            ItemType.Sand => new Color(238, 214, 175),
            ItemType.Wood => new Color(160, 82, 45),
            ItemType.Torch => Color.Yellow,
            ItemType.CopperOre => new Color(184, 115, 51),
            ItemType.IronOre => new Color(165, 142, 142),
            ItemType.SilverOre => new Color(192, 192, 210),
            ItemType.GoldOre => new Color(255, 215, 0),

            // Tools
            ItemType.WoodPickaxe => new Color(139, 90, 43),
            ItemType.CopperPickaxe => new Color(184, 115, 51),
            ItemType.IronPickaxe => new Color(150, 150, 160),

            // Swords
            ItemType.WoodSword => new Color(180, 140, 100),
            ItemType.CopperSword => new Color(200, 130, 80),
            ItemType.IronSword => new Color(180, 180, 195),
            ItemType.SilverSword => new Color(210, 210, 230),
            ItemType.GoldSword => new Color(255, 215, 0),

            // Spears
            ItemType.WoodSpear => new Color(160, 120, 80),
            ItemType.CopperSpear => new Color(190, 120, 70),
            ItemType.IronSpear => new Color(170, 170, 185),

            // Axes
            ItemType.BattleAxe => new Color(140, 100, 70),

            // Bows
            ItemType.WoodBow => new Color(150, 100, 60),
            ItemType.CopperBow => new Color(180, 110, 60),
            ItemType.IronBow => new Color(160, 160, 175),

            // Whips
            ItemType.LeatherWhip => new Color(139, 90, 60),
            ItemType.ChainWhip => new Color(170, 170, 180),

            // Staves
            ItemType.WoodStaff => new Color(120, 90, 140),
            ItemType.ApprenticeStaff => new Color(140, 100, 180),
            ItemType.MageStaff => new Color(160, 80, 200),

            // Gloves
            ItemType.LeatherGloves => new Color(180, 140, 100),
            ItemType.IronKnuckles => new Color(160, 160, 170),

            // Boomerangs
            ItemType.WoodBoomerang => new Color(170, 130, 80),
            ItemType.IronBoomerang => new Color(165, 165, 180),

            // Stackable items - Common
            ItemType.SoldiersSyringeItem => new Color(200, 50, 50),
            ItemType.TougherTimesItem => new Color(150, 150, 200),
            ItemType.BisonSteakItem => new Color(180, 80, 80),
            ItemType.PaulsGoatHoofItem => new Color(139, 90, 60),
            ItemType.CritGlassesItem => new Color(200, 200, 220),
            ItemType.MonsterToothItem => new Color(220, 220, 200),

            // Stackable items - Uncommon
            ItemType.HopooFeatherItem => new Color(100, 200, 100),
            ItemType.PredatoryInstinctsItem => new Color(180, 100, 100),
            ItemType.HarvestersScytheItem => new Color(150, 150, 180),
            ItemType.UkuleleItem => new Color(200, 150, 100),
            ItemType.AtgMissileItem => new Color(80, 120, 80),

            // Stackable items - Rare
            ItemType.BrilliantBehemothItem => new Color(255, 100, 50),
            ItemType.ShapedGlassItem => new Color(200, 150, 255),
            ItemType.CestiusItem => new Color(200, 180, 100),

            // Stackable items - Legendary
            ItemType.SoulboundCatalystItem => new Color(255, 180, 50),
            ItemType.FiftySevenLeafCloverItem => new Color(50, 255, 50),

            _ => Color.Magenta
        };
    }

    /// <summary>
    /// Get color for item rarity.
    /// </summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => Color.White,
            ItemRarity.Uncommon => Color.LimeGreen,
            ItemRarity.Rare => Color.DeepSkyBlue,
            ItemRarity.Epic => Color.MediumPurple,
            ItemRarity.Legendary => Color.Orange,
            ItemRarity.Boss => Color.Red,
            _ => Color.White
        };
    }
}