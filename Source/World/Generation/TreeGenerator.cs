namespace Terrascent.World.Generation;

/// <summary>
/// Generates trees on the world surface.
/// </summary>
public class TreeGenerator
{
    private readonly Random _random;
    private readonly PerlinNoise _treeNoise;

    // Tree parameters
    public float TreeDensity { get; set; } = 0.15f;  // Chance per valid surface tile
    public int MinTreeHeight { get; set; } = 5;
    public int MaxTreeHeight { get; set; } = 12;
    public int MinTreeSpacing { get; set; } = 3;     // Minimum tiles between trees

    public TreeGenerator(int seed)
    {
        _random = new Random(seed);
        _treeNoise = new PerlinNoise(seed + 5000);
    }

    /// <summary>
    /// Check if a tree should spawn at this X position.
    /// </summary>
    public bool ShouldPlaceTree(int worldX)
    {
        // Use noise to create natural clustering of trees
        float noise = _treeNoise.Noise01(worldX * 0.1f, 0);

        // Create tree "zones" - some areas have more trees than others
        float density = noise * TreeDensity * 2f;

        // Use position hash for deterministic placement
        int hash = HashPosition(worldX);
        float roll = (hash % 1000) / 1000f;

        return roll < density;
    }

    /// <summary>
    /// Generate a tree at the given surface position.
    /// </summary>
    public TreeData GenerateTree(int worldX, int surfaceY)
    {
        int hash = HashPosition(worldX);

        // Determine tree height based on position
        int heightRange = MaxTreeHeight - MinTreeHeight;
        int trunkHeight = MinTreeHeight + (hash % heightRange);

        // Determine canopy size (roughly proportional to height)
        int canopyRadius = 2 + (trunkHeight / 4);
        int canopyHeight = 3 + (trunkHeight / 3);

        return new TreeData
        {
            TrunkX = worldX,
            TrunkBaseY = surfaceY - 1,  // One tile above surface (surface is grass)
            TrunkHeight = trunkHeight,
            CanopyRadius = canopyRadius,
            CanopyHeight = canopyHeight
        };
    }

    /// <summary>
    /// Place a tree's tiles into the chunk manager.
    /// </summary>
    public void PlaceTree(TreeData tree, ChunkManager chunks)
    {
        // Place trunk (from bottom to top)
        for (int y = 0; y < tree.TrunkHeight; y++)
        {
            int tileY = tree.TrunkBaseY - y;
            chunks.SetTileTypeAt(tree.TrunkX, tileY, TileType.Wood);
        }

        // Place canopy (leaves) - oval/circular shape
        int canopyTopY = tree.TrunkBaseY - tree.TrunkHeight;
        int canopyCenterY = canopyTopY - (tree.CanopyHeight / 2);

        for (int dy = -tree.CanopyHeight; dy <= 0; dy++)
        {
            int y = canopyTopY + dy;

            // Calculate radius at this height (wider in middle, narrower at top/bottom)
            float heightRatio = 1f - MathF.Abs(dy + tree.CanopyHeight / 2f) / (tree.CanopyHeight / 2f + 1);
            int radiusAtHeight = (int)(tree.CanopyRadius * heightRatio) + 1;

            for (int dx = -radiusAtHeight; dx <= radiusAtHeight; dx++)
            {
                int x = tree.TrunkX + dx;

                // Skip if this is the trunk position (except at very top)
                if (dx == 0 && dy > -tree.CanopyHeight + 1)
                    continue;

                // Circular check with some randomness for natural look
                float dist = MathF.Sqrt(dx * dx + (dy * 1.5f) * (dy * 1.5f));
                if (dist <= radiusAtHeight + 0.5f)
                {
                    // Only place leaves in air
                    var existing = chunks.GetTileAt(x, y);
                    if (existing.IsAir)
                    {
                        chunks.SetTileTypeAt(x, y, TileType.Leaves);
                    }
                }
            }
        }
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