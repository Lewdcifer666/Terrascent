namespace Terrascent.World.Generation;

/// <summary>
/// Adds surface decorations like grass tufts, flowers, and small plants.
/// </summary>
public class SurfaceDecorator
{
    private readonly PerlinNoise _decorNoise;

    public float GrassChance { get; set; } = 0.3f;
    public float FlowerChance { get; set; } = 0.05f;

    public SurfaceDecorator(int seed)
    {
        _decorNoise = new PerlinNoise(seed + 6000);
    }

    /// <summary>
    /// Get what decoration (if any) should be placed above a grass tile.
    /// </summary>
    public TileType GetDecoration(int worldX, int surfaceY)
    {
        // Use noise for natural clustering
        float noise = _decorNoise.Noise01(worldX * 0.2f, surfaceY * 0.2f);

        int hash = HashPosition(worldX, surfaceY);
        float roll = (hash % 1000) / 1000f;

        // Flowers are rarer but cluster together
        if (noise > 0.6f && roll < FlowerChance)
        {
            // For now, we don't have flower tiles, so skip
            // return TileType.Flower;
        }

        // Tall grass
        if (roll < GrassChance * noise * 2f)
        {
            // For now, we don't have tall grass tiles
            // return TileType.TallGrass;
        }

        return TileType.Air;
    }

    private static int HashPosition(int x, int y)
    {
        int hash = x * 374761393 + y * 668265263;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        return Math.Abs(hash);
    }
}