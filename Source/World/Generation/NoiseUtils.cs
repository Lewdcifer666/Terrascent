namespace Terrascent.World.Generation;

/// <summary>
/// Utility functions for working with noise values.
/// </summary>
public static class NoiseUtils
{
    /// <summary>
    /// Remap a value from one range to another.
    /// </summary>
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float t = (value - fromMin) / (fromMax - fromMin);
        return toMin + t * (toMax - toMin);
    }

    /// <summary>
    /// Clamp a value between min and max.
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return MathF.Max(min, MathF.Min(max, value));
    }

    /// <summary>
    /// Clamp to [0, 1] range.
    /// </summary>
    public static float Clamp01(float value)
    {
        return Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Apply a power curve to make terrain more dramatic.
    /// Values > 1 create steeper peaks, values < 1 flatten peaks.
    /// </summary>
    public static float PowerCurve(float value, float power)
    {
        return MathF.Pow(Clamp01(value), power);
    }

    /// <summary>
    /// Create terraced/stepped values (like mesa terrain).
    /// </summary>
    public static float Terrace(float value, int steps)
    {
        return MathF.Floor(value * steps) / steps;
    }

    /// <summary>
    /// Blend between two values based on a mask.
    /// </summary>
    public static float Blend(float a, float b, float mask)
    {
        return a * (1f - mask) + b * mask;
    }

    /// <summary>
    /// Create ridged noise from regular noise (inverts valleys to create ridges).
    /// </summary>
    public static float Ridge(float noiseValue)
    {
        return 1f - MathF.Abs(noiseValue);
    }

    /// <summary>
    /// Apply ridged transformation to octave noise.
    /// </summary>
    public static float RidgedOctaveNoise(PerlinNoise noise, float x, float y, int octaves,
                                          float persistence = 0.5f, float frequency = 0.01f)
    {
        float total = 0f;
        float amplitude = 1f;
        float maxValue = 0f;
        float freq = frequency;

        for (int i = 0; i < octaves; i++)
        {
            float n = noise.Noise(x * freq, y * freq);
            n = 1f - MathF.Abs(n); // Ridge transformation
            n = n * n;              // Square for sharper ridges

            total += n * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            freq *= 2f;
        }

        return total / maxValue;
    }

    /// <summary>
    /// Create billowy/cloud-like noise.
    /// </summary>
    public static float BillowNoise(PerlinNoise noise, float x, float y, int octaves,
                                    float persistence = 0.5f, float frequency = 0.01f)
    {
        float total = 0f;
        float amplitude = 1f;
        float maxValue = 0f;
        float freq = frequency;

        for (int i = 0; i < octaves; i++)
        {
            float n = MathF.Abs(noise.Noise(x * freq, y * freq)); // Absolute value
            total += n * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            freq *= 2f;
        }

        return total / maxValue;
    }
}