using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
    public float AirControl { get; set; } = 0.6f;

    // Jump buffering and coyote time
    private float _jumpBufferTime = 0f;
    private float _coyoteTime = 0f;
    private const float JUMP_BUFFER_DURATION = 0.15f;  // Increased to 150ms
    private const float COYOTE_DURATION = 0.12f;       // Increased to 120ms

    // State
    public bool IsJumping { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    // Track if we were on ground last frame (for coyote time)
    private bool _wasOnGround;

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

        // Update coyote time - only start counting down when we LEAVE the ground
        if (OnGround)
        {
            _coyoteTime = COYOTE_DURATION;
            IsJumping = false;
        }
        else if (_wasOnGround && !OnGround)
        {
            // Just left the ground - start coyote timer (but don't reset if already in air)
            // _coyoteTime already has its value from being on ground
        }
        else
        {
            // In the air - count down
            _coyoteTime -= deltaTime;
        }

        _wasOnGround = OnGround;

        // Jump input buffering - check for jump press
        bool jumpPressed = input.IsKeyPressed(Keys.Space) || input.IsKeyPressed(Keys.W);

        if (jumpPressed)
        {
            _jumpBufferTime = JUMP_BUFFER_DURATION;
        }
        else
        {
            _jumpBufferTime -= deltaTime;
        }

        // Execute jump if:
        // 1. Jump was pressed recently (buffer > 0)
        // 2. We can jump (on ground or in coyote time)
        // 3. We're not already in an upward jump
        bool canJump = _coyoteTime > 0 || OnGround;
        bool notRising = Velocity.Y >= 0 || !IsJumping;

        if (_jumpBufferTime > 0 && canJump && notRising)
        {
            Jump();
            _jumpBufferTime = 0;
            _coyoteTime = 0;
        }

        // Variable jump height - release early for shorter jump
        bool jumpHeld = input.IsKeyDown(Keys.Space) || input.IsKeyDown(Keys.W);

        if (IsJumping && Velocity.Y < 0 && !jumpHeld)
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
        _wasOnGround = false;
    }

    /// <summary>
    /// Additional ground check - call after ApplyMovement.
    /// </summary>
    public void CheckGroundState(World.ChunkManager chunks)
    {
        // If we think we're not on ground, do an extra check
        // This helps with corner cases
        if (!OnGround && Velocity.Y >= 0)
        {
            // Check 1 pixel below our feet
            Vector2 checkPos = new(Position.X, Position.Y + 1);
            if (WouldCollide(checkPos, chunks))
            {
                OnGround = true;
            }
        }
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
        OnGround = false;
        _wasOnGround = false;
        IsJumping = false;
        _coyoteTime = 0;
        _jumpBufferTime = 0;
    }
}