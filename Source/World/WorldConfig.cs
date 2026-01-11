namespace Terrascent.World;

/// <summary>
/// World size presets matching Terraria's world dimensions.
/// </summary>
public enum WorldSize
{
    Small,   // 4200 x 1200
    Medium,  // 6400 x 1800
    Large    // 8400 x 2400
}

/// <summary>
/// World layer types for depth-based biome detection.
/// </summary>
public enum WorldLayer
{
    Space,       // Above world surface
    Surface,     // Ground level
    Underground, // Just below surface (dirt layer)
    Cavern,      // Deep underground (stone layer)
    Underworld   // Hell layer (bottom ~200 tiles)
}

/// <summary>
/// Configuration class containing all world parameters based on Terraria's system.
/// Provides world size, layer boundaries, and biome placement rules.
/// </summary>
public class WorldConfig
{
    // === World Dimensions ===
    public int Width { get; init; }
    public int Height { get; init; }
    public WorldSize Size { get; init; }

    // === Layer Boundaries (in tiles from top) ===
    /// <summary>Y coordinate where Space ends and Surface begins.</summary>
    public int SpaceBoundary { get; init; }

    /// <summary>Y coordinate of the default surface level (0 depth reference).</summary>
    public int SurfaceLevel { get; init; }

    /// <summary>Y coordinate where Underground ends and Cavern begins.</summary>
    public int CavernBoundary { get; init; }

    /// <summary>Y coordinate where Cavern ends and Underworld begins.</summary>
    public int UnderworldBoundary { get; init; }

    // === Biome Detection (Terraria uses 169x124 rectangle) ===
    /// <summary>Horizontal detection radius (84 tiles each side = 169 wide).</summary>
    public const int BIOME_DETECT_RADIUS_X = 84;

    /// <summary>Vertical detection above player (62 tiles).</summary>
    public const int BIOME_DETECT_ABOVE = 62;

    /// <summary>Vertical detection below player (61 tiles).</summary>
    public const int BIOME_DETECT_BELOW = 61;

    // === Ocean Boundaries ===
    /// <summary>Width of ocean biome at world edges.</summary>
    public int OceanWidth { get; init; }

    // === Biome Placement ===
    /// <summary>True if Jungle is on the left side of the world.</summary>
    public bool JungleOnLeft { get; init; }

    /// <summary>True if world has Crimson (false = Corruption).</summary>
    public bool HasCrimson { get; init; }

    /// <summary>
    /// Create a world configuration for the specified size.
    /// </summary>
    public static WorldConfig Create(WorldSize size, int seed)
    {
        var random = new Random(seed);
        bool jungleOnLeft = random.Next(2) == 0;
        bool hasCrimson = random.Next(2) == 0;

        return size switch
        {
            WorldSize.Small => new WorldConfig
            {
                Size = size,
                Width = 4200,
                Height = 1200,
                SpaceBoundary = 250,           // ~250 tiles for space
                SurfaceLevel = 350,            // Surface at ~350 from top
                CavernBoundary = 650,          // Cavern starts ~300 below surface
                UnderworldBoundary = 1000,     // Underworld is bottom 200 tiles
                OceanWidth = 250,
                JungleOnLeft = jungleOnLeft,
                HasCrimson = hasCrimson
            },
            WorldSize.Medium => new WorldConfig
            {
                Size = size,
                Width = 6400,
                Height = 1800,
                SpaceBoundary = 375,           // ~375 tiles for space
                SurfaceLevel = 500,            // Surface at ~500 from top
                CavernBoundary = 950,          // Cavern starts ~450 below surface
                UnderworldBoundary = 1600,     // Underworld is bottom 200 tiles
                OceanWidth = 380,
                JungleOnLeft = jungleOnLeft,
                HasCrimson = hasCrimson
            },
            WorldSize.Large => new WorldConfig
            {
                Size = size,
                Width = 8400,
                Height = 2400,
                SpaceBoundary = 500,           // ~500 tiles for space
                SurfaceLevel = 650,            // Surface at ~650 from top
                CavernBoundary = 1250,         // Cavern starts ~600 below surface
                UnderworldBoundary = 2200,     // Underworld is bottom 200 tiles
                OceanWidth = 500,
                JungleOnLeft = jungleOnLeft,
                HasCrimson = hasCrimson
            },
            _ => Create(WorldSize.Medium, seed)
        };
    }

    /// <summary>
    /// Get the world layer at a given Y coordinate.
    /// </summary>
    public WorldLayer GetLayerAt(int worldY)
    {
        if (worldY < SpaceBoundary)
            return WorldLayer.Space;
        if (worldY < SurfaceLevel + 50) // Some buffer for terrain variation
            return WorldLayer.Surface;
        if (worldY < CavernBoundary)
            return WorldLayer.Underground;
        if (worldY < UnderworldBoundary)
            return WorldLayer.Cavern;
        return WorldLayer.Underworld;
    }

    /// <summary>
    /// Get depth relative to surface (positive = below surface).
    /// </summary>
    public int GetDepth(int worldY)
    {
        return worldY - SurfaceLevel;
    }

    /// <summary>
    /// Check if a position is in the ocean biome area.
    /// </summary>
    public bool IsOceanArea(int worldX)
    {
        return worldX < OceanWidth || worldX >= Width - OceanWidth;
    }

    /// <summary>
    /// Get the horizontal zone for biome placement.
    /// Returns a value from 0.0 (far left) to 1.0 (far right).
    /// </summary>
    public float GetHorizontalZone(int worldX)
    {
        return (float)worldX / Width;
    }

    /// <summary>
    /// Check if a position is on the Jungle side of the world.
    /// </summary>
    public bool IsJungleSide(int worldX)
    {
        float zone = GetHorizontalZone(worldX);
        return JungleOnLeft ? zone < 0.5f : zone >= 0.5f;
    }

    /// <summary>
    /// Check if a position is on the Snow/Dungeon side of the world.
    /// </summary>
    public bool IsSnowSide(int worldX)
    {
        return !IsJungleSide(worldX);
    }
}

/// <summary>
/// Tile count thresholds for biome detection, matching Terraria's values.
/// </summary>
public static class BiomeTileThresholds
{
    // Major biomes
    public const int Forest = 0;           // Default (no minimum)
    public const int Corruption = 300;     // Evil biome (increased from 200 in v1.4.1)
    public const int Crimson = 300;        // Evil biome
    public const int Hallow = 125;         // Good biome (lower threshold)
    public const int Jungle = 80;          // For background/music change
    public const int Snow = 1500;          // High threshold
    public const int Desert = 1500;        // High threshold (was 1000 sand blocks)
    public const int Mushroom = 100;       // Glowing mushroom (101 functional, 201 background)
    public const int Ocean = 0;            // Position-based, not tile-based

    // Special biomes
    public const int Meteorite = 75;       // Meteorite ore on screen
    public const int Dungeon = 250;        // Dungeon bricks + specific wall
    public const int GraniteCave = 50;     // Mini-biome
    public const int MarbleCave = 50;      // Mini-biome
    public const int SpiderNest = 30;      // Mini-biome
    public const int BeeHive = 20;         // Mini-biome

    // Layer-based (no tile count, uses position)
    public const int Space = 0;            // Height-based
    public const int Underground = 0;      // Depth-based
    public const int Underworld = 50;      // Ash/Hellstone tiles
}