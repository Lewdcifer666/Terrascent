using Microsoft.Xna.Framework;
using Terrascent.Entities;
using Terrascent.Items;
using Terrascent.World;

namespace Terrascent.Systems;

/// <summary>
/// Handles tile placement with range and validity checking.
/// </summary>
public class BuildingSystem
{
    public float BuildRange { get; set; } = 4.5f;

    public bool IsInRange(Player player, Point tilePos)
    {
        Vector2 tileCenter = WorldCoordinates.TileToWorldCenter(tilePos.X, tilePos.Y);
        float distance = Vector2.Distance(player.Center, tileCenter);
        float rangePixels = BuildRange * WorldCoordinates.TILE_SIZE;
        return distance <= rangePixels;
    }

    public bool CanPlace(Point tilePos, ChunkManager chunks, Player player)
    {
        if (!IsInRange(player, tilePos))
            return false;

        // Check selected item is placeable
        var selectedItem = player.Inventory.SelectedItem;
        if (selectedItem.IsEmpty)
            return false;

        var placesTile = ItemRegistry.GetPlacesTile(selectedItem.Type);
        if (!placesTile.HasValue)
            return false;

        // Target must be air
        var tile = chunks.GetTileAt(tilePos);
        if (!tile.IsAir)
            return false;

        // Must not overlap with player (unless tile is non-solid like torch)
        var tileProps = TileRegistry.Get(placesTile.Value);
        if (tileProps.IsSolid)
        {
            Rectangle tileBounds = new(
                tilePos.X * WorldCoordinates.TILE_SIZE,
                tilePos.Y * WorldCoordinates.TILE_SIZE,
                WorldCoordinates.TILE_SIZE,
                WorldCoordinates.TILE_SIZE
            );

            if (player.Hitbox.Intersects(tileBounds))
                return false;
        }

        // Must be adjacent to a solid tile
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
    /// Attempt to place a tile from the player's selected item.
    /// </summary>
    public bool TryPlace(Point tilePos, ChunkManager chunks, Player player)
    {
        if (!CanPlace(tilePos, chunks, player))
            return false;

        var selectedItem = player.Inventory.SelectedItem;
        var placesTile = ItemRegistry.GetPlacesTile(selectedItem.Type);

        if (!placesTile.HasValue)
            return false;

        // Place the tile
        chunks.SetTileTypeAt(tilePos.X, tilePos.Y, placesTile.Value);

        // Consume item from inventory
        player.Inventory.RemoveFromSelected();

        return true;
    }
}