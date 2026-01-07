namespace Terrascent.Economy;

/// <summary>
/// Tracks player currency (gold).
/// </summary>
public class Currency
{
    /// <summary>
    /// Current gold amount.
    /// </summary>
    public int Gold { get; private set; }

    /// <summary>
    /// Total gold earned this run (for statistics).
    /// </summary>
    public int TotalGoldEarned { get; private set; }

    /// <summary>
    /// Total gold spent this run.
    /// </summary>
    public int TotalGoldSpent { get; private set; }

    /// <summary>
    /// Event fired when gold changes.
    /// </summary>
    public event Action<int, int>? OnGoldChanged; // (oldValue, newValue)

    /// <summary>
    /// Add gold to the player's balance.
    /// </summary>
    /// <param name="amount">Amount to add (must be positive)</param>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        int oldGold = Gold;
        Gold += amount;
        TotalGoldEarned += amount;

        OnGoldChanged?.Invoke(oldGold, Gold);
    }

    /// <summary>
    /// Try to spend gold. Returns true if successful.
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if player had enough gold</returns>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Gold < amount)
            return false;

        int oldGold = Gold;
        Gold -= amount;
        TotalGoldSpent += amount;

        OnGoldChanged?.Invoke(oldGold, Gold);
        return true;
    }

    /// <summary>
    /// Check if player can afford an amount.
    /// </summary>
    public bool CanAfford(int amount) => Gold >= amount;

    /// <summary>
    /// Set gold directly (for loading saves).
    /// </summary>
    public void SetGold(int amount)
    {
        int oldGold = Gold;
        Gold = Math.Max(0, amount);
        OnGoldChanged?.Invoke(oldGold, Gold);
    }

    /// <summary>
    /// Reset currency (for new game).
    /// </summary>
    public void Reset()
    {
        Gold = 0;
        TotalGoldEarned = 0;
        TotalGoldSpent = 0;
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(Gold);
        writer.Write(TotalGoldEarned);
        writer.Write(TotalGoldSpent);
    }

    public void LoadFrom(BinaryReader reader)
    {
        Gold = reader.ReadInt32();
        TotalGoldEarned = reader.ReadInt32();
        TotalGoldSpent = reader.ReadInt32();
    }

    #endregion
}