using Microsoft.Xna.Framework;
using Terrascent.Entities;
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
        // Get tile center in world coordinates
        Vector2 tileCenter = WorldCoordinates.TileToWorldCenter(tilePos.X, tilePos.Y);

        // Distance from player center to tile center
        float distance = Vector2.Distance(player.Center, tileCenter);

        // Convert range from tiles to pixels
        float rangePixels = MiningRange * WorldCoordinates.TILE_SIZE;

        return distance <= rangePixels;
    }

    /// <summary>
    /// Update mining progress. Call every fixed update while mining.
    /// </summary>
    /// <param name="tilePos">Tile position being mined</param>
    /// <param name="chunks">Chunk manager for tile access</param>
    /// <param name="player">Player for range check</param>
    /// <param name="deltaTime">Fixed timestep delta</param>
    /// <returns>True if tile was successfully mined this frame</returns>
    public bool UpdateMining(Point tilePos, ChunkManager chunks, Player player, float deltaTime)
    {
        // Check range
        if (!IsInRange(player, tilePos))
        {
            CancelMining();
            return false;
        }

        // Get tile
        var tile = chunks.GetTileAt(tilePos);

        // Can't mine air
        if (tile.IsAir)
        {
            CancelMining();
            return false;
        }

        // Check if target changed
        if (_currentTarget != tilePos || _targetTileType != tile.Type)
        {
            // New target - reset progress
            _currentTarget = tilePos;
            _targetTileType = tile.Type;
            _miningProgress = 0f;
        }

        // Get mining time for this tile
        var props = TileRegistry.Get(tile.Type);

        // Can't mine unbreakable tiles
        if (props.MiningTime < 0)
        {
            CancelMining();
            return false;
        }

        // TODO: Check pickaxe power requirement
        // if (player.PickaxePower < props.PickaxeRequired) return false;

        // Calculate mining speed (ticks to seconds)
        float miningTimeSeconds = props.MiningTime / 60f;

        // Prevent division by zero for instant-break tiles
        if (miningTimeSeconds <= 0)
        {
            miningTimeSeconds = 0.05f; // Minimum 3 frames
        }

        // Progress mining
        _miningProgress += deltaTime / miningTimeSeconds;

        // Check if complete
        if (_miningProgress >= 1f)
        {
            // Mine the tile!
            chunks.ClearTileAt(tilePos.X, tilePos.Y);

            // TODO: Spawn item drop
            // SpawnDrop(tilePos, tile.Type);

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