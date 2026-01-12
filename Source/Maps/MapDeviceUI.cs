using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terrascent.Items;

namespace Terrascent.Maps;

/// <summary>
/// UI state for the map device.
/// </summary>
public enum MapDeviceUIState
{
    Closed,
    Open,
    RollingMap,
    Activating
}

/// <summary>
/// UI for interacting with the Map Device.
/// Allows players to insert maps, roll them with currency, and activate zones.
/// </summary>
public class MapDeviceUI
{
    // UI State
    public MapDeviceUIState State { get; private set; } = MapDeviceUIState.Closed;
    public MapDevice? ActiveDevice { get; private set; }

    // Map currently being modified (before activation)
    private MapItem? _insertedMap;
    private int? _sourceInventorySlot;

    // UI Layout
    private Rectangle _panelBounds;
    private Rectangle _mapSlotBounds;
    private Rectangle _activateButtonBounds;
    private Rectangle _removeButtonBounds;
    private Rectangle _closeButtonBounds;

    // Currency slot bounds (for dragging currency onto map)
    private Rectangle[] _currencySlotBounds = new Rectangle[10];

    // Dimensions
    private const int PANEL_WIDTH = 400;
    private const int PANEL_HEIGHT = 500;
    private const int SLOT_SIZE = 64;
    private const int BUTTON_WIDTH = 100;
    private const int BUTTON_HEIGHT = 32;
    private const int PADDING = 16;

    // Colors
    private static readonly Color PANEL_BG = new(30, 30, 40, 240);
    private static readonly Color SLOT_BG = new(20, 20, 30);
    private static readonly Color SLOT_BORDER = new(80, 80, 100);
    private static readonly Color BUTTON_BG = new(50, 50, 70);
    private static readonly Color BUTTON_HOVER = new(70, 70, 100);
    private static readonly Color BUTTON_DISABLED = new(40, 40, 50);
    private static readonly Color TEXT_COLOR = Color.White;
    private static readonly Color TEXT_DIM = new(150, 150, 150);

    // Hover state
    private bool _isHoveringActivate;
    private bool _isHoveringRemove;
    private bool _isHoveringClose;
    private int _hoveringCurrencySlot = -1;

    // Tooltip
    private string? _tooltipText;
    private Vector2 _tooltipPosition;

    // Dependencies
    private readonly MapItemStorage _mapStorage;
    private readonly Inventory _playerInventory;

    // Events
    public event Action<MapZone>? OnZoneActivated;
    public event Action? OnClosed;

    public MapDeviceUI(MapItemStorage mapStorage, Inventory playerInventory)
    {
        _mapStorage = mapStorage;
        _playerInventory = playerInventory;
        CalculateBounds();
    }

    /// <summary>
    /// Helper to offset a rectangle by a point (returns new rectangle).
    /// </summary>
    private static Rectangle OffsetRect(Rectangle rect, Point offset)
    {
        return new Rectangle(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height);
    }

    /// <summary>
    /// Calculate UI element bounds based on screen center.
    /// </summary>
    private void CalculateBounds()
    {
        // Panel centered on screen (will be updated in Draw with actual viewport)
        _panelBounds = new Rectangle(0, 0, PANEL_WIDTH, PANEL_HEIGHT);

        // Map slot at top center of panel
        _mapSlotBounds = new Rectangle(
            PADDING + (PANEL_WIDTH - SLOT_SIZE) / 2,
            PADDING + 40,
            SLOT_SIZE,
            SLOT_SIZE
        );

        // Currency slots below map slot (2 rows of 5)
        for (int i = 0; i < 10; i++)
        {
            int col = i % 5;
            int row = i / 5;
            int slotSize = 48;
            int startX = PADDING + (PANEL_WIDTH - 5 * slotSize - 4 * 8) / 2;
            int startY = _mapSlotBounds.Bottom + 100;

            _currencySlotBounds[i] = new Rectangle(
                startX + col * (slotSize + 8),
                startY + row * (slotSize + 8),
                slotSize,
                slotSize
            );
        }

        // Buttons at bottom
        int buttonY = PANEL_HEIGHT - PADDING - BUTTON_HEIGHT;
        _activateButtonBounds = new Rectangle(PADDING, buttonY, BUTTON_WIDTH, BUTTON_HEIGHT);
        _removeButtonBounds = new Rectangle(PADDING + BUTTON_WIDTH + 10, buttonY, BUTTON_WIDTH, BUTTON_HEIGHT);
        _closeButtonBounds = new Rectangle(PANEL_WIDTH - PADDING - BUTTON_WIDTH, buttonY, BUTTON_WIDTH, BUTTON_HEIGHT);
    }

    /// <summary>
    /// Open the UI for a specific map device.
    /// </summary>
    public void Open(MapDevice device)
    {
        ActiveDevice = device;
        State = MapDeviceUIState.Open;
        _insertedMap = device.InsertedMap;
        _sourceInventorySlot = null;
        _tooltipText = null;

        System.Diagnostics.Debug.WriteLine("MapDeviceUI: Opened");
    }

    /// <summary>
    /// Close the UI.
    /// </summary>
    public void Close()
    {
        // If there's a map inserted, return it to inventory or device
        if (_insertedMap != null && ActiveDevice != null)
        {
            if (_sourceInventorySlot.HasValue)
            {
                // Return to inventory slot
                _mapStorage.StoreMap(_sourceInventorySlot.Value, _insertedMap);
            }
            else if (ActiveDevice.InsertedMap == null)
            {
                // Insert into device
                ActiveDevice.InsertMap(_insertedMap);
            }
        }

        State = MapDeviceUIState.Closed;
        ActiveDevice = null;
        _insertedMap = null;
        _sourceInventorySlot = null;
        _tooltipText = null;

        OnClosed?.Invoke();
        System.Diagnostics.Debug.WriteLine("MapDeviceUI: Closed");
    }

    /// <summary>
    /// Update UI state and handle input.
    /// </summary>
    public void Update(GameTime gameTime, MouseState mouse, MouseState prevMouse, KeyboardState keyboard)
    {
        if (State == MapDeviceUIState.Closed) return;

        // Get mouse position
        Point mousePos = mouse.Position;
        Point panelOffset = _panelBounds.Location;

        // Update hover states
        _isHoveringActivate = OffsetRect(_activateButtonBounds, panelOffset).Contains(mousePos);
        _isHoveringRemove = OffsetRect(_removeButtonBounds, panelOffset).Contains(mousePos);
        _isHoveringClose = OffsetRect(_closeButtonBounds, panelOffset).Contains(mousePos);

        _hoveringCurrencySlot = -1;
        for (int i = 0; i < _currencySlotBounds.Length; i++)
        {
            if (OffsetRect(_currencySlotBounds[i], panelOffset).Contains(mousePos))
            {
                _hoveringCurrencySlot = i;
                break;
            }
        }

        // Update tooltip
        UpdateTooltip(mousePos);

        // Handle input
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool rightClick = mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released;

        if (leftClick)
        {
            HandleLeftClick(mousePos);
        }

        if (rightClick)
        {
            HandleRightClick(mousePos);
        }

        // ESC to close
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    /// <summary>
    /// Handle left click interactions.
    /// </summary>
    private void HandleLeftClick(Point mousePos)
    {
        // Close button
        if (_isHoveringClose)
        {
            Close();
            return;
        }

        // Activate button
        if (_isHoveringActivate && _insertedMap != null && ActiveDevice != null)
        {
            ActivateMap();
            return;
        }

        // Remove button
        if (_isHoveringRemove && _insertedMap != null)
        {
            RemoveMap();
            return;
        }

        // Map slot click (for inserting/removing map)
        var mapSlotAbsolute = OffsetRect(_mapSlotBounds, _panelBounds.Location);
        if (mapSlotAbsolute.Contains(mousePos))
        {
            HandleMapSlotClick();
            return;
        }

        // Currency slot click (apply currency to map)
        if (_hoveringCurrencySlot >= 0 && _insertedMap != null)
        {
            ApplyCurrency(_hoveringCurrencySlot);
        }
    }

    /// <summary>
    /// Handle right click interactions.
    /// </summary>
    private void HandleRightClick(Point mousePos)
    {
        // Right click on map slot to quick-remove
        var mapSlotAbsolute = OffsetRect(_mapSlotBounds, _panelBounds.Location);
        if (mapSlotAbsolute.Contains(mousePos) && _insertedMap != null)
        {
            RemoveMap();
        }
    }

    /// <summary>
    /// Handle clicking on the map slot.
    /// </summary>
    private void HandleMapSlotClick()
    {
        if (_insertedMap == null)
        {
            // TODO: Try to insert map from cursor/held item
            System.Diagnostics.Debug.WriteLine("MapDeviceUI: Map slot clicked (empty)");
        }
        else
        {
            // Pick up the map
            System.Diagnostics.Debug.WriteLine($"MapDeviceUI: Map slot clicked ({_insertedMap.GetDisplayName()})");
        }
    }

    /// <summary>
    /// Insert a map into the UI slot.
    /// </summary>
    public bool InsertMap(MapItem map, int? fromInventorySlot = null)
    {
        if (_insertedMap != null) return false;

        _insertedMap = map;
        _sourceInventorySlot = fromInventorySlot;

        System.Diagnostics.Debug.WriteLine($"MapDeviceUI: Inserted {map.GetDisplayName()}");
        return true;
    }

    /// <summary>
    /// Remove the map from the UI slot.
    /// </summary>
    private void RemoveMap()
    {
        if (_insertedMap == null) return;

        // Try to return to inventory
        // TODO: Add to player inventory or cursor

        System.Diagnostics.Debug.WriteLine($"MapDeviceUI: Removed {_insertedMap.GetDisplayName()}");
        _insertedMap = null;
        _sourceInventorySlot = null;
    }

    /// <summary>
    /// Apply currency to the inserted map.
    /// </summary>
    private void ApplyCurrency(int currencyIndex)
    {
        if (_insertedMap == null) return;

        var currencyTypes = new[]
        {
            MapCurrency.OrbOfTransmutation,
            MapCurrency.OrbOfAlteration,
            MapCurrency.OrbOfAugmentation,
            MapCurrency.OrbOfScouring,
            MapCurrency.OrbOfAlchemy,
            MapCurrency.ChaosOrb,
            MapCurrency.RegalOrb,
            MapCurrency.VaalOrb,
            MapCurrency.DivineOrb,
            MapCurrency.ExaltedOrb,
        };

        if (currencyIndex < 0 || currencyIndex >= currencyTypes.Length) return;

        var currency = currencyTypes[currencyIndex];

        // Check if player has the currency
        var itemType = currency.ToItemType();
        if (!_playerInventory.HasItem(itemType, 1))
        {
            System.Diagnostics.Debug.WriteLine($"MapDeviceUI: No {currency.GetDisplayName()} in inventory");
            return;
        }

        // Check if currency can be applied
        if (!currency.CanApplyTo(_insertedMap))
        {
            System.Diagnostics.Debug.WriteLine($"MapDeviceUI: Cannot apply {currency.GetDisplayName()} to {_insertedMap.GetDisplayName()}");
            return;
        }

        // Apply currency
        var result = MapRoller.ApplyCurrency(_insertedMap, currency);

        if (result == MapRollResult.Destroyed)
        {
            System.Diagnostics.Debug.WriteLine($"MapDeviceUI: Map destroyed by {currency.GetDisplayName()}!");
            _insertedMap = null;
            _sourceInventorySlot = null;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"MapDeviceUI: Applied {currency.GetDisplayName()}, result: {result}");
        }

        // Consume currency
        _playerInventory.RemoveItem(itemType, 1);
    }

    /// <summary>
    /// Activate the map and create a zone.
    /// </summary>
    private void ActivateMap()
    {
        if (_insertedMap == null || ActiveDevice == null) return;

        // Insert map into device
        if (!ActiveDevice.InsertMap(_insertedMap))
        {
            System.Diagnostics.Debug.WriteLine("MapDeviceUI: Failed to insert map into device");
            return;
        }

        // Activate the device
        if (!ActiveDevice.Activate())
        {
            System.Diagnostics.Debug.WriteLine("MapDeviceUI: Failed to activate device");
            return;
        }

        _insertedMap = null;
        _sourceInventorySlot = null;
        State = MapDeviceUIState.Activating;

        System.Diagnostics.Debug.WriteLine("MapDeviceUI: Map activated!");

        // Close UI after activation
        Close();

        // Notify zone activation
        if (ActiveDevice.ActiveZone != null)
        {
            OnZoneActivated?.Invoke(ActiveDevice.ActiveZone);
        }
    }

    /// <summary>
    /// Update tooltip based on mouse position.
    /// </summary>
    private void UpdateTooltip(Point mousePos)
    {
        _tooltipText = null;

        // Map slot tooltip
        var mapSlotAbsolute = OffsetRect(_mapSlotBounds, _panelBounds.Location);
        if (mapSlotAbsolute.Contains(mousePos) && _insertedMap != null)
        {
            _tooltipText = GetMapTooltip(_insertedMap);
            _tooltipPosition = new Vector2(mousePos.X + 16, mousePos.Y);
            return;
        }

        // Currency slot tooltips
        if (_hoveringCurrencySlot >= 0)
        {
            var currencyTypes = new[]
            {
                MapCurrency.OrbOfTransmutation,
                MapCurrency.OrbOfAlteration,
                MapCurrency.OrbOfAugmentation,
                MapCurrency.OrbOfScouring,
                MapCurrency.OrbOfAlchemy,
                MapCurrency.ChaosOrb,
                MapCurrency.RegalOrb,
                MapCurrency.VaalOrb,
                MapCurrency.DivineOrb,
                MapCurrency.ExaltedOrb,
            };

            var currency = currencyTypes[_hoveringCurrencySlot];
            _tooltipText = $"{currency.GetDisplayName()}\n{currency.GetDescription()}";
            _tooltipPosition = new Vector2(mousePos.X + 16, mousePos.Y);
        }
    }

    /// <summary>
    /// Get tooltip text for a map.
    /// </summary>
    private string GetMapTooltip(MapItem map)
    {
        var lines = new List<string>
        {
            map.GetDisplayName(),
            $"Tier: {MapTier.GetDisplayName(map.Tier)}",
            $"Monster Level: {map.MonsterLevel}",
            $"Rarity: {map.Rarity.GetDisplayName()}",
            "",
            map.GetStatsSummary()
        };

        // Add affixes
        var affixDescs = map.GetAffixDescriptions().ToList();
        if (affixDescs.Count > 0)
        {
            lines.Add("");
            lines.AddRange(affixDescs);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Draw the map device UI.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Rectangle viewport)
    {
        if (State == MapDeviceUIState.Closed) return;

        // Center panel on screen
        _panelBounds = new Rectangle(
            (viewport.Width - PANEL_WIDTH) / 2,
            (viewport.Height - PANEL_HEIGHT) / 2,
            PANEL_WIDTH,
            PANEL_HEIGHT
        );

        // Draw panel background
        spriteBatch.Draw(pixel, _panelBounds, PANEL_BG);

        // Draw border
        DrawBorder(spriteBatch, pixel, _panelBounds, SLOT_BORDER, 2);

        // Draw title
        string title = "Map Device";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title,
            new Vector2(_panelBounds.X + (PANEL_WIDTH - titleSize.X) / 2, _panelBounds.Y + PADDING),
            TEXT_COLOR);

        // Draw map slot
        DrawMapSlot(spriteBatch, font, pixel);

        // Draw map info
        DrawMapInfo(spriteBatch, font);

        // Draw currency slots
        DrawCurrencySlots(spriteBatch, font, pixel);

        // Draw buttons
        DrawButtons(spriteBatch, font, pixel);

        // Draw tooltip
        if (_tooltipText != null)
        {
            DrawTooltip(spriteBatch, font, pixel);
        }
    }

    private void DrawMapSlot(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
    {
        var slotBounds = OffsetRect(_mapSlotBounds, _panelBounds.Location);

        // Slot background
        spriteBatch.Draw(pixel, slotBounds, SLOT_BG);
        DrawBorder(spriteBatch, pixel, slotBounds, SLOT_BORDER, 1);

        if (_insertedMap != null)
        {
            // Draw map representation (tier + rarity color)
            Color mapColor = _insertedMap.GetDisplayColor();
            var innerBounds = new Rectangle(slotBounds.X + 4, slotBounds.Y + 4, slotBounds.Width - 8, slotBounds.Height - 8);
            spriteBatch.Draw(pixel, innerBounds, mapColor);

            // Draw tier number
            string tierText = $"T{_insertedMap.Tier}";
            Vector2 tierSize = font.MeasureString(tierText);
            spriteBatch.DrawString(font, tierText,
                new Vector2(slotBounds.Center.X - tierSize.X / 2, slotBounds.Center.Y - tierSize.Y / 2),
                Color.Black);
        }
        else
        {
            // Draw empty slot text
            string emptyText = "Drop Map";
            Vector2 textSize = font.MeasureString(emptyText) * 0.5f;
            spriteBatch.DrawString(font, emptyText,
                new Vector2(slotBounds.Center.X - textSize.X / 2, slotBounds.Center.Y - textSize.Y / 2),
                TEXT_DIM, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }

    private void DrawMapInfo(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (_insertedMap == null) return;

        var infoY = _panelBounds.Y + _mapSlotBounds.Bottom + PADDING;
        var infoX = _panelBounds.X + PADDING;

        // Map name
        spriteBatch.DrawString(font, _insertedMap.GetDisplayName(),
            new Vector2(infoX, infoY), _insertedMap.GetDisplayColor(), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        // Stats summary
        infoY += 24;
        spriteBatch.DrawString(font, _insertedMap.GetStatsSummary(),
            new Vector2(infoX, infoY), TEXT_DIM, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

        // Affixes (first 3)
        infoY += 20;
        int affixCount = 0;
        foreach (var desc in _insertedMap.GetAffixDescriptions())
        {
            if (affixCount++ >= 3) break;
            spriteBatch.DrawString(font, $"• {desc}",
                new Vector2(infoX, infoY), TEXT_COLOR, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            infoY += 14;
        }

        if (_insertedMap.AffixCount > 3)
        {
            spriteBatch.DrawString(font, $"  ...and {_insertedMap.AffixCount - 3} more",
                new Vector2(infoX, infoY), TEXT_DIM, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }

    private void DrawCurrencySlots(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
    {
        var currencyTypes = new[]
        {
            (MapCurrency.OrbOfTransmutation, "T"),
            (MapCurrency.OrbOfAlteration, "A"),
            (MapCurrency.OrbOfAugmentation, "+"),
            (MapCurrency.OrbOfScouring, "S"),
            (MapCurrency.OrbOfAlchemy, "Al"),
            (MapCurrency.ChaosOrb, "C"),
            (MapCurrency.RegalOrb, "R"),
            (MapCurrency.VaalOrb, "V"),
            (MapCurrency.DivineOrb, "D"),
            (MapCurrency.ExaltedOrb, "E"),
        };

        // Draw label
        string label = "Currency (click to apply)";
        var labelPos = new Vector2(_panelBounds.X + PADDING, _panelBounds.Y + _currencySlotBounds[0].Y - 24);
        spriteBatch.DrawString(font, label, labelPos, TEXT_DIM, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

        for (int i = 0; i < _currencySlotBounds.Length && i < currencyTypes.Length; i++)
        {
            var slotBounds = OffsetRect(_currencySlotBounds[i], _panelBounds.Location);
            var (currency, symbol) = currencyTypes[i];

            // Background
            bool isHovering = _hoveringCurrencySlot == i;
            Color bgColor = isHovering ? BUTTON_HOVER : SLOT_BG;
            spriteBatch.Draw(pixel, slotBounds, bgColor);

            // Border in currency color
            Color borderColor = currency.GetColor();
            DrawBorder(spriteBatch, pixel, slotBounds, borderColor, 1);

            // Symbol
            Vector2 symbolSize = font.MeasureString(symbol);
            spriteBatch.DrawString(font, symbol,
                new Vector2(slotBounds.Center.X - symbolSize.X / 2, slotBounds.Center.Y - symbolSize.Y / 2),
                currency.GetColor());

            // Count from inventory
            var itemType = currency.ToItemType();
            int count = _playerInventory.CountItem(itemType);
            if (count > 0)
            {
                string countText = count.ToString();
                Vector2 countSize = font.MeasureString(countText) * 0.5f;
                spriteBatch.DrawString(font, countText,
                    new Vector2(slotBounds.Right - countSize.X - 2, slotBounds.Bottom - countSize.Y - 2),
                    TEXT_COLOR, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawButtons(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
    {
        // Activate button
        bool canActivate = _insertedMap != null;
        DrawButton(spriteBatch, font, pixel,
            OffsetRect(_activateButtonBounds, _panelBounds.Location),
            "Activate", canActivate, _isHoveringActivate);

        // Remove button
        bool canRemove = _insertedMap != null;
        DrawButton(spriteBatch, font, pixel,
            OffsetRect(_removeButtonBounds, _panelBounds.Location),
            "Remove", canRemove, _isHoveringRemove);

        // Close button
        DrawButton(spriteBatch, font, pixel,
            OffsetRect(_closeButtonBounds, _panelBounds.Location),
            "Close", true, _isHoveringClose);
    }

    private void DrawButton(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel,
        Rectangle bounds, string text, bool enabled, bool hovering)
    {
        Color bgColor = !enabled ? BUTTON_DISABLED : (hovering ? BUTTON_HOVER : BUTTON_BG);
        Color textColor = enabled ? TEXT_COLOR : TEXT_DIM;

        spriteBatch.Draw(pixel, bounds, bgColor);
        DrawBorder(spriteBatch, pixel, bounds, SLOT_BORDER, 1);

        Vector2 textSize = font.MeasureString(text) * 0.7f;
        spriteBatch.DrawString(font, text,
            new Vector2(bounds.Center.X - textSize.X / 2, bounds.Center.Y - textSize.Y / 2),
            textColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private void DrawTooltip(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
    {
        if (_tooltipText == null) return;

        var lines = _tooltipText.Split('\n');
        float maxWidth = 0;
        float totalHeight = 0;

        foreach (var line in lines)
        {
            var size = font.MeasureString(line) * 0.6f;
            maxWidth = Math.Max(maxWidth, size.X);
            totalHeight += size.Y + 2;
        }

        var tooltipBounds = new Rectangle(
            (int)_tooltipPosition.X,
            (int)_tooltipPosition.Y,
            (int)maxWidth + 16,
            (int)totalHeight + 12
        );

        // Background
        spriteBatch.Draw(pixel, tooltipBounds, new Color(20, 20, 30, 240));
        DrawBorder(spriteBatch, pixel, tooltipBounds, SLOT_BORDER, 1);

        // Text
        float y = _tooltipPosition.Y + 6;
        foreach (var line in lines)
        {
            spriteBatch.DrawString(font, line,
                new Vector2(_tooltipPosition.X + 8, y),
                TEXT_COLOR, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            y += font.MeasureString(line).Y * 0.6f + 2;
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }

    /// <summary>
    /// Check if UI is open and blocking input.
    /// </summary>
    public bool IsOpen => State != MapDeviceUIState.Closed;
}