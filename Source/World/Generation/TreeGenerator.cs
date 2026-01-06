namespace Terrascent.World.Generation;

/// <summary>
/// Generates trees on the world surface.
/// </summary>
public class TreeGenerator
{
    private readonly PerlinNoise _treeNoise;

    // Tree parameters
    public float TreeDensity { get; set; } = 0.08f;   // Reduced from 0.15
    public int MinTreeHeight { get; set; } = 4;
    public int MaxTreeHeight { get; set; } = 9;
    public int MinTreeSpacing { get; set; } = 4;      // Minimum tiles between trees

    public TreeGenerator(int seed)
    {
        _treeNoise = new PerlinNoise(seed + 5000);
    }

    /// <summary>
    /// Check if a tree should spawn at this X position.
    /// Uses spacing to prevent trees from being too close.
    /// </summary>
    public bool ShouldPlaceTree(int worldX)
    {
        // Enforce minimum spacing by only allowing trees at certain intervals
        // Use hash to create pseudo-random but deterministic spacing
        int spacingHash = HashPosition(worldX / MinTreeSpacing);
        int selectedSlot = spacingHash % MinTreeSpacing;

        // Only one position per spacing interval can have a tree
        if ((worldX % MinTreeSpacing) != selectedSlot)
            return false;

        // Use noise to create natural clustering/gaps in forests
        float noise = _treeNoise.Noise01(worldX * 0.05f, 0);

        // Create tree "zones" - some areas have forests, others are clearings
        // Noise > 0.4 means we're in a potential tree zone
        if (noise < 0.35f)
            return false;

        // Additional random check based on density
        int hash = HashPosition(worldX);
        float roll = (hash % 1000) / 1000f;

        // Scale density by how "forested" this area is
        float localDensity = TreeDensity * ((noise - 0.35f) / 0.65f) * 2f;

        return roll < localDensity;
    }

    /// <summary>
    /// Generate a tree at the given surface position.
    /// </summary>
    public TreeData GenerateTree(int worldX, int surfaceY)
    {
        int hash = HashPosition(worldX);

        // Determine tree height based on position
        int heightRange = MaxTreeHeight - MinTreeHeight;
        int trunkHeight = MinTreeHeight + (hash % (heightRange + 1));

        // Canopy size scales with trunk height
        int canopyRadius = 2 + (trunkHeight / 3);
        int canopyHeight = 2 + (trunkHeight / 2);

        return new TreeData
        {
            TrunkX = worldX,
            TrunkBaseY = surfaceY - 1,  // Start trunk one tile above surface
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