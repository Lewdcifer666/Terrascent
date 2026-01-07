using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Combat;
using Terrascent.Core;
using Terrascent.Items;
using Terrascent.Items.Effects;

namespace Terrascent.UI;

/// <summary>
/// Terraria-style inventory UI with drag and drop functionality.
/// Features beautiful tooltips with item stats and rarity-colored borders.
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

    // Tooltip data
    private ItemStack _tooltipItem;
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
        _tooltipItem = ItemStack.Empty;

        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (_slotBounds[i].Contains(mousePos))
            {
                _hoveredSlot = i;

                // Show tooltip for items
                var stack = _inventory.GetSlot(i);
                if (!stack.IsEmpty)
                {
                    _tooltipItem = stack;
                    _showTooltip = true;
                }
                break;
            }
        }

        // Handle left click - consume immediately
        if (input.IsLeftMousePressed())
        {
            if (_hoveredSlot >= 0)
            {
                input.ConsumeMousePress(left: true);
                _uiManager.HandleSlotClick(_hoveredSlot, rightClick: false, shiftHeld);
            }
            else if (!_panelBounds.Contains(mousePos))
            {
                // Clicked outside inventory - drop held item
                if (_uiManager.IsHoldingItem)
                {
                    input.ConsumeMousePress(left: true);
                    _uiManager.DropHeldItem();
                }
            }
        }

        // Handle right click - consume immediately
        if (input.IsRightMousePressed())
        {
            if (_hoveredSlot >= 0)
            {
                input.ConsumeMousePress(right: true);
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

        // Draw title bar
        DrawHeader(spriteBatch, pixelTexture);

        // Draw hotbar separator line (between row 0 and row 1)
        DrawHotbarSeparator(spriteBatch, pixelTexture);

        // Draw all slots
        for (int i = 0; i < _slotBounds.Length; i++)
        {
            DrawSlot(spriteBatch, pixelTexture, i);
        }

        // Draw tooltip (last so it's on top)
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
        // Main background with slight transparency
        spriteBatch.Draw(pixelTexture, _panelBounds, new Color(25, 25, 45, 240));

        // Outer border (darker)
        DrawBorder(spriteBatch, pixelTexture, _panelBounds, new Color(60, 60, 90), 2);

        // Inner highlight (lighter, creates depth)
        Rectangle innerBounds = new(
            _panelBounds.X + 2,
            _panelBounds.Y + 2,
            _panelBounds.Width - 4,
            _panelBounds.Height - 4
        );
        DrawBorder(spriteBatch, pixelTexture, innerBounds, new Color(80, 80, 120, 100), 1);
    }

    /// <summary>
    /// Draw a border around a rectangle.
    /// </summary>
    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        // Left
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        // Right
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }

    /// <summary>
    /// Draw header bar.
    /// </summary>
    private void DrawHeader(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        Rectangle headerBounds = new(
            _panelBounds.X + PANEL_PADDING,
            _panelBounds.Y + PANEL_PADDING,
            _panelBounds.Width - PANEL_PADDING * 2,
            20
        );

        // Header background with gradient effect
        spriteBatch.Draw(pixelTexture, headerBounds, new Color(45, 45, 75, 220));

        // Header highlight on top edge
        spriteBatch.Draw(pixelTexture,
            new Rectangle(headerBounds.X, headerBounds.Y, headerBounds.Width, 1),
            new Color(100, 100, 150, 150));

        // Draw "INVENTORY" title
        DrawText(spriteBatch, pixelTexture, "INVENTORY", headerBounds.X + 4, headerBounds.Y + 6, new Color(200, 200, 220));
    }

    /// <summary>
    /// Draw separator between hotbar and main inventory.
    /// </summary>
    private void DrawHotbarSeparator(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        if (_slotBounds.Length > COLUMNS)
        {
            int y = _slotBounds[COLUMNS].Y - SLOT_PADDING / 2 - 1;

            // Gradient separator line
            spriteBatch.Draw(pixelTexture,
                new Rectangle(_panelBounds.X + PANEL_PADDING + 10, y, _panelBounds.Width - PANEL_PADDING * 2 - 20, 1),
                new Color(100, 100, 140, 150));
            spriteBatch.Draw(pixelTexture,
                new Rectangle(_panelBounds.X + PANEL_PADDING + 10, y + 1, _panelBounds.Width - PANEL_PADDING * 2 - 20, 1),
                new Color(40, 40, 60, 100));
        }
    }

    /// <summary>
    /// Draw a single inventory slot with item and stack count.
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
            slotBgColor = new Color(70, 70, 120, 230);
        else if (isHotbar)
            slotBgColor = new Color(45, 45, 65, 220);
        else
            slotBgColor = new Color(35, 35, 50, 220);

        spriteBatch.Draw(pixelTexture, bounds, slotBgColor);

        // Hover highlight
        if (isHovered)
        {
            spriteBatch.Draw(pixelTexture, bounds, new Color(255, 255, 255, 35));
        }

        // Slot border - rarity colored if has item, otherwise default
        Color borderColor;
        int borderWidth;

        if (isSelected)
        {
            borderColor = Color.Gold;
            borderWidth = 2;
        }
        else if (!stack.IsEmpty)
        {
            // Use rarity color for border when item is present
            borderColor = GetRarityColor(stack.Properties.Rarity) * 0.7f;
            borderWidth = 1;
        }
        else if (isHotbar)
        {
            borderColor = new Color(80, 80, 110);
            borderWidth = 1;
        }
        else
        {
            borderColor = new Color(55, 55, 75);
            borderWidth = 1;
        }

        DrawBorder(spriteBatch, pixelTexture, bounds, borderColor, borderWidth);

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
    /// Draw an item in a slot with rarity glow and stack count.
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
        Color rarityColor = GetRarityColor(stack.Properties.Rarity);

        // Draw rarity glow behind item (subtle)
        if (rarityColor != Color.White)
        {
            Rectangle glowBounds = new(
                itemBounds.X - 2,
                itemBounds.Y - 2,
                itemBounds.Width + 4,
                itemBounds.Height + 4
            );
            spriteBatch.Draw(pixelTexture, glowBounds, rarityColor * 0.25f);
        }

        // Draw item
        spriteBatch.Draw(pixelTexture, itemBounds, itemColor);

        // Draw stack count in bottom-right corner
        if (stack.Count > 1)
        {
            DrawStackNumber(spriteBatch, pixelTexture, slotBounds, stack.Count);
        }
    }

    /// <summary>
    /// Draw stack count number using simple digit representation.
    /// </summary>
    public static void DrawStackNumber(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle slotBounds, int count)
    {
        string countStr = count.ToString();
        int digitWidth = 5;
        int digitHeight = 7;
        int spacing = 1;
        int totalWidth = countStr.Length * (digitWidth + spacing) - spacing;

        int x = slotBounds.Right - totalWidth - 3;
        int y = slotBounds.Bottom - digitHeight - 3;

        // Background for readability
        Rectangle bgBounds = new(x - 2, y - 1, totalWidth + 4, digitHeight + 2);
        spriteBatch.Draw(pixelTexture, bgBounds, new Color(0, 0, 0, 200));

        // Draw each digit
        foreach (char c in countStr)
        {
            DrawDigit(spriteBatch, pixelTexture, x, y, c - '0', Color.White);
            x += digitWidth + spacing;
        }
    }

    /// <summary>
    /// Draw a single digit using pixel art style.
    /// </summary>
    public static void DrawDigit(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int digit, Color color)
    {
        // Simple 5x7 pixel font patterns for digits 0-9
        bool[,] patterns = digit switch
        {
            0 => new bool[,] { { true, true, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true } },
            1 => new bool[,] { { false, false, true, false, false }, { false, true, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, true, true, true, false } },
            2 => new bool[,] { { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, false, true }, { true, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true } },
            3 => new bool[,] { { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, false, true }, { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, false, true }, { true, true, true, true, true } },
            4 => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, false, true }, { false, false, false, false, true } },
            5 => new bool[,] { { true, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, false, true }, { true, true, true, true, true } },
            6 => new bool[,] { { true, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true } },
            7 => new bool[,] { { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, false, true }, { false, false, false, true, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false } },
            8 => new bool[,] { { true, true, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true } },
            9 => new bool[,] { { true, true, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, false, true }, { true, true, true, true, true } },
            _ => new bool[7, 5]
        };

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (patterns[row, col])
                {
                    spriteBatch.Draw(pixelTexture, new Rectangle(x + col, y + row, 1, 1), color);
                }
            }
        }
    }

    /// <summary>
    /// Draw slot number indicator for hotbar.
    /// </summary>
    public static void DrawSlotNumber(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, int slotIndex)
    {
        int number = (slotIndex + 1) % 10; // 1-9, then 0

        int x = bounds.X + 2;
        int y = bounds.Y + 2;

        // Small background
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, 7, 9), new Color(0, 0, 0, 150));

        // Draw the number
        DrawDigit(spriteBatch, pixelTexture, x + 1, y + 1, number, new Color(180, 180, 200));
    }

    /// <summary>
    /// Draw beautiful tooltip with item information and stats.
    /// </summary>
    private void DrawTooltip(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        if (_tooltipItem.IsEmpty)
            return;

        var props = _tooltipItem.Properties;
        Color rarityColor = GetRarityColor(props.Rarity);

        // Build tooltip content
        List<(string text, Color color)> lines = new();

        // Item name (rarity colored)
        lines.Add((props.Name, rarityColor));

        // Stack count if > 1
        if (_tooltipItem.Count > 1)
        {
            lines.Add(($"Stack: {_tooltipItem.Count}/{props.MaxStack}", new Color(180, 180, 180)));
        }

        // Category
        lines.Add(($"[{props.Category}]", new Color(150, 150, 150)));

        // Weapon stats
        if (WeaponRegistry.IsWeapon(_tooltipItem.Type))
        {
            var weaponData = WeaponRegistry.Get(_tooltipItem.Type);
            lines.Add(("", Color.Transparent)); // Spacer
            lines.Add(($"Damage: {weaponData.BaseDamage}", new Color(255, 100, 100)));
            lines.Add(($"Type: {weaponData.WeaponType}", new Color(200, 200, 200)));
            lines.Add(($"Range: {weaponData.BaseRange:F0}px", new Color(200, 200, 200)));
            lines.Add(($"Speed: {weaponData.AttackSpeed:F1}/s", new Color(200, 200, 200)));

            if (!string.IsNullOrEmpty(weaponData.Description))
            {
                lines.Add(("", Color.Transparent)); // Spacer
                lines.Add((weaponData.Description, new Color(150, 150, 180)));
            }
        }

        // Tool power
        if (props.ToolPower > 0)
        {
            lines.Add(("", Color.Transparent)); // Spacer
            lines.Add(($"Power: {props.ToolPower}%", new Color(100, 200, 255)));
        }

        // Stackable item effects
        var stackableItem = StackableItemRegistry.Get(_tooltipItem.Type);
        if (stackableItem != null)
        {
            lines.Add(("", Color.Transparent)); // Spacer

            foreach (var effect in stackableItem.Effects)
            {
                string effectDesc = effect.GetDescription(_tooltipItem.Count);
                Color effectColor = effect.Type switch
                {
                    EffectType.Damage or EffectType.DamageMult or EffectType.CritChance or EffectType.CritDamageMult
                        => new Color(255, 150, 150),
                    EffectType.MaxHealth or EffectType.HealthRegen or EffectType.OnKillHeal or EffectType.OnHitHeal or EffectType.LifeSteal
                        => new Color(150, 255, 150),
                    EffectType.Armor or EffectType.BlockChance or EffectType.DodgeChance
                        => new Color(150, 150, 255),
                    EffectType.MoveSpeedMult or EffectType.AttackSpeedMult
                        => new Color(255, 255, 150),
                    EffectType.ExtraJump
                        => new Color(150, 255, 255),
                    _ => new Color(200, 200, 200)
                };
                lines.Add((effectDesc, effectColor));
            }

            if (!string.IsNullOrEmpty(stackableItem.Lore))
            {
                lines.Add(("", Color.Transparent)); // Spacer
                lines.Add(($"\"{stackableItem.Lore}\"", new Color(120, 120, 140)));
            }
        }

        // Sell value
        if (props.SellValue > 0)
        {
            lines.Add(("", Color.Transparent)); // Spacer
            lines.Add(($"Sell: {props.SellValue} copper", new Color(200, 180, 100)));
        }

        // Calculate tooltip size
        int lineHeight = 10;
        int tooltipWidth = 180;
        int tooltipHeight = PANEL_PADDING * 2 + lines.Count * lineHeight;

        // Adjust width based on longest line (estimate)
        foreach (var (text, _) in lines)
        {
            int estimatedWidth = text.Length * 6 + PANEL_PADDING * 2;
            if (estimatedWidth > tooltipWidth)
                tooltipWidth = Math.Min(estimatedWidth, 300);
        }

        int offsetX = 18;
        int offsetY = 18;

        int x = (int)mousePosition.X + offsetX;
        int y = (int)mousePosition.Y + offsetY;

        // Keep tooltip on screen
        if (x + tooltipWidth > _screenWidth - 10)
            x = (int)mousePosition.X - tooltipWidth - 5;
        if (y + tooltipHeight > _screenHeight - 10)
            y = (int)mousePosition.Y - tooltipHeight - 5;
        if (x < 10)
            x = 10;
        if (y < 10)
            y = 10;

        Rectangle tooltipBounds = new(x, y, tooltipWidth, tooltipHeight);

        // Background with slight transparency
        spriteBatch.Draw(pixelTexture, tooltipBounds, new Color(15, 15, 25, 245));

        // Rarity-colored border
        DrawBorder(spriteBatch, pixelTexture, tooltipBounds, rarityColor * 0.8f, 2);

        // Inner highlight
        Rectangle innerBounds = new(x + 2, y + 2, tooltipWidth - 4, tooltipHeight - 4);
        DrawBorder(spriteBatch, pixelTexture, innerBounds, rarityColor * 0.3f, 1);

        // Draw each line
        int textY = y + PANEL_PADDING;
        foreach (var (text, color) in lines)
        {
            if (color != Color.Transparent && !string.IsNullOrEmpty(text))
            {
                DrawText(spriteBatch, pixelTexture, text, x + PANEL_PADDING, textY, color);
            }
            textY += lineHeight;
        }
    }

    /// <summary>
    /// Draw text using simple pixel font.
    /// </summary>
    private static void DrawText(SpriteBatch spriteBatch, Texture2D pixelTexture, string text, int x, int y, Color color)
    {
        int charWidth = 5;
        int spacing = 1;

        foreach (char c in text)
        {
            if (c >= '0' && c <= '9')
            {
                DrawDigit(spriteBatch, pixelTexture, x, y, c - '0', color);
            }
            else if (c >= 'A' && c <= 'Z')
            {
                DrawLetter(spriteBatch, pixelTexture, x, y, c, color);
            }
            else if (c >= 'a' && c <= 'z')
            {
                DrawLetter(spriteBatch, pixelTexture, x, y, (char)(c - 32), color); // Convert to uppercase
            }
            else if (c == ' ')
            {
                // Space - just advance
            }
            else
            {
                DrawSymbol(spriteBatch, pixelTexture, x, y, c, color);
            }

            x += charWidth + spacing;
        }
    }

    /// <summary>
    /// Draw a letter using pixel patterns.
    /// </summary>
    private static void DrawLetter(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, char letter, Color color)
    {
        // Simplified 5x7 letter patterns for A-Z
        bool[,] pattern = letter switch
        {
            'A' => new bool[,] { { false, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true } },
            'B' => new bool[,] { { true, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, false } },
            'C' => new bool[,] { { false, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { false, true, true, true, true } },
            'D' => new bool[,] { { true, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, false } },
            'E' => new bool[,] { { true, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true } },
            'F' => new bool[,] { { true, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false } },
            'G' => new bool[,] { { false, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { false, true, true, true, true } },
            'H' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true } },
            'I' => new bool[,] { { false, true, true, true, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, true, true, true, false } },
            'J' => new bool[,] { { false, false, false, false, true }, { false, false, false, false, true }, { false, false, false, false, true }, { false, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { false, true, true, true, false } },
            'K' => new bool[,] { { true, false, false, false, true }, { true, false, false, true, false }, { true, false, true, false, false }, { true, true, false, false, false }, { true, false, true, false, false }, { true, false, false, true, false }, { true, false, false, false, true } },
            'L' => new bool[,] { { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true } },
            'M' => new bool[,] { { true, false, false, false, true }, { true, true, false, true, true }, { true, false, true, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true } },
            'N' => new bool[,] { { true, false, false, false, true }, { true, true, false, false, true }, { true, false, true, false, true }, { true, false, false, true, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true } },
            'O' => new bool[,] { { false, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { false, true, true, true, false } },
            'P' => new bool[,] { { true, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false } },
            'Q' => new bool[,] { { false, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, true, false, true }, { true, false, false, true, false }, { false, true, true, false, true } },
            'R' => new bool[,] { { true, true, true, true, false }, { true, false, false, false, true }, { true, false, false, false, true }, { true, true, true, true, false }, { true, false, true, false, false }, { true, false, false, true, false }, { true, false, false, false, true } },
            'S' => new bool[,] { { false, true, true, true, true }, { true, false, false, false, false }, { true, false, false, false, false }, { false, true, true, true, false }, { false, false, false, false, true }, { false, false, false, false, true }, { true, true, true, true, false } },
            'T' => new bool[,] { { true, true, true, true, true }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false } },
            'U' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { false, true, true, true, false } },
            'V' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { false, true, false, true, false }, { false, false, true, false, false } },
            'W' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { true, false, false, false, true }, { true, false, true, false, true }, { true, false, true, false, true }, { true, true, false, true, true }, { true, false, false, false, true } },
            'X' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { false, true, false, true, false }, { false, false, true, false, false }, { false, true, false, true, false }, { true, false, false, false, true }, { true, false, false, false, true } },
            'Y' => new bool[,] { { true, false, false, false, true }, { true, false, false, false, true }, { false, true, false, true, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false } },
            'Z' => new bool[,] { { true, true, true, true, true }, { false, false, false, false, true }, { false, false, false, true, false }, { false, false, true, false, false }, { false, true, false, false, false }, { true, false, false, false, false }, { true, true, true, true, true } },
            _ => new bool[7, 5]
        };

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (pattern[row, col])
                {
                    spriteBatch.Draw(pixelTexture, new Rectangle(x + col, y + row, 1, 1), color);
                }
            }
        }
    }

    /// <summary>
    /// Draw common symbols.
    /// </summary>
    private static void DrawSymbol(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, char symbol, Color color)
    {
        bool[,] pattern = symbol switch
        {
            ':' => new bool[,] { { false, false, false, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, false, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, false, false, false } },
            '.' => new bool[,] { { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, true, false, false }, { false, false, true, false, false } },
            ',' => new bool[,] { { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, true, false, false, false } },
            '/' => new bool[,] { { false, false, false, false, true }, { false, false, false, true, false }, { false, false, false, true, false }, { false, false, true, false, false }, { false, true, false, false, false }, { false, true, false, false, false }, { true, false, false, false, false } },
            '%' => new bool[,] { { true, true, false, false, true }, { true, true, false, true, false }, { false, false, false, true, false }, { false, false, true, false, false }, { false, true, false, false, false }, { false, true, false, true, true }, { true, false, false, true, true } },
            '+' => new bool[,] { { false, false, false, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { true, true, true, true, true }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, false, false, false } },
            '-' => new bool[,] { { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { true, true, true, true, true }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false } },
            '"' => new bool[,] { { false, true, false, true, false }, { false, true, false, true, false }, { false, true, false, true, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false } },
            '\'' => new bool[,] { { false, false, true, false, false }, { false, false, true, false, false }, { false, false, true, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false }, { false, false, false, false, false } },
            '[' => new bool[,] { { false, true, true, true, false }, { false, true, false, false, false }, { false, true, false, false, false }, { false, true, false, false, false }, { false, true, false, false, false }, { false, true, false, false, false }, { false, true, true, true, false } },
            ']' => new bool[,] { { false, true, true, true, false }, { false, false, false, true, false }, { false, false, false, true, false }, { false, false, false, true, false }, { false, false, false, true, false }, { false, false, false, true, false }, { false, true, true, true, false } },
            '(' => new bool[,] { { false, false, true, false, false }, { false, true, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { true, false, false, false, false }, { false, true, false, false, false }, { false, false, true, false, false } },
            ')' => new bool[,] { { false, false, true, false, false }, { false, false, false, true, false }, { false, false, false, false, true }, { false, false, false, false, true }, { false, false, false, false, true }, { false, false, false, true, false }, { false, false, true, false, false } },
            _ => new bool[7, 5]
        };

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (pattern[row, col])
                {
                    spriteBatch.Draw(pixelTexture, new Rectangle(x + col, y + row, 1, 1), color);
                }
            }
        }
    }

    /// <summary>
    /// Get color for item type.
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
            ItemRarity.Uncommon => new Color(100, 255, 100),  // Bright green
            ItemRarity.Rare => new Color(100, 150, 255),      // Bright blue
            ItemRarity.Epic => new Color(200, 100, 255),      // Purple
            ItemRarity.Legendary => new Color(255, 180, 50),  // Orange/gold
            ItemRarity.Boss => new Color(255, 80, 80),        // Red
            _ => Color.White
        };
    }
}