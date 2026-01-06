using Microsoft.Xna.Framework;
using Terrascent.Core;

namespace Terrascent.Entities;

/// <summary>
/// The player entity with movement, jumping, and input handling.
/// </summary>
public class Player : Entity
{
    // Movement parameters
    public float MoveSpeed { get; set; } = 180f;
    public float JumpForce { get; set; } = 350f;
    public float Acceleration { get; set; } = 1200f;
    public float Friction { get; set; } = 800f;
    public float AirControl { get; set; } = 0.6f;  // Reduced control in air

    // Jump buffering and coyote time
    private float _jumpBufferTime = 0f;
    private float _coyoteTime = 0f;
    private const float JUMP_BUFFER_DURATION = 0.1f;  // 100ms buffer
    private const float COYOTE_DURATION = 0.1f;       // 100ms coyote time

    // State
    public bool IsJumping { get; private set; }
    public int FacingDirection { get; private set; } = 1;  // 1 = right, -1 = left

    public Player()
    {
        Width = 24;
        Height = 42;
        Gravity = 900f;
        MaxFallSpeed = 500f;
    }

    /// <summary>
    /// Handle player input and update state.
    /// </summary>
    public void HandleInput(InputManager input, float deltaTime)
    {
        // Horizontal movement
        int moveDir = input.GetHorizontalAxis();

        if (moveDir != 0)
        {
            FacingDirection = moveDir;
        }

        // Calculate target velocity
        float targetVelX = moveDir * MoveSpeed;

        // Apply acceleration/friction based on ground state
        float accel = OnGround ? Acceleration : Acceleration * AirControl;
        float fric = OnGround ? Friction : Friction * AirControl * 0.5f;

        if (moveDir != 0)
        {
            // Accelerate toward target
            if (Velocity.X < targetVelX)
            {
                Velocity = new Vector2(
                    MathF.Min(Velocity.X + accel * deltaTime, targetVelX),
                    Velocity.Y
                );
            }
            else if (Velocity.X > targetVelX)
            {
                Velocity = new Vector2(
                    MathF.Max(Velocity.X - accel * deltaTime, targetVelX),
                    Velocity.Y
                );
            }
        }
        else
        {
            // Apply friction when not pressing movement
            if (Velocity.X > 0)
            {
                Velocity = new Vector2(
                    MathF.Max(0, Velocity.X - fric * deltaTime),
                    Velocity.Y
                );
            }
            else if (Velocity.X < 0)
            {
                Velocity = new Vector2(
                    MathF.Min(0, Velocity.X + fric * deltaTime),
                    Velocity.Y
                );
            }
        }

        // Update coyote time
        if (OnGround)
        {
            _coyoteTime = COYOTE_DURATION;
            IsJumping = false;
        }
        else
        {
            _coyoteTime -= deltaTime;
        }

        // Jump input buffering
        if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Space) ||
            input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.W))
        {
            _jumpBufferTime = JUMP_BUFFER_DURATION;
        }
        else
        {
            _jumpBufferTime -= deltaTime;
        }

        // Execute jump if buffered and can jump
        if (_jumpBufferTime > 0 && _coyoteTime > 0 && !IsJumping)
        {
            Jump();
            _jumpBufferTime = 0;
            _coyoteTime = 0;
        }

        // Variable jump height - release early for shorter jump
        if (IsJumping && Velocity.Y < 0 &&
            !input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space) &&
            !input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
        {
            // Cut jump short
            Velocity = new Vector2(Velocity.X, Velocity.Y * 0.5f);
            IsJumping = false;
        }
    }

    /// <summary>
    /// Execute a jump.
    /// </summary>
    public void Jump()
    {
        Velocity = new Vector2(Velocity.X, -JumpForce);
        IsJumping = true;
        OnGround = false;
    }

    /// <summary>
    /// Spawn the player at a safe position above the given surface.
    /// </summary>
    public void SpawnAt(int tileX, int surfaceY)
    {
        Position = new Vector2(
            tileX * World.WorldCoordinates.TILE_SIZE - Width / 2f,
            (surfaceY - 3) * World.WorldCoordinates.TILE_SIZE - Height
        );
        Velocity = Vector2.Zero;
    }
}