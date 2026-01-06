using Microsoft.Xna.Framework;

namespace Terrascent.World;

/// <summary>
/// Helper methods for converting between coordinate systems.
/// </summary>
public static class WorldCoordinates
{
    public const int TILE_SIZE = 16;
    public const int CHUNK_SIZE = 32;
    public const int CHUNK_PIXEL_SIZE = CHUNK_SIZE * TILE_SIZE;  // 512

    #region World (Pixel) to Tile

    /// <summary>
    /// Convert world/pixel position to tile coordinates.
    /// </summary>
    public static Point WorldToTile(Vector2 worldPosition)
    {
        return new Point(
            (int)MathF.Floor(worldPosition.X / TILE_SIZE),
            (int)MathF.Floor(worldPosition.Y / TILE_SIZE)
        );
    }

    /// <summary>
    /// Convert world/pixel position to tile coordinates.
    /// </summary>
    public static Point WorldToTile(float worldX, float worldY)
    {
        return new Point(
            (int)MathF.Floor(worldX / TILE_SIZE),
            (int)MathF.Floor(worldY / TILE_SIZE)
        );
    }

    #endregion

    #region Tile to World (Pixel)

    /// <summary>
    /// Convert tile coordinates to world/pixel position (top-left of tile).
    /// </summary>
    public static Vector2 TileToWorld(Point tilePosition)
    {
        return new Vector2(
            tilePosition.X * TILE_SIZE,
            tilePosition.Y * TILE_SIZE
        );
    }

    /// <summary>
    /// Convert tile coordinates to world/pixel position (top-left of tile).
    /// </summary>
    public static Vector2 TileToWorld(int tileX, int tileY)
    {
        return new Vector2(tileX * TILE_SIZE, tileY * TILE_SIZE);
    }

    /// <summary>
    /// Convert tile coordinates to world/pixel position (center of tile).
    /// </summary>
    public static Vector2 TileToWorldCenter(int tileX, int tileY)
    {
        return new Vector2(
            tileX * TILE_SIZE + TILE_SIZE / 2f,
            tileY * TILE_SIZE + TILE_SIZE / 2f
        );
    }

    #endregion

    #region Tile to Chunk

    /// <summary>
    /// Convert tile coordinates to chunk coordinates.
    /// </summary>
    public static Point TileToChunk(Point tilePosition)
    {
        return new Point(
            (int)MathF.Floor((float)tilePosition.X / CHUNK_SIZE),
            (int)MathF.Floor((float)tilePosition.Y / CHUNK_SIZE)
        );
    }

    /// <summary>
    /// Convert tile coordinates to chunk coordinates.
    /// </summary>
    public static Point TileToChunk(int tileX, int tileY)
    {
        return new Point(
            (int)MathF.Floor((float)tileX / CHUNK_SIZE),
            (int)MathF.Floor((float)tileY / CHUNK_SIZE)
        );
    }

    #endregion

    #region Tile to Local (within Chunk)

    /// <summary>
    /// Convert tile coordinates to local chunk coordinates (0-31).
    /// </summary>
    public static Point TileToLocal(Point tilePosition)
    {
        // Handle negative coordinates properly with modulo
        int localX = ((tilePosition.X % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;
        int localY = ((tilePosition.Y % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;
        return new Point(localX, localY);
    }

    /// <summary>
    /// Convert tile coordinates to local chunk coordinates (0-31).
    /// </summary>
    public static Point TileToLocal(int tileX, int tileY)
    {
        int localX = ((tileX % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;
        int localY = ((tileY % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;
        return new Point(localX, localY);
    }

    #endregion

    #region World to Chunk

    /// <summary>
    /// Convert world/pixel position to chunk coordinates.
    /// </summary>
    public static Point WorldToChunk(Vector2 worldPosition)
    {
        return new Point(
            (int)MathF.Floor(worldPosition.X / CHUNK_PIXEL_SIZE),
            (int)MathF.Floor(worldPosition.Y / CHUNK_PIXEL_SIZE)
        );
    }

    #endregion

    #region Chunk to Tile/World

    /// <summary>
    /// Convert chunk coordinates to tile coordinates (top-left of chunk).
    /// </summary>
    public static Point ChunkToTile(Point chunkPosition)
    {
        return new Point(
            chunkPosition.X * CHUNK_SIZE,
            chunkPosition.Y * CHUNK_SIZE
        );
    }

    /// <summary>
    /// Convert chunk coordinates to world/pixel position (top-left of chunk).
    /// </summary>
    public static Vector2 ChunkToWorld(Point chunkPosition)
    {
        return new Vector2(
            chunkPosition.X * CHUNK_PIXEL_SIZE,
            chunkPosition.Y * CHUNK_PIXEL_SIZE
        );
    }

    #endregion

    #region Combined Conversions

    /// <summary>
    /// Get both chunk and local coordinates from a tile position.
    /// </summary>
    public static (Point chunk, Point local) TileToChunkAndLocal(int tileX, int tileY)
    {
        return (TileToChunk(tileX, tileY), TileToLocal(tileX, tileY));
    }

    /// <summary>
    /// Get both chunk and local coordinates from a world position.
    /// </summary>
    public static (Point chunk, Point local) WorldToChunkAndLocal(Vector2 worldPosition)
    {
        var tile = WorldToTile(worldPosition);
        return TileToChunkAndLocal(tile.X, tile.Y);
    }

    #endregion
}