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
    public bool OnGround { get; set; }
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

        // Don't reset OnGround here - we'll check it properly
        bool wasOnGround = OnGround;
        OnGround = false;

        // Move horizontally first, then vertically (avoids corner issues)
        MoveHorizontal(Velocity.X * deltaTime, chunks);
        MoveVertical(Velocity.Y * deltaTime, chunks);

        // Always check ground state, even if we didn't move vertically
        // This prevents OnGround from flickering when standing still
        if (!OnGround && Velocity.Y >= 0)
        {
            CheckGroundBelow(chunks);
        }
    }

    /// <summary>
    /// Move horizontally with collision.
    /// </summary>
    protected void MoveHorizontal(float amount, World.ChunkManager chunks)
    {
        if (MathF.Abs(amount) < 0.001f) return;

        float sign = MathF.Sign(amount);
        float remaining = MathF.Abs(amount);

        while (remaining > 0.001f)
        {
            float step = MathF.Min(remaining, 1f);
            Vector2 newPos = new(Position.X + step * sign, Position.Y);

            if (!WouldCollide(newPos, chunks))
            {
                Position = newPos;
                remaining -= step;
            }
            else
            {
                // Hit something - snap to tile edge
                if (sign > 0)
                {
                    // Moving right - snap to left edge of tile
                    int tileX = (int)MathF.Floor((Position.X + Width + step * sign) / World.WorldCoordinates.TILE_SIZE);
                    float snapX = tileX * World.WorldCoordinates.TILE_SIZE - Width;
                    if (snapX > Position.X)
                        Position = new Vector2(snapX, Position.Y);
                    CollidingRight = true;
                }
                else
                {
                    // Moving left - snap to right edge of tile
                    int tileX = (int)MathF.Floor((Position.X + step * sign) / World.WorldCoordinates.TILE_SIZE);
                    float snapX = (tileX + 1) * World.WorldCoordinates.TILE_SIZE;
                    if (snapX < Position.X)
                        Position = new Vector2(snapX, Position.Y);
                    CollidingLeft = true;
                }

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
        if (MathF.Abs(amount) < 0.001f) return;

        float sign = MathF.Sign(amount);
        float remaining = MathF.Abs(amount);

        while (remaining > 0.001f)
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
                // Hit something - snap to tile edge
                if (sign > 0)
                {
                    // Moving down - snap to top of tile (land on ground)
                    int tileY = (int)MathF.Floor((Position.Y + Height + step * sign) / World.WorldCoordinates.TILE_SIZE);
                    float snapY = tileY * World.WorldCoordinates.TILE_SIZE - Height;
                    if (snapY > Position.Y)
                        Position = new Vector2(Position.X, snapY);
                    OnGround = true;
                }
                else
                {
                    // Moving up - snap to bottom of tile (hit ceiling)
                    int tileY = (int)MathF.Floor((Position.Y + step * sign) / World.WorldCoordinates.TILE_SIZE);
                    float snapY = (tileY + 1) * World.WorldCoordinates.TILE_SIZE;
                    if (snapY < Position.Y)
                        Position = new Vector2(Position.X, snapY);
                    CollidingAbove = true;
                }

                Velocity = new Vector2(Velocity.X, 0);
                break;
            }
        }
    }

    /// <summary>
    /// Check if there's ground directly below (for standing still detection).
    /// </summary>
    protected void CheckGroundBelow(World.ChunkManager chunks)
    {
        // Check 1-2 pixels below feet
        Vector2 checkPos = new(Position.X, Position.Y + 1);
        if (WouldCollide(checkPos, chunks))
        {
            OnGround = true;

            // Snap to ground if we're floating slightly
            int tileY = (int)MathF.Floor((Position.Y + Height + 1) / World.WorldCoordinates.TILE_SIZE);
            float groundY = tileY * World.WorldCoordinates.TILE_SIZE - Height;
            if (groundY >= Position.Y && groundY < Position.Y + 2)
            {
                Position = new Vector2(Position.X, groundY);
            }
        }
    }

    /// <summary>
    /// Check if the entity would collide at a given position.
    /// </summary>
    protected bool WouldCollide(Vector2 position, World.ChunkManager chunks)
    {
        // Use Floor to handle negative coordinates correctly
        int left = (int)MathF.Floor(position.X / World.WorldCoordinates.TILE_SIZE);
        int right = (int)MathF.Floor((position.X + Width - 0.001f) / World.WorldCoordinates.TILE_SIZE);
        int top = (int)MathF.Floor(position.Y / World.WorldCoordinates.TILE_SIZE);
        int bottom = (int)MathF.Floor((position.Y + Height - 0.001f) / World.WorldCoordinates.TILE_SIZE);

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