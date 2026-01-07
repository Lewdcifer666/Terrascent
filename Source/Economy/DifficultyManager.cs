namespace Terrascent.Economy;

/// <summary>
/// Manages game difficulty that scales over time.
/// Inspired by Risk of Rain's difficulty coefficient.
/// </summary>
public class DifficultyManager
{
    // Difficulty scaling constants
    private const float BASE_DIFFICULTY = 1.0f;
    private const float DIFFICULTY_PER_MINUTE = 0.0506f;  // ~3% per minute
    private const float DIFFICULTY_PER_STAGE = 0.5f;
    private const float PLAYER_SCALING = 0.3f;  // Per additional player (multiplayer)

    /// <summary>
    /// Total elapsed time in seconds since the run started.
    /// </summary>
    public float ElapsedTime { get; private set; }

    /// <summary>
    /// Current stage/level number (starts at 1).
    /// </summary>
    public int CurrentStage { get; private set; } = 1;

    /// <summary>
    /// Number of players (for multiplayer scaling).
    /// </summary>
    public int PlayerCount { get; set; } = 1;

    /// <summary>
    /// Current difficulty coefficient.
    /// Starts at 1.0, increases over time and with stages.
    /// </summary>
    public float Coefficient { get; private set; } = BASE_DIFFICULTY;

    /// <summary>
    /// Difficulty tier name for UI display.
    /// </summary>
    public string DifficultyTier => GetDifficultyTier();

    /// <summary>
    /// Color associated with current difficulty tier.
    /// </summary>
    public (byte R, byte G, byte B) DifficultyColor => GetDifficultyColor();

    /// <summary>
    /// Event fired when difficulty tier changes.
    /// </summary>
    public event Action<string>? OnDifficultyTierChanged;

    private string _lastTier = "";

    /// <summary>
    /// Update difficulty based on elapsed time.
    /// Call this every frame or fixed update.
    /// </summary>
    public void Update(float deltaTime)
    {
        ElapsedTime += deltaTime;
        RecalculateCoefficient();

        // Check for tier change
        string currentTier = DifficultyTier;
        if (currentTier != _lastTier)
        {
            _lastTier = currentTier;
            OnDifficultyTierChanged?.Invoke(currentTier);
            System.Diagnostics.Debug.WriteLine($"Difficulty tier changed to: {currentTier} (Coefficient: {Coefficient:F2})");
        }
    }

    /// <summary>
    /// Advance to the next stage.
    /// </summary>
    public void AdvanceStage()
    {
        CurrentStage++;
        RecalculateCoefficient();
        System.Diagnostics.Debug.WriteLine($"Advanced to stage {CurrentStage}, difficulty: {Coefficient:F2}");
    }

    /// <summary>
    /// Reset difficulty for a new run.
    /// </summary>
    public void Reset()
    {
        ElapsedTime = 0f;
        CurrentStage = 1;
        PlayerCount = 1;
        Coefficient = BASE_DIFFICULTY;
        _lastTier = "";
    }

    /// <summary>
    /// Recalculate the difficulty coefficient.
    /// Formula: base + (time * timeScale) + (stage * stageScale) + (players * playerScale)
    /// </summary>
    private void RecalculateCoefficient()
    {
        float timeMinutes = ElapsedTime / 60f;

        // Time scaling (exponential for late game)
        float timeScaling = timeMinutes * DIFFICULTY_PER_MINUTE;
        if (timeMinutes > 15f)
        {
            // Accelerate after 15 minutes
            timeScaling += (timeMinutes - 15f) * DIFFICULTY_PER_MINUTE * 0.5f;
        }

        // Stage scaling
        float stageScaling = (CurrentStage - 1) * DIFFICULTY_PER_STAGE;

        // Player scaling (for future multiplayer)
        float playerScaling = (PlayerCount - 1) * PLAYER_SCALING;

        Coefficient = BASE_DIFFICULTY + timeScaling + stageScaling + playerScaling;

        // Apply exponential curve for late game feel
        if (Coefficient > 3f)
        {
            Coefficient = 3f + MathF.Pow(Coefficient - 3f, 1.1f);
        }
    }

    /// <summary>
    /// Get enemy health multiplier based on difficulty.
    /// </summary>
    public float GetEnemyHealthMultiplier()
    {
        return MathF.Pow(Coefficient, 0.75f);
    }

    /// <summary>
    /// Get enemy damage multiplier based on difficulty.
    /// </summary>
    public float GetEnemyDamageMultiplier()
    {
        return MathF.Pow(Coefficient, 0.5f);
    }

    /// <summary>
    /// Get gold reward multiplier based on difficulty.
    /// Higher difficulty = more gold from kills.
    /// </summary>
    public float GetGoldMultiplier()
    {
        return 1f + (Coefficient - 1f) * 0.25f;
    }

    /// <summary>
    /// Get XP reward multiplier.
    /// </summary>
    public float GetXPMultiplier()
    {
        return 1f + (Coefficient - 1f) * 0.15f;
    }

    /// <summary>
    /// Get difficulty tier name based on coefficient.
    /// </summary>
    private string GetDifficultyTier()
    {
        return Coefficient switch
        {
            < 1.5f => "Easy",
            < 2.0f => "Medium",
            < 2.5f => "Hard",
            < 3.0f => "Very Hard",
            < 4.0f => "Insane",
            < 5.0f => "Nightmare",
            < 7.0f => "Hell",
            < 10.0f => "HAHAHA",
            _ => "I SEE YOU"
        };
    }

    /// <summary>
    /// Get color for difficulty tier (for UI).
    /// </summary>
    private (byte R, byte G, byte B) GetDifficultyColor()
    {
        return Coefficient switch
        {
            < 1.5f => (100, 200, 100),   // Green
            < 2.0f => (200, 200, 100),   // Yellow
            < 2.5f => (255, 150, 50),    // Orange
            < 3.0f => (255, 100, 100),   // Red
            < 4.0f => (200, 50, 50),     // Dark red
            < 5.0f => (150, 50, 150),    // Purple
            < 7.0f => (100, 50, 150),    // Dark purple
            _ => (50, 50, 50)            // Nearly black
        };
    }

    #region Serialization

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(ElapsedTime);
        writer.Write(CurrentStage);
        writer.Write(PlayerCount);
    }

    public void LoadFrom(BinaryReader reader)
    {
        ElapsedTime = reader.ReadSingle();
        CurrentStage = reader.ReadInt32();
        PlayerCount = reader.ReadInt32();
        RecalculateCoefficient();
    }

    #endregion
}