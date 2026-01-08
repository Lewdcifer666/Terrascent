using Microsoft.Xna.Framework;

namespace Terrascent.Entities.Drops;

/// <summary>
/// Type of drop that enemies can spawn.
/// </summary>
public enum DropType
{
    Gold,
    XPGem,
    HealthOrb,
    ManaOrb
}

/// <summary>
/// A collectible drop in the world (gold, XP gems, etc.).
/// </summary>
public class Drop : Entity
{
    public DropType Type { get; }
    public int Value { get; }

    // Physics
    private float _age;
    private const float LIFETIME = 30f;  // Despawn after 30 seconds
    private const float PICKUP_DELAY = 0.3f;  // Can't pick up immediately
    private const float MAGNET_RANGE = 80f;
    private const float MAGNET_SPEED = 300f;

    // Bobbing animation
    private float _bobPhase;
    private Vector2 _basePosition;

    public bool CanPickup => _age >= PICKUP_DELAY;
    public bool IsExpired => _age >= LIFETIME;

    public Drop(DropType type, int value, Vector2 position)
    {
        Type = type;
        Value = value;
        Position = position;
        _basePosition = position;

        // Size based on value
        int size = type switch
        {
            DropType.Gold => Math.Clamp(8 + value / 5, 8, 20),
            DropType.XPGem => Math.Clamp(10 + value / 10, 10, 24),
            _ => 12
        };

        Width = size;
        Height = size;

        // Small random velocity for "pop" effect
        Velocity = new Vector2(
            (Random.Shared.NextSingle() - 0.5f) * 100f,
            -150f - Random.Shared.NextSingle() * 50f
        );

        Gravity = 400f;
        MaxFallSpeed = 300f;

        _bobPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
    }

    public override void Update(float deltaTime)
    {
        _age += deltaTime;
        _bobPhase += deltaTime * 3f;

        base.Update(deltaTime);

        // Bob up and down when resting
        if (OnGround)
        {
            Velocity = Vector2.Zero;
        }
    }

    /// <summary>
    /// Update with magnet effect toward player.
    /// </summary>
    public void UpdateWithMagnet(float deltaTime, Vector2 playerPosition)
    {
        Update(deltaTime);

        if (!CanPickup) return;

        float distance = Vector2.Distance(Center, playerPosition);
        if (distance < MAGNET_RANGE)
        {
            // Accelerate toward player
            Vector2 direction = playerPosition - Center;
            direction.Normalize();

            float speedMult = 1f - (distance / MAGNET_RANGE);  // Faster when closer
            Velocity = direction * MAGNET_SPEED * speedMult;
            AffectedByGravity = false;  // Override gravity while being pulled
        }
    }

    /// <summary>
    /// Check if this drop intersects with a target.
    /// </summary>
    public bool IntersectsWith(Rectangle targetHitbox)
    {
        return CanPickup && Hitbox.Intersects(targetHitbox);
    }

    /// <summary>
    /// Get the color for this drop type.
    /// </summary>
    public Color GetColor()
    {
        return Type switch
        {
            DropType.Gold => Color.Gold,
            DropType.XPGem => Color.Cyan,
            DropType.HealthOrb => Color.Red,
            DropType.ManaOrb => Color.Blue,
            _ => Color.White
        };
    }

    /// <summary>
    /// Get render position with bobbing offset.
    /// </summary>
    public Vector2 GetRenderPosition()
    {
        if (OnGround)
        {
            return new Vector2(Position.X, Position.Y + MathF.Sin(_bobPhase) * 2f);
        }
        return Position;
    }
}