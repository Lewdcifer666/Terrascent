using Microsoft.Xna.Framework;
using Terrascent.Progression;

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

    /// <summary>XP gem tier for visual differentiation (only relevant for XPGem type).</summary>
    public XPGemTier GemTier { get; }

    // Physics
    private float _age;
    private const float LIFETIME = 30f;  // Despawn after 30 seconds
    private const float PICKUP_DELAY = 0.3f;  // Can't pick up immediately
    private const float MAGNET_RANGE = 80f;
    private const float MAGNET_SPEED = 300f;

    // XP gems have larger magnet range
    private const float XP_MAGNET_RANGE = 120f;
    private const float XP_MAGNET_SPEED = 400f;

    // Bobbing animation
    private float _bobPhase;
    private Vector2 _basePosition;

    // Glow pulse for XP gems
    private float _glowPhase;

    public bool CanPickup => _age >= PICKUP_DELAY;
    public bool IsExpired => _age >= LIFETIME;

    /// <summary>Get the appropriate magnet range for this drop type.</summary>
    public float MagnetRange => Type == DropType.XPGem ? XP_MAGNET_RANGE : MAGNET_RANGE;

    /// <summary>Get the appropriate magnet speed for this drop type.</summary>
    public float MagnetSpeed => Type == DropType.XPGem ? XP_MAGNET_SPEED : MAGNET_SPEED;

    public Drop(DropType type, int value, Vector2 position)
    {
        Type = type;
        Value = value;
        Position = position;
        _basePosition = position;

        // Calculate XP gem tier
        GemTier = type == DropType.XPGem ? XPSystem.GetGemTier(value) : XPGemTier.Small;

        // Size based on type and value
        int size = type switch
        {
            DropType.Gold => Math.Clamp(8 + value / 5, 8, 20),
            DropType.XPGem => CalculateXPGemSize(value),
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
        _glowPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
    }

    /// <summary>
    /// Calculate XP gem size based on value and tier.
    /// </summary>
    private static int CalculateXPGemSize(int value)
    {
        var tier = XPSystem.GetGemTier(value);
        float baseSize = 12f;
        float multiplier = XPSystem.GetGemSizeMultiplier(tier);
        return (int)(baseSize * multiplier);
    }

    public override void Update(float deltaTime)
    {
        _age += deltaTime;
        _bobPhase += deltaTime * 3f;
        _glowPhase += deltaTime * 4f;  // Glow pulse animation

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
        if (distance < MagnetRange)
        {
            // Accelerate toward player
            Vector2 direction = playerPosition - Center;
            direction.Normalize();

            float speedMult = 1f - (distance / MagnetRange);  // Faster when closer
            Velocity = direction * MagnetSpeed * speedMult;
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
        if (Type == DropType.XPGem)
        {
            var (r, g, b) = XPSystem.GetGemColor(GemTier);
            return new Color(r, g, b);
        }

        return Type switch
        {
            DropType.Gold => Color.Gold,
            DropType.HealthOrb => Color.Red,
            DropType.ManaOrb => Color.Blue,
            _ => Color.White
        };
    }

    /// <summary>
    /// Get the glow intensity for XP gems (0-1).
    /// </summary>
    public float GetGlowIntensity()
    {
        if (Type != DropType.XPGem) return 0f;

        // Pulsing glow effect
        float glow = 0.5f + MathF.Sin(_glowPhase) * 0.3f;

        // Larger gems glow brighter
        glow *= XPSystem.GetGemSizeMultiplier(GemTier);

        return Math.Clamp(glow, 0f, 1f);
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