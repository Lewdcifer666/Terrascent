using Microsoft.Xna.Framework;
using Terrascent.Entities;
using Terrascent.Items;
using Terrascent.World;

namespace Terrascent.Systems;

/// <summary>
/// Handles tile mining with progress, range checking, and drops.
/// </summary>
public class MiningSystem
{
    // Mining range in tiles
    public float MiningRange { get; set; } = 4.5f;

    // Current mining state
    private Point? _currentTarget;
    private float _miningProgress;
    private TileType _targetTileType;

    /// <summary>
    /// Current tile being mined (null if none).
    /// </summary>
    public Point? CurrentTarget => _currentTarget;

    /// <summary>
    /// Mining progress from 0 to 1.
    /// </summary>
    public float Progress => _currentTarget.HasValue ? _miningProgress : 0f;

    /// <summary>
    /// Check if a tile position is within mining range of the player.
    /// </summary>
    public bool IsInRange(Player player, Point tilePos)
    {
        Vector2 tileCenter = WorldCoordinates.TileToWorldCenter(tilePos.X, tilePos.Y);
        float distance = Vector2.Distance(player.Center, tileCenter);
        float rangePixels = MiningRange * WorldCoordinates.TILE_SIZE;
        return distance <= rangePixels;
    }

    /// <summary>
    /// Update mining progress. Call every fixed update while mining.
    /// </summary>
    /// <returns>True if tile was successfully mined this frame</returns>
    public bool UpdateMining(Point tilePos, ChunkManager chunks, Player player, float deltaTime)
    {
        if (!IsInRange(player, tilePos))
        {
            CancelMining();
            return false;
        }

        var tile = chunks.GetTileAt(tilePos);

        if (tile.IsAir)
        {
            CancelMining();
            return false;
        }

        if (_currentTarget != tilePos || _targetTileType != tile.Type)
        {
            _currentTarget = tilePos;
            _targetTileType = tile.Type;
            _miningProgress = 0f;
        }

        var props = TileRegistry.Get(tile.Type);

        if (props.MiningTime < 0)
        {
            CancelMining();
            return false;
        }

        // Calculate mining speed (ticks to seconds)
        float miningTimeSeconds = props.MiningTime / 60f;

        if (miningTimeSeconds <= 0)
        {
            miningTimeSeconds = 0.05f;
        }

        _miningProgress += deltaTime / miningTimeSeconds;

        if (_miningProgress >= 1f)
        {
            // Get the item to drop before clearing the tile
            var dropItem = ItemRegistry.GetItemForTile(tile.Type);

            // Mine the tile
            chunks.ClearTileAt(tilePos.X, tilePos.Y);

            // Add item directly to player inventory (for now, skip dropped items)
            if (dropItem.HasValue)
            {
                int overflow = player.Inventory.AddItem(dropItem.Value, 1);
                if (overflow > 0)
                {
                    // TODO: Spawn dropped item entity in world
                    System.Diagnostics.Debug.WriteLine($"Inventory full! Couldn't pick up {dropItem.Value}");
                }
            }

            CancelMining();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cancel current mining operation.
    /// </summary>
    public void CancelMining()
    {
        _currentTarget = null;
        _miningProgress = 0f;
        _targetTileType = TileType.Air;
    }
}