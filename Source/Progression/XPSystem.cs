using Terrascent.Entities;

namespace Terrascent.Progression;

/// <summary>
/// Vampire Survivors-style XP and leveling system.
/// Tracks experience points, current level, and fires events on level-up.
/// </summary>
public class XPSystem
{
    // === Level State ===

    /// <summary>Current player level (starts at 1).</summary>
    public int Level { get; private set; } = 1;

    /// <summary>Current XP towards next level.</summary>
    public int CurrentXP { get; private set; } = 0;

    /// <summary>Total XP earned this run.</summary>
    public int TotalXPEarned { get; private set; } = 0;

    /// <summary>XP required to reach the next level.</summary>
    public int XPToNextLevel => GetXPRequirement(Level + 1);

    /// <summary>Progress to next level (0-1).</summary>
    public float LevelProgress => XPToNextLevel > 0 ? (float)CurrentXP / XPToNextLevel : 0f;

    /// <summary>Maximum level cap.</summary>
    public const int MAX_LEVEL = 100;

    // === XP Multipliers ===

    /// <summary>Global XP multiplier (from items, difficulty, etc.).</summary>
    public float XPMultiplier { get; set; } = 1.0f;

    // === Events ===

    /// <summary>Fired when XP is gained. Args: (amount gained, new total)</summary>
    public event Action<int, int>? OnXPGained;

    /// <summary>Fired when player levels up. Args: (new level, overflow XP)</summary>
    public event Action<int, int>? OnLevelUp;

    /// <summary>Fired when max level is reached.</summary>
    public event Action? OnMaxLevelReached;

    // === Statistics ===

    /// <summary>Number of enemies killed this run.</summary>
    public int EnemiesKilled { get; private set; } = 0;

    // === XP Formula ===

    /// <summary>
    /// Calculate XP required to reach a specific level.
    /// Uses the Vampire Survivors-style scaling formula from documentation:
    /// - Levels 1-20:  5 + (level - 1) * 10  = 5, 15, 25, 35...
    /// - Levels 21-40: 195 + (level - 20) * 13 = 208, 221, 234...
    /// - Levels 41+:   455 + (level - 40) * 16 = 471, 487, 503...
    /// </summary>
    public static int GetXPRequirement(int level)
    {
        if (level <= 1) return 0;  // Level 1 is free

        if (level <= 20)
            return 5 + (level - 1) * 10;

        if (level <= 40)
            return 195 + (level - 20) * 13;

        return 455 + (level - 40) * 16;
    }

    /// <summary>
    /// Calculate total XP required to reach a specific level from level 1.
    /// </summary>
    public static int GetTotalXPToLevel(int targetLevel)
    {
        int total = 0;
        for (int i = 2; i <= targetLevel; i++)
        {
            total += GetXPRequirement(i);
        }
        return total;
    }

    // === XP Gaining ===

    /// <summary>
    /// Add XP from a collected XP gem.
    /// Returns true if a level-up occurred.
    /// </summary>
    public bool AddXP(int baseAmount)
    {
        if (Level >= MAX_LEVEL) return false;

        // Apply multiplier
        int amount = (int)(baseAmount * XPMultiplier);
        if (amount <= 0) return false;

        CurrentXP += amount;
        TotalXPEarned += amount;

        OnXPGained?.Invoke(amount, TotalXPEarned);

        // Check for level-up(s)
        bool leveledUp = false;
        while (CurrentXP >= XPToNextLevel && Level < MAX_LEVEL)
        {
            LevelUp();
            leveledUp = true;
        }

        return leveledUp;
    }

    /// <summary>
    /// Add XP from killing an enemy.
    /// Tracks kill count separately.
    /// </summary>
    public bool AddXPFromKill(int baseXP)
    {
        EnemiesKilled++;
        return AddXP(baseXP);
    }

    /// <summary>
    /// Process a level-up.
    /// </summary>
    private void LevelUp()
    {
        // Calculate overflow XP
        int overflow = CurrentXP - XPToNextLevel;
        CurrentXP = Math.Max(0, overflow);

        Level++;

        System.Diagnostics.Debug.WriteLine($"LEVEL UP! Now level {Level}");

        OnLevelUp?.Invoke(Level, overflow);

        if (Level >= MAX_LEVEL)
        {
            OnMaxLevelReached?.Invoke();
        }
    }

    // === XP Gem Value Calculation ===

    /// <summary>
    /// Calculate XP gem value based on enemy type and player level.
    /// Lower level enemies give less XP as player outlevels them.
    /// </summary>
    public static int CalculateXPFromEnemy(int baseEnemyXP, int enemyTier, int playerLevel)
    {
        // Base XP from enemy data
        float xp = baseEnemyXP;

        // Apply tier bonus (higher tier enemies give more XP)
        xp *= 1f + (enemyTier - 1) * 0.25f;

        // Apply level scaling (enemies become less rewarding as player overlevels)
        int levelDiff = playerLevel - enemyTier * 10;
        if (levelDiff > 0)
        {
            // Reduce XP for trivial enemies (minimum 25% of base)
            float penalty = MathF.Max(0.25f, 1f - levelDiff * 0.05f);
            xp *= penalty;
        }

        return Math.Max(1, (int)xp);
    }

    // === Gem Size Tiers ===

    /// <summary>
    /// Determine XP gem tier based on value.
    /// Larger gems are more valuable and have different visuals.
    /// </summary>
    public static XPGemTier GetGemTier(int xpValue)
    {
        if (xpValue >= 100) return XPGemTier.Huge;
        if (xpValue >= 50) return XPGemTier.Large;
        if (xpValue >= 25) return XPGemTier.Medium;
        if (xpValue >= 10) return XPGemTier.Small;
        return XPGemTier.Tiny;
    }

    /// <summary>
    /// Get the visual size multiplier for a gem tier.
    /// </summary>
    public static float GetGemSizeMultiplier(XPGemTier tier)
    {
        return tier switch
        {
            XPGemTier.Tiny => 0.6f,
            XPGemTier.Small => 0.8f,
            XPGemTier.Medium => 1.0f,
            XPGemTier.Large => 1.3f,
            XPGemTier.Huge => 1.6f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Get the color tint for a gem tier.
    /// </summary>
    public static (byte R, byte G, byte B) GetGemColor(XPGemTier tier)
    {
        return tier switch
        {
            XPGemTier.Tiny => (100, 200, 255),    // Light blue
            XPGemTier.Small => (50, 180, 255),    // Blue
            XPGemTier.Medium => (0, 255, 200),    // Cyan
            XPGemTier.Large => (0, 255, 100),     // Green
            XPGemTier.Huge => (255, 215, 0),      // Gold
            _ => (0, 255, 255)
        };
    }

    // === State Management ===

    /// <summary>
    /// Reset XP system for a new run.
    /// </summary>
    public void Reset()
    {
        Level = 1;
        CurrentXP = 0;
        TotalXPEarned = 0;
        EnemiesKilled = 0;
        XPMultiplier = 1.0f;
    }

    /// <summary>
    /// Force set level (for testing or save loading).
    /// </summary>
    public void SetLevel(int level, int currentXP = 0)
    {
        Level = Math.Clamp(level, 1, MAX_LEVEL);
        CurrentXP = Math.Max(0, currentXP);
    }

    // === Serialization ===

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(Level);
        writer.Write(CurrentXP);
        writer.Write(TotalXPEarned);
        writer.Write(EnemiesKilled);
        writer.Write(XPMultiplier);
    }

    public void LoadFrom(BinaryReader reader)
    {
        Level = reader.ReadInt32();
        CurrentXP = reader.ReadInt32();
        TotalXPEarned = reader.ReadInt32();
        EnemiesKilled = reader.ReadInt32();
        XPMultiplier = reader.ReadSingle();
    }

    // === Debug ===

    public override string ToString()
    {
        return $"Level {Level} ({CurrentXP}/{XPToNextLevel} XP) | Total: {TotalXPEarned} | Kills: {EnemiesKilled}";
    }
}

/// <summary>
/// Visual tier of XP gems based on their value.
/// </summary>
public enum XPGemTier
{
    Tiny,    // 1-9 XP
    Small,   // 10-24 XP
    Medium,  // 25-49 XP
    Large,   // 50-99 XP
    Huge     // 100+ XP
}