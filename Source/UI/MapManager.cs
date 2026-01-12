using Microsoft.Xna.Framework;
using Terrascent.World;
using Terrascent.World.Biomes;
using Terrascent.World.Generation;

namespace Terrascent.UI;

/// <summary>
/// Manages world map state including explored tiles, zoom/pan, and fog of war.
/// </summary>
public class MapManager
{
    private readonly WorldGenerator _worldGenerator;
    private readonly ChunkManager _chunkManager;

    // Explored tiles tracking (for fog of war)
    private readonly HashSet<Point> _exploredChunks = new();
    private const int EXPLORE_RADIUS = 3;  // Chunks around player that get explored

    // Map view state
    public float Zoom { get; private set; } = 1.0f;
    public Vector2 PanOffset { get; private set; } = Vector2.Zero;
    public bool IsOpen { get; set; } = false;

    // Zoom limits - allows zooming in to see individual tiles
    public const float MIN_ZOOM = 0.1f;   // See entire world
    public const float MAX_ZOOM = 100.0f;  // See individual pixels at max zoom
    public const float ZOOM_STEP = 0.25f;

    // Pan limits (in world tiles)
    public int WorldWidth => _worldGenerator.Config.Width;
    public int WorldHeight => _worldGenerator.Config.Height;

    // Hover info
    public Point? HoveredTile { get; private set; }
    public TileType HoveredTileType { get; private set; }
    public BiomeType HoveredBiome { get; private set; }
    public WorldLayer HoveredLayer { get; private set; }
    public int HoveredDepth { get; private set; }

    public MapManager(WorldGenerator worldGenerator, ChunkManager chunkManager)
    {
        _worldGenerator = worldGenerator;
        _chunkManager = chunkManager;
    }

    /// <summary>
    /// Update map state - called when map is open.
    /// </summary>
    public void Update(float deltaTime, Point playerTilePos, Vector2 mousePosition, Rectangle mapBounds)
    {
        // Update explored area around player
        UpdateExploredArea(playerTilePos);

        // Update hovered tile info
        UpdateHoverInfo(mousePosition, mapBounds);
    }

    /// <summary>
    /// Mark chunks around the player as explored.
    /// </summary>
    public void UpdateExploredArea(Point playerTilePos)
    {
        // Convert to chunk coordinates
        int chunkX = playerTilePos.X / Chunk.SIZE;
        int chunkY = playerTilePos.Y / Chunk.SIZE;

        // Explore chunks in a radius around player
        for (int dx = -EXPLORE_RADIUS; dx <= EXPLORE_RADIUS; dx++)
        {
            for (int dy = -EXPLORE_RADIUS; dy <= EXPLORE_RADIUS; dy++)
            {
                Point chunkPos = new Point(chunkX + dx, chunkY + dy);
                _exploredChunks.Add(chunkPos);
            }
        }
    }

    /// <summary>
    /// Check if a chunk has been explored (for fog of war).
    /// </summary>
    public bool IsChunkExplored(int chunkX, int chunkY)
    {
        return _exploredChunks.Contains(new Point(chunkX, chunkY));
    }

    /// <summary>
    /// Check if a world tile has been explored.
    /// </summary>
    public bool IsTileExplored(int worldX, int worldY)
    {
        int chunkX = worldX / Chunk.SIZE;
        int chunkY = worldY / Chunk.SIZE;
        return IsChunkExplored(chunkX, chunkY);
    }

    /// <summary>
    /// Zoom in on the map (multiplicative for smooth scaling).
    /// </summary>
    public void ZoomIn()
    {
        Zoom = Math.Min(Zoom * 1.25f, MAX_ZOOM);
    }

    /// <summary>
    /// Zoom out on the map (multiplicative for smooth scaling).
    /// </summary>
    public void ZoomOut()
    {
        Zoom = Math.Max(Zoom / 1.25f, MIN_ZOOM);
    }

    /// <summary>
    /// Set zoom level directly.
    /// </summary>
    public void SetZoom(float zoom)
    {
        Zoom = Math.Clamp(zoom, MIN_ZOOM, MAX_ZOOM);
    }

    /// <summary>
    /// Pan the map by a delta amount.
    /// </summary>
    public void Pan(Vector2 delta)
    {
        PanOffset += delta;

        // Clamp pan to world bounds (accounting for zoom)
        float maxPanX = WorldWidth * 0.5f;
        float maxPanY = WorldHeight * 0.5f;

        PanOffset = new Vector2(
            Math.Clamp(PanOffset.X, -maxPanX, maxPanX),
            Math.Clamp(PanOffset.Y, -maxPanY, maxPanY)
        );
    }

    /// <summary>
    /// Center the map on a world position.
    /// </summary>
    public void CenterOn(Point worldTilePos)
    {
        PanOffset = new Vector2(
            worldTilePos.X - WorldWidth / 2f,
            worldTilePos.Y - WorldHeight / 2f
        );
    }

    /// <summary>
    /// Reset zoom and pan to default.
    /// </summary>
    public void ResetView()
    {
        Zoom = 1.0f;
        PanOffset = Vector2.Zero;
    }

    /// <summary>
    /// Convert screen position to world tile position.
    /// Must match the rendering logic in DrawMap exactly.
    /// </summary>
    public Point ScreenToWorldTile(Vector2 screenPos, Rectangle mapBounds)
    {
        // Get visible world bounds (same as DrawMap)
        Rectangle visibleWorld = GetVisibleWorldBounds();

        // Calculate scale (same as DrawMap)
        float scaleX = (float)mapBounds.Width / visibleWorld.Width;
        float scaleY = (float)mapBounds.Height / visibleWorld.Height;
        float scale = Math.Min(scaleX, scaleY);

        // Calculate actual rendered size and offset (same as DrawMap)
        int renderedWidth = (int)(visibleWorld.Width * scale);
        int renderedHeight = (int)(visibleWorld.Height * scale);
        int offsetX = mapBounds.X + (mapBounds.Width - renderedWidth) / 2;
        int offsetY = mapBounds.Y + (mapBounds.Height - renderedHeight) / 2;

        // Convert screen position to world tile
        // screenX = offsetX + (worldX - visibleWorld.X) * scale
        // So: worldX = visibleWorld.X + (screenX - offsetX) / scale
        int worldX = visibleWorld.X + (int)((screenPos.X - offsetX) / scale);
        int worldY = visibleWorld.Y + (int)((screenPos.Y - offsetY) / scale);

        return new Point(
            Math.Clamp(worldX, 0, WorldWidth - 1),
            Math.Clamp(worldY, 0, WorldHeight - 1)
        );
    }

    /// <summary>
    /// Convert world tile position to screen position.
    /// Must match the rendering logic in DrawMap exactly.
    /// </summary>
    public Vector2 WorldTileToScreen(Point worldTile, Rectangle mapBounds)
    {
        // Get visible world bounds (same as DrawMap)
        Rectangle visibleWorld = GetVisibleWorldBounds();

        // Calculate scale (same as DrawMap)
        float scaleX = (float)mapBounds.Width / visibleWorld.Width;
        float scaleY = (float)mapBounds.Height / visibleWorld.Height;
        float scale = Math.Min(scaleX, scaleY);

        // Calculate actual rendered size and offset (same as DrawMap)
        int renderedWidth = (int)(visibleWorld.Width * scale);
        int renderedHeight = (int)(visibleWorld.Height * scale);
        int offsetX = mapBounds.X + (mapBounds.Width - renderedWidth) / 2;
        int offsetY = mapBounds.Y + (mapBounds.Height - renderedHeight) / 2;

        // Convert world tile to screen position
        float screenX = offsetX + (worldTile.X - visibleWorld.X) * scale;
        float screenY = offsetY + (worldTile.Y - visibleWorld.Y) * scale;

        return new Vector2(screenX, screenY);
    }

    /// <summary>
    /// Update hover info based on mouse position.
    /// </summary>
    private void UpdateHoverInfo(Vector2 mousePosition, Rectangle mapBounds)
    {
        // Calculate actual rendered area (same as DrawMap and ScreenToWorldTile)
        Rectangle visibleWorld = GetVisibleWorldBounds();
        float scaleX = (float)mapBounds.Width / visibleWorld.Width;
        float scaleY = (float)mapBounds.Height / visibleWorld.Height;
        float scale = Math.Min(scaleX, scaleY);

        int renderedWidth = (int)(visibleWorld.Width * scale);
        int renderedHeight = (int)(visibleWorld.Height * scale);
        int offsetX = mapBounds.X + (mapBounds.Width - renderedWidth) / 2;
        int offsetY = mapBounds.Y + (mapBounds.Height - renderedHeight) / 2;

        Rectangle actualRenderArea = new Rectangle(offsetX, offsetY, renderedWidth, renderedHeight);

        // Check if mouse is within the actual rendered map area
        if (!actualRenderArea.Contains(mousePosition.ToPoint()))
        {
            HoveredTile = null;
            return;
        }

        Point worldTile = ScreenToWorldTile(mousePosition, mapBounds);
        HoveredTile = worldTile;

        // Get tile info
        if (IsTileExplored(worldTile.X, worldTile.Y))
        {
            var tile = _chunkManager.GetTileAt(worldTile);
            HoveredTileType = tile.Type;
            HoveredBiome = _worldGenerator.GetSurfaceBiome(worldTile.X);
            HoveredLayer = _worldGenerator.Config.GetLayerAt(worldTile.Y);

            int surfaceY = _worldGenerator.GetSurfaceHeight(worldTile.X);
            HoveredDepth = worldTile.Y - surfaceY;
        }
        else
        {
            HoveredTileType = TileType.Air;
            HoveredBiome = BiomeType.Forest;
            HoveredLayer = WorldLayer.Surface;
            HoveredDepth = 0;
        }
    }

    /// <summary>
    /// Get the visible world bounds for rendering.
    /// </summary>
    public Rectangle GetVisibleWorldBounds()
    {
        float visibleWidth = WorldWidth / Zoom;
        float visibleHeight = WorldHeight / Zoom;

        float centerX = WorldWidth / 2f + PanOffset.X;
        float centerY = WorldHeight / 2f + PanOffset.Y;

        return new Rectangle(
            (int)(centerX - visibleWidth / 2),
            (int)(centerY - visibleHeight / 2),
            (int)visibleWidth,
            (int)visibleHeight
        );
    }

    /// <summary>
    /// Get total explored tile count.
    /// </summary>
    public int ExploredChunkCount => _exploredChunks.Count;

    /// <summary>
    /// Get exploration percentage.
    /// </summary>
    public float ExplorationPercent
    {
        get
        {
            int totalChunks = (WorldWidth / Chunk.SIZE) * (WorldHeight / Chunk.SIZE);
            return (float)_exploredChunks.Count / totalChunks * 100f;
        }
    }

    /// <summary>
    /// Mark all chunks as explored (debug/cheat).
    /// </summary>
    public void RevealAll()
    {
        int chunksX = WorldWidth / Chunk.SIZE;
        int chunksY = WorldHeight / Chunk.SIZE;

        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                _exploredChunks.Add(new Point(x, y));
            }
        }
    }

    /// <summary>
    /// Clear exploration data.
    /// </summary>
    public void ClearExploration()
    {
        _exploredChunks.Clear();
    }
}