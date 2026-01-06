namespace Terrascent.Core;

/// <summary>
/// Manages fixed timestep game loop with accumulator pattern.
/// Ensures deterministic physics regardless of frame rate.
/// </summary>
public class GameLoop
{
    public const float TICK_RATE = 60f;
    public const float TICK_DURATION = 1f / TICK_RATE;

    private float _accumulator;

    /// <summary>
    /// Current interpolation alpha for rendering between physics states.
    /// Value between 0 and 1 representing progress to next tick.
    /// </summary>
    public float Alpha { get; private set; }

    /// <summary>
    /// Total number of fixed updates that have occurred.
    /// </summary>
    public ulong TickCount { get; private set; }

    /// <summary>
    /// Total elapsed game time in seconds.
    /// </summary>
    public float TotalTime => TickCount * TICK_DURATION;

    /// <summary>
    /// Processes the game loop, calling fixedUpdate for each tick needed.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame</param>
    /// <param name="fixedUpdate">Action to call for each fixed timestep</param>
    /// <returns>Number of fixed updates performed this frame</returns>
    public int Update(float deltaTime, Action fixedUpdate)
    {
        int updates = 0;

        // Cap delta time to prevent spiral of death
        // (if game lags, don't try to catch up infinitely)
        deltaTime = Math.Min(deltaTime, TICK_DURATION * 5);

        _accumulator += deltaTime;

        while (_accumulator >= TICK_DURATION)
        {
            fixedUpdate();
            _accumulator -= TICK_DURATION;
            TickCount++;
            updates++;
        }

        // Alpha for interpolation (0 = at last tick, 1 = at next tick)
        Alpha = _accumulator / TICK_DURATION;

        return updates;
    }
}