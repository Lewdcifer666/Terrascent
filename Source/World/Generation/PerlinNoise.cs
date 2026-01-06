namespace Terrascent.World.Generation;

/// <summary>
/// Perlin noise generator for procedural terrain.
/// Attempt to produce smooth, natural-looking random values.
/// </summary>
public class PerlinNoise
{
    private readonly int[] _permutation;
    private readonly int[] _p; // Doubled permutation array for overflow handling

    /// <summary>
    /// The seed used to generate this noise.
    /// </summary>
    public int Seed { get; }

    /// <summary>
    /// Create a new Perlin noise generator with the specified seed.
    /// </summary>
    public PerlinNoise(int seed)
    {
        Seed = seed;
        _permutation = new int[256];
        _p = new int[512];

        // Initialize with values 0-255
        for (int i = 0; i < 256; i++)
        {
            _permutation[i] = i;
        }

        // Shuffle using Fisher-Yates with seeded random
        var random = new Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (_permutation[i], _permutation[j]) = (_permutation[j], _permutation[i]);
        }

        // Double the permutation array to avoid overflow handling
        for (int i = 0; i < 512; i++)
        {
            _p[i] = _permutation[i & 255];
        }
    }

    #region Core Perlin Noise (2D)

    /// <summary>
    /// Get raw Perlin noise value at coordinates. Returns value in range [-1, 1].
    /// </summary>
    public float Noise(float x, float y)
    {
        // Find unit grid cell containing point
        int xi = (int)MathF.Floor(x) & 255;
        int yi = (int)MathF.Floor(y) & 255;

        // Get relative position within cell
        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);

        // Compute fade curves for smoothing
        float u = Fade(xf);
        float v = Fade(yf);

        // Hash coordinates of the 4 corners
        int aa = _p[_p[xi] + yi];
        int ab = _p[_p[xi] + yi + 1];
        int ba = _p[_p[xi + 1] + yi];
        int bb = _p[_p[xi + 1] + yi + 1];

        // Blend the gradients
        float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

        return Lerp(x1, x2, v);
    }

    /// <summary>
    /// Get noise value normalized to [0, 1] range.
    /// </summary>
    public float Noise01(float x, float y)
    {
        return (Noise(x, y) + 1f) * 0.5f;
    }

    #endregion

    #region Octave Noise (Fractal Brownian Motion)

    /// <summary>
    /// Get multi-octave noise for more natural-looking terrain.
    /// Returns value approximately in range [-1, 1].
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="octaves">Number of noise layers (4-8 typical)</param>
    /// <param name="persistence">Amplitude multiplier per octave (0.5 typical)</param>
    /// <param name="frequency">Starting frequency/scale (0.01-0.1 typical for terrain)</param>
    /// <param name="lacunarity">Frequency multiplier per octave (2.0 typical)</param>
    public float OctaveNoise(float x, float y, int octaves, float persistence = 0.5f,
                             float frequency = 0.01f, float lacunarity = 2f)
    {
        float total = 0f;
        float amplitude = 1f;
        float maxValue = 0f;
        float freq = frequency;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise(x * freq, y * freq) * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            freq *= lacunarity;
        }

        // Normalize to [-1, 1]
        return total / maxValue;
    }

    /// <summary>
    /// Get multi-octave noise normalized to [0, 1] range.
    /// </summary>
    public float OctaveNoise01(float x, float y, int octaves, float persistence = 0.5f,
                               float frequency = 0.01f, float lacunarity = 2f)
    {
        return (OctaveNoise(x, y, octaves, persistence, frequency, lacunarity) + 1f) * 0.5f;
    }

    #endregion

    #region 1D Noise (useful for terrain height)

    /// <summary>
    /// Get 1D noise (uses 2D noise with y=0).
    /// Good for terrain heightmaps.
    /// </summary>
    public float Noise1D(float x)
    {
        return Noise(x, 0);
    }

    /// <summary>
    /// Get 1D octave noise.
    /// </summary>
    public float OctaveNoise1D(float x, int octaves, float persistence = 0.5f,
                               float frequency = 0.01f, float lacunarity = 2f)
    {
        return OctaveNoise(x, 0, octaves, persistence, frequency, lacunarity);
    }

    #endregion

    #region Helper Functions

    /// <summary>
    /// Fade function for smooth interpolation (6t^5 - 15t^4 + 10t^3).
    /// </summary>
    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    /// <summary>
    /// Linear interpolation.
    /// </summary>
    private static float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    /// <summary>
    /// Gradient function - compute dot product of distance and gradient vectors.
    /// </summary>
    private static float Grad(int hash, float x, float y)
    {
        // Use last 2 bits to select gradient direction
        int h = hash & 3;

        return h switch
        {
            0 => x + y,
            1 => -x + y,
            2 => x - y,
            3 => -x - y,
            _ => 0
        };
    }

    #endregion
}