using Microsoft.Xna.Framework;
using Terrascent.Economy;
using Terrascent.World;

namespace Terrascent.Entities.Bosses;

/// <summary>
/// Base class for all bosses with phase system, enrage timer, and complex AI.
/// Extends the basic Entity class with boss-specific functionality.
/// </summary>
public class Boss : Entity
{
    // === IDENTITY ===
    public BossType BossType { get; }
    public BossData Data { get; }

    // === STATS (scaled by difficulty) ===
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public int Damage { get; private set; }
    public float MoveSpeed { get; private set; }
    public int Defense { get; private set; }

    public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

    // === REWARDS ===
    public int GoldReward { get; private set; }
    public int XPReward { get; private set; }

    // === PHASE SYSTEM ===
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;
    private BossPhaseData _currentPhaseData;
    public BossPhase _previousPhase = BossPhase.Phase1;

    // === ENRAGE SYSTEM ===
    private float _fightTimer;
    public float FightTime => _fightTimer;
    public float EnrageTime => Data.EnrageTime;
    public bool IsEnraged => _fightTimer >= Data.EnrageTime;
    public float TimeToEnrage => MathF.Max(0, Data.EnrageTime - _fightTimer);

    // === AI STATE ===
    private float _attackCooldownTimer;
    private float _currentAttackCooldown;
    private BossAttack? _currentAttack;
    private bool _isAttacking;
    private float _attackTimer;

    private Entity? _target;
    public Vector2 _targetPosition;
    private float _dashVelocityX;
    private float _dashVelocityY;
    private bool _isDashing;
    private float _dashTimer;

    // === MOVEMENT SPECIFIC ===
    private float _hoverTimer;
    private float _bounceTimer;
    public float _teleportCooldown;
    public Vector2 _patternOffset;
    public int _patternIndex;

    // === MINION SPAWNING ===
    private float _minionSpawnTimer;
    private int _currentMinionCount;

    // === DESPAWN ===
    private float _despawnTimer;
    private const float DESPAWN_TIME = 5f;
    public bool ShouldDespawn { get; private set; }

    // === DEATH ===
    public bool IsDead => CurrentPhase == BossPhase.Dead;
    public bool IsFullyDead { get; private set; }
    private float _deathTimer;
    private const float DEATH_DURATION = 2f;  // Longer death animation for bosses

    // === INVINCIBILITY ===
    private float _iFrameTimer;
    private const float IFRAME_DURATION = 0.1f;  // Shorter i-frames for bosses
    public bool IsInvincible => _iFrameTimer > 0 || (_currentPhaseData?.IsInvulnerable ?? false);

    // === VISUAL ===
    public int FacingDirection { get; private set; } = 1;
    private float _damageFlashTimer;
    private float _phaseTransitionTimer;
    private const float PHASE_TRANSITION_DURATION = 1f;

    /// <summary>Is the boss currently flashing from damage?</summary>
    public bool IsDamageFlashing => _damageFlashTimer > 0;

    /// <summary>Is the boss transitioning between phases?</summary>
    public bool IsPhaseTransitioning => _phaseTransitionTimer > 0;

    /// <summary>Is the boss currently performing an attack?</summary>
    public bool IsAttacking => _isAttacking;

    // === RANDOM ===
    private readonly Random _random;

    // === EVENTS ===
    public event Action<Boss, BossPhase>? OnPhaseChanged;
    public event Action<Boss>? OnEnraged;
    public event Action<Boss>? OnDeath;
    public event Action<Boss, BossAttack>? OnAttack;
    public event Action<Boss>? OnMinionSpawn;

    public Boss(BossType type, Vector2 position, DifficultyManager difficulty, int? seed = null)
    {
        BossType = type;
        Data = BossRegistry.Get(type);
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;

        // Set entity size
        Width = Data.Width;
        Height = Data.Height;

        // Bosses typically aren't affected by gravity (they fly/hover)
        AffectedByGravity = Data.Movement == BossMovement.WalkLeap ||
                           Data.Movement == BossMovement.Bouncing;

        Position = position;

        // Scale stats by difficulty
        ApplyDifficultyScaling(difficulty);

        CurrentHealth = MaxHealth;

        // Initialize phase data
        _currentPhaseData = Data.Phases.Length > 0 ? Data.Phases[0] : new BossPhaseData
        {
            Phase = BossPhase.Phase1,
            HealthThreshold = 1f,
            SpeedMultiplier = 1f,
            AttackSpeedMultiplier = 1f,
            DamageMultiplier = 1f,
            Defense = 0
        };

        UpdatePhase();
    }

    private void ApplyDifficultyScaling(DifficultyManager difficulty)
    {
        float healthMult = difficulty.GetEnemyHealthMultiplier();
        float damageMult = difficulty.GetEnemyDamageMultiplier();
        float goldMult = difficulty.GetGoldMultiplier();
        float xpMult = difficulty.GetXPMultiplier();

        MaxHealth = (int)(Data.BaseHealth * healthMult);
        Damage = (int)(Data.BaseDamage * damageMult);
        MoveSpeed = Data.BaseSpeed;
        Defense = Data.BaseDefense;

        GoldReward = (int)(Data.BaseGoldReward * goldMult);
        XPReward = (int)(Data.BaseXPReward * xpMult);
    }

    /// <summary>
    /// Set the target entity (player).
    /// </summary>
    public void SetTarget(Entity target)
    {
        _target = target;
    }

    /// <summary>
    /// Main update loop.
    /// </summary>
    public override void Update(float deltaTime)
    {
        if (IsFullyDead) return;

        // Update timers
        _fightTimer += deltaTime;
        if (_iFrameTimer > 0) _iFrameTimer -= deltaTime;
        if (_damageFlashTimer > 0) _damageFlashTimer -= deltaTime;
        if (_phaseTransitionTimer > 0) _phaseTransitionTimer -= deltaTime;

        // Check for death
        if (IsDead)
        {
            UpdateDeath(deltaTime);
            return;
        }

        // Update despawn check
        UpdateDespawn(deltaTime);
        if (ShouldDespawn) return;

        // Check for enrage
        if (_fightTimer >= Data.EnrageTime && CurrentPhase != BossPhase.Enraged)
        {
            EnterEnrage();
        }

        // Update phase based on health
        UpdatePhase();

        // Skip AI during phase transition
        if (_phaseTransitionTimer > 0) return;

        // Update attack cooldown
        if (_attackCooldownTimer > 0)
        {
            _attackCooldownTimer -= deltaTime;
        }

        // Update current attack
        if (_isAttacking)
        {
            UpdateAttack(deltaTime);
        }
        else
        {
            // Choose and execute attacks
            TryStartAttack();
        }

        // Update movement
        UpdateMovement(deltaTime);

        // Update minion spawning
        if (_currentPhaseData?.SpawnsMinions ?? false)
        {
            UpdateMinionSpawning(deltaTime);
        }

        // Update facing direction based on target
        if (_target != null)
        {
            FacingDirection = _target.Center.X > Center.X ? 1 : -1;
        }

        base.Update(deltaTime);
    }

    /// <summary>
    /// Update phase based on current health.
    /// </summary>
    private void UpdatePhase()
    {
        if (CurrentPhase == BossPhase.Dead || CurrentPhase == BossPhase.Enraged) return;

        BossPhaseData newPhaseData = Data.GetPhaseForHealth(HealthPercent);

        if (newPhaseData.Phase != CurrentPhase)
        {
            _previousPhase = CurrentPhase;
            CurrentPhase = newPhaseData.Phase;
            _currentPhaseData = newPhaseData;

            // Apply phase modifiers
            ApplyPhaseModifiers();

            // Trigger phase transition effects
            _phaseTransitionTimer = PHASE_TRANSITION_DURATION;

            OnPhaseChanged?.Invoke(this, CurrentPhase);
            Console.WriteLine($"[BOSS] {Data.Name} entered {CurrentPhase}!");
        }
    }

    /// <summary>
    /// Apply stat modifiers from current phase.
    /// </summary>
    private void ApplyPhaseModifiers()
    {
        MoveSpeed = Data.BaseSpeed * _currentPhaseData.SpeedMultiplier;
        Defense = (int)(Data.BaseDefense + _currentPhaseData.Defense);

        // Enrage multiplier stacks
        if (IsEnraged)
        {
            MoveSpeed *= Data.EnrageMultiplier;
            Damage = (int)(Data.BaseDamage * _currentPhaseData.DamageMultiplier * Data.EnrageMultiplier);
        }
        else
        {
            Damage = (int)(Data.BaseDamage * _currentPhaseData.DamageMultiplier);
        }
    }

    /// <summary>
    /// Enter enraged state.
    /// </summary>
    private void EnterEnrage()
    {
        CurrentPhase = BossPhase.Enraged;
        ApplyPhaseModifiers();

        OnEnraged?.Invoke(this);
        Console.WriteLine($"[BOSS] {Data.Name} has ENRAGED!");
    }

    /// <summary>
    /// Check for despawn conditions.
    /// </summary>
    private void UpdateDespawn(float deltaTime)
    {
        if (_target == null)
        {
            _despawnTimer += deltaTime;
            if (_despawnTimer >= DESPAWN_TIME)
            {
                ShouldDespawn = true;
                CurrentPhase = BossPhase.Despawning;
            }
            return;
        }

        float distance = Vector2.Distance(Center, _target.Center);

        if (distance > Data.DespawnDistance)
        {
            _despawnTimer += deltaTime;
            if (_despawnTimer >= DESPAWN_TIME)
            {
                ShouldDespawn = true;
                CurrentPhase = BossPhase.Despawning;
                Console.WriteLine($"[BOSS] {Data.Name} has despawned - player fled too far!");
            }
        }
        else
        {
            _despawnTimer = 0f;
        }
    }

    /// <summary>
    /// Try to start a new attack.
    /// </summary>
    private void TryStartAttack()
    {
        if (_attackCooldownTimer > 0 || _target == null) return;

        // Get available attacks for current phase
        var availableAttacks = Data.GetAttacksForPhase(CurrentPhase).ToList();
        if (availableAttacks.Count == 0) return;

        // Check range to target
        float distance = Vector2.Distance(Center, _target.Center);

        // Filter attacks by range
        var inRangeAttacks = availableAttacks.Where(a => distance <= a.Range).ToList();
        if (inRangeAttacks.Count == 0)
        {
            // No attacks in range, just move toward target
            return;
        }

        // Weight attacks (prefer attacks that haven't been used recently)
        _currentAttack = inRangeAttacks[_random.Next(inRangeAttacks.Count)];

        // Start the attack
        _isAttacking = true;
        _attackTimer = 0f;
        _currentAttackCooldown = _currentAttack.Cooldown / _currentPhaseData.AttackSpeedMultiplier;

        // Handle special attack types
        if (_currentAttack.Name.Contains("Dash") || _currentAttack.Name.Contains("Charge"))
        {
            StartDash();
        }
        else if (_currentAttack.Name.Contains("Teleport"))
        {
            StartTeleport();
        }

        OnAttack?.Invoke(this, _currentAttack);
    }

    /// <summary>
    /// Update the current attack.
    /// </summary>
    private void UpdateAttack(float deltaTime)
    {
        _attackTimer += deltaTime;

        // Attack duration (longer for AoE/dash attacks)
        float attackDuration = _currentAttack!.IsAoE || _isDashing ? 0.8f : 0.4f;

        if (_attackTimer >= attackDuration)
        {
            // Attack finished
            _isAttacking = false;
            _attackCooldownTimer = _currentAttackCooldown;
            _isDashing = false;
        }
    }

    /// <summary>
    /// Start a dash attack.
    /// </summary>
    private void StartDash()
    {
        if (_target == null) return;

        _isDashing = true;
        _dashTimer = 0.5f;

        Vector2 direction = _target.Center - Center;
        if (direction.Length() > 0.1f)
        {
            direction.Normalize();
            float dashSpeed = MoveSpeed * 3f;
            _dashVelocityX = direction.X * dashSpeed;
            _dashVelocityY = direction.Y * dashSpeed;
        }
    }

    /// <summary>
    /// Start a teleport attack.
    /// </summary>
    private void StartTeleport()
    {
        if (_target == null) return;

        // Teleport near the target, but ensure we're ABOVE the target's position
        float horizontalOffset = 80f + _random.NextSingle() * 120f;
        float direction = _random.NextSingle() > 0.5f ? 1f : -1f;

        // Position horizontally offset from target
        float newX = _target.Center.X + (direction * horizontalOffset);

        // Position ABOVE the target (never below) to avoid teleporting into ground
        // Use the target's top position minus some height to ensure we're in the air
        float verticalOffset = 50f + _random.NextSingle() * 100f;
        float newY = _target.Position.Y - verticalOffset - Height;

        Position = new Vector2(newX - Width / 2, newY);
        _teleportCooldown = 2f;
    }

    /// <summary>
    /// Update movement based on movement pattern.
    /// </summary>
    private void UpdateMovement(float deltaTime)
    {
        if (_target == null) return;

        // Handle dash movement
        if (_isDashing && _dashTimer > 0)
        {
            _dashTimer -= deltaTime;
            Velocity = new Vector2(_dashVelocityX, _dashVelocityY);
            return;
        }

        // Movement pattern
        switch (Data.Movement)
        {
            case BossMovement.HoverDash:
                UpdateHoverDashMovement(deltaTime);
                break;

            case BossMovement.Bouncing:
                UpdateBouncingMovement(deltaTime);
                break;

            case BossMovement.FloatTeleport:
                UpdateFloatTeleportMovement(deltaTime);
                break;

            case BossMovement.WalkLeap:
                UpdateWalkLeapMovement(deltaTime);
                break;

            case BossMovement.FlyPattern:
                UpdateFlyPatternMovement(deltaTime);
                break;

            case BossMovement.HorizontalSweep:
                UpdateHorizontalSweepMovement(deltaTime);
                break;

            default:
                // Default: move toward target
                MoveTowardTarget(deltaTime);
                break;
        }
    }

    private void UpdateHoverDashMovement(float deltaTime)
    {
        _hoverTimer += deltaTime;

        // Hover with slight bobbing
        float hoverY = MathF.Sin(_hoverTimer * 2f) * 20f;

        // Move toward target position (above player)
        Vector2 targetPos = _target!.Center - new Vector2(0, 100);
        Vector2 toTarget = targetPos - Center;

        if (toTarget.Length() > 50f)
        {
            toTarget.Normalize();
            Velocity = new Vector2(
                toTarget.X * MoveSpeed,
                toTarget.Y * MoveSpeed + hoverY
            );
        }
        else
        {
            Velocity = new Vector2(0, hoverY);
        }
    }

    private void UpdateBouncingMovement(float deltaTime)
    {
        if (!OnGround) return;

        _bounceTimer -= deltaTime;
        if (_bounceTimer <= 0)
        {
            // Jump toward target
            Vector2 toTarget = _target!.Center - Center;
            float direction = toTarget.X > 0 ? 1 : -1;

            float jumpForce = 300f + _random.NextSingle() * 100f;
            float horizontalSpeed = MoveSpeed * (0.5f + _random.NextSingle() * 0.5f);

            Velocity = new Vector2(direction * horizontalSpeed, -jumpForce);
            _bounceTimer = 0.5f + _random.NextSingle() * 1f;
        }
    }

    private void UpdateFloatTeleportMovement(float deltaTime)
    {
        _teleportCooldown -= deltaTime;

        // Float gently
        float floatY = MathF.Sin(_hoverTimer * 1.5f) * 30f;
        _hoverTimer += deltaTime;

        // Gentle drift toward target
        Vector2 toTarget = _target!.Center - Center;
        if (toTarget.Length() > 200f)
        {
            toTarget.Normalize();
            Velocity = new Vector2(toTarget.X * MoveSpeed * 0.3f, floatY);
        }
        else
        {
            Velocity = new Vector2(0, floatY);
        }
    }

    private void UpdateWalkLeapMovement(float deltaTime)
    {
        Vector2 toTarget = _target!.Center - Center;
        int direction = toTarget.X > 0 ? 1 : -1;

        if (OnGround)
        {
            // Walk toward target
            Velocity = new Vector2(direction * MoveSpeed, Velocity.Y);

            // Leap if target is above or at medium distance
            float verticalDiff = Center.Y - _target.Center.Y;
            float horizontalDist = MathF.Abs(toTarget.X);

            if (verticalDiff > 50f || (horizontalDist > 150f && horizontalDist < 400f))
            {
                if (_random.NextSingle() < 0.02f)  // Random chance to leap
                {
                    Velocity = new Vector2(direction * MoveSpeed * 1.5f, -400f);
                }
            }
        }
    }

    private void UpdateFlyPatternMovement(float deltaTime)
    {
        _hoverTimer += deltaTime;

        // Figure-8 or circular pattern around target
        float patternRadius = 200f + MathF.Sin(_hoverTimer * 0.5f) * 100f;
        float patternAngle = _hoverTimer * 1.5f;

        Vector2 patternPos = _target!.Center + new Vector2(
            MathF.Cos(patternAngle) * patternRadius,
            MathF.Sin(patternAngle * 0.5f) * patternRadius * 0.5f - 50f
        );

        Vector2 toPattern = patternPos - Center;
        if (toPattern.Length() > 10f)
        {
            toPattern.Normalize();
            Velocity = toPattern * MoveSpeed;
        }
    }

    private void UpdateHorizontalSweepMovement(float deltaTime)
    {
        // Wall of Shadows style - constant horizontal movement
        float direction = FacingDirection;
        Velocity = new Vector2(direction * MoveSpeed, 0);

        // Stay at player's vertical level
        float verticalDiff = _target!.Center.Y - Center.Y;
        if (MathF.Abs(verticalDiff) > 50f)
        {
            Velocity = new Vector2(Velocity.X, MathF.Sign(verticalDiff) * MoveSpeed * 0.3f);
        }
    }

    private void MoveTowardTarget(float deltaTime)
    {
        Vector2 toTarget = _target!.Center - Center;
        if (toTarget.Length() > 50f)
        {
            toTarget.Normalize();
            Velocity = toTarget * MoveSpeed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }
    }

    /// <summary>
    /// Update minion spawning.
    /// </summary>
    private void UpdateMinionSpawning(float deltaTime)
    {
        if (!Data.MinionType.HasValue) return;
        if (_currentMinionCount >= Data.MaxMinions) return;

        _minionSpawnTimer -= deltaTime;
        if (_minionSpawnTimer <= 0)
        {
            _minionSpawnTimer = Data.MinionSpawnInterval / _currentPhaseData.AttackSpeedMultiplier;
            OnMinionSpawn?.Invoke(this);
            _currentMinionCount++;
        }
    }

    /// <summary>
    /// Notify that a minion was killed.
    /// </summary>
    public void NotifyMinionKilled()
    {
        _currentMinionCount = Math.Max(0, _currentMinionCount - 1);
    }

    /// <summary>
    /// Update death animation.
    /// </summary>
    private void UpdateDeath(float deltaTime)
    {
        _deathTimer += deltaTime;
        if (_deathTimer >= DEATH_DURATION)
        {
            IsFullyDead = true;
        }
    }

    /// <summary>
    /// Take damage from an attack.
    /// </summary>
    public bool TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (IsDead || IsInvincible) return false;

        // Apply defense
        int effectiveDamage = Math.Max(1, damage - Defense);
        CurrentHealth -= effectiveDamage;
        _iFrameTimer = IFRAME_DURATION;
        _damageFlashTimer = 0.1f;

        // Bosses are immune to knockback
        // (but we could add small knockback for very powerful attacks)

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handle boss death.
    /// </summary>
    private void Die()
    {
        CurrentPhase = BossPhase.Dead;
        Velocity = Vector2.Zero;
        OnDeath?.Invoke(this);
        Console.WriteLine($"[BOSS] {Data.Name} has been DEFEATED!");
    }

    /// <summary>
    /// Get the attack hitbox for contact damage.
    /// </summary>
    public Rectangle GetContactHitbox()
    {
        return Hitbox;
    }

    /// <summary>
    /// Get the current attack's AoE hitbox (if any).
    /// </summary>
    public Rectangle? GetAttackHitbox()
    {
        if (!_isAttacking || _currentAttack == null) return null;

        if (_currentAttack.IsAoE)
        {
            int radius = (int)_currentAttack.AoERadius;
            return new Rectangle(
                (int)Center.X - radius,
                (int)Center.Y - radius,
                radius * 2,
                radius * 2
            );
        }

        // Melee attack in front of boss
        int attackWidth = (int)_currentAttack.Range;
        int attackHeight = Height;
        int x = FacingDirection > 0
            ? (int)Position.X + Width
            : (int)Position.X - attackWidth;

        return new Rectangle(x, (int)Position.Y, attackWidth, attackHeight);
    }

    /// <summary>
    /// Check if contact damage should be applied.
    /// </summary>
    public bool CanDamageOnContact(Rectangle targetHitbox)
    {
        if (IsDead) return false;
        return Hitbox.Intersects(targetHitbox);
    }

    /// <summary>
    /// Get contact damage amount.
    /// </summary>
    public int GetContactDamage()
    {
        return Damage;
    }

    /// <summary>
    /// Get current attack damage (if attacking).
    /// </summary>
    public int GetAttackDamage()
    {
        if (!_isAttacking || _currentAttack == null) return Damage;

        return (int)(_currentAttack.Damage * _currentPhaseData.DamageMultiplier *
                    (IsEnraged ? Data.EnrageMultiplier : 1f));
    }

    /// <summary>
    /// Get the color for rendering (with effects).
    /// </summary>
    public Color GetRenderColor()
    {
        Color baseColor = new Color(Data.Color.R, Data.Color.G, Data.Color.B);

        if (IsDead)
        {
            // Fade out
            float alpha = 1f - (_deathTimer / DEATH_DURATION);
            return baseColor * alpha;
        }

        if (_damageFlashTimer > 0)
        {
            // Flash white on damage
            return Color.White;
        }

        if (IsEnraged)
        {
            // Pulsing red tint when enraged
            float pulse = MathF.Sin(_fightTimer * 5f) * 0.3f + 0.7f;
            return new Color(
                (int)(baseColor.R + (255 - baseColor.R) * pulse * 0.5f),
                (int)(baseColor.G * (1f - pulse * 0.3f)),
                (int)(baseColor.B * (1f - pulse * 0.3f))
            );
        }

        if (_phaseTransitionTimer > 0)
        {
            // Flash during phase transition
            float flash = MathF.Sin(_phaseTransitionTimer * 15f) * 0.5f + 0.5f;
            return Color.Lerp(baseColor, Color.White, flash);
        }

        return baseColor;
    }

    /// <summary>
    /// Get the health bar color.
    /// </summary>
    public Color GetHealthBarColor()
    {
        Color baseColor = new Color(Data.HealthBarColor.R, Data.HealthBarColor.G, Data.HealthBarColor.B);

        if (IsEnraged)
        {
            // Pulsing effect
            float pulse = MathF.Sin(_fightTimer * 3f) * 0.2f + 0.8f;
            return baseColor * pulse;
        }

        return baseColor;
    }
}
