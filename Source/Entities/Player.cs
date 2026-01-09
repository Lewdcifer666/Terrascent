using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terrascent.Combat;
using Terrascent.Core;
using Terrascent.Economy;
using Terrascent.Items;
using Terrascent.Items.Effects;
using Terrascent.Progression;

namespace Terrascent.Entities;

/// <summary>
/// The player entity with movement, jumping, health, and input handling.
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

    // === HEALTH SYSTEM ===
    public int MaxHealth { get; private set; } = 100;
    public int CurrentHealth { get; private set; } = 100;
    public bool IsDead { get; private set; }
    public float HealthPercent => (float)CurrentHealth / MaxHealth;

    // Invincibility frames
    private float _iFrameTimer;
    private const float IFRAME_DURATION = 1.0f;  // 1 second of invincibility after hit
    public bool IsInvincible => _iFrameTimer > 0;

    // Knockback
    private Vector2 _knockbackVelocity;
    private float _knockbackTimer;
    private const float KNOCKBACK_DURATION = 0.25f;
    private const float KNOCKBACK_FORCE = 250f;
    public bool IsKnockedBack => _knockbackTimer > 0;

    // Death/Respawn
    private float _deathTimer;
    private const float DEATH_DURATION = 2.0f;  // Time before respawn
    private Point _spawnPoint;  // Where to respawn

    // Visual flash for damage
    private float _damageFlashTimer;
    private const float DAMAGE_FLASH_DURATION = 0.1f;
    public bool IsDamageFlashing => _damageFlashTimer > 0;

    // Health regeneration
    private float _regenTimer;
    private const float REGEN_INTERVAL = 5f;  // Seconds between regen ticks
    private const float REGEN_DELAY = 3f;     // Seconds after damage before regen starts
    private float _timeSinceLastDamage;

    // Inventory
    public Inventory Inventory { get; } = new(40, 10);

    // Weapon management
    public WeaponManager Weapons { get; } = new();

    // Stats system (Risk of Rain style)
    public PlayerStats Stats { get; } = new();

    // Currency (Risk of Rain style economy)
    public Currency Currency { get; } = new();

    // XP and Leveling System (Vampire Survivors style)
    public XPSystem XP { get; } = new();

    // Level-Up System (Vampire Survivors style)
    public LevelUpManager LevelUp { get; }

    // Upgrade Stats (tracks bonuses from upgrades)
    public UpgradeStats UpgradeStats { get; }

    // Events
    public event Action<int, int>? OnHealthChanged;  // current, max
    public event Action? OnDeath;
    public event Action? OnRespawn;
    public event Action<int>? OnDamageTaken;
    public event Action<int>? OnLevelUp;  // new level

    public Player()
    {
        Width = 24;
        Height = 42;
        Gravity = 900f;
        MaxFallSpeed = 500f;

        // Initialize level-up system
        LevelUp = new LevelUpManager();
        UpgradeStats = new UpgradeStats(LevelUp);

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

        // Stackable items for testing
        Inventory.AddItem(ItemType.SoldiersSyringeItem, 3);  // +45% attack speed
        Inventory.AddItem(ItemType.PaulsGoatHoofItem, 2);    // +28% move speed
        Inventory.AddItem(ItemType.CritGlassesItem, 5);      // +50% crit chance

        // Starting gold for testing
        Currency.AddGold(100);

        // Initialize stats
        Stats.Recalculate(Inventory);

        // Set max health from stats
        UpdateMaxHealth();

        // Recalculate stats when inventory changes
        Inventory.OnSlotChanged += _ =>
        {
            Stats.Recalculate(Inventory);
            UpdateMaxHealth();
        };

        // Subscribe to XP level-up events
        XP.OnLevelUp += (newLevel, overflow) =>
        {
            System.Diagnostics.Debug.WriteLine($"Player leveled up to {newLevel}!");
            OnLevelUp?.Invoke(newLevel);

            // Queue level-up choice (Step 3.2)
            LevelUp.QueueLevelUp();
        };

        // Subscribe to upgrade selection
        LevelUp.OnUpgradeSelected += (upgrade, stacks) =>
        {
            System.Diagnostics.Debug.WriteLine($"Applied upgrade: {upgrade.Name} (x{stacks})");
            UpgradeStats.Recalculate();
            ApplyUpgradeStats();
        };
    }

    /// <summary>
    /// Apply upgrade bonuses to player stats.
    /// </summary>
    private void ApplyUpgradeStats()
    {
        // Apply flat health bonus from upgrades
        UpdateMaxHealth();

        // Apply XP multiplier from upgrades
        XP.XPMultiplier = 1f + UpgradeStats.XPGain;

        // Note: Other stats are applied through PlayerStats.Recalculate
        // which is called from the existing inventory change handler
    }

    /// <summary>
    /// Update max health based on stats and upgrades.
    /// </summary>
    private void UpdateMaxHealth()
    {
        int oldMax = MaxHealth;

        // Base health from items
        float baseHealth = Stats.MaxHealth;

        // Add flat health from upgrades
        baseHealth += UpgradeStats.MaxHealth;

        // Apply Glass Cannon penalty if active
        if (UpgradeStats.HasGlassCannon)
        {
            baseHealth *= 0.5f;
        }

        MaxHealth = (int)baseHealth;

        // If max health increased, heal the difference
        if (MaxHealth > oldMax)
        {
            CurrentHealth += MaxHealth - oldMax;
        }

        // Clamp current health
        CurrentHealth = Math.Clamp(CurrentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Handle player input and update state.
    /// </summary>
    public void HandleInput(InputManager input, float deltaTime)
    {
        // Can't move while dead or knocked back
        if (IsDead || IsKnockedBack) return;

        // Horizontal movement
        int moveDir = input.GetHorizontalAxis();

        if (moveDir != 0)
        {
            FacingDirection = moveDir;
        }

        // Calculate target velocity using stats
        float currentMoveSpeed = Stats.MoveSpeed > 0 ? Stats.MoveSpeed : MoveSpeed;
        float targetVelX = moveDir * currentMoveSpeed;

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
    /// Update player state (call every frame).
    /// </summary>
    public override void Update(float deltaTime)
    {
        // Update timers
        if (_iFrameTimer > 0) _iFrameTimer -= deltaTime;
        if (_damageFlashTimer > 0) _damageFlashTimer -= deltaTime;
        _timeSinceLastDamage += deltaTime;

        // Handle death timer
        if (IsDead)
        {
            _deathTimer -= deltaTime;
            if (_deathTimer <= 0)
            {
                Respawn();
            }
            return;  // Don't update physics while dead
        }

        // Handle knockback
        if (_knockbackTimer > 0)
        {
            _knockbackTimer -= deltaTime;

            // Apply knockback velocity
            Velocity = new Vector2(
                _knockbackVelocity.X * (_knockbackTimer / KNOCKBACK_DURATION),
                Velocity.Y
            );

            // Knockback also applies upward force
            if (_knockbackTimer > KNOCKBACK_DURATION * 0.8f && OnGround)
            {
                Velocity = new Vector2(Velocity.X, -150f);
            }
        }

        // Health regeneration
        if (!IsDead && _timeSinceLastDamage >= REGEN_DELAY && CurrentHealth < MaxHealth)
        {
            _regenTimer += deltaTime;
            if (_regenTimer >= REGEN_INTERVAL)
            {
                _regenTimer = 0f;
                int regenAmount = (int)MathF.Max(1, Stats.HealthRegen);
                Heal(regenAmount);
            }
        }

        base.Update(deltaTime);
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

    /// <summary>
    /// Take damage from an enemy or hazard.
    /// </summary>
    public bool TakeDamage(int damage, Vector2 damageSourcePosition)
    {
        if (IsDead || IsInvincible) return false;

        // Apply armor reduction
        float damageReduction = Stats.Armor / (Stats.Armor + 100f);  // Diminishing returns
        int finalDamage = (int)MathF.Max(1, damage * (1f - damageReduction));

        // Check for dodge
        if (Stats.RollProc(Stats.DodgeChance))
        {
            System.Diagnostics.Debug.WriteLine("DODGED!");
            return false;
        }

        // Check for block
        if (Stats.RollProc(Stats.BlockChance))
        {
            finalDamage = (int)(finalDamage * 0.5f);  // Block reduces damage by 50%
            System.Diagnostics.Debug.WriteLine("BLOCKED! (50% damage)");
        }

        CurrentHealth -= finalDamage;
        _iFrameTimer = IFRAME_DURATION;
        _damageFlashTimer = DAMAGE_FLASH_DURATION;
        _timeSinceLastDamage = 0f;
        _regenTimer = 0f;

        // Calculate knockback direction (away from damage source)
        Vector2 knockbackDir = Center - damageSourcePosition;
        if (knockbackDir.Length() > 0.1f)
        {
            knockbackDir.Normalize();
        }
        else
        {
            knockbackDir = new Vector2(FacingDirection * -1, 0);  // Default: push backward
        }

        // Apply knockback
        _knockbackVelocity = new Vector2(knockbackDir.X * KNOCKBACK_FORCE, -100f);
        _knockbackTimer = KNOCKBACK_DURATION;

        OnDamageTaken?.Invoke(finalDamage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        System.Diagnostics.Debug.WriteLine($"Player took {finalDamage} damage ({CurrentHealth}/{MaxHealth})");

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Heal the player.
    /// </summary>
    public void Heal(int amount)
    {
        if (IsDead) return;

        int oldHealth = CurrentHealth;
        CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);

        if (CurrentHealth != oldHealth)
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }

    /// <summary>
    /// Handle player death.
    /// </summary>
    private void Die()
    {
        IsDead = true;
        _deathTimer = DEATH_DURATION;
        Velocity = Vector2.Zero;

        System.Diagnostics.Debug.WriteLine("PLAYER DIED!");
        OnDeath?.Invoke();
    }

    /// <summary>
    /// Respawn the player at spawn point.
    /// </summary>
    private void Respawn()
    {
        IsDead = false;
        CurrentHealth = MaxHealth;
        _iFrameTimer = IFRAME_DURATION * 2f;  // Extra i-frames after respawn
        _knockbackTimer = 0f;
        _knockbackVelocity = Vector2.Zero;

        // Respawn at spawn point
        Position = new Vector2(
            _spawnPoint.X * World.WorldCoordinates.TILE_SIZE - Width / 2f,
            (_spawnPoint.Y - 3) * World.WorldCoordinates.TILE_SIZE
        );
        Velocity = Vector2.Zero;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnRespawn?.Invoke();

        System.Diagnostics.Debug.WriteLine("Player respawned!");
    }

    /// <summary>
    /// Get the render color (with damage flash and invincibility).
    /// </summary>
    public Color GetRenderColor()
    {
        if (IsDead)
        {
            // Fade out
            float alpha = _deathTimer / DEATH_DURATION;
            return Color.White * alpha;
        }

        if (IsDamageFlashing)
        {
            return Color.Red;
        }

        if (IsInvincible)
        {
            // Flicker effect
            float flicker = MathF.Sin(_iFrameTimer * 30f);
            return flicker > 0 ? Color.White : Color.White * 0.5f;
        }

        return Color.White;
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
        _spawnPoint = new Point(tileX, surfaceY);

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

        // Reset health on spawn
        IsDead = false;
        CurrentHealth = MaxHealth;
        _iFrameTimer = 0f;
        _knockbackTimer = 0f;
    }

    /// <summary>
    /// Set spawn point without teleporting.
    /// </summary>
    public void SetSpawnPoint(int tileX, int tileY)
    {
        _spawnPoint = new Point(tileX, tileY);
    }
}