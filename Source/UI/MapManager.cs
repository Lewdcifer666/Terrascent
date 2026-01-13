using Microsoft.Xna.Framework;
using Terrascent.World;
using Terrascent.World.Biomes;
using Terrascent.World.Generation;

namespace Terrascent.UI;

/// <summary>
/// Manages world map state including explored tiles, zoom/pan, and fog of war.
/// Caches tile colors so the map can display explored areas even when chunks are unloaded.
/// </summary>
public class MapManager
{
    private readonly WorldGenerator _worldGenerator;
    private readonly ChunkManager _chunkManager;

    // Explored tiles tracking (for fog of war)
    private readonly HashSet<Point> _exploredChunks = new();
    private const int EXPLORE_RADIUS = 5;  // Chunks around player that get explored (5 * 32 = 160 tiles)

    // Tile color cache - stores packed RGB values for each explored tile
    // Key: chunk position, Value: 32x32 array of packed colors (RGB as int)
    private readonly Dictionary<Point, int[,]> _tileColorCache = new();

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
    /// Mark chunks around the player as explored and cache their tile colors.
    /// Only caches colors for chunks that are currently loaded in memory.
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

                // Mark as explored
                _exploredChunks.Add(chunkPos);

                // Only cache colors if chunk is loaded AND not already cached
                if (!_tileColorCache.ContainsKey(chunkPos))
                {
                    TryCacheChunkColors(chunkPos);
                }
            }
        }
    }

    /// <summary>
    /// Try to cache tile colors for a chunk. Only succeeds if chunk is loaded.
    /// </summary>
    private bool TryCacheChunkColors(Point chunkPos)
    {
        // Check if chunk is loaded in ChunkManager
        var chunk = _chunkManager.GetChunk(chunkPos);
        if (chunk == null)
            return false;  // Chunk not loaded, can't cache yet

        int[,] colors = new int[Chunk.SIZE, Chunk.SIZE];

        int baseX = chunkPos.X * Chunk.SIZE;
        int baseY = chunkPos.Y * Chunk.SIZE;

        for (int lx = 0; lx < Chunk.SIZE; lx++)
        {
            for (int ly = 0; ly < Chunk.SIZE; ly++)
            {
                // Get tile directly from chunk (we know it's loaded)
                var tile = chunk.GetTile(lx, ly);
                Color tileColor = GetTileMapColor(tile.Type);
                colors[lx, ly] = PackColor(tileColor);
            }
        }

        _tileColorCache[chunkPos] = colors;
        return true;
    }

    /// <summary>
    /// Cache tile colors for a chunk from the ChunkManager.
    /// Called when loading from save file.
    /// </summary>
    private void CacheChunkColors(Point chunkPos)
    {
        TryCacheChunkColors(chunkPos);
    }

    /// <summary>
    /// Get cached tile color for a world position.
    /// Returns null if no cached color is available (caller should use live data).
    /// This is used for displaying explored areas where chunks are unloaded.
    /// </summary>
    public Color? GetCachedTileColor(int worldX, int worldY)
    {
        int chunkX = worldX / Chunk.SIZE;
        int chunkY = worldY / Chunk.SIZE;
        Point chunkPos = new Point(chunkX, chunkY);

        // Check if we have cached colors for this chunk
        if (!_tileColorCache.TryGetValue(chunkPos, out var colors))
            return null;

        int localX = worldX - (chunkX * Chunk.SIZE);
        int localY = worldY - (chunkY * Chunk.SIZE);

        if (localX >= 0 && localX < Chunk.SIZE && localY >= 0 && localY < Chunk.SIZE)
        {
            return UnpackColor(colors[localX, localY]);
        }

        return null;
    }

    /// <summary>
    /// Pack a Color into an int for storage.
    /// </summary>
    private static int PackColor(Color c)
    {
        return (c.R << 16) | (c.G << 8) | c.B;
    }

    /// <summary>
    /// Unpack an int back to a Color.
    /// </summary>
    private static Color UnpackColor(int packed)
    {
        return new Color((packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
    }

    /// <summary>
    /// Get map display color for a tile type.
    /// </summary>
    private static Color GetTileMapColor(TileType type)
    {
        return type switch
        {
            TileType.Air => new Color(135, 206, 235),  // Sky blue
            TileType.Dirt => new Color(139, 90, 43),   // Brown
            TileType.Stone => new Color(128, 128, 128), // Gray
            TileType.Grass => new Color(34, 139, 34),  // Forest green
            TileType.Sand => new Color(238, 214, 175), // Sandy
            TileType.Snow => new Color(255, 250, 250), // Snow white
            TileType.Ice => new Color(173, 216, 230),  // Light blue
            TileType.ThinIce => new Color(200, 230, 255), // Lighter blue
            TileType.Mud => new Color(92, 64, 51),     // Dark brown
            TileType.Clay => new Color(188, 143, 143), // Rosy brown
            TileType.Ash => new Color(105, 105, 105),  // Dim gray
            TileType.Hellstone => new Color(255, 69, 0), // Red-orange
            TileType.Obsidian => new Color(30, 30, 30), // Near black
            TileType.JungleGrass => new Color(0, 100, 0), // Dark green
            TileType.Sandstone => new Color(210, 180, 100), // Sandy brown
            TileType.HardenedSand => new Color(194, 178, 128), // Khaki
            TileType.DesertFossil => new Color(180, 160, 120), // Tan
            TileType.CorruptGrass => new Color(75, 0, 130),  // Indigo
            TileType.CrimsonGrass => new Color(139, 0, 0),   // Dark red
            TileType.HallowedGrass => new Color(255, 182, 193), // Light pink
            TileType.Ebonstone => new Color(50, 0, 75),  // Dark purple
            TileType.Crimstone => new Color(100, 0, 0),  // Deep red
            TileType.Pearlstone => new Color(255, 200, 220), // Pink
            TileType.HallowedSand => new Color(255, 230, 200), // Light peach
            TileType.HallowedIce => new Color(200, 180, 255),  // Light purple
            TileType.CorruptSand => new Color(100, 80, 130), // Purple-tan
            TileType.CorruptSandstone => new Color(80, 60, 100), // Dark purple-tan
            TileType.CorruptIce => new Color(100, 80, 150), // Purple ice
            TileType.CrimsonSand => new Color(150, 80, 80), // Red-tan
            TileType.CrimsonSandstone => new Color(120, 60, 60), // Dark red-tan
            TileType.CrimsonIce => new Color(150, 100, 120), // Pink ice
            TileType.Flesh => new Color(139, 69, 69), // Flesh color
            TileType.HallowedSandstone => new Color(230, 200, 180), // Light peachy
            TileType.MushroomGrass => new Color(0, 0, 200), // Blue (glowing mushroom)
            TileType.Wood => new Color(139, 69, 19),    // Saddle brown
            TileType.LivingWood => new Color(120, 80, 30), // Darker wood
            TileType.Leaves => new Color(0, 128, 0),    // Green
            TileType.Cactus => new Color(50, 150, 50),  // Cactus green
            TileType.BorealWood => new Color(180, 160, 140), // Light wood
            TileType.PalmWood => new Color(200, 150, 100), // Palm color
            TileType.RichMahogany => new Color(130, 50, 20), // Dark red wood
            TileType.Ebonwood => new Color(60, 40, 80), // Dark purple wood
            TileType.Shadewood => new Color(100, 40, 40), // Dark red wood
            TileType.Pearlwood => new Color(240, 220, 240), // Light pink wood
            TileType.SnowBrick => new Color(240, 240, 255), // Light blue-white
            TileType.Hive => new Color(200, 150, 50), // Honey yellow
            TileType.HoneyBlock => new Color(255, 200, 50), // Bright honey
            TileType.LivingMahogany => new Color(100, 60, 30), // Mahogany
            TileType.CopperOre => new Color(184, 115, 51), // Copper
            TileType.IronOre => new Color(165, 137, 121),  // Iron gray
            TileType.SilverOre => new Color(192, 192, 192), // Silver
            TileType.GoldOre => new Color(255, 215, 0),    // Gold
            TileType.CobaltOre => new Color(0, 71, 171),   // Cobalt blue
            TileType.MythrilOre => new Color(0, 191, 179), // Teal
            TileType.AdamantiteOre => new Color(220, 20, 60), // Crimson
            TileType.DemoniteOre => new Color(90, 50, 150), // Purple
            TileType.CrimtaneOre => new Color(180, 50, 50), // Dark red
            TileType.Vines => new Color(0, 100, 0), // Dark green
            TileType.JungleVines => new Color(0, 120, 0), // Jungle green
            TileType.CorruptVines => new Color(60, 0, 100), // Purple vines
            TileType.CrimsonVines => new Color(120, 0, 0), // Red vines
            TileType.HallowedVines => new Color(200, 150, 200), // Pink vines
            TileType.StoneBrick => new Color(100, 100, 100), // Gray brick
            TileType.WoodPlatform => new Color(160, 120, 80), // Wood platform
            TileType.Torch => new Color(255, 200, 50), // Torch glow
            TileType.GrayBrick => new Color(90, 90, 90), // Gray
            TileType.RedBrick => new Color(150, 60, 60), // Red brick
            TileType.DungeonBrick => new Color(40, 60, 80), // Dungeon blue
            TileType.CrackedDungeonBrick => new Color(50, 70, 90), // Cracked dungeon
            TileType.LihzahrdBrick => new Color(150, 100, 50), // Temple brown
            TileType.CrystalBlock => new Color(200, 150, 255), // Crystal purple
            TileType.GraniteBlock => new Color(60, 60, 80), // Dark blue-gray
            TileType.MarbleBlock => new Color(220, 220, 230), // Light gray-white
            TileType.Mushroom => new Color(200, 180, 150), // Mushroom tan
            TileType.GlowingMushroom => new Color(50, 50, 200), // Glowing blue
            TileType.Bedrock => new Color(20, 20, 20), // Near black
            _ => new Color(128, 128, 128)  // Default gray
        };
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
    /// Clear exploration data and cached colors.
    /// </summary>
    public void ClearExploration()
    {
        _exploredChunks.Clear();
        _tileColorCache.Clear();
    }

    /// <summary>
    /// Get all explored chunk coordinates for saving.
    /// </summary>
    public IEnumerable<Point> GetExploredChunks()
    {
        return _exploredChunks;
    }

    /// <summary>
    /// Get tile color cache for saving.
    /// Returns dictionary of chunk positions to packed color arrays.
    /// </summary>
    public Dictionary<Point, int[,]> GetTileColorCache()
    {
        return _tileColorCache;
    }

    /// <summary>
    /// Load explored chunks from save data.
    /// </summary>
    public void LoadExploredChunks(IEnumerable<Point> chunks)
    {
        _exploredChunks.Clear();
        foreach (var chunk in chunks)
        {
            _exploredChunks.Add(chunk);
        }
    }

    /// <summary>
    /// Load tile color cache from save data.
    /// </summary>
    public void LoadTileColorCache(Dictionary<Point, int[,]> cache)
    {
        _tileColorCache.Clear();
        foreach (var kvp in cache)
        {
            _tileColorCache[kvp.Key] = kvp.Value;
        }
    }
}