using Microsoft.Xna.Framework;
using Terrascent.Entities;
using Terrascent.World;

namespace Terrascent.Systems;

/// <summary>
/// Handles tile placement with range and validity checking.
/// </summary>
public class BuildingSystem
{
    // Building range in tiles
    public float BuildRange { get; set; } = 4.5f;

    // Currently selected tile type to place
    public TileType SelectedTile { get; set; } = TileType.Dirt;

    /// <summary>
    /// Check if a tile position is within building range of the player.
    /// </summary>
    public bool IsInRange(Player player, Point tilePos)
    {
        Vector2 tileCenter = WorldCoordinates.TileToWorldCenter(tilePos.X, tilePos.Y);
        float distance = Vector2.Distance(player.Center, tileCenter);
        float rangePixels = BuildRange * WorldCoordinates.TILE_SIZE;
        return distance <= rangePixels;
    }

    /// <summary>
    /// Check if a tile can be placed at this position.
    /// </summary>
    public bool CanPlace(Point tilePos, ChunkManager chunks, Player player)
    {
        // Must be in range
        if (!IsInRange(player, tilePos))
            return false;

        // Target must be air
        var tile = chunks.GetTileAt(tilePos);
        if (!tile.IsAir)
            return false;

        // Must not overlap with player
        Rectangle tileBounds = new(
            tilePos.X * WorldCoordinates.TILE_SIZE,
            tilePos.Y * WorldCoordinates.TILE_SIZE,
            WorldCoordinates.TILE_SIZE,
            WorldCoordinates.TILE_SIZE
        );

        if (player.Hitbox.Intersects(tileBounds))
            return false;

        // Must be adjacent to a solid tile (can't place floating blocks)
        bool hasAdjacentSolid =
            chunks.IsSolidAt(tilePos.X - 1, tilePos.Y) ||
            chunks.IsSolidAt(tilePos.X + 1, tilePos.Y) ||
            chunks.IsSolidAt(tilePos.X, tilePos.Y - 1) ||
            chunks.IsSolidAt(tilePos.X, tilePos.Y + 1);

        if (!hasAdjacentSolid)
            return false;

        return true;
    }

    /// <summary>
    /// Attempt to place a tile.
    /// </summary>
    /// <returns>True if tile was placed</returns>
    public bool TryPlace(Point tilePos, ChunkManager chunks, Player player)
    {
        if (!CanPlace(tilePos, chunks, player))
            return false;

        chunks.SetTileTypeAt(tilePos.X, tilePos.Y, SelectedTile);

        // TODO: Remove item from inventory

        return true;
    }
}