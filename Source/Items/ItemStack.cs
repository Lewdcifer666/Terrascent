namespace Terrascent.Items;

/// <summary>
/// A stack of items (type + quantity).
/// </summary>
public struct ItemStack
{
    /// <summary>
    /// The type of item in this stack.
    /// </summary>
    public ItemType Type { get; set; }

    /// <summary>
    /// Number of items in this stack.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Is this stack empty?
    /// </summary>
    public readonly bool IsEmpty => Type == ItemType.None || Count <= 0;

    /// <summary>
    /// Get properties for this item type.
    /// </summary>
    public readonly ItemProperties Properties => ItemRegistry.Get(Type);

    /// <summary>
    /// Maximum stack size for this item type.
    /// </summary>
    public readonly int MaxStack => Properties.MaxStack;

    /// <summary>
    /// How many more items can fit in this stack?
    /// </summary>
    public readonly int SpaceRemaining => IsEmpty ? 0 : MaxStack - Count;

    /// <summary>
    /// Is this stack at maximum capacity?
    /// </summary>
    public readonly bool IsFull => !IsEmpty && Count >= MaxStack;

    public ItemStack(ItemType type, int count = 1)
    {
        Type = type;
        Count = count;
    }

    /// <summary>
    /// Create an empty stack.
    /// </summary>
    public static ItemStack Empty => new(ItemType.None, 0);

    /// <summary>
    /// Try to add items to this stack.
    /// </summary>
    /// <param name="amount">Amount to add</param>
    /// <returns>Amount that couldn't fit (overflow)</returns>
    public int Add(int amount)
    {
        if (IsEmpty) return amount; // Can't add to empty slot this way

        int canFit = MaxStack - Count;
        int toAdd = Math.Min(amount, canFit);
        Count += toAdd;
        return amount - toAdd; // Return overflow
    }

    /// <summary>
    /// Try to remove items from this stack.
    /// </summary>
    /// <param name="amount">Amount to remove</param>
    /// <returns>Amount actually removed</returns>
    public int Remove(int amount)
    {
        int toRemove = Math.Min(amount, Count);
        Count -= toRemove;

        if (Count <= 0)
        {
            Type = ItemType.None;
            Count = 0;
        }

        return toRemove;
    }

    /// <summary>
    /// Clear this stack.
    /// </summary>
    public void Clear()
    {
        Type = ItemType.None;
        Count = 0;
    }

    public override readonly string ToString()
    {
        return IsEmpty ? "Empty" : $"{Properties.Name} x{Count}";
    }
}