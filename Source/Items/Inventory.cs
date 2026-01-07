namespace Terrascent.Items;

/// <summary>
/// Container for item stacks with add/remove/find operations.
/// </summary>
public class Inventory
{
    private readonly ItemStack[] _slots;

    /// <summary>
    /// Number of slots in this inventory.
    /// </summary>
    public int Size => _slots.Length;

    /// <summary>
    /// Hotbar size (first N slots).
    /// </summary>
    public int HotbarSize { get; }

    /// <summary>
    /// Currently selected hotbar slot (0-indexed).
    /// </summary>
    public int SelectedSlot { get; set; }

    /// <summary>
    /// Get the currently selected item stack.
    /// </summary>
    public ItemStack SelectedItem => _slots[SelectedSlot];

    /// <summary>
    /// Event fired when inventory contents change.
    /// </summary>
    public event Action<int>? OnSlotChanged;

    public Inventory(int size = 40, int hotbarSize = 10)
    {
        _slots = new ItemStack[size];
        HotbarSize = Math.Min(hotbarSize, size);
        SelectedSlot = 0;

        // Initialize all slots as empty
        for (int i = 0; i < size; i++)
        {
            _slots[i] = ItemStack.Empty;
        }
    }

    #region Slot Access

    /// <summary>
    /// Get a slot by index.
    /// </summary>
    public ItemStack GetSlot(int index)
    {
        if (index < 0 || index >= Size)
            return ItemStack.Empty;
        return _slots[index];
    }

    /// <summary>
    /// Set a slot directly.
    /// </summary>
    public void SetSlot(int index, ItemStack stack)
    {
        if (index < 0 || index >= Size)
            return;

        _slots[index] = stack;
        OnSlotChanged?.Invoke(index);
    }

    /// <summary>
    /// Get a reference to a slot for direct modification.
    /// </summary>
    public ref ItemStack GetSlotRef(int index)
    {
        return ref _slots[index];
    }

    #endregion

    #region Adding Items

    /// <summary>
    /// Try to add an item stack to the inventory.
    /// First tries to stack with existing items, then uses empty slots.
    /// </summary>
    /// <param name="type">Item type to add</param>
    /// <param name="count">Amount to add</param>
    /// <returns>Amount that couldn't fit (overflow)</returns>
    public int AddItem(ItemType type, int count = 1)
    {
        if (type == ItemType.None || count <= 0)
            return 0;

        int remaining = count;
        int maxStack = ItemRegistry.GetMaxStack(type);

        // First pass: try to stack with existing items
        for (int i = 0; i < Size && remaining > 0; i++)
        {
            if (_slots[i].Type == type && !_slots[i].IsFull)
            {
                int canFit = maxStack - _slots[i].Count;
                int toAdd = Math.Min(remaining, canFit);
                _slots[i].Count += toAdd;
                remaining -= toAdd;
                OnSlotChanged?.Invoke(i);
            }
        }

        // Second pass: use empty slots
        for (int i = 0; i < Size && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty)
            {
                int toAdd = Math.Min(remaining, maxStack);
                _slots[i] = new ItemStack(type, toAdd);
                remaining -= toAdd;
                OnSlotChanged?.Invoke(i);
            }
        }

        return remaining; // Return overflow
    }

    /// <summary>
    /// Try to add an item stack.
    /// </summary>
    public int AddItem(ItemStack stack)
    {
        return AddItem(stack.Type, stack.Count);
    }

    #endregion

    #region Removing Items

    /// <summary>
    /// Remove items of a type from the inventory.
    /// </summary>
    /// <param name="type">Item type to remove</param>
    /// <param name="count">Amount to remove</param>
    /// <returns>Amount actually removed</returns>
    public int RemoveItem(ItemType type, int count = 1)
    {
        if (type == ItemType.None || count <= 0)
            return 0;

        int remaining = count;

        // Remove from slots (prefer non-hotbar first to preserve hotbar)
        for (int i = Size - 1; i >= 0 && remaining > 0; i--)
        {
            if (_slots[i].Type == type)
            {
                int toRemove = Math.Min(remaining, _slots[i].Count);
                _slots[i].Count -= toRemove;
                remaining -= toRemove;

                if (_slots[i].Count <= 0)
                {
                    _slots[i] = ItemStack.Empty;
                }

                OnSlotChanged?.Invoke(i);
            }
        }

        return count - remaining; // Return amount actually removed
    }

    /// <summary>
    /// Remove one item from the selected hotbar slot.
    /// </summary>
    /// <returns>True if an item was removed</returns>
    public bool RemoveFromSelected()
    {
        if (_slots[SelectedSlot].IsEmpty)
            return false;

        _slots[SelectedSlot].Count--;

        if (_slots[SelectedSlot].Count <= 0)
        {
            _slots[SelectedSlot] = ItemStack.Empty;
        }

        OnSlotChanged?.Invoke(SelectedSlot);
        return true;
    }

    #endregion

    #region Queries

    /// <summary>
    /// Count total items of a type in inventory.
    /// </summary>
    public int CountItem(ItemType type)
    {
        int total = 0;
        for (int i = 0; i < Size; i++)
        {
            if (_slots[i].Type == type)
                total += _slots[i].Count;
        }
        return total;
    }

    /// <summary>
    /// Check if inventory contains at least this many of an item.
    /// </summary>
    public bool HasItem(ItemType type, int count = 1)
    {
        return CountItem(type) >= count;
    }

    /// <summary>
    /// Find the first slot containing an item type.
    /// </summary>
    /// <returns>Slot index, or -1 if not found</returns>
    public int FindItem(ItemType type)
    {
        for (int i = 0; i < Size; i++)
        {
            if (_slots[i].Type == type)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Find the first empty slot.
    /// </summary>
    /// <returns>Slot index, or -1 if full</returns>
    public int FindEmptySlot()
    {
        for (int i = 0; i < Size; i++)
        {
            if (_slots[i].IsEmpty)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Check if inventory is completely full.
    /// </summary>
    public bool IsFull()
    {
        for (int i = 0; i < Size; i++)
        {
            if (_slots[i].IsEmpty || !_slots[i].IsFull)
                return false;
        }
        return true;
    }

    #endregion

    #region Hotbar

    /// <summary>
    /// Select a hotbar slot (0 to HotbarSize-1).
    /// </summary>
    public void SelectSlot(int slot)
    {
        SelectedSlot = Math.Clamp(slot, 0, HotbarSize - 1);
    }

    /// <summary>
    /// Scroll hotbar selection.
    /// </summary>
    public void ScrollSelection(int delta)
    {
        SelectedSlot = ((SelectedSlot + delta) % HotbarSize + HotbarSize) % HotbarSize;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Swap two slots.
    /// </summary>
    public void SwapSlots(int slotA, int slotB)
    {
        if (slotA < 0 || slotA >= Size || slotB < 0 || slotB >= Size)
            return;

        (_slots[slotA], _slots[slotB]) = (_slots[slotB], _slots[slotA]);
        OnSlotChanged?.Invoke(slotA);
        OnSlotChanged?.Invoke(slotB);
    }

    /// <summary>
    /// Clear the entire inventory.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < Size; i++)
        {
            _slots[i] = ItemStack.Empty;
            OnSlotChanged?.Invoke(i);
        }
    }

    /// <summary>
    /// Get all non-empty slots for iteration.
    /// </summary>
    public IEnumerable<(int index, ItemStack stack)> GetNonEmptySlots()
    {
        for (int i = 0; i < Size; i++)
        {
            if (!_slots[i].IsEmpty)
                yield return (i, _slots[i]);
        }
    }

    #endregion
}