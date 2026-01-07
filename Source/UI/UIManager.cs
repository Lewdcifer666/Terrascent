using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terrascent.Core;
using Terrascent.Entities;
using Terrascent.Items;

namespace Terrascent.UI;

/// <summary>
/// Manages all UI elements and coordinates their state.
/// </summary>
public class UIManager
{
    private readonly Player _player;
    private readonly InputManager _input;

    // UI Components
    public InventoryUI InventoryUI { get; private set; } = null!;

    // Held item (being dragged)
    private ItemStack _heldItem = ItemStack.Empty;
    private int _heldFromSlot = -1;

    /// <summary>
    /// Is the inventory currently open?
    /// </summary>
    public bool IsInventoryOpen { get; private set; }

    /// <summary>
    /// Is any UI panel open that should pause/block gameplay?
    /// </summary>
    public bool IsAnyPanelOpen => IsInventoryOpen;

    /// <summary>
    /// The item currently being held/dragged by the mouse.
    /// </summary>
    public ItemStack HeldItem => _heldItem;

    /// <summary>
    /// Is the player currently holding an item?
    /// </summary>
    public bool IsHoldingItem => !_heldItem.IsEmpty;

    public UIManager(Player player, InputManager input)
    {
        _player = player;
        _input = input;
    }

    /// <summary>
    /// Initialize UI components. Call after graphics device is ready.
    /// </summary>
    public void Initialize(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        InventoryUI = new InventoryUI(_player.Inventory, this, screenWidth, screenHeight);
    }

    /// <summary>
    /// Update UI state. Call every frame.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Toggle inventory with I key - consume immediately to prevent double-trigger
        if (_input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.I))
        {
            _input.ConsumeKeyPress(Microsoft.Xna.Framework.Input.Keys.I);
            ToggleInventory();
        }

        // Close inventory with Escape (if open) - consume immediately
        if (IsInventoryOpen && _input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
        {
            _input.ConsumeKeyPress(Microsoft.Xna.Framework.Input.Keys.Escape);
            CloseInventory();
        }

        // Update inventory UI if open
        if (IsInventoryOpen)
        {
            InventoryUI.Update(_input, deltaTime);
        }
    }

    /// <summary>
    /// Toggle inventory open/closed.
    /// </summary>
    public void ToggleInventory()
    {
        if (IsInventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    /// <summary>
    /// Open the inventory.
    /// </summary>
    public void OpenInventory()
    {
        IsInventoryOpen = true;
        System.Diagnostics.Debug.WriteLine("Inventory opened");
    }

    /// <summary>
    /// Close the inventory. Returns held item to inventory if possible.
    /// </summary>
    public void CloseInventory()
    {
        IsInventoryOpen = false;

        // Return held item to inventory
        if (!_heldItem.IsEmpty)
        {
            int overflow = _player.Inventory.AddItem(_heldItem);
            if (overflow > 0)
            {
                // TODO: Drop remaining items on ground
                System.Diagnostics.Debug.WriteLine($"Dropped {overflow} items (inventory full)");
            }
            _heldItem = ItemStack.Empty;
            _heldFromSlot = -1;
        }

        System.Diagnostics.Debug.WriteLine("Inventory closed");
    }

    #region Item Holding/Dragging

    /// <summary>
    /// Pick up an item stack from a slot.
    /// </summary>
    public void PickUpItem(int slotIndex, bool pickUpHalf = false)
    {
        var slot = _player.Inventory.GetSlot(slotIndex);

        if (slot.IsEmpty)
            return;

        if (pickUpHalf && slot.Count > 1)
        {
            // Pick up half the stack
            int halfCount = slot.Count / 2;
            _heldItem = new ItemStack(slot.Type, halfCount);

            // Update inventory slot
            var remaining = new ItemStack(slot.Type, slot.Count - halfCount);
            _player.Inventory.SetSlot(slotIndex, remaining);
        }
        else
        {
            // Pick up entire stack
            _heldItem = slot;
            _player.Inventory.SetSlot(slotIndex, ItemStack.Empty);
        }

        _heldFromSlot = slotIndex;
    }

    /// <summary>
    /// Place held item into a slot. Handles swapping, stacking, etc.
    /// </summary>
    public void PlaceItem(int slotIndex, bool placeOne = false)
    {
        if (_heldItem.IsEmpty)
            return;

        var targetSlot = _player.Inventory.GetSlot(slotIndex);

        if (placeOne)
        {
            // Place just one item
            if (targetSlot.IsEmpty)
            {
                // Place one into empty slot
                _player.Inventory.SetSlot(slotIndex, new ItemStack(_heldItem.Type, 1));
                _heldItem.Count--;
                if (_heldItem.Count <= 0)
                    _heldItem = ItemStack.Empty;
            }
            else if (targetSlot.Type == _heldItem.Type && !targetSlot.IsFull)
            {
                // Stack one onto matching stack
                var updated = targetSlot;
                updated.Count++;
                _player.Inventory.SetSlot(slotIndex, updated);
                _heldItem.Count--;
                if (_heldItem.Count <= 0)
                    _heldItem = ItemStack.Empty;
            }
            // Can't place one on different item type - do nothing
        }
        else
        {
            // Place entire stack
            if (targetSlot.IsEmpty)
            {
                // Place into empty slot
                _player.Inventory.SetSlot(slotIndex, _heldItem);
                _heldItem = ItemStack.Empty;
            }
            else if (targetSlot.Type == _heldItem.Type)
            {
                // Stack onto matching type
                int canFit = targetSlot.MaxStack - targetSlot.Count;
                int toPlace = Math.Min(canFit, _heldItem.Count);

                var updated = targetSlot;
                updated.Count += toPlace;
                _player.Inventory.SetSlot(slotIndex, updated);

                _heldItem.Count -= toPlace;
                if (_heldItem.Count <= 0)
                    _heldItem = ItemStack.Empty;
            }
            else
            {
                // Swap items
                _player.Inventory.SetSlot(slotIndex, _heldItem);
                _heldItem = targetSlot;
            }
        }

        _heldFromSlot = -1;
    }

    /// <summary>
    /// Handle clicking on a slot (combines pick up and place logic).
    /// </summary>
    public void HandleSlotClick(int slotIndex, bool rightClick, bool shiftHeld)
    {
        var targetSlot = _player.Inventory.GetSlot(slotIndex);

        if (shiftHeld && !targetSlot.IsEmpty)
        {
            // Shift+click: Quick move between hotbar and main inventory
            QuickMoveItem(slotIndex);
            return;
        }

        if (_heldItem.IsEmpty)
        {
            // Not holding anything - pick up from slot
            if (!targetSlot.IsEmpty)
            {
                PickUpItem(slotIndex, rightClick);
            }
        }
        else
        {
            // Holding something - place or swap
            PlaceItem(slotIndex, rightClick);
        }
    }

    /// <summary>
    /// Quick-move item between hotbar and main inventory.
    /// </summary>
    private void QuickMoveItem(int fromSlot)
    {
        var item = _player.Inventory.GetSlot(fromSlot);
        if (item.IsEmpty) return;

        bool isInHotbar = fromSlot < _player.Inventory.HotbarSize;
        int searchStart = isInHotbar ? _player.Inventory.HotbarSize : 0;
        int searchEnd = isInHotbar ? _player.Inventory.Size : _player.Inventory.HotbarSize;

        // First try to stack with existing items
        for (int i = searchStart; i < searchEnd; i++)
        {
            var targetSlot = _player.Inventory.GetSlot(i);
            if (targetSlot.Type == item.Type && !targetSlot.IsFull)
            {
                int canFit = targetSlot.MaxStack - targetSlot.Count;
                int toMove = Math.Min(canFit, item.Count);

                var updated = targetSlot;
                updated.Count += toMove;
                _player.Inventory.SetSlot(i, updated);

                item.Count -= toMove;
                if (item.Count <= 0)
                {
                    _player.Inventory.SetSlot(fromSlot, ItemStack.Empty);
                    return;
                }
            }
        }

        // Then try to find empty slot
        for (int i = searchStart; i < searchEnd; i++)
        {
            var targetSlot = _player.Inventory.GetSlot(i);
            if (targetSlot.IsEmpty)
            {
                _player.Inventory.SetSlot(i, item);
                _player.Inventory.SetSlot(fromSlot, ItemStack.Empty);
                return;
            }
        }

        // Update remaining in original slot if partial move
        _player.Inventory.SetSlot(fromSlot, item);
    }

    /// <summary>
    /// Drop the held item (when clicking outside inventory).
    /// </summary>
    public void DropHeldItem()
    {
        if (_heldItem.IsEmpty)
            return;

        // TODO: Spawn dropped item entity in world
        System.Diagnostics.Debug.WriteLine($"Dropped {_heldItem.Count}x {_heldItem.Type}");
        _heldItem = ItemStack.Empty;
        _heldFromSlot = -1;
    }

    #endregion

    /// <summary>
    /// Draw all active UI elements.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        if (IsInventoryOpen)
        {
            InventoryUI.Draw(spriteBatch, pixelTexture, mousePosition);

            // Draw held item at mouse cursor
            if (!_heldItem.IsEmpty)
            {
                DrawHeldItem(spriteBatch, pixelTexture, mousePosition);
            }
        }
    }

    /// <summary>
    /// Draw the item being held at the mouse cursor.
    /// </summary>
    private void DrawHeldItem(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 mousePosition)
    {
        int size = 32;
        Vector2 position = mousePosition - new Vector2(size / 2f);

        // Get item color
        Color itemColor = InventoryUI.GetItemColor(_heldItem.Type);

        // Draw item (slightly transparent and offset)
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle((int)position.X, (int)position.Y, size, size),
            itemColor * 0.9f
        );

        // Draw stack count
        if (_heldItem.Count > 1)
        {
            int countWidth = _heldItem.Count >= 100 ? 20 : (_heldItem.Count >= 10 ? 14 : 8);
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(
                    (int)(position.X + size - countWidth - 2),
                    (int)(position.Y + size - 10),
                    countWidth, 8
                ),
                new Color(0, 0, 0, 200)
            );
        }
    }
}