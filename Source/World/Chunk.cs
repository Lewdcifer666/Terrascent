using Microsoft.Xna.Framework;

namespace Terrascent.World;

/// <summary>
/// A 32x32 region of tiles. The basic unit of world storage and rendering.
/// </summary>
public class Chunk
{
    public const int SIZE = 32;
    public const int TILE_COUNT = SIZE * SIZE;  // 1024 tiles

    /// <summary>
    /// The chunk's position in chunk coordinates.
    /// </summary>
    public Point Position { get; }

    /// <summary>
    /// The chunk's position in tile coordinates (top-left corner).
    /// </summary>
    public Point TilePosition => new(Position.X * SIZE, Position.Y * SIZE);

    /// <summary>
    /// The chunk's position in world/pixel coordinates (top-left corner).
    /// </summary>
    public Vector2 WorldPosition => new(
        Position.X * SIZE * TerrascentGame.TILE_SIZE,
        Position.Y * SIZE * TerrascentGame.TILE_SIZE
    );

    /// <summary>
    /// Bounding rectangle in world coordinates.
    /// </summary>
    public Rectangle WorldBounds => new(
        (int)WorldPosition.X,
        (int)WorldPosition.Y,
        SIZE * TerrascentGame.TILE_SIZE,
        SIZE * TerrascentGame.TILE_SIZE
    );

    /// <summary>
    /// The tile data array. Accessed as [x, y] where x and y are 0-31.
    /// </summary>
    private readonly Tile[,] _tiles;

    /// <summary>
    /// Has this chunk been modified since last save?
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Does this chunk need its render mesh rebuilt?
    /// </summary>
    public bool NeedsRebuild { get; private set; }

    /// <summary>
    /// Has this chunk been generated/loaded?
    /// </summary>
    public bool IsLoaded { get; private set; }

    public Chunk(int chunkX, int chunkY)
    {
        Position = new Point(chunkX, chunkY);
        _tiles = new Tile[SIZE, SIZE];
        IsDirty = false;
        NeedsRebuild = true;
        IsLoaded = false;
    }

    public Chunk(Point position) : this(position.X, position.Y) { }

    #region Tile Access

    /// <summary>
    /// Get a tile at local chunk coordinates (0-31).
    /// </summary>
    public ref Tile GetTile(int localX, int localY)
    {
        if (localX < 0 || localX >= SIZE || localY < 0 || localY >= SIZE)
            throw new ArgumentOutOfRangeException($"Local coordinates ({localX}, {localY}) out of chunk bounds");

        return ref _tiles[localX, localY];
    }

    /// <summary>
    /// Get a tile at local chunk coordinates (0-31).
    /// </summary>
    public ref Tile GetTile(Point local) => ref GetTile(local.X, local.Y);

    /// <summary>
    /// Try to get a tile at local coordinates. Returns false if out of bounds.
    /// </summary>
    public bool TryGetTile(int localX, int localY, out Tile tile)
    {
        if (localX < 0 || localX >= SIZE || localY < 0 || localY >= SIZE)
        {
            tile = Tile.Empty;
            return false;
        }

        tile = _tiles[localX, localY];
        return true;
    }

    /// <summary>
    /// Set a tile at local chunk coordinates.
    /// </summary>
    public void SetTile(int localX, int localY, Tile tile)
    {
        if (localX < 0 || localX >= SIZE || localY < 0 || localY >= SIZE)
            throw new ArgumentOutOfRangeException($"Local coordinates ({localX}, {localY}) out of chunk bounds");

        _tiles[localX, localY] = tile;
        MarkDirty();
    }

    /// <summary>
    /// Set the tile type at local coordinates.
    /// </summary>
    public void SetTileType(int localX, int localY, TileType type)
    {
        ref var tile = ref GetTile(localX, localY);
        tile.SetType(type);
        MarkDirty();
    }

    /// <summary>
    /// Clear a tile (make it air) at local coordinates.
    /// </summary>
    public void ClearTile(int localX, int localY)
    {
        ref var tile = ref GetTile(localX, localY);
        tile.Clear();
        MarkDirty();
    }

    #endregion

    #region Chunk State

    /// <summary>
    /// Mark the chunk as modified and needing rebuild.
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
        NeedsRebuild = true;
    }

    /// <summary>
    /// Mark the chunk as saved (clears dirty flag).
    /// </summary>
    public void MarkSaved()
    {
        IsDirty = false;
    }

    /// <summary>
    /// Mark the chunk's render data as rebuilt.
    /// </summary>
    public void MarkRebuilt()
    {
        NeedsRebuild = false;
    }

    /// <summary>
    /// Mark the chunk as loaded/generated.
    /// </summary>
    public void MarkLoaded()
    {
        IsLoaded = true;
    }

    /// <summary>
    /// Fill the entire chunk with a tile type (useful for testing/generation).
    /// </summary>
    public void Fill(TileType type)
    {
        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                _tiles[x, y] = new Tile(type);
            }
        }
        MarkDirty();
    }

    /// <summary>
    /// Clear the entire chunk to air.
    /// </summary>
    public void Clear()
    {
        Fill(TileType.Air);
    }

    #endregion

    #region Iteration

    /// <summary>
    /// Iterate over all tiles in the chunk with their local coordinates.
    /// </summary>
    public IEnumerable<(int x, int y, Tile tile)> EnumerateTiles()
    {
        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                yield return (x, y, _tiles[x, y]);
            }
        }
    }

    #endregion

    public override string ToString()
    {
        return $"Chunk({Position.X}, {Position.Y})";
    }
}