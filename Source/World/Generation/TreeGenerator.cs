namespace Terrascent.World.Generation;

/// <summary>
/// Generates trees on the world surface.
/// </summary>
public class TreeGenerator
{
    private readonly PerlinNoise _treeNoise;

    // Tree parameters - tuned for ~2-3 trees per chunk
    public float TreeDensity { get; set; } = 0.25f;   // Increased significantly
    public int MinTreeHeight { get; set; } = 4;
    public int MaxTreeHeight { get; set; } = 12;
    public int MinTreeSpacing { get; set; } = 6;      // ~4 trees max per 32-tile chunk

    public TreeGenerator(int seed)
    {
        _treeNoise = new PerlinNoise(seed + 5000);
    }

    /// <summary>
    /// Check if a tree should spawn at this X position.
    /// </summary>
    public bool ShouldPlaceTree(int worldX)
    {
        // Enforce minimum spacing - only check at spacing intervals
        int slot = ((worldX % MinTreeSpacing) + MinTreeSpacing) % MinTreeSpacing;
        int hash = HashPosition(worldX / MinTreeSpacing);
        int selectedSlot = hash % MinTreeSpacing;

        if (slot != selectedSlot)
            return false;

        // Use noise for natural variation (forests vs clearings)
        float noise = _treeNoise.Noise01(worldX * 0.03f, 0);

        // Always allow some trees, but more in "forest" areas
        float localDensity = TreeDensity * (0.5f + noise);

        // Final random check
        int rollHash = HashPosition(worldX * 7);
        float roll = (rollHash % 1000) / 1000f;

        return roll < localDensity;
    }

    /// <summary>
    /// Generate a tree at the given surface position.
    /// </summary>
    public TreeData GenerateTree(int worldX, int surfaceY)
    {
        int hash = HashPosition(worldX);

        int heightRange = MaxTreeHeight - MinTreeHeight;
        int trunkHeight = MinTreeHeight + (hash % (heightRange + 1));

        // Canopy proportional to trunk
        int canopyRadius = 2 + (trunkHeight / 4);
        int canopyHeight = 2 + (trunkHeight / 3);

        return new TreeData
        {
            TrunkX = worldX,
            TrunkBaseY = surfaceY - 1,
            TrunkHeight = trunkHeight,
            CanopyRadius = canopyRadius,
            CanopyHeight = canopyHeight
        };
    }

    private static int HashPosition(int x)
    {
        int hash = x * 374761393;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        return Math.Abs(hash);
    }
}

/// <summary>
/// Data for a single tree.
/// </summary>
public struct TreeData
{
    public int TrunkX;
    public int TrunkBaseY;
    public int TrunkHeight;
    public int CanopyRadius;
    public int CanopyHeight;
}