namespace Terrascent.Entities.Bosses;

/// <summary>
/// All boss types in the game.
/// Organized by progression tier.
/// </summary>
public enum BossType
{
    None = 0,

    // === PRE-HARDMODE BOSSES (1-99) ===

    /// <summary>
    /// Giant Eye - First boss, tests basic combat skills.
    /// Summoned with: Suspicious Looking Eye (at night)
    /// </summary>
    EyeOfTerror = 1,

    /// <summary>
    /// King Slime - Early boss, can be encountered naturally.
    /// Summoned with: Slime Crown
    /// </summary>
    KingSlime = 2,

    /// <summary>
    /// Brain of the Depths - Underground horror boss.
    /// Summoned with: Bloody Spine (in corruption/crimson)
    /// </summary>
    BrainOfDepths = 3,

    /// <summary>
    /// Skeletal Warlord - Dungeon guardian boss.
    /// Summoned with: Ancient Skull
    /// </summary>
    SkeletalWarlord = 4,

    /// <summary>
    /// Queen Bee - Jungle hive boss.
    /// Summoned with: Abeemination or destroying hive
    /// </summary>
    QueenBee = 5,

    /// <summary>
    /// Wall of Shadows - Pre-Hardmode final boss.
    /// Summoned by: Throwing Guide Voodoo Doll into lava in underworld
    /// Defeating unlocks Hardmode.
    /// </summary>
    WallOfShadows = 6,

    // === HARDMODE BOSSES (100-199) ===

    /// <summary>
    /// Twins - Mechanical eye boss duo.
    /// </summary>
    TheTwins = 100,

    /// <summary>
    /// Destroyer - Mechanical worm boss.
    /// </summary>
    TheDestroyer = 101,

    /// <summary>
    /// Skeletron Prime - Mechanical skeleton boss.
    /// </summary>
    SkeletronPrime = 102,

    /// <summary>
    /// Plantera - Jungle hardmode boss.
    /// </summary>
    Plantera = 103,

    /// <summary>
    /// Golem - Temple boss.
    /// </summary>
    Golem = 104,

    /// <summary>
    /// Moon Lord - Final boss.
    /// </summary>
    MoonLord = 105,
}

/// <summary>
/// Boss AI behavior phases.
/// Most bosses have multiple phases with different attack patterns.
/// </summary>
public enum BossPhase
{
    /// <summary>Phase 1 - Initial phase, basic attacks.</summary>
    Phase1 = 1,

    /// <summary>Phase 2 - Triggered at ~50% health, more aggressive.</summary>
    Phase2 = 2,

    /// <summary>Phase 3 - Final phase, most dangerous attacks.</summary>
    Phase3 = 3,

    /// <summary>Enrage - Boss is enraged (past enrage timer or player fled).</summary>
    Enraged = 10,

    /// <summary>Despawning - Boss is leaving (player fled too far).</summary>
    Despawning = 99,

    /// <summary>Dead - Boss is defeated.</summary>
    Dead = 100,
}

/// <summary>
/// Boss movement patterns.
/// </summary>
public enum BossMovement
{
    /// <summary>Hovers and dashes at player (Eye of Terror).</summary>
    HoverDash,

    /// <summary>Bounces around (King Slime).</summary>
    Bouncing,

    /// <summary>Floats and teleports (Brain of Depths).</summary>
    FloatTeleport,

    /// <summary>Walks and leaps (Skeletal Warlord).</summary>
    WalkLeap,

    /// <summary>Flies in patterns (Queen Bee).</summary>
    FlyPattern,

    /// <summary>Horizontal sweep (Wall of Shadows).</summary>
    HorizontalSweep,

    /// <summary>Worm-like segmented movement (Destroyer).</summary>
    Worm,

    /// <summary>Stationary with teleport (Golem).</summary>
    StationaryTeleport,
}
