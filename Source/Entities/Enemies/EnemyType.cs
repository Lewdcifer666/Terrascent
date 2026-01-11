namespace Terrascent.Entities.Enemies;

/// <summary>
/// All enemy types in the game, organized by biome.
/// </summary>
public enum EnemyType
{
    // === FOREST/SURFACE (Default Biome) ===
    Slime,              // Basic bouncing enemy
    Zombie,             // Slow walker, high HP (night)
    Skeleton,           // Fast walker, medium HP (night)
    Bat,                // Flying, erratic movement
    DemonEye,           // Flying eyeball (night)

    // === DESERT BIOME ===
    Antlion,            // Burrows in sand, ambush predator
    Vulture,            // Flying, swoops down
    Mummy,              // Desert zombie variant
    DesertSlime,        // Yellow slime, desert-themed
    Scorpion,           // Fast, venomous ground enemy

    // === SNOW BIOME ===
    IceSlime,           // Blue slime, cold-themed
    ZombieEskimo,       // Snow zombie variant
    IceBat,             // Cold bat variant
    UndeadViking,       // Strong melee enemy
    IceGolem,           // Hardmode, ranged ice attacks

    // === JUNGLE BIOME ===
    JungleSlime,        // Green slime, jungle-themed
    Hornet,             // Flying, ranged stinger
    ManEater,           // Stationary vine plant
    SnatcherPlant,      // Grabs players
    JungleBat,          // Stronger bat variant
    Piranha,            // Water enemy

    // === CORRUPTION BIOME ===
    Eater,              // Small worm segment
    DevourerHead,       // Large burrowing worm
    CorruptSlime,       // Purple corrupted slime
    ShadowZombie,       // Corrupted zombie
    DarkMummy,          // Corrupted mummy (hardmode)
    Corruptor,          // Flying, spits projectiles (hardmode)

    // === CRIMSON BIOME ===
    FaceMonster,        // Fast, aggressive
    BloodCrawler,       // Wall-climbing spider
    CrimsonSlime,       // Red bloody slime
    Crimera,            // Flying brain-like enemy
    BloodZombie,        // Crimson zombie variant
    Ichor,              // Flying, debuff attacks (hardmode)

    // === HALLOW BIOME (Hardmode) ===
    Pixie,              // Fast flying, drops dust
    Unicorn,            // Charging ground enemy
    Gastropod,          // Floating, shoots lasers
    IlluminantBat,      // Hallow bat variant
    IlluminantSlime,    // Glowing hallow slime
    ChaosMage,          // Teleporting caster

    // === UNDERWORLD BIOME ===
    FireImp,            // Ranged fire attacks
    HellBat,            // Fire-resistant bat
    LavaBat,            // Stronger fire bat
    Hellhound,          // Fast charging dog
    BoneSerpent,        // Large flying snake
    VoodooDemon,        // Drops guide voodoo doll

    // === DUNGEON BIOME ===
    AngryBones,         // Fast skeleton variant
    DarkCaster,         // Ranged magic attacks
    CursedSkull,        // Flying, curses player
    DungeonSlime,       // Blue dungeon slime
    SpikeBall,          // Bouncing hazard

    // === MUSHROOM BIOME ===
    SporeBat,           // Mushroom bat variant
    FungiSwarmer,       // Small mushroom enemy
    GiantFungus,        // Large mushroom enemy

    // === UNDERGROUND GENERAL ===
    CaveSpider,         // Fast, low HP, wall climbing
    Worm,               // Burrows through tiles
    Ghost,              // Phases through walls

    // === DANGEROUS/RARE ===
    Demon,              // Ranged attacks
    Golem,              // Slow, very high HP
    Wraith,             // Fast, teleports (hardmode)

    // === SPECIAL ===
    Mimic,              // Disguised as chest
    EliteSlime,         // Larger, tougher slime variant
}

/// <summary>
/// AI behavior states for enemies.
/// </summary>
public enum EnemyAIState
{
    Idle,       // Not doing anything
    Patrol,     // Walking around randomly
    Chase,      // Pursuing the player
    Attack,     // Executing an attack
    Flee,       // Running away (low HP)
    Stunned,    // Temporarily unable to act
    Dead        // Death animation playing
}

/// <summary>
/// Movement patterns for enemies.
/// </summary>
public enum MovementPattern
{
    Walker,     // Ground-based, walks left/right
    Hopper,     // Hops/bounces (like slimes)
    Flyer,      // Ignores gravity, flies freely
    Floater,    // Hovers, slow vertical movement
    Burrower,   // Can move through tiles
    Teleporter, // Blinks to new positions
    Charger,    // Rushes at player
    Stationary  // Doesn't move (plants, turrets)
}