using Microsoft.Xna.Framework;
using Terrascent.Saves;
using Terrascent.World.Generation;

namespace Terrascent.World;

/// <summary>
/// Manages chunk loading, unloading, and access.
/// </summary>
public class ChunkManager
{
    private readonly Dictionary<Point, Chunk> _chunks = new();

    public WorldGenerator? Generator { get; set; }

    /// <summary>
    /// Optional save manager for loading/saving chunks.
    /// </summary>
    public SaveManager? SaveManager { get; set; }

    public int LoadRadius { get; set; } = 3;
    public int UnloadBuffer { get; set; } = 2;
    public Point CenterChunk { get; private set; }
    public int LoadedChunkCount => _chunks.Count;

    public event Action<Chunk>? OnChunkLoaded;
    public event Action<Chunk>? OnChunkUnloaded;

    #region Chunk Access

    public Chunk? GetChunk(int chunkX, int chunkY)
    {
        return _chunks.GetValueOrDefault(new Point(chunkX, chunkY));
    }

    public Chunk? GetChunk(Point chunkPos)
    {
        return _chunks.GetValueOrDefault(chunkPos);
    }

    public Chunk GetOrCreateChunk(int chunkX, int chunkY)
    {
        var pos = new Point(chunkX, chunkY);

        if (!_chunks.TryGetValue(pos, out var chunk))
        {
            // Try to load from disk first
            chunk = SaveManager?.LoadChunk(chunkX, chunkY);

            if (chunk == null)
            {
                // Generate new chunk
                chunk = new Chunk(pos);
                Generator?.GenerateChunk(chunk);
            }

            _chunks[pos] = chunk;
            OnChunkLoaded?.Invoke(chunk);
        }

        return chunk;
    }

    public Chunk GetOrCreateChunk(Point chunkPos) => GetOrCreateChunk(chunkPos.X, chunkPos.Y);

    public bool IsChunkLoaded(int chunkX, int chunkY)
    {
        return _chunks.ContainsKey(new Point(chunkX, chunkY));
    }

    public bool IsChunkLoaded(Point chunkPos) => _chunks.ContainsKey(chunkPos);

    #endregion

    #region Tile Access (World Coordinates)

    public Tile GetTileAt(int tileX, int tileY)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetChunk(chunkPos);

        if (chunk == null)
            return Tile.Empty;

        return chunk.GetTile(local.X, local.Y);
    }

    public Tile GetTileAt(Point tilePos) => GetTileAt(tilePos.X, tilePos.Y);

    public void SetTileAt(int tileX, int tileY, Tile tile)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetOrCreateChunk(chunkPos);
        chunk.SetTile(local.X, local.Y, tile);
    }

    public void SetTileTypeAt(int tileX, int tileY, TileType type)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetOrCreateChunk(chunkPos);
        chunk.SetTileType(local.X, local.Y, type);
    }

    public void ClearTileAt(int tileX, int tileY)
    {
        var (chunkPos, local) = WorldCoordinates.TileToChunkAndLocal(tileX, tileY);
        var chunk = GetChunk(chunkPos);
        chunk?.ClearTile(local.X, local.Y);
    }

    public bool IsSolidAt(int tileX, int tileY)
    {
        var tile = GetTileAt(tileX, tileY);
        return tile.IsActive && TileRegistry.IsSolid(tile.Type);
    }

    #endregion

    #region Chunk Loading/Unloading

    public void UpdateLoadedChunks(Vector2 worldPosition)
    {
        var newCenter = WorldCoordinates.WorldToChunk(worldPosition);

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
                    // Try to load from disk first
                    var chunk = SaveManager?.LoadChunk(chunkPos.X, chunkPos.Y);

                    if (chunk == null)
                    {
                        chunk = new Chunk(chunkPos);
                        Generator?.GenerateChunk(chunk);
                    }

                    _chunks[chunkPos] = chunk;
                    OnChunkLoaded?.Invoke(chunk);
                }
            }
        }

        // Unload and save chunks outside radius + buffer
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
                // Save dirty chunks before unloading
                if (chunk.IsDirty && SaveManager != null)
                {
                    SaveManager.SaveChunk(chunk);
                }

                OnChunkUnloaded?.Invoke(chunk);
                _chunks.Remove(pos);
            }
        }
    }

    public IEnumerable<Chunk> GetLoadedChunks() => _chunks.Values;

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