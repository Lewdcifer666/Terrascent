namespace Terrascent.Items.Effects;

/// <summary>
/// How an item's effect scales with multiple stacks.
/// Inspired by Risk of Rain's stacking mechanics.
/// </summary>
public enum StackType
{
    /// <summary>
    /// Linear: value = base + (stacks - 1) * stackBonus
    /// Example: +15 damage, +10 per stack = 15, 25, 35, 45...
    /// </summary>
    Linear,

    /// <summary>
    /// Hyperbolic: value = 1 - 1/(1 + coefficient * stacks)
    /// Approaches but never reaches 100%. Good for crit chance, dodge, etc.
    /// Example: 25% per stack = 25%, 40%, 50%, 57%, 62%...
    /// </summary>
    Hyperbolic,

    /// <summary>
    /// Exponential: value = base^stacks
    /// Multiplies with each stack. Can get very powerful.
    /// Example: 2x damage per stack = 2x, 4x, 8x, 16x...
    /// </summary>
    Exponential,

    /// <summary>
    /// Flat: Same value regardless of stacks. Used for unique effects.
    /// </summary>
    Flat,

    /// <summary>
    /// Diminishing: Each stack adds less than the previous.
    /// value = base * (1 - (1 - diminishRate)^stacks)
    /// </summary>
    Diminishing,
}