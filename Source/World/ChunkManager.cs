using Microsoft.Xna.Framework;
using Terrascent.World.Generation;

namespace Terrascent.World;

/// <summary>
/// Manages chunk loading, unloading, and access.
/// </summary>
public class ChunkManager
{
    /// <summary>
    /// All currently loaded chunks, keyed by chunk coordinate.
    /// </summary>
    private readonly Dictionary<Point, Chunk> _chunks = new();

    /// <summary>
    /// The world generator used to create new chunks.
    /// </summary>
    public WorldGenerator? Generator { get; set; }

    /// <summary>
    /// How many chunks to keep loaded around the player (radius).
    /// 3 = 7x7 grid = 49 chunks loaded.
    /// </summary>
    public int LoadRadius { get; set; } = 3;

    /// <summary>
    /// How far beyond LoadRadius before chunks are unloaded.
    /// </summary>
    public int UnloadBuffer { get; set; } = 2;

    /// <summary>
    /// Current center chunk (usually where the player is).
    /// </summary>
    public Point CenterChunk { get; private set; }

    /// <summary>
    /// Number of currently loaded chunks.
    /// </summary>
    public int LoadedChunkCount => _chunks.Count;

    /// <summary>
    /// Event fired when a chunk is loaded/created.
    /// </summary>
    public event Action<Chunk>? OnChunkLoaded;

    /// <summary>
    /// Event fired when a chunk is unloaded.
    /// </summary>
    public event Action<Chunk>? OnChunkUnloaded;

    #region Chunk Access

    /// <summary>
    /// Get a chunk by chunk coordinates. Returns null if not loaded.
    /// </summary>
    public Chunk? GetChunk(int chunkX, int chunkY)
    {
        return _chunks.GetValueOrDefault(new Point(chunkX, chunkY));
    }

    /// <summary>
    /// Get a chunk by chunk coordinates. Returns null if not loaded.
    /// </summary>
    public Chunk? GetChunk(Point chunkPos)
    {
        return _chunks.GetValueOrDefault(chunkPos);
    }

    /// <summary>
    /// Get or create a chunk at the specified coordinates.
    /// </summary>
    public Chunk GetOrCreateChunk(int chunkX, int chunkY)
    {
        var pos = new Point(chunkX, chunkY);

        if (!_chunks.TryGetValue(pos, out var chunk))
        {
            chunk = new Chunk(pos);

            // Generate the chunk if we have a generator
            Generator?.GenerateChunk(chunk);

            _chunks[pos] = chunk;
            OnChunkLoaded?.Invoke(chunk);
        }

        return chunk;
    }

    /// <summary>
    /// Get or create a chunk at the specified coordinates.
    /// </summary>
    public Chunk GetOrCreateChunk(Point chunkPos) => GetOrCreateChunk(chunkPos.X, chunkPos.Y);

    /// <summary>
    /// Check if a chunk is loaded.
    /// </summary>
    public bool IsChunkLoaded(int chunkX, int chunkY)
    {
        return _chunks.ContainsKey(new Point(chunkX, chunkY));
    }

    /// <summary>
    /// Check if a chunk is loaded.
    /// </summary>
    public bool IsChunkLoaded(Point chunkPos) => _chunks.ContainsKey(chunkPos);

    #endregion

    #region Tile Access (World Coordinates)

    /// <summary>
    /// Get a tile at world tile coordinates. Returns empty tile if chunk not loaded.
    /// </summary>
    public Tile GetTileAt(int tileX, int tileY)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetChunk(chunkPos);

        if (chunk == null)
            return Tile.Empty;

        return chunk.GetTile(local.X, local.Y);
    }

    /// <summary>
    /// Get a tile at world tile coordinates. Returns empty tile if chunk not loaded.
    /// </summary>
    public Tile GetTileAt(Point tilePos) => GetTileAt(tilePos.X, tilePos.Y);

    /// <summary>
    /// Set a tile at world tile coordinates. Creates chunk if needed.
    /// </summary>
    public void SetTileAt(int tileX, int tileY, Tile tile)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetOrCreateChunk(chunkPos);
        chunk.SetTile(local.X, local.Y, tile);
    }

    /// <summary>
    /// Set a tile type at world tile coordinates. Creates chunk if needed.
    /// </summary>
    public void SetTileTypeAt(int tileX, int tileY, TileType type)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetOrCreateChunk(chunkPos);
        chunk.SetTileType(local.X, local.Y, type);
    }

    /// <summary>
    /// Clear a tile at world tile coordinates.
    /// </summary>
    public void ClearTileAt(int tileX, int tileY)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetChunk(chunkPos);
        chunk?.ClearTile(local.X, local.Y);
    }

    /// <summary>
    /// Check if a tile position is solid.
    /// </summary>
    public bool IsSolidAt(int tileX, int tileY)
    {
        var tile = GetTileAt(tileX, tileY);
        return tile.IsActive && TileRegistry.IsSolid(tile.Type);
    }

    #endregion

    #region Chunk Loading/Unloading

    /// <summary>
    /// Update chunk loading based on a world position (usually player position).
    /// Call this every frame or when the player moves significantly.
    /// </summary>
    public void UpdateLoadedChunks(Vector2 worldPosition)
    {
        var newCenter = WorldCoordinates.WorldToChunk(worldPosition);

        // Only update if center changed
        if (newCenter == CenterChunk && _chunks.Count > 0)
            return;

        CenterChunk = newCenter;

        // Load chunks in radius
        for (int y = -LoadRadius; y <= LoadRadius; y++)
        {
            for (int x = -LoadRadius; x <= LoadRadius; x++)
            {
                var chunkPos = new Point(CenterChunk.X + x, CenterChunk.Y + y);
                if (!_chunks.ContainsKey(chunkPos))
                {
                    var chunk = new Chunk(chunkPos);

                    // GENERATE THE CHUNK - This was missing!
                    Generator?.GenerateChunk(chunk);

                    _chunks[chunkPos] = chunk;
                    OnChunkLoaded?.Invoke(chunk);
                }
            }
        }

        // Unload chunks outside radius + buffer
        int unloadDistance = LoadRadius + UnloadBuffer;
        var chunksToRemove = new List<Point>();

        foreach (var chunkPos in _chunks.Keys)
        {
            int dx = Math.Abs(chunkPos.X - CenterChunk.X);
            int dy = Math.Abs(chunkPos.Y - CenterChunk.Y);

            if (dx > unloadDistance || dy > unloadDistance)
            {
                chunksToRemove.Add(chunkPos);
            }
        }

        foreach (var pos in chunksToRemove)
        {
            if (_chunks.TryGetValue(pos, out var chunk))
            {
                OnChunkUnloaded?.Invoke(chunk);
                _chunks.Remove(pos);
            }
        }
    }

    /// <summary>
    /// Get all currently loaded chunks.
    /// </summary>
    public IEnumerable<Chunk> GetLoadedChunks() => _chunks.Values;

    /// <summary>
    /// Get all loaded chunks that intersect with a rectangle (in world coordinates).
    /// </summary>
    public IEnumerable<Chunk> GetChunksInBounds(Rectangle worldBounds)
    {
        var minChunk = WorldCoordinates.WorldToChunk(new Vector2(worldBounds.Left, worldBounds.Top));
        var maxChunk = WorldCoordinates.WorldToChunk(new Vector2(worldBounds.Right, worldBounds.Bottom));

        for (int y = minChunk.Y; y <= maxChunk.Y; y++)
        {
            for (int x = minChunk.X; x <= maxChunk.X; x++)
            {
                var chunk = GetChunk(x, y);
                if (chunk != null)
                    yield return chunk;
            }
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Clear all chunks.
    /// </summary>
    public void Clear()
    {
        foreach (var chunk in _chunks.Values)
        {
            OnChunkUnloaded?.Invoke(chunk);
        }
        _chunks.Clear();
    }

    #endregion
}