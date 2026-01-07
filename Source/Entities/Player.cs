using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terrascent.Core;
using Terrascent.Items;
using Terrascent.Combat;

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
    private const float JUMP_BUFFER_DURATION = 0.2f;
    private const float COYOTE_DURATION = 0.15f;

    // State
    public bool IsJumping { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    // Ground state tracking
    private int _groundedFrames = 0;
    private const int GROUND_STICKY_FRAMES = 3;

    // Inventory
    public Inventory Inventory { get; } = new(40, 10);

    // Weapon management
    public WeaponManager Weapons { get; } = new();

    public Player()
    {
        Width = 24;
        Height = 42;
        Gravity = 900f;
        MaxFallSpeed = 500f;

        // Give starting items for testing
        Inventory.AddItem(ItemType.Dirt, 50);
        Inventory.AddItem(ItemType.Stone, 50);
        Inventory.AddItem(ItemType.Wood, 30);
        Inventory.AddItem(ItemType.Torch, 20);
        Inventory.AddItem(ItemType.WoodPickaxe, 1);

        // Starting weapons for testing
        Inventory.AddItem(ItemType.WoodSword, 1);
        Inventory.AddItem(ItemType.WoodSpear, 1);
        Inventory.AddItem(ItemType.WoodBow, 1);
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
        bool effectivelyGrounded = OnGround || _groundedFrames > 0;
        float accel = effectivelyGrounded ? Acceleration : Acceleration * AirControl;
        float fric = effectivelyGrounded ? Friction : Friction * AirControl * 0.5f;

        if (moveDir != 0)
        {
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

        // Track grounded state with sticky frames
        if (OnGround)
        {
            _groundedFrames = GROUND_STICKY_FRAMES;
            _coyoteTime = COYOTE_DURATION;
            IsJumping = false;
        }
        else
        {
            if (_groundedFrames > 0)
                _groundedFrames--;
            _coyoteTime -= deltaTime;
        }

        // Jump input detection
        bool jumpPressed = input.IsKeyPressed(Keys.Space) ||
                          input.IsKeyPressed(Keys.W) ||
                          input.IsKeyPressed(Keys.Up);

        bool jumpHeld = input.IsKeyDown(Keys.Space) ||
                       input.IsKeyDown(Keys.W) ||
                       input.IsKeyDown(Keys.Up);

        if (jumpPressed)
        {
            _jumpBufferTime = JUMP_BUFFER_DURATION;
        }
        else
        {
            _jumpBufferTime -= deltaTime;
        }

        // Jump conditions
        bool canJump = OnGround || _coyoteTime > 0 || _groundedFrames > 0;
        bool notCurrentlyJumping = Velocity.Y >= -10f || !IsJumping;
        bool shouldJump = _jumpBufferTime > 0 && canJump && notCurrentlyJumping;

        if (shouldJump)
        {
            ExecuteJump();
        }

        // Variable jump height
        if (IsJumping && Velocity.Y < -50f && !jumpHeld)
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y * 0.4f);
            IsJumping = false;
        }

        // Hotbar selection with number keys
        if (input.IsKeyPressed(Keys.D1)) Inventory.SelectSlot(0);
        if (input.IsKeyPressed(Keys.D2)) Inventory.SelectSlot(1);
        if (input.IsKeyPressed(Keys.D3)) Inventory.SelectSlot(2);
        if (input.IsKeyPressed(Keys.D4)) Inventory.SelectSlot(3);
        if (input.IsKeyPressed(Keys.D5)) Inventory.SelectSlot(4);
        if (input.IsKeyPressed(Keys.D6)) Inventory.SelectSlot(5);
        if (input.IsKeyPressed(Keys.D7)) Inventory.SelectSlot(6);
        if (input.IsKeyPressed(Keys.D8)) Inventory.SelectSlot(7);
        if (input.IsKeyPressed(Keys.D9)) Inventory.SelectSlot(8);
        if (input.IsKeyPressed(Keys.D0)) Inventory.SelectSlot(9);
    }

    /// <summary>
    /// Update equipped weapon based on selected hotbar item.
    /// </summary>
    public void UpdateEquippedWeapon()
    {
        var selectedStack = Inventory.SelectedItem;

        if (selectedStack.IsEmpty)
        {
            Weapons.Unequip();
            return;
        }

        // Check if selected item is a weapon
        if (WeaponRegistry.IsWeapon(selectedStack.Type))
        {
            // Only re-equip if different weapon
            if (Weapons.EquippedType != selectedStack.Type)
            {
                Weapons.Equip(selectedStack.Type);
            }
        }
        else
        {
            Weapons.Unequip();
        }
    }

    private void ExecuteJump()
    {
        Velocity = new Vector2(Velocity.X, -JumpForce);
        IsJumping = true;
        OnGround = false;
        _groundedFrames = 0;
        _coyoteTime = 0;
        _jumpBufferTime = 0;
    }

    public void Jump()
    {
        ExecuteJump();
    }

    public void SpawnAt(int tileX, int surfaceY)
    {
        float spawnY = (surfaceY - 3) * World.WorldCoordinates.TILE_SIZE;

        Position = new Vector2(
            tileX * World.WorldCoordinates.TILE_SIZE - Width / 2f,
            spawnY
        );
        Velocity = Vector2.Zero;
        OnGround = false;
        IsJumping = false;
        _groundedFrames = 0;
        _coyoteTime = 0;
        _jumpBufferTime = 0;
    }
}