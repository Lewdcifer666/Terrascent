namespace Terrascent.Combat;

/// <summary>
/// A weapon instance with Secret of Mana-style leveling (0-8) and charge attacks.
/// Weapons gain XP from use and unlock stronger charge attacks as they level up.
/// </summary>
public class Weapon
{
    // Constants
    public const int MAX_LEVEL = 8;
    public const float BASE_CHARGE_TIME = 1.0f;  // Seconds to reach charge level 1
    public const float CHARGE_LEVEL_TIME = 0.5f; // Additional seconds per charge level

    /// <summary>The weapon's unique identifier (matches ItemType for the weapon item).</summary>
    public Items.ItemType ItemType { get; }

    /// <summary>The category of weapon (Sword, Spear, etc).</summary>
    public WeaponType Type { get; }

    /// <summary>Current weapon level (0-8). Higher = stronger charge attacks.</summary>
    public int Level { get; private set; }

    /// <summary>Current XP towards next level.</summary>
    public int Experience { get; private set; }

    /// <summary>XP required for the next level.</summary>
    public int ExperienceToNextLevel => GetXPRequirement(Level + 1);

    /// <summary>Maximum charge level available (equals weapon level).</summary>
    public int MaxChargeLevel => Level;

    /// <summary>Current charge level being built (0 = not charging).</summary>
    public int CurrentChargeLevel { get; private set; }

    /// <summary>Time spent charging at current level.</summary>
    public float ChargeTime { get; private set; }

    /// <summary>Is the weapon currently being charged?</summary>
    public bool IsCharging { get; private set; }

    /// <summary>Is the charge ready to release at current level?</summary>
    public bool IsChargeReady => CurrentChargeLevel > 0 && ChargeTime >= GetChargeTimeForLevel(CurrentChargeLevel);

    /// <summary>Progress to next charge level (0-1).</summary>
    public float ChargeProgress
    {
        get
        {
            if (CurrentChargeLevel >= MaxChargeLevel) return 1f;
            int nextLevel = CurrentChargeLevel + 1;
            float timeForNext = GetChargeTimeForLevel(nextLevel);
            float timeForCurrent = CurrentChargeLevel > 0 ? GetChargeTimeForLevel(CurrentChargeLevel) : 0f;
            float progressTime = ChargeTime - timeForCurrent;
            float requiredTime = timeForNext - timeForCurrent;
            return Math.Clamp(progressTime / requiredTime, 0f, 1f);
        }
    }

    /// <summary>Base damage from weapon data.</summary>
    public int BaseDamage => WeaponRegistry.Get(ItemType).BaseDamage;

    /// <summary>Base attack range in pixels.</summary>
    public float BaseRange => WeaponRegistry.Get(ItemType).BaseRange;

    public Weapon(Items.ItemType itemType)
    {
        ItemType = itemType;
        var data = WeaponRegistry.Get(itemType);
        Type = data.WeaponType;
        Level = 0;
        Experience = 0;
        CurrentChargeLevel = 0;
        ChargeTime = 0f;
        IsCharging = false;
    }

    #region Charging

    /// <summary>
    /// Update charge state. Call every frame while attack button is held.
    /// </summary>
    /// <param name="deltaTime">Time since last update</param>
    /// <param name="isHoldingAttack">Is the attack button being held?</param>
    public void UpdateCharge(float deltaTime, bool isHoldingAttack)
    {
        if (!isHoldingAttack)
        {
            // Released - reset charging state (attack should be triggered externally)
            IsCharging = false;
            return;
        }

        IsCharging = true;
        ChargeTime += deltaTime;

        // Check if we've reached the next charge level
        int newChargeLevel = CalculateChargeLevel();
        if (newChargeLevel != CurrentChargeLevel)
        {
            CurrentChargeLevel = newChargeLevel;
            // Could trigger a visual/audio cue here
            System.Diagnostics.Debug.WriteLine($"Weapon charge level: {CurrentChargeLevel}");
        }
    }

    /// <summary>
    /// Calculate what charge level we're at based on accumulated time.
    /// </summary>
    private int CalculateChargeLevel()
    {
        if (ChargeTime < BASE_CHARGE_TIME)
            return 0;

        // Each level after 0 requires CHARGE_LEVEL_TIME more seconds
        int level = 1;
        float timeAccumulated = BASE_CHARGE_TIME;

        while (level < MaxChargeLevel)
        {
            float nextLevelTime = timeAccumulated + CHARGE_LEVEL_TIME;
            if (ChargeTime < nextLevelTime)
                break;
            timeAccumulated = nextLevelTime;
            level++;
        }

        return Math.Min(level, MaxChargeLevel);
    }

    /// <summary>
    /// Get total time required to reach a charge level.
    /// </summary>
    public static float GetChargeTimeForLevel(int level)
    {
        if (level <= 0) return 0f;
        if (level == 1) return BASE_CHARGE_TIME;
        return BASE_CHARGE_TIME + (level - 1) * CHARGE_LEVEL_TIME;
    }

    /// <summary>
    /// Release the charge and reset state. Returns the charge level that was released.
    /// </summary>
    public int ReleaseCharge()
    {
        int released = CurrentChargeLevel;
        CurrentChargeLevel = 0;
        ChargeTime = 0f;
        IsCharging = false;
        return released;
    }

    /// <summary>
    /// Cancel charging without attacking.
    /// </summary>
    public void CancelCharge()
    {
        CurrentChargeLevel = 0;
        ChargeTime = 0f;
        IsCharging = false;
    }

    #endregion

    #region Damage Calculation

    /// <summary>
    /// Calculate damage for an attack at a specific charge level.
    /// </summary>
    public int GetDamage(int chargeLevel)
    {
        // Level bonus: +10% damage per weapon level
        float levelBonus = 1f + Level * 0.1f;

        // Charge bonus: +50% damage per charge level
        float chargeBonus = 1f + chargeLevel * 0.5f;

        // Get charge attack multiplier
        var chargeAttack = GetChargeAttack(chargeLevel);
        float attackMultiplier = chargeAttack.DamageMultiplier;

        return (int)(BaseDamage * levelBonus * chargeBonus * attackMultiplier);
    }

    /// <summary>
    /// Get the charge attack data for a specific charge level.
    /// </summary>
    public ChargeAttack GetChargeAttack(int chargeLevel)
    {
        return ChargeAttackRegistry.Get(Type, chargeLevel);
    }

    /// <summary>
    /// Get attack range for a specific charge level.
    /// </summary>
    public float GetRange(int chargeLevel)
    {
        var chargeAttack = GetChargeAttack(chargeLevel);
        return BaseRange * chargeAttack.RangeMultiplier;
    }

    #endregion

    #region Experience & Leveling

    /// <summary>
    /// Add experience to the weapon. Returns true if leveled up.
    /// </summary>
    public bool AddExperience(int xp)
    {
        if (Level >= MAX_LEVEL)
            return false;

        Experience += xp;

        bool leveledUp = false;
        while (Level < MAX_LEVEL && Experience >= ExperienceToNextLevel)
        {
            Experience -= ExperienceToNextLevel;
            Level++;
            leveledUp = true;
            System.Diagnostics.Debug.WriteLine($"Weapon leveled up! {ItemType} is now level {Level}");
        }

        return leveledUp;
    }

    /// <summary>
    /// Get XP required to reach a specific level.
    /// Uses a scaling formula similar to the game's XP system.
    /// </summary>
    public static int GetXPRequirement(int level)
    {
        if (level <= 0) return 0;
        if (level == 1) return 100;
        if (level <= 4) return 100 + (level - 1) * 50;   // 100, 150, 200, 250
        if (level <= 6) return 250 + (level - 4) * 100;  // 350, 450
        return 450 + (level - 6) * 150;                   // 600, 750
    }

    /// <summary>
    /// Get XP gained from killing an enemy based on enemy level.
    /// </summary>
    public static int GetXPFromKill(int enemyLevel)
    {
        return 10 + enemyLevel * 5;
    }

    #endregion

    #region Serialization

    /// <summary>
    /// Save weapon state to binary.
    /// </summary>
    public void SaveTo(BinaryWriter writer)
    {
        writer.Write((ushort)ItemType);
        writer.Write((byte)Level);
        writer.Write(Experience);
    }

    /// <summary>
    /// Load weapon state from binary.
    /// </summary>
    public static Weapon LoadFrom(BinaryReader reader)
    {
        var itemType = (Items.ItemType)reader.ReadUInt16();
        var weapon = new Weapon(itemType);
        weapon.Level = reader.ReadByte();
        weapon.Experience = reader.ReadInt32();
        return weapon;
    }

    #endregion

    public override string ToString()
    {
        var data = WeaponRegistry.Get(ItemType);
        return $"{data.Name} Lv.{Level} ({Experience}/{ExperienceToNextLevel} XP)";
    }
}