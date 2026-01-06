using Microsoft.Xna.Framework;

namespace Terrascent.Entities;

/// <summary>
/// Base class for all entities (players, enemies, NPCs, projectiles).
/// Handles position, velocity, collision, and physics.
/// </summary>
public abstract class Entity
{
    // Position and movement
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }

    // Collision box (relative to position, which is top-left)
    public int Width { get; protected set; } = 24;
    public int Height { get; protected set; } = 48;

    // Physics flags
    public bool OnGround { get; protected set; }
    public bool CollidingLeft { get; protected set; }
    public bool CollidingRight { get; protected set; }
    public bool CollidingAbove { get; protected set; }

    // Physics constants (can be overridden per entity)
    public float Gravity { get; protected set; } = 800f;
    public float MaxFallSpeed { get; protected set; } = 600f;
    public bool AffectedByGravity { get; protected set; } = true;

    /// <summary>
    /// Bounding box in world coordinates.
    /// </summary>
    public Rectangle Hitbox => new(
        (int)Position.X,
        (int)Position.Y,
        Width,
        Height
    );

    /// <summary>
    /// Center position of the entity.
    /// </summary>
    public Vector2 Center => new(
        Position.X + Width / 2f,
        Position.Y + Height / 2f
    );

    /// <summary>
    /// Bottom center of the entity (useful for ground checks).
    /// </summary>
    public Vector2 Bottom => new(
        Position.X + Width / 2f,
        Position.Y + Height
    );

    /// <summary>
    /// Called every fixed update tick (60 times/second).
    /// </summary>
    public virtual void Update(float deltaTime)
    {
        // Apply gravity
        if (AffectedByGravity && !OnGround)
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * deltaTime);

            // Cap fall speed
            if (Velocity.Y > MaxFallSpeed)
            {
                Velocity = new Vector2(Velocity.X, MaxFallSpeed);
            }
        }
    }

    /// <summary>
    /// Apply velocity and handle collisions.
    /// Call this after Update() with the chunk manager.
    /// </summary>
    public virtual void ApplyMovement(float deltaTime, World.ChunkManager chunks)
    {
        // Reset collision flags
        CollidingLeft = false;
        CollidingRight = false;
        CollidingAbove = false;
        OnGround = false;

        // Move horizontally first, then vertically (avoids corner issues)
        MoveHorizontal(Velocity.X * deltaTime, chunks);
        MoveVertical(Velocity.Y * deltaTime, chunks);
    }

    /// <summary>
    /// Move horizontally with collision.
    /// </summary>
    protected void MoveHorizontal(float amount, World.ChunkManager chunks)
    {
        if (amount == 0) return;

        float sign = MathF.Sign(amount);
        float remaining = MathF.Abs(amount);

        while (remaining > 0)
        {
            float step = MathF.Min(remaining, 1f); // Move 1 pixel at a time for precision
            Vector2 newPos = new(Position.X + step * sign, Position.Y);

            if (!WouldCollide(newPos, chunks))
            {
                Position = newPos;
                remaining -= step;
            }
            else
            {
                // Hit something
                if (sign > 0) CollidingRight = true;
                else CollidingLeft = true;

                Velocity = new Vector2(0, Velocity.Y);
                break;
            }
        }
    }

    /// <summary>
    /// Move vertically with collision.
    /// </summary>
    protected void MoveVertical(float amount, World.ChunkManager chunks)
    {
        if (amount == 0) return;

        float sign = MathF.Sign(amount);
        float remaining = MathF.Abs(amount);

        while (remaining > 0)
        {
            float step = MathF.Min(remaining, 1f);
            Vector2 newPos = new(Position.X, Position.Y + step * sign);

            if (!WouldCollide(newPos, chunks))
            {
                Position = newPos;
                remaining -= step;
            }
            else
            {
                // Hit something
                if (sign > 0)
                {
                    OnGround = true;
                }
                else
                {
                    CollidingAbove = true;
                }

                Velocity = new Vector2(Velocity.X, 0);
                break;
            }
        }
    }

    /// <summary>
    /// Check if the entity would collide at a given position.
    /// </summary>
    protected bool WouldCollide(Vector2 position, World.ChunkManager chunks)
    {
        // Get the tiles this hitbox overlaps
        int left = (int)position.X / World.WorldCoordinates.TILE_SIZE;
        int right = (int)(position.X + Width - 1) / World.WorldCoordinates.TILE_SIZE;
        int top = (int)position.Y / World.WorldCoordinates.TILE_SIZE;
        int bottom = (int)(position.Y + Height - 1) / World.WorldCoordinates.TILE_SIZE;

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                if (chunks.IsSolidAt(x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }
}