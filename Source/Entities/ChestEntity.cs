using Microsoft.Xna.Framework;
using Terrascent.Economy;

namespace Terrascent.Entities;

/// <summary>
/// A chest that can be placed in the world and opened by players.
/// </summary>
public class ChestEntity : Entity
{
    /// <summary>Type of this chest.</summary>
    public ChestType ChestType { get; }

    /// <summary>Has this chest been opened?</summary>
    public bool IsOpened { get; private set; }

    /// <summary>Time since chest was opened (for animation).</summary>
    public float OpenedTime { get; private set; }

    /// <summary>Unique identifier for save/load.</summary>
    public Guid Id { get; }

    /// <summary>Tile position in the world.</summary>
    public Point TilePosition { get; }

    /// <summary>Interaction range in pixels.</summary>
    public float InteractionRange { get; set; } = 64f;

    /// <summary>Event fired when chest is opened.</summary>
    public event Action<ChestEntity, ChestDrop>? OnOpened;

    public ChestEntity(ChestType type, Point tilePosition)
    {
        Id = Guid.NewGuid();
        ChestType = type;
        TilePosition = tilePosition;

        // Set position from tile
        Position = new Vector2(
            tilePosition.X * World.WorldCoordinates.TILE_SIZE,
            tilePosition.Y * World.WorldCoordinates.TILE_SIZE
        );

        // Chest dimensions
        Width = 32;
        Height = 32;

        // Chests don't fall or move
        AffectedByGravity = false;
    }

    public override void Update(float deltaTime)
    {
        if (IsOpened)
        {
            OpenedTime += deltaTime;
        }
    }

    /// <summary>
    /// Check if the player is close enough to interact.
    /// </summary>
    public bool IsInRange(Player player)
    {
        float distance = Vector2.Distance(Center, player.Center);
        return distance <= InteractionRange;
    }

    /// <summary>
    /// Try to open this chest.
    /// </summary>
    /// <param name="chestManager">Chest manager for economy</param>
    /// <param name="currency">Player's currency</param>
    /// <param name="luckBonus">Player's luck stat</param>
    /// <returns>Drops if successful, null if failed</returns>
    public ChestDrop? TryOpen(ChestManager chestManager, Currency currency, int luckBonus = 0)
    {
        if (IsOpened)
            return null;

        var drop = chestManager.TryOpenChest(ChestType, currency, luckBonus);

        if (drop.HasValue)
        {
            IsOpened = true;
            OpenedTime = 0f;
            OnOpened?.Invoke(this, drop.Value);
        }

        return drop;
    }

    /// <summary>
    /// Force open (for boss chests, etc).
    /// </summary>
    public ChestDrop ForceOpen(ChestManager chestManager, int luckBonus = 0)
    {
        if (IsOpened)
            return new ChestDrop { ChestType = ChestType, Items = new() };

        var drop = chestManager.OpenFreeChest(ChestType, luckBonus);
        IsOpened = true;
        OpenedTime = 0f;
        OnOpened?.Invoke(this, drop);

        return drop;
    }

    /// <summary>
    /// Get the current cost to open this chest.
    /// </summary>
    public int GetCost(ChestManager chestManager)
    {
        if (IsOpened)
            return 0;
        return chestManager.GetChestCost(ChestType);
    }

    /// <summary>
    /// Get display color based on chest type and state.
    /// </summary>
    public Color GetColor()
    {
        var data = ChestTypeRegistry.Get(ChestType);
        Color baseColor = new(data.Color.R, data.Color.G, data.Color.B);

        if (IsOpened)
        {
            // Darken opened chests
            return new Color(
                (int)(baseColor.R * 0.4f),
                (int)(baseColor.G * 0.4f),
                (int)(baseColor.B * 0.4f)
            );
        }

        return baseColor;
    }
}