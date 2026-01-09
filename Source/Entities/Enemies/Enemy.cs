using Microsoft.Xna.Framework;
using Terrascent.Economy;
using Terrascent.World;

namespace Terrascent.Entities.Enemies;

/// <summary>
/// Base class for all enemies with AI state machine, health, and combat.
/// </summary>
public class Enemy : Entity
{
    // Identity
    public EnemyType EnemyType { get; }
    public EnemyData Data { get; }

    // Stats (scaled by difficulty)
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public int Damage { get; private set; }
    public float MoveSpeed { get; private set; }

    // Rewards (scaled by difficulty)
    public int GoldReward { get; private set; }
    public int XPReward { get; private set; }

    // AI State
    public EnemyAIState AIState { get; private set; } = EnemyAIState.Idle;
    private float _stateTimer;
    private float _attackCooldownTimer;

    // Knockback - improved to prevent ground clipping
    private Vector2 _knockbackVelocity;
    private float _knockbackTimer;
    private const float KNOCKBACK_DURATION = 0.2f;
    private bool _isBeingKnockedBack;

    // Stun
    private float _stunTimer;

    // Death
    public bool IsDead => AIState == EnemyAIState.Dead;
    public bool IsFullyDead { get; private set; }  // After death animation
    private float _deathTimer;
    private const float DEATH_DURATION = 0.5f;

    // Invincibility frames after taking damage
    private float _iFrameTimer;
    private const float IFRAME_DURATION = 0.15f;
    public bool IsInvincible => _iFrameTimer > 0;

    // Facing direction (-1 left, 1 right)
    public int FacingDirection { get; private set; } = 1;

    // Reference to target (player)
    private Entity? _target;

    // Random for behavior variation
    private readonly Random _random;

    // Patrol state
    private float _patrolTimer;
    private int _patrolDirection = 1;

    // === OBSTACLE JUMPING (for walkers) ===
    private int _normalJumpCount = 0;
    private const int JUMPS_FOR_SUPER = 5;
    private const float NORMAL_JUMP_FORCE = 280f;
    private const float SUPER_JUMP_FORCE = 450f;
    private float _jumpCooldown = 0f;
    private const float JUMP_COOLDOWN_TIME = 0.4f;

    // === DESPAWN SYSTEM (Terraria-style) ===
    private float _despawnTimer;
    private const float DESPAWN_TIME = 12.5f;  // 750 ticks at 60fps = 12.5 seconds
    private const float TIMER_REGION_WIDTH = 120f * 16f;   // 120 tiles * 16 pixels
    private const float TIMER_REGION_HEIGHT = 67.5f * 16f; // 67.5 tiles * 16 pixels
    private const float ACTIVE_REGION_WIDTH = 504f * 16f;  // 504 tiles - instant despawn beyond this
    private const float ACTIVE_REGION_HEIGHT = 283.5f * 16f;
    public bool ShouldDespawn { get; private set; }

    // === INTEREST/CHASE TIMEOUT ===
    private float _chaseTimer;
    private const float MAX_CHASE_TIME = 10f;  // Lose interest after 10 seconds of chasing
    private float _lastSeenTargetTime;

    // === STUCK DETECTION (Terraria-style) ===
    private Vector2 _lastPosition;
    private float _stuckCheckTimer;
    private float _stuckTime;              // How long we've been stuck (not making progress)
    private const float STUCK_CHECK_INTERVAL = 0.5f;  // Check progress every 0.5 seconds
    private const float STUCK_THRESHOLD = 8f;         // Pixels moved to not be considered stuck
    private const float STUCK_PATROL_TIME = 1.5f;     // Start patrolling after 1.5 seconds stuck
    private const float STUCK_GIVEUP_TIME = 4f;       // Give up chase after 4 seconds stuck

    // === NATURAL MOVEMENT (Terraria-style) ===
    private float _directionChangeTimer;              // Timer for random direction changes
    private float _nextDirectionChange;               // When to next consider changing direction
    private bool _isPatrollingDuringChase;            // Currently doing patrol movement in chase
    private int _currentMoveDirection = 1;            // Current movement direction

    // === AGGRESSION TRACKING ===
    private bool _hasBeenDamaged;                     // Has this enemy been damaged by player?
    private float _aggroDecayTimer;                   // Timer for losing aggro after being damaged
    private const float AGGRO_DECAY_TIME = 15f;       // Lose aggro after 15 seconds without taking damage

    // === DEBUG ===
    private static bool _debugEnabled = true;        // Toggle debug output
    private float _debugTimer;
    private const float DEBUG_INTERVAL = 1f;         // Print debug every 1 second
    private static int _debugEnemyCount = 0;
    private int _debugId;
    private string _lastDebugReason = "";

    public Enemy(EnemyType type, Vector2 position, DifficultyManager difficulty, int? seed = null)
    {
        _debugId = ++_debugEnemyCount;  // Assign unique ID for debug output
        EnemyType = type;
        Data = EnemyRegistry.Get(type);
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;

        // Set entity size
        Width = Data.Width;
        Height = Data.Height;

        // Set physics based on movement type
        AffectedByGravity = Data.Movement != MovementPattern.Flyer &&
                           Data.Movement != MovementPattern.Floater &&
                           Data.Movement != MovementPattern.Burrower;

        Position = position;

        // Scale stats by difficulty
        ApplyDifficultyScaling(difficulty);

        CurrentHealth = MaxHealth;

        // Start in idle state
        TransitionToState(EnemyAIState.Idle);
    }

    /// <summary>
    /// Apply difficulty scaling to stats.
    /// </summary>
    private void ApplyDifficultyScaling(DifficultyManager difficulty)
    {
        float healthMult = difficulty.GetEnemyHealthMultiplier();
        float damageMult = difficulty.GetEnemyDamageMultiplier();
        float goldMult = difficulty.GetGoldMultiplier();
        float xpMult = difficulty.GetXPMultiplier();

        MaxHealth = (int)(Data.BaseHealth * healthMult);
        Damage = (int)(Data.BaseDamage * damageMult);
        MoveSpeed = Data.BaseSpeed;  // Speed doesn't scale

        GoldReward = (int)(Data.BaseGold * goldMult);
        XPReward = (int)(Data.BaseXP * xpMult);
    }

    /// <summary>
    /// Set the target entity (usually the player).
    /// </summary>
    public void SetTarget(Entity target)
    {
        _target = target;
    }

    /// <summary>
    /// Transition to a new AI state.
    /// </summary>
    private void TransitionToState(EnemyAIState newState)
    {
        if (AIState == newState) return;

        if (_debugEnabled)
        {
            Console.WriteLine($"[Enemy {_debugId} {Data.Name}] STATE CHANGE: {AIState} -> {newState} | Reason={_lastDebugReason}");
        }

        // Exit current state
        OnExitState(AIState);

        // Enter new state
        AIState = newState;
        _stateTimer = 0f;
        OnEnterState(newState);
    }

    private void OnEnterState(EnemyAIState state)
    {
        switch (state)
        {
            case EnemyAIState.Idle:
                Velocity = new Vector2(0, Velocity.Y);
                _stateTimer = 0.5f + (float)_random.NextDouble() * 1.5f;  // Shorter idle time
                break;

            case EnemyAIState.Patrol:
                _patrolTimer = 3f + (float)_random.NextDouble() * 4f;  // Patrol for 3-7 seconds
                _patrolDirection = _random.Next(2) == 0 ? -1 : 1;
                break;

            case EnemyAIState.Chase:
                _chaseTimer = 0f;
                _stuckTime = 0f;
                _stuckCheckTimer = 0f;
                _lastPosition = Position;
                _isPatrollingDuringChase = false;
                _nextDirectionChange = 1f + (float)_random.NextDouble() * 2f;
                break;

            case EnemyAIState.Attack:
                _attackCooldownTimer = Data.AttackCooldown;
                break;

            case EnemyAIState.Stunned:
                Velocity = new Vector2(0, Velocity.Y);
                break;

            case EnemyAIState.Dead:
                Velocity = Vector2.Zero;
                _deathTimer = DEATH_DURATION;
                break;
        }
    }

    private void OnExitState(EnemyAIState state)
    {
        // Cleanup if needed
    }

    /// <summary>
    /// Main update loop.
    /// </summary>
    public override void Update(float deltaTime)
    {
        if (IsFullyDead) return;

        // Update timers
        if (_iFrameTimer > 0) _iFrameTimer -= deltaTime;
        if (_stunTimer > 0) _stunTimer -= deltaTime;
        if (_attackCooldownTimer > 0) _attackCooldownTimer -= deltaTime;
        if (_jumpCooldown > 0) _jumpCooldown -= deltaTime;

        // Update aggro decay - passive enemies lose aggro over time
        if (_hasBeenDamaged && Data.Aggression == AggressionType.PassiveUntilDamaged)
        {
            _aggroDecayTimer -= deltaTime;
            if (_aggroDecayTimer <= 0)
            {
                _hasBeenDamaged = false;  // Lose aggro, go back to passive
            }
        }

        // Update despawn timer
        UpdateDespawnTimer(deltaTime);

        // Handle knockback - apply as velocity, not position offset
        if (_knockbackTimer > 0)
        {
            _knockbackTimer -= deltaTime;
            _isBeingKnockedBack = true;

            // Gradually reduce knockback velocity
            _knockbackVelocity *= 0.9f;

            if (_knockbackTimer <= 0)
            {
                _isBeingKnockedBack = false;
                _knockbackVelocity = Vector2.Zero;
            }
        }

        // Update AI state (skip if being knocked back)
        if (!_isBeingKnockedBack)
        {
            UpdateAI(deltaTime);
        }

        // Apply knockback to velocity (horizontal only to prevent ground clipping)
        if (_isBeingKnockedBack)
        {
            Velocity = new Vector2(_knockbackVelocity.X, Velocity.Y);
        }

        // Apply base physics
        base.Update(deltaTime);
    }

    /// <summary>
    /// Update despawn timer based on distance from player.
    /// </summary>
    private void UpdateDespawnTimer(float deltaTime)
    {
        if (_target == null || IsDead) return;

        float distX = MathF.Abs(Center.X - _target.Center.X);
        float distY = MathF.Abs(Center.Y - _target.Center.Y);

        // Instant despawn if outside active region
        if (distX > ACTIVE_REGION_WIDTH / 2f || distY > ACTIVE_REGION_HEIGHT / 2f)
        {
            ShouldDespawn = true;
            return;
        }

        // Timer-based despawn if outside timer region
        bool inTimerRegion = distX <= TIMER_REGION_WIDTH / 2f && distY <= TIMER_REGION_HEIGHT / 2f;

        if (inTimerRegion)
        {
            // Reset timer when in view
            _despawnTimer = DESPAWN_TIME;
        }
        else
        {
            // Count down when out of view
            _despawnTimer -= deltaTime;
            if (_despawnTimer <= 0)
            {
                ShouldDespawn = true;
            }
        }
    }

    /// <summary>
    /// Update AI behavior based on current state.
    /// </summary>
    private void UpdateAI(float deltaTime)
    {
        _stateTimer += deltaTime;

        // Check for state transitions
        if (_stunTimer > 0 && AIState != EnemyAIState.Stunned && AIState != EnemyAIState.Dead)
        {
            TransitionToState(EnemyAIState.Stunned);
            return;
        }

        if (AIState == EnemyAIState.Stunned && _stunTimer <= 0)
        {
            TransitionToState(EnemyAIState.Idle);
            return;
        }

        if (AIState == EnemyAIState.Dead)
        {
            _deathTimer -= deltaTime;
            if (_deathTimer <= 0)
            {
                IsFullyDead = true;
            }
            return;
        }

        // Check for target detection
        float distanceToTarget = _target != null ? Vector2.Distance(Center, _target.Center) : float.MaxValue;
        bool canSeeTarget = distanceToTarget <= Data.DetectionRange;
        bool inAttackRange = distanceToTarget <= Data.AttackRange;

        // State-specific behavior
        switch (AIState)
        {
            case EnemyAIState.Idle:
                UpdateIdle(deltaTime, canSeeTarget);
                break;

            case EnemyAIState.Patrol:
                UpdatePatrol(deltaTime, canSeeTarget);
                break;

            case EnemyAIState.Chase:
                UpdateChase(deltaTime, canSeeTarget, inAttackRange);
                break;

            case EnemyAIState.Attack:
                UpdateAttack(deltaTime, inAttackRange);
                break;

            case EnemyAIState.Flee:
                UpdateFlee(deltaTime);
                break;
        }
    }

    private void UpdateIdle(float deltaTime, bool canSeeTarget)
    {
        Velocity = new Vector2(0, Velocity.Y);

        // Check if we should chase based on aggression type
        if (canSeeTarget && ShouldChaseTarget())
        {
            _lastDebugReason = $"canSee={canSeeTarget} shouldChase={ShouldChaseTarget()}";
            TransitionToState(EnemyAIState.Chase);
            return;
        }

        // Transition to patrol after short idle
        if (_stateTimer >= 1.5f)
        {
            _lastDebugReason = "idle timeout";
            TransitionToState(EnemyAIState.Patrol);
        }
    }

    private void UpdatePatrol(float deltaTime, bool canSeeTarget)
    {
        // Check if we should chase based on aggression type
        if (canSeeTarget && ShouldChaseTarget())
        {
            TransitionToState(EnemyAIState.Chase);
            return;
        }

        _patrolTimer -= deltaTime;

        // Return to idle briefly, then patrol again
        if (_patrolTimer <= 0)
        {
            TransitionToState(EnemyAIState.Idle);
            return;
        }

        // Move in patrol direction
        ApplyMovement(_patrolDirection, deltaTime);

        // Turn around if hitting a wall (and can't jump over) or at edge
        if (CollidingLeft || CollidingRight)
        {
            // Try to jump first if we're a walker
            if (Data.Movement == MovementPattern.Walker && !TryObstacleJump())
            {
                _patrolDirection *= -1;
            }
        }
    }

    /// <summary>
    /// Check if this enemy should chase the target based on aggression type.
    /// </summary>
    private bool ShouldChaseTarget()
    {
        switch (Data.Aggression)
        {
            case AggressionType.AlwaysAggressive:
                // Always chase when target is in range
                return true;

            case AggressionType.PassiveUntilDamaged:
                // Only chase if we've been damaged by the player
                return _hasBeenDamaged;

            case AggressionType.ChaseWithRetry:
                // Chase, but the retry/give-up logic is in UpdateChase
                return true;

            default:
                return true;
        }
    }

    private void UpdateChase(float deltaTime, bool canSeeTarget, bool inAttackRange)
    {
        _chaseTimer += deltaTime;
        _directionChangeTimer += deltaTime;

        // === DEBUG OUTPUT ===
        if (_debugEnabled)
        {
            _debugTimer += deltaTime;
            if (_debugTimer >= DEBUG_INTERVAL)
            {
                _debugTimer = 0f;

                float distToTarget = _target != null ? Vector2.Distance(Center, _target.Center) : -1;
                float targetYDiff = _target != null ? _target.Center.Y - Center.Y : 0;
                float targetXDiff = _target != null ? _target.Center.X - Center.X : 0;
                bool targetBelow = _target != null && _target.Center.Y > Center.Y + Height;
                bool targetAbove = _target != null && _target.Center.Y < Center.Y - Height;

                Console.WriteLine($"[Enemy {_debugId} {Data.Name}] State={AIState} | " +
                    $"Pos=({Position.X:F0},{Position.Y:F0}) | " +
                    $"Vel=({Velocity.X:F1},{Velocity.Y:F1}) | " +
                    $"OnGround={OnGround} | " +
                    $"ColL={CollidingLeft} ColR={CollidingRight} | " +
                    $"TargetDist={distToTarget:F0} | " +
                    $"TargetRel=({targetXDiff:F0},{targetYDiff:F0}) | " +
                    $"Below={targetBelow} Above={targetAbove} | " +
                    $"StuckTime={_stuckTime:F1} | " +
                    $"ChaseTime={_chaseTimer:F1} | " +
                    $"Pacing={_isPatrollingDuringChase} | " +
                    $"MoveDir={_currentMoveDirection} | " +
                    $"Reason={_lastDebugReason}");
            }
        }

        // === STUCK DETECTION (only for ChaseWithRetry enemies) ===
        if (Data.Aggression == AggressionType.ChaseWithRetry)
        {
            _stuckCheckTimer += deltaTime;
            if (_stuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                _stuckCheckTimer = 0f;

                // Check if we've made progress toward the target
                float distanceMoved = Vector2.Distance(Position, _lastPosition);
                float distanceToTarget = _target != null ? Vector2.Distance(Center, _target.Center) : float.MaxValue;
                float lastDistanceToTarget = _target != null ? Vector2.Distance(_lastPosition + new Vector2(Width / 2, Height / 2), _target.Center) : float.MaxValue;

                // Check if target is below us (we need to walk to an edge to drop down)
                bool targetIsBelow = _target != null && _target.Center.Y > Center.Y + Height;

                // Determine if we made progress:
                // - Normal case: moved enough OR got closer to target
                // - Target below: horizontal movement counts as progress (walking to edge)
                bool madeProgress;
                if (targetIsBelow)
                {
                    // Target is below - horizontal movement toward them is progress
                    int dirToTarget = _target!.Center.X > Center.X ? 1 : -1;
                    float horizontalProgress = (Position.X - _lastPosition.X) * dirToTarget;
                    madeProgress = horizontalProgress > 2f || distanceMoved > STUCK_THRESHOLD;
                    _lastDebugReason = $"TargetBelow hProg={horizontalProgress:F1} moved={distanceMoved:F1} progress={madeProgress}";
                }
                else
                {
                    // Normal case - getting closer or moving enough
                    madeProgress = distanceMoved > STUCK_THRESHOLD || distanceToTarget < lastDistanceToTarget - 5f;
                    _lastDebugReason = $"Normal moved={distanceMoved:F1} distDelta={lastDistanceToTarget - distanceToTarget:F1} progress={madeProgress}";
                }

                if (!madeProgress)
                {
                    _stuckTime += STUCK_CHECK_INTERVAL;
                }
                else
                {
                    _stuckTime = MathF.Max(0, _stuckTime - STUCK_CHECK_INTERVAL * 2f);
                    _isPatrollingDuringChase = false;
                }

                _lastPosition = Position;
            }

            // Give up after being stuck too long (ChaseWithRetry only)
            if (_stuckTime >= STUCK_GIVEUP_TIME)
            {
                _lastDebugReason = "GAVE UP - stuck too long";
                _stuckTime = 0f;
                _isPatrollingDuringChase = false;
                TransitionToState(EnemyAIState.Patrol);
                return;
            }

            // Lose interest after chasing too long overall (ChaseWithRetry only)
            if (_chaseTimer >= MAX_CHASE_TIME)
            {
                _lastDebugReason = "GAVE UP - chase timeout";
                _isPatrollingDuringChase = false;
                TransitionToState(EnemyAIState.Patrol);
                return;
            }
        }

        // === PASSIVE ENEMIES: Check if aggro has decayed ===
        if (Data.Aggression == AggressionType.PassiveUntilDamaged && !_hasBeenDamaged)
        {
            // Lost aggro - go back to patrol
            _isPatrollingDuringChase = false;
            TransitionToState(EnemyAIState.Patrol);
            return;
        }

        // Give up if we haven't seen the target for a while (all types)
        if (!canSeeTarget)
        {
            _lastSeenTargetTime += deltaTime;
            float giveUpTime = Data.Aggression == AggressionType.AlwaysAggressive ? 8f : 3f;
            if (_lastSeenTargetTime >= giveUpTime)
            {
                _isPatrollingDuringChase = false;
                TransitionToState(EnemyAIState.Patrol);
                return;
            }
        }
        else
        {
            _lastSeenTargetTime = 0f;
        }

        // === ATTACK IF IN RANGE ===
        if (inAttackRange && _attackCooldownTimer <= 0)
        {
            TransitionToState(EnemyAIState.Attack);
            _chaseTimer = 0f;
            _stuckTime = 0f;
            return;
        }

        // === CHECK FOR FLEE (low HP) ===
        if (CurrentHealth < MaxHealth * 0.2f && Data.Movement != MovementPattern.Hopper)
        {
            if (_random.NextDouble() < 0.01)
            {
                TransitionToState(EnemyAIState.Flee);
                return;
            }
        }

        // === MOVEMENT BEHAVIOR ===
        if (_target != null)
        {
            // Pacing behavior only for ChaseWithRetry enemies when stuck
            if (Data.Aggression == AggressionType.ChaseWithRetry && _stuckTime >= STUCK_PATROL_TIME)
            {
                _isPatrollingDuringChase = true;
            }

            if (_isPatrollingDuringChase && Data.Aggression == AggressionType.ChaseWithRetry)
            {
                // TERRARIA-STYLE: Patrol/pace while still technically "chasing"
                // Change direction periodically
                if (_directionChangeTimer >= _nextDirectionChange)
                {
                    _directionChangeTimer = 0f;
                    _nextDirectionChange = 0.8f + (float)_random.NextDouble() * 1.5f;

                    // 70% chance to change direction, 30% chance to try toward player again
                    if (_random.NextDouble() < 0.7f)
                    {
                        _currentMoveDirection *= -1;
                    }
                    else
                    {
                        // Try toward player
                        _currentMoveDirection = _target.Center.X > Center.X ? 1 : -1;
                        _isPatrollingDuringChase = false;  // Give direct chase another try
                        _stuckTime = 0f;
                    }
                }

                ApplyMovement(_currentMoveDirection, deltaTime);

                // Turn around if hitting a wall
                if (CollidingLeft || CollidingRight)
                {
                    _currentMoveDirection *= -1;

                    // Try to jump obstacles (walkers only)
                    if (Data.Movement == MovementPattern.Walker)
                    {
                        TryObstacleJump();
                    }
                }
            }
            else
            {
                // NORMAL CHASE: Move toward target (all aggression types)
                int directionToTarget = _target.Center.X > Center.X ? 1 : -1;
                _currentMoveDirection = directionToTarget;
                ApplyMovement(directionToTarget, deltaTime);

                // Try to jump obstacles when blocked (walkers only)
                if ((CollidingLeft || CollidingRight) && Data.Movement == MovementPattern.Walker)
                {
                    TryObstacleJump();
                }
            }
        }

        // Update facing direction
        FacingDirection = _currentMoveDirection;
    }

    private void UpdateAttack(float deltaTime, bool inAttackRange)
    {
        // Face the target
        if (_target != null)
        {
            FacingDirection = _target.Center.X > Center.X ? 1 : -1;
        }

        // Attack duration
        if (_stateTimer >= 0.3f)
        {
            // Attack complete, return to chase
            TransitionToState(EnemyAIState.Chase);
        }
    }

    private void UpdateFlee(float deltaTime)
    {
        if (_target == null)
        {
            TransitionToState(EnemyAIState.Idle);
            return;
        }

        // Move away from target
        int direction = _target.Center.X > Center.X ? -1 : 1;
        ApplyMovement(direction, deltaTime);

        // Try to jump obstacles when fleeing
        if ((CollidingLeft || CollidingRight) && Data.Movement == MovementPattern.Walker)
        {
            TryObstacleJump();
        }

        // Stop fleeing after some distance or time
        float distance = Vector2.Distance(Center, _target.Center);
        if (distance > Data.DetectionRange * 1.5f || _stateTimer > 5f)
        {
            TransitionToState(EnemyAIState.Patrol);
        }
    }

    /// <summary>
    /// Try to jump over an obstacle. Returns true if jump was executed.
    /// </summary>
    private bool TryObstacleJump()
    {
        // Can only jump if on ground and cooldown expired
        if (!OnGround || _jumpCooldown > 0) return false;

        // Only walkers can obstacle jump
        if (Data.Movement != MovementPattern.Walker) return false;

        _normalJumpCount++;
        _jumpCooldown = JUMP_COOLDOWN_TIME;

        // Check if it's time for a super jump
        if (_normalJumpCount >= JUMPS_FOR_SUPER)
        {
            // SUPER JUMP!
            Velocity = new Vector2(Velocity.X, -SUPER_JUMP_FORCE);
            _normalJumpCount = 0;  // Reset counter
            return true;
        }
        else
        {
            // Normal jump
            Velocity = new Vector2(Velocity.X, -NORMAL_JUMP_FORCE);
            return true;
        }
    }

    /// <summary>
    /// Apply movement based on movement pattern.
    /// </summary>
    private void ApplyMovement(int direction, float deltaTime)
    {
        FacingDirection = direction;

        switch (Data.Movement)
        {
            case MovementPattern.Walker:
                Velocity = new Vector2(direction * MoveSpeed, Velocity.Y);
                break;

            case MovementPattern.Hopper:
                // Only hop if on ground
                if (OnGround && _random.NextDouble() < 0.05)  // Hop chance
                {
                    Velocity = new Vector2(direction * MoveSpeed * 0.8f, -200f);
                }
                break;

            case MovementPattern.Flyer:
                if (_target != null)
                {
                    Vector2 toTarget = _target.Center - Center;
                    if (toTarget.Length() > 1f)
                    {
                        toTarget.Normalize();
                        Velocity = toTarget * MoveSpeed;
                    }
                }
                else
                {
                    Velocity = new Vector2(direction * MoveSpeed, MathF.Sin(_stateTimer * 2f) * 30f);
                }
                break;

            case MovementPattern.Floater:
                Velocity = new Vector2(direction * MoveSpeed * 0.5f, MathF.Sin(_stateTimer) * 20f);
                break;

            case MovementPattern.Burrower:
                // Can move vertically too
                if (_target != null)
                {
                    Vector2 toTarget = _target.Center - Center;
                    if (toTarget.Length() > 1f)
                    {
                        toTarget.Normalize();
                        Velocity = toTarget * MoveSpeed * 0.7f;
                    }
                }
                break;

            case MovementPattern.Teleporter:
                // Periodic teleport
                if (_stateTimer > 2f && _target != null)
                {
                    float dist = Vector2.Distance(Center, _target.Center);
                    if (dist > 50f)
                    {
                        // Teleport partway toward target
                        Vector2 toTarget = _target.Center - Center;
                        toTarget.Normalize();
                        Position += toTarget * MathF.Min(dist * 0.5f, 100f);
                        _stateTimer = 0f;
                    }
                }
                Velocity = new Vector2(direction * MoveSpeed * 0.3f, Velocity.Y);
                break;
        }
    }

    /// <summary>
    /// Take damage from an attack.
    /// Returns true if the enemy died.
    /// </summary>
    public bool TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (IsDead || IsInvincible) return false;

        CurrentHealth -= damage;
        _iFrameTimer = IFRAME_DURATION;

        // Mark as damaged - this activates aggro for PassiveUntilDamaged enemies
        _hasBeenDamaged = true;
        _aggroDecayTimer = AGGRO_DECAY_TIME;

        // Apply knockback (reduced by resistance) - HORIZONTAL ONLY to prevent ground clipping
        float effectiveKnockback = knockbackForce * (1f - Data.KnockbackResistance);
        if (effectiveKnockback > 0 && knockbackDirection.Length() > 0.1f)
        {
            knockbackDirection.Normalize();

            // Only apply horizontal knockback to prevent enemies getting stuck in ground
            _knockbackVelocity = new Vector2(knockbackDirection.X * effectiveKnockback, 0);
            _knockbackTimer = KNOCKBACK_DURATION;
            _isBeingKnockedBack = true;

            // Small upward pop if on ground (to look more natural)
            if (OnGround)
            {
                Velocity = new Vector2(Velocity.X, -80f);
            }
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Apply stun effect.
    /// </summary>
    public void Stun(float duration)
    {
        _stunTimer = MathF.Max(_stunTimer, duration);
    }

    /// <summary>
    /// Handle enemy death.
    /// </summary>
    private void Die()
    {
        TransitionToState(EnemyAIState.Dead);
    }

    /// <summary>
    /// Check if this enemy's attack hits a target hitbox.
    /// </summary>
    public bool CanDamageTarget(Rectangle targetHitbox)
    {
        if (AIState != EnemyAIState.Attack) return false;
        if (_stateTimer < 0.1f || _stateTimer > 0.25f) return false;  // Attack window

        // Create attack hitbox in front of enemy
        Rectangle attackBox = GetAttackHitbox();
        return attackBox.Intersects(targetHitbox);
    }

    /// <summary>
    /// Get the attack hitbox for this enemy.
    /// </summary>
    public Rectangle GetAttackHitbox()
    {
        int attackWidth = (int)Data.AttackRange;
        int attackHeight = Height;

        int x = FacingDirection > 0
            ? (int)Position.X + Width
            : (int)Position.X - attackWidth;

        return new Rectangle(x, (int)Position.Y, attackWidth, attackHeight);
    }

    /// <summary>
    /// Get the color for rendering (with damage flash).
    /// </summary>
    public Color GetRenderColor()
    {
        if (IsDead)
        {
            // Fade out
            float alpha = _deathTimer / DEATH_DURATION;
            return new Color(Data.Color.R, Data.Color.G, Data.Color.B) * alpha;
        }

        if (IsInvincible)
        {
            // Flash white
            return Color.White;
        }

        return new Color(Data.Color.R, Data.Color.G, Data.Color.B);
    }
}