namespace Terrascent.Entities.Enemies;

/// <summary>
/// All enemy types in the game.
/// </summary>
public enum EnemyType
{
    // Surface enemies
    Slime,          // Basic bouncing enemy
    Zombie,         // Slow walker, high HP
    Skeleton,       // Fast walker, medium HP
    Bat,            // Flying, erratic movement

    // Underground enemies
    CaveSpider,     // Fast, low HP, wall climbing
    Worm,           // Burrows through tiles
    Ghost,          // Phases through walls

    // Dangerous enemies
    Demon,          // Ranged attacks
    Golem,          // Slow, very high HP, high damage
    Wraith,         // Fast, teleports

    // Special
    Mimic,          // Disguised as chest
    EliteSlime,     // Larger, tougher slime variant
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
    Teleporter  // Blinks to new positions
}